using System.Text.Json;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ChatRole = HealthPlanChat.Core.Domain.Chat.Role;

namespace HealthPlanChat.Infrastructure.Redis;

/// <summary>
/// Redis-backed implementation of <see cref="IChatSessionStore"/>.
/// Uses Azure Managed Redis (Redis Enterprise) for session storage with TTL.
/// </summary>
public sealed class RedisChatSessionStore : IChatSessionStore, IDisposable
{
    private readonly ILogger<RedisChatSessionStore> _logger;
    private readonly RedisOptions _options;
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly TimeSpan _sessionTtl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisChatSessionStore(
        IOptions<RedisOptions> options,
        ILogger<RedisChatSessionStore> logger)
    {
        _logger = logger;
        _options = options.Value;
        _sessionTtl = TimeSpan.FromMinutes(60); // Default TTL

        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            _logger.LogInformation("Connecting to Redis...");
            return ConnectionMultiplexer.Connect(_options.ConnectionString);
        });
    }

    private IDatabase Database => _connection.Value.GetDatabase();

    private string GetSessionKey(string sessionId) => $"{_options.KeyPrefix}{sessionId}";
    private string GetMessagesKey(string sessionId) => $"{_options.KeyPrefix}{sessionId}:messages";

    /// <inheritdoc />
    public async Task<ChatSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = ChatSession.Create(_sessionTtl);
        var sessionKey = GetSessionKey(session.ChatSessionId);

        var sessionData = new SessionData
        {
            ChatSessionId = session.ChatSessionId,
            CreatedAtUtc = session.CreatedAtUtc,
            LastUpdatedAtUtc = session.LastUpdatedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc
        };

        var json = JsonSerializer.Serialize(sessionData, JsonOptions);
        await Database.StringSetAsync(sessionKey, json, _sessionTtl);

        _logger.LogInformation(
            "Created session. SessionId: {SessionId}",
            session.ChatSessionId);

        return session;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var sessionKey = GetSessionKey(sessionId);
        var json = await Database.StringGetAsync(sessionKey);

        if (json.IsNullOrEmpty)
        {
            _logger.LogInformation("Session not found or expired. SessionId: {SessionId}", sessionId);
            return null;
        }

        var sessionData = JsonSerializer.Deserialize<SessionData>(json.ToString(), JsonOptions);
        if (sessionData is null)
        {
            return null;
        }

        // Load messages
        var messages = await GetMessagesAsync(sessionId, cancellationToken);

        // Create session with restored messages
        var session = ChatSession.CreateWithMessages(
            sessionData.ChatSessionId,
            sessionData.CreatedAtUtc,
            sessionData.ExpiresAtUtc,
            messages);

        return session;
    }

    /// <inheritdoc />
    public async Task AppendMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(message);

        var messagesKey = GetMessagesKey(sessionId);
        var messageJson = JsonSerializer.Serialize(new MessageData(message), JsonOptions);

        await Database.ListRightPushAsync(messagesKey, messageJson);
        await Database.KeyExpireAsync(messagesKey, _sessionTtl);

        // Update session last updated time
        var sessionKey = GetSessionKey(sessionId);
        var sessionJson = await Database.StringGetAsync(sessionKey);
        if (!sessionJson.IsNullOrEmpty)
        {
            var sessionData = JsonSerializer.Deserialize<SessionData>(sessionJson.ToString(), JsonOptions);
            if (sessionData is not null)
            {
                sessionData.LastUpdatedAtUtc = DateTime.UtcNow;
                var updatedJson = JsonSerializer.Serialize(sessionData, JsonOptions);
                await Database.StringSetAsync(sessionKey, updatedJson, _sessionTtl);
            }
        }

        _logger.LogInformation(
            "Appended message. SessionId: {SessionId}, Role: {Role}",
            sessionId,
            message.Role);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var messagesKey = GetMessagesKey(sessionId);
        var messageValues = await Database.ListRangeAsync(messagesKey, 0, -1);

        var messages = new List<ChatMessage>();
        foreach (var value in messageValues)
        {
            if (!value.IsNullOrEmpty)
            {
                var messageData = JsonSerializer.Deserialize<MessageData>(value.ToString(), JsonOptions);
                if (messageData is not null)
                {
                    messages.Add(messageData.ToChatMessage());
                }
            }
        }

        return messages.AsReadOnly();
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }

    /// <summary>
    /// Internal session data for Redis storage.
    /// </summary>
    private sealed class SessionData
    {
        public string ChatSessionId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// Internal message data for Redis storage.
    /// </summary>
    private sealed record MessageData
    {
        public string ChatMessageId { get; init; } = string.Empty;
        public string ChatSessionId { get; init; } = string.Empty;
        public int Role { get; init; }
        public string Text { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }

        public MessageData() { }

        public MessageData(ChatMessage message)
        {
            ChatMessageId = message.ChatMessageId;
            ChatSessionId = message.ChatSessionId;
            Role = (int)message.Role;
            Text = message.Text;
            CreatedAtUtc = message.CreatedAtUtc;
        }

        public ChatMessage ToChatMessage() => new()
        {
            ChatMessageId = ChatMessageId,
            ChatSessionId = ChatSessionId,
            Role = (ChatRole)Role,
            Text = Text,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}

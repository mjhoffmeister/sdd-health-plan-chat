namespace HealthPlanChat.Core.Domain.Chat;

/// <summary>
/// Represents a chat session with ordered messages.
/// </summary>
public sealed class ChatSession
{
    private readonly List<ChatMessage> _messages = [];

    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public string ChatSessionId { get; init; } = string.Empty;

    /// <summary>
    /// When the session was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// When the session was last updated (UTC).
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; private set; }

    /// <summary>
    /// When the session expires (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// The ordered list of messages in this session.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Creates a new chat session with the specified TTL.
    /// </summary>
    /// <param name="ttl">Time to live for the session.</param>
    /// <returns>A new chat session.</returns>
    public static ChatSession Create(TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        return new ChatSession
        {
            ChatSessionId = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now,
            ExpiresAtUtc = now.Add(ttl)
        };
    }

    /// <summary>
    /// Creates a new chat session with the specified ID and TTL.
    /// </summary>
    public static ChatSession Create(string sessionId, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var now = DateTime.UtcNow;
        return new ChatSession
        {
            ChatSessionId = sessionId,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now,
            ExpiresAtUtc = now.Add(ttl)
        };
    }

    /// <summary>
    /// Creates a chat session restored from storage with existing messages.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="createdAtUtc">When the session was created.</param>
    /// <param name="expiresAtUtc">When the session expires.</param>
    /// <param name="messages">The messages to restore.</param>
    /// <returns>A restored chat session.</returns>
    public static ChatSession CreateWithMessages(
        string sessionId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        IReadOnlyList<ChatMessage> messages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var session = new ChatSession
        {
            ChatSessionId = sessionId,
            CreatedAtUtc = createdAtUtc,
            LastUpdatedAtUtc = messages.Count > 0
                ? messages.Max(m => m.CreatedAtUtc)
                : createdAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
        session._messages.AddRange(messages);
        return session;
    }

    /// <summary>
    /// Adds a message to this session.
    /// </summary>
    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Add(message);
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores messages from storage (used during deserialization).
    /// </summary>
    internal void RestoreMessages(IEnumerable<ChatMessage> messages)
    {
        _messages.Clear();
        _messages.AddRange(messages);
    }
}

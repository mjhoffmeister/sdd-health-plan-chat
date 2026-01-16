using System.Collections.Concurrent;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;

namespace HealthPlanChat.Infrastructure.IntegrationTests.Fakes;

/// <summary>
/// In-memory implementation of IChatSessionStore for testing.
/// Provides realistic session storage behavior without external dependencies.
/// </summary>
public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _messages = new();
    private readonly TimeSpan _sessionTtl;

    public InMemoryChatSessionStore(TimeSpan? sessionTtl = null)
    {
        _sessionTtl = sessionTtl ?? TimeSpan.FromHours(1);
    }

    public Task<ChatSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = ChatSession.Create(_sessionTtl);
        _sessions[session.ChatSessionId] = session;
        _messages[session.ChatSessionId] = [];
        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            // Check if expired
            if (session.ExpiresAtUtc > DateTime.UtcNow)
            {
                return Task.FromResult<ChatSession?>(session);
            }
            // Expired - remove it
            _sessions.TryRemove(sessionId, out _);
            _messages.TryRemove(sessionId, out _);
        }
        return Task.FromResult<ChatSession?>(null);
    }

    public Task AppendMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (!_messages.ContainsKey(sessionId))
        {
            throw new InvalidOperationException($"Session not found: {sessionId}");
        }
        _messages[sessionId].Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(sessionId, out var messages))
        {
            return Task.FromResult<IReadOnlyList<ChatMessage>>(messages.ToList());
        }
        return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
    }
}

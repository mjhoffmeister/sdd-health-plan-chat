namespace HealthPlanChat.Core.Domain.Chat;

/// <summary>
/// A single message within a chat session.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string ChatMessageId { get; init; } = string.Empty;

    /// <summary>
    /// The session this message belongs to.
    /// </summary>
    public string ChatSessionId { get; init; } = string.Empty;

    /// <summary>
    /// The role of the message sender.
    /// </summary>
    public Role Role { get; init; }

    /// <summary>
    /// The message content.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// When the message was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Creates a new user message.
    /// </summary>
    public static ChatMessage CreateUserMessage(string sessionId, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new ChatMessage
        {
            ChatMessageId = Guid.NewGuid().ToString("N"),
            ChatSessionId = sessionId,
            Role = Role.User,
            Text = text,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new assistant message.
    /// </summary>
    public static ChatMessage CreateAssistantMessage(string sessionId, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new ChatMessage
        {
            ChatMessageId = Guid.NewGuid().ToString("N"),
            ChatSessionId = sessionId,
            Role = Role.Assistant,
            Text = text,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

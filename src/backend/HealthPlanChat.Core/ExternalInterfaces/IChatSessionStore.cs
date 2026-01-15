using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.ExternalInterfaces;

/// <summary>
/// Interface for managing chat session storage.
/// </summary>
public interface IChatSessionStore
{
    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session.</returns>
    Task<ChatSession> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session if found, or null if not found or expired.</returns>
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a message to the session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="message">The message to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AppendMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the message history for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ordered list of messages.</returns>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
}

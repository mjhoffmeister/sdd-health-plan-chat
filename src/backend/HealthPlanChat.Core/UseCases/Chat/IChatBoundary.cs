namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Output boundary interface for the Chat use case.
/// Defines outcome methods for each possible result.
/// </summary>
/// <typeparam name="TOutput">The output type returned by boundary methods.</typeparam>
public interface IChatBoundary<TOutput>
{
    /// <summary>
    /// Called when the chat completed successfully.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <returns>The output.</returns>
    TOutput ChatCompleted(ChatResponse response);

    /// <summary>
    /// Called when the session was not found.
    /// </summary>
    /// <param name="sessionId">The session ID that was not found.</param>
    /// <returns>The output.</returns>
    TOutput SessionNotFound(string sessionId);

    /// <summary>
    /// Called when validation failed.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>The output.</returns>
    TOutput ValidationFailed(string errorMessage);
}

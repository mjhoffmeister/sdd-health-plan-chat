namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Input for the Chat use case.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="UserMessage">The user's message.</param>
public sealed record ChatInput(
    string SessionId,
    string UserMessage)
{
    /// <summary>
    /// Validates the input and throws if invalid.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(SessionId, nameof(SessionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(UserMessage, nameof(UserMessage));
    }
}

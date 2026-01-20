namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Request for the Chat use case.
/// </summary>
/// <param name="SessionId">The session identifier. If null or empty, a new session is created.</param>
/// <param name="UserMessage">The user's message.</param>
public sealed record ChatRequest(
    string? SessionId,
    string UserMessage);

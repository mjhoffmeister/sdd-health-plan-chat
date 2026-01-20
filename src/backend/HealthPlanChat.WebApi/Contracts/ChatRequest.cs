namespace HealthPlanChat.WebApi.Contracts;

/// <summary>
/// API request to send a chat message.
/// </summary>
/// <param name="SessionId">The session identifier. If null or empty, a new session is created.</param>
/// <param name="Message">The user's message.</param>
public sealed record ChatRequest(
    string? SessionId,
    string Message);

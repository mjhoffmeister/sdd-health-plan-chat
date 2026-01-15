namespace HealthPlanChat.Core.UseCases.Contracts;

/// <summary>
/// Request to send a chat message.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Message">The user's message.</param>
public sealed record ChatRequest(
    string SessionId,
    string Message);

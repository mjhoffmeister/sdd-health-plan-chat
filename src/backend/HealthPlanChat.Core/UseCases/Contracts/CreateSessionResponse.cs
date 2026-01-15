namespace HealthPlanChat.Core.UseCases.Contracts;

/// <summary>
/// Response returned when a new chat session is created.
/// </summary>
/// <param name="SessionId">The server-issued session identifier.</param>
public sealed record CreateSessionResponse(string SessionId);

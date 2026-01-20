using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.WebApi.Contracts;

/// <summary>
/// API response containing the assistant's answer.
/// </summary>
/// <param name="SessionId">The session identifier (useful when a new session was created).</param>
/// <param name="AnswerType">Indicates whether the answer is grounded or general guidance.</param>
/// <param name="AnswerText">The assistant's response text.</param>
/// <param name="References">References to plan documents (empty for GeneralGuidance).</param>
public sealed record ChatResponse(
    string SessionId,
    AnswerType AnswerType,
    string AnswerText,
    IReadOnlyList<Reference> References);

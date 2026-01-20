using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Response from the Chat use case.
/// </summary>
/// <param name="SessionId">The session identifier (useful when a new session was created).</param>
/// <param name="AnswerText">The assistant's response text.</param>
/// <param name="AnswerType">Indicates whether the answer is grounded or general guidance.</param>
/// <param name="References">References to plan documents (empty for GeneralGuidance).</param>
public sealed record ChatResponse(
    string SessionId,
    string AnswerText,
    AnswerType AnswerType,
    IReadOnlyList<Reference> References);

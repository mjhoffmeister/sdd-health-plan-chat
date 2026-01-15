namespace HealthPlanChat.Core.UseCases.Contracts;

/// <summary>
/// Response containing the assistant's answer.
/// </summary>
/// <param name="AnswerType">Indicates whether the answer is grounded or general guidance.</param>
/// <param name="AnswerText">The assistant's response text.</param>
/// <param name="References">References to plan documents (empty for GeneralGuidance).</param>
public sealed record ChatResponse(
    AnswerType AnswerType,
    string AnswerText,
    IReadOnlyList<Reference> References);

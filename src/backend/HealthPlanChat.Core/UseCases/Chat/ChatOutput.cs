using HealthPlanChat.Core.UseCases.Contracts;

namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Output from the Chat use case.
/// </summary>
/// <param name="AnswerText">The assistant's response text.</param>
/// <param name="AnswerType">Indicates whether the answer is grounded or general guidance.</param>
/// <param name="References">References to plan documents (empty for GeneralGuidance).</param>
public sealed record ChatOutput(
    string AnswerText,
    AnswerType AnswerType,
    IReadOnlyList<Reference> References)
{
    /// <summary>
    /// Creates a successful grounded output.
    /// </summary>
    public static ChatOutput Grounded(string answerText, IReadOnlyList<Reference> references) =>
        new(answerText, AnswerType.Grounded, references);

    /// <summary>
    /// Creates a general guidance output.
    /// </summary>
    public static ChatOutput GeneralGuidance(string answerText) =>
        new(answerText, AnswerType.GeneralGuidance, []);
}

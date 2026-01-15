namespace HealthPlanChat.Core.UseCases.Contracts;

/// <summary>
/// Indicates how the answer was derived.
/// </summary>
public enum AnswerType
{
    /// <summary>
    /// Answer is grounded in plan materials with explicit references.
    /// </summary>
    Grounded,

    /// <summary>
    /// Answer is general guidance when materials do not contain a direct answer.
    /// </summary>
    GeneralGuidance
}

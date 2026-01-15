namespace HealthPlanChat.Core.UseCases.Contracts;

/// <summary>
/// A citation reference to a plan document.
/// </summary>
/// <param name="PlanDocumentId">The ID of the plan document.</param>
/// <param name="Anchor">Page, section, or chunk identifier.</param>
/// <param name="Quote">A short snippet from the referenced content.</param>
public sealed record Reference(
    string PlanDocumentId,
    string Anchor,
    string Quote);

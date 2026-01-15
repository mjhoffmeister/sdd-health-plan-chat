namespace HealthPlanChat.Core.Domain.Chat;

/// <summary>
/// A retrieved chunk from a plan document used for grounding.
/// </summary>
/// <param name="ChunkId">Unique identifier for this chunk.</param>
/// <param name="PlanDocumentId">The ID of the plan document this chunk belongs to.</param>
/// <param name="PlanName">The name of the plan (e.g., "Contoso Health PPO Silver").</param>
/// <param name="Section">The section or heading this chunk belongs to.</param>
/// <param name="Text">The text content of the chunk.</param>
/// <param name="PageOrAnchor">Page number or anchor identifier.</param>
/// <param name="Score">The relevance score from retrieval.</param>
public sealed record RetrievedChunk(
    string ChunkId,
    string PlanDocumentId,
    string PlanName,
    string Section,
    string Text,
    string PageOrAnchor,
    double Score);

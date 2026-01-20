using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.ExternalInterfaces;

/// <summary>
/// Interface for searching plan materials.
/// </summary>
/// <remarks>
/// <b>Note:</b> As of the agent-native RAG refactor, the chat agent handles retrieval
/// internally via the Azure AI Search tool. This interface is retained for:
/// - Index maintenance utilities
/// - Direct search scenarios outside the chat flow
/// - Backward compatibility during transition
/// </remarks>
public interface IPlanMaterialSearch
{
    /// <summary>
    /// Searches for relevant chunks based on the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked list of retrieved chunks.</returns>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.ExternalInterfaces;

/// <summary>
/// Interface for searching plan materials.
/// </summary>
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

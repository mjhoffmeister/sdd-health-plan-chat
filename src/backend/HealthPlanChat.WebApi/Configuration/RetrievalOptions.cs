namespace HealthPlanChat.WebApi.Configuration;

/// <summary>
/// Retrieval policy configuration for determining grounding confidence.
/// </summary>
public sealed class RetrievalOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Retrieval";

    /// <summary>
    /// Minimum number of hits required for grounded response. Default: 1.
    /// </summary>
    public int MinHits { get; set; } = 1;

    /// <summary>
    /// Minimum top score required for grounded response. Default: 0.7.
    /// </summary>
    public double MinTopScore { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of chunks to retrieve. Default: 5.
    /// </summary>
    public int TopK { get; set; } = 5;
}

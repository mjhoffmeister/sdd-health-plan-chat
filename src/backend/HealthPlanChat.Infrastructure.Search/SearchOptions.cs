namespace HealthPlanChat.Infrastructure.Search;

/// <summary>
/// Configuration options for Azure AI Search.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Search";

    /// <summary>
    /// Azure AI Search endpoint URL. Required.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Index name for plan materials. Default: "plan-materials".
    /// </summary>
    public string IndexName { get; set; } = "plan-materials";
}

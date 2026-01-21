namespace HealthPlanChat.Infrastructure.AgentFramework;

/// <summary>
/// Configuration options for Azure AI Foundry.
/// </summary>
public sealed class FoundryOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Foundry";

    /// <summary>
    /// Azure AI Foundry endpoint URL. Required.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Model deployment name for chat completions. Default: "gpt-5-mini".
    /// </summary>
    public string ChatModelDeployment { get; set; } = "gpt-4o";

    /// <summary>
    /// Model deployment name for embeddings. Default: "text-embedding-3-small".
    /// </summary>
    public string EmbeddingModelDeployment { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Maximum tokens for chat completion responses. Default: 1024.
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Temperature for chat completion. Default: 0.7.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Connection ID linking Foundry project to Azure AI Search.
    /// Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{account}/projects/{project}/connections/{connection}
    /// Required for AzureAISearchAgentTool.
    /// </summary>
    public string SearchConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Search index name for plan materials. Default: "plan-materials".
    /// </summary>
    public string SearchIndexName { get; set; } = "plan-materials";

    /// <summary>
    /// Maximum number of search results to retrieve. Default: 5.
    /// </summary>
    public int SearchTopK { get; set; } = 5;
}

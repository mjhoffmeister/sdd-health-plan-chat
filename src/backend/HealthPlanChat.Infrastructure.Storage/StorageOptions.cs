namespace HealthPlanChat.Infrastructure.Storage;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Storage";

    /// <summary>
    /// Azure Blob Storage service URL. Required.
    /// </summary>
    public string BlobServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Container name for plan materials. Default: "plan-materials".
    /// </summary>
    public string ContainerName { get; set; } = "plan-materials";
}

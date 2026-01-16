namespace HealthPlanChat.Infrastructure.Redis;

/// <summary>
/// Configuration options for Azure Managed Redis.
/// Uses Microsoft Entra Authentication (managed identity) instead of access keys.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Redis";

    /// <summary>
    /// Redis endpoint (hostname:port). Required.
    /// Example: "myredis.eastus.redis.azure.net:10000"
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for session data. Default: "session:".
    /// </summary>
    public string KeyPrefix { get; set; } = "session:";
}

namespace HealthPlanChat.Infrastructure.Redis;

/// <summary>
/// Configuration options for Azure Managed Redis.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "Redis";

    /// <summary>
    /// Redis connection string. Required.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for session data. Default: "session:".
    /// </summary>
    public string KeyPrefix { get; set; } = "session:";
}

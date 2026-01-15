namespace HealthPlanChat.WebApi.Configuration;

/// <summary>
/// Top-level application configuration options.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionKey = "App";

    /// <summary>
    /// Session time-to-live in minutes. Default: 60.
    /// </summary>
    public int SessionTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum number of messages per session. Default: 50.
    /// </summary>
    public int MaxMessagesPerSession { get; set; } = 50;

    /// <summary>
    /// Maximum length of a user message. Default: 4000.
    /// </summary>
    public int MaxMessageLength { get; set; } = 4000;
}

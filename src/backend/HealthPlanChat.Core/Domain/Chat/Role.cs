namespace HealthPlanChat.Core.Domain.Chat;

/// <summary>
/// The role of the participant in a chat message.
/// </summary>
public enum Role
{
    /// <summary>User-provided message.</summary>
    User,

    /// <summary>Assistant-generated response.</summary>
    Assistant,

    /// <summary>System instruction message.</summary>
    System
}

namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Controls how uploaded photos are moderated before becoming visible
/// </summary>
public enum AlbumModerationMode
{
    /// <summary>No moderation — all uploads immediately visible</summary>
    None = 0,

    /// <summary>Post-moderation — photos visible immediately, organizer can delete (default)</summary>
    PostModeration = 1,

    /// <summary>Pre-approval — photos hidden until organizer approves</summary>
    PreApproval = 2
}

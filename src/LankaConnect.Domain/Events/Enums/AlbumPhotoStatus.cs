namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Moderation status of an individual photo in a photo album
/// </summary>
public enum AlbumPhotoStatus
{
    /// <summary>Photo awaiting organizer approval (PreApproval moderation mode)</summary>
    PendingApproval = 0,

    /// <summary>Photo approved and visible in gallery</summary>
    Approved = 1,

    /// <summary>Photo rejected by organizer</summary>
    Rejected = 2
}

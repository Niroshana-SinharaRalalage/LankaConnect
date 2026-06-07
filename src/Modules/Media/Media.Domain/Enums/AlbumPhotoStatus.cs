namespace LankaConnect.Modules.Media.Domain.Enums;

/// <summary>
/// Status of an individual photo in a photo album.
/// All photos are immediately approved (no moderation).
/// Kept as enum for forward compatibility.
/// </summary>
public enum AlbumPhotoStatus
{
    /// <summary>Photo approved and visible in gallery</summary>
    Approved = 1
}

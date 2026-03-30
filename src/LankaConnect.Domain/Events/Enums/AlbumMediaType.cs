namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Discriminator for media type within a photo album.
/// Photos are processed (EXIF stripped, 3 sizes generated).
/// Videos are stored as-is with a separate thumbnail.
/// </summary>
public enum AlbumMediaType
{
    Photo = 1,
    Video = 2
}

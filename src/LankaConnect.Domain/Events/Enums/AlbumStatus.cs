namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status of a photo album's lifecycle
/// </summary>
public enum AlbumStatus
{
    /// <summary>Album created but not yet visible to attendees</summary>
    Draft = 0,

    /// <summary>Album is published and visible — uploads accepted per permission settings</summary>
    Published = 1,

    /// <summary>Album is closed — no more uploads accepted, photos still viewable until expiry</summary>
    Closed = 2
}

namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status of a photo album's lifecycle.
/// Draft → Published (manual publish by organizer).
/// </summary>
public enum AlbumStatus
{
    /// <summary>Album created but not yet visible to attendees</summary>
    Draft = 0,

    /// <summary>Album is published and visible to attendees</summary>
    Published = 1
}

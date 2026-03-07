namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Controls who can upload photos to an event's photo album
/// </summary>
public enum AlbumUploadPermission
{
    /// <summary>Only the event organizer can upload photos (default)</summary>
    OrganizerOnly = 0,

    /// <summary>Registered attendees (Confirmed/Attended status) can upload</summary>
    RegisteredAttendees = 1,

    /// <summary>Any authenticated LankaConnect user can upload</summary>
    AnyAuthenticated = 2
}

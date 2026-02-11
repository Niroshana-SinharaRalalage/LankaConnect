namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Status lifecycle for custom event forms.
/// Forms progress through: Draft -> Active -> Closed -> Archived
/// </summary>
public enum EventFormStatus
{
    /// <summary>
    /// Form is being designed by the organizer. Not visible to attendees. Not accepting responses.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Form is published and accepting responses from attendees.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Form is no longer accepting responses (deadline passed or manually closed).
    /// Existing responses are preserved. Can be reopened.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// Form is archived and hidden from both organizer list views and attendees.
    /// Data is preserved but not actively displayed.
    /// </summary>
    Archived = 3
}

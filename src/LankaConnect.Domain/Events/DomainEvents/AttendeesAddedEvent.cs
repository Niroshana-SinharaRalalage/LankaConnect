using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when attendees are successfully added to an existing paid registration.
/// This event triggers:
/// - Sending "Attendees Added" confirmation email to user
/// - Notifying event organizer of the update
/// - Updating event capacity tracking
///
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record AttendeesAddedEvent(
    /// <summary>
    /// ID of the event.
    /// </summary>
    Guid EventId,

    /// <summary>
    /// ID of the registration that was updated.
    /// </summary>
    Guid RegistrationId,

    /// <summary>
    /// ID of the user who owns the registration (null for anonymous).
    /// </summary>
    Guid? UserId,

    /// <summary>
    /// Contact email for the registration.
    /// </summary>
    string ContactEmail,

    /// <summary>
    /// Number of attendees BEFORE this addition.
    /// </summary>
    int PreviousAttendeeCount,

    /// <summary>
    /// Number of NEW attendees added in this update.
    /// </summary>
    int AddedAttendeeCount,

    /// <summary>
    /// Total attendee count AFTER this addition.
    /// </summary>
    int NewTotalAttendeeCount,

    /// <summary>
    /// Additional amount paid for the new attendees.
    /// </summary>
    decimal AdditionalAmountPaid,

    /// <summary>
    /// Currency of the additional payment.
    /// </summary>
    string Currency,

    /// <summary>
    /// Total amount paid for the entire registration.
    /// </summary>
    decimal TotalAmountPaid,

    /// <summary>
    /// ID of the RegistrationAddition record that was merged.
    /// </summary>
    Guid RegistrationAdditionId,

    /// <summary>
    /// When the attendees were added.
    /// </summary>
    DateTime AddedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

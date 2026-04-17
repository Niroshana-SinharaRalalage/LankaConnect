namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Phase 7A: Types of WhatsApp notification preferences that users can toggle.
/// Used by UserWhatsAppPreferences.ShouldNotify() for compile-time safe preference checks.
/// </summary>
public enum WhatsAppNotificationType
{
    /// <summary>
    /// Registration confirmation for paid or free events
    /// </summary>
    EventRegistration = 1,

    /// <summary>
    /// Event reminders (7-day, 2-day, 1-day windows)
    /// </summary>
    EventReminder = 2,

    /// <summary>
    /// Event cancellation notifications
    /// </summary>
    EventCancellation = 3,

    /// <summary>
    /// Event details update notifications
    /// </summary>
    EventUpdate = 4,

    /// <summary>
    /// Sign-up list commitment confirmations and updates
    /// </summary>
    SignupCommitment = 5,

    /// <summary>
    /// Refund initiated and completed notifications
    /// </summary>
    Refund = 6,

    /// <summary>
    /// Newsletter broadcast messages
    /// </summary>
    Newsletter = 7,

    /// <summary>
    /// New event announcement (marketing category)
    /// </summary>
    NewEvent = 8,

    /// <summary>
    /// Payment and ticket confirmation notifications
    /// </summary>
    Payment = 9,

    /// <summary>
    /// Phase 7B.3: Pending payment reminder (24-hour expiry)
    /// </summary>
    PaymentPending = 10,

    /// <summary>
    /// Phase 7B.3: Event postponement notifications
    /// </summary>
    EventPostponed = 11,

    /// <summary>
    /// Phase 7B.3: Donation receipt confirmations
    /// </summary>
    Donation = 12,

    /// <summary>
    /// Phase 7B.3: Event approval/rejection notifications (organizer)
    /// </summary>
    EventApproval = 13,

    /// <summary>
    /// Phase 7B.3: Additional attendees added confirmations
    /// </summary>
    AttendeesAdded = 14,

    /// <summary>
    /// Phase 7B.3: Add-on purchase receipt
    /// </summary>
    AddOnPurchase = 15,

    /// <summary>
    /// Phase 7B.3: Collection contribution receipt
    /// </summary>
    Collection = 16,

    /// <summary>
    /// Phase 7B.3: Sponsorship confirmation
    /// </summary>
    Sponsorship = 17,

    /// <summary>
    /// Phase 7B.3: Form/survey response confirmation
    /// </summary>
    FormResponse = 18,

    /// <summary>
    /// Phase 7B.3: Photo album published notification
    /// </summary>
    PhotoAlbum = 19
}

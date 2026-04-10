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
    Payment = 9
}

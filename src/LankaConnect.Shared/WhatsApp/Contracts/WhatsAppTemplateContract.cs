namespace LankaConnect.Shared.WhatsApp.Contracts;

/// <summary>
/// Phase 7A: Single source of truth for all WhatsApp template names and parameter keys.
/// Mirrors EmailTemplateContract.cs pattern for WhatsApp Business API templates.
/// Template names MUST match exactly what is registered with Meta Business Manager.
/// Parameter names MUST match the positional order defined in each Meta template.
/// </summary>
public static class WhatsAppTemplateContract
{
    #region Template Names

    public static class TemplateNames
    {
        // Tier 1: Must Have — Event Registration & Payment
        public const string EventRegistrationConfirmed = "event_registration_confirmed";
        public const string EventTicketConfirmation = "event_ticket_confirmation";

        // Tier 1: Must Have — Event Lifecycle
        public const string EventReminder = "event_reminder";
        public const string EventCancelled = "event_cancelled";
        public const string NewEventAnnouncement = "new_event_announcement";
        public const string EventDetailsUpdate = "event_details_update";

        // Tier 1: Must Have — Newsletter
        public const string NewsletterBroadcast = "newsletter_broadcast";

        // Tier 1: Must Have — Registration Management
        public const string RegistrationCancelled = "registration_cancelled";

        // Tier 1: Must Have — Sign-up Commitments
        public const string SignupCommitmentConfirmed = "signup_commitment_confirmed";
        public const string SignupCommitmentUpdated = "signup_commitment_updated";
        public const string SignupCommitmentCancelled = "signup_commitment_cancelled";

        // Tier 1: Must Have — Refunds
        public const string RefundInitiated = "refund_initiated";
        public const string RefundCompleted = "refund_completed";

        // Verification (used as fallback if SMS is unavailable)
        public const string PhoneVerification = "phone_verification";
    }

    #endregion

    #region Parameter Names

    /// <summary>
    /// Common parameters shared across multiple templates.
    /// </summary>
    public static class Common
    {
        public const string UserName = "user_name";
        public const string EventTitle = "event_title";
        public const string EventDate = "event_date";
        public const string EventTime = "event_time";
        public const string EventLocation = "event_location";
        public const string EventUrl = "event_url";
    }

    /// <summary>
    /// Registration-specific parameters.
    /// </summary>
    public static class Registration
    {
        public const string RegistrationQuantity = "registration_quantity";
        public const string TicketCode = "ticket_code";
        public const string CancellationReason = "cancellation_reason";
        public const string CancellationDate = "cancellation_date";
    }

    /// <summary>
    /// Event reminder parameters.
    /// </summary>
    public static class Reminder
    {
        public const string ReminderTimeframe = "reminder_timeframe";
        public const string ReminderMessage = "reminder_message";
    }

    /// <summary>
    /// Event cancellation parameters.
    /// </summary>
    public static class Cancellation
    {
        public const string CancellationReason = "cancellation_reason";
        public const string RefundMessage = "refund_message";
    }

    /// <summary>
    /// Sign-up commitment parameters.
    /// </summary>
    public static class Signup
    {
        public const string ListName = "list_name";
        public const string CommitmentItem = "commitment_item";
        public const string CommitmentQuantity = "commitment_quantity";
    }

    /// <summary>
    /// Refund-specific parameters.
    /// </summary>
    public static class Refund
    {
        public const string RefundAmount = "refund_amount";
        public const string RefundStatus = "refund_status";
        public const string ReferenceId = "reference_id";
    }

    /// <summary>
    /// Newsletter-specific parameters.
    /// </summary>
    public static class Newsletter
    {
        public const string NewsletterTitle = "newsletter_title";
        public const string NewsletterPreview = "newsletter_preview";
        public const string NewsletterUrl = "newsletter_url";
    }

    /// <summary>
    /// Phone verification parameter.
    /// </summary>
    public static class Verification
    {
        public const string VerificationCode = "verification_code";
    }

    #endregion
}

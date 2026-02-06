namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.97: Single source of truth for ALL email template parameter names.
///
/// PURPOSE:
/// These constants MUST match the Handlebars placeholders in database templates.
/// When adding new parameters, ADD them here FIRST, then update the TypedEmailParams class.
///
/// USAGE:
/// - Use these constants in ToDictionary() methods instead of hardcoded strings
/// - Use these constants when creating database migrations for templates
/// - Reference this file when troubleshooting "raw placeholder" issues in emails
///
/// MAINTENANCE:
/// - When adding a new template: Add to TemplateNames class
/// - When adding a new parameter: Add to appropriate parameter class
/// - Keep this file in sync with database templates
/// </summary>
public static class EmailTemplateContract
{
    #region Template Names

    /// <summary>
    /// All email template names in the system.
    /// These must match the 'name' column in communications.email_templates table.
    /// </summary>
    public static class TemplateNames
    {
        // Authentication Templates
        public const string PasswordReset = "template-password-reset";
        public const string PasswordChangeConfirmation = "template-password-change-confirmation";
        public const string EmailVerification = "template-email-verification";
        public const string WelcomeEmail = "template-welcome-email";

        // Event Publication Templates
        public const string NewEventPublication = "template-new-event-publication";
        public const string EventDetailsPublication = "template-event-details-publication";

        // Registration Templates
        public const string PaidEventRegistration = "template-paid-event-registration";
        public const string FreeEventRegistration = "template-free-event-registration";
        public const string EventRegistrationCancellation = "template-event-registration-cancellation";
        public const string TicketConfirmation = "template-ticket-confirmation";

        // Refund Templates
        public const string RefundRequested = "template-refund-requested";
        public const string RefundCompleted = "template-refund-completed";

        // Event Management Templates
        public const string EventApproved = "template-event-approved";
        public const string EventCancellation = "template-event-cancellation";
        public const string EventReminder = "template-event-reminder";
        public const string EventReminder24Hr = "template-event-reminder-24hr";
        public const string AttendeesAdded = "template-attendees-added";

        // Sign-up List Templates
        public const string SignupCommitmentConfirmation = "template-signup-list-commitment-confirmation";
        public const string SignupCommitmentUpdate = "template-signup-list-commitment-update";
        public const string SignupCommitmentCancellation = "template-signup-list-commitment-cancellation";

        // Support Templates
        public const string SupportTicketReceived = "template-support-ticket-received";
        public const string SupportTicketReply = "template-support-ticket-reply";

        // Admin Templates
        public const string AdminUserActivation = "template-admin-user-activation";
        public const string AdminUserDeactivation = "template-admin-user-deactivation";
    }

    #endregion

    #region Common Parameters (Used across ALL templates)

    /// <summary>
    /// Parameters common to all or most email templates.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Recipient's display name. Used in greeting: "Hi {{UserName}},"
        /// </summary>
        public const string UserName = "UserName";

        /// <summary>
        /// Current year for footer copyright. E.g., "© {{Year}} LankaConnect"
        /// </summary>
        public const string Year = "Year";

        /// <summary>
        /// Support email address for help links.
        /// </summary>
        public const string SupportEmail = "SupportEmail";

        /// <summary>
        /// Company name for branding.
        /// </summary>
        public const string CompanyName = "CompanyName";
    }

    #endregion

    #region Event Parameters

    /// <summary>
    /// Parameters related to event information.
    /// </summary>
    public static class Event
    {
        /// <summary>
        /// Event title/name.
        /// </summary>
        public const string EventTitle = "EventTitle";

        /// <summary>
        /// Formatted event date (e.g., "February 15, 2026").
        /// </summary>
        public const string EventStartDate = "EventStartDate";

        /// <summary>
        /// Formatted event time (e.g., "5:00 PM EST").
        /// </summary>
        public const string EventStartTime = "EventStartTime";

        /// <summary>
        /// Combined date and time (e.g., "February 15, 2026 at 5:00 PM EST").
        /// Preferred for standardized templates.
        /// </summary>
        public const string EventDateTime = "EventDateTime";

        /// <summary>
        /// Event location/address.
        /// </summary>
        public const string EventLocation = "EventLocation";

        /// <summary>
        /// URL to the event details page.
        /// Used for "View Event Details" CTA button.
        /// </summary>
        public const string EventDetailsUrl = "EventDetailsUrl";

        /// <summary>
        /// Alias for EventDetailsUrl (some templates use this).
        /// </summary>
        public const string EventUrl = "EventUrl";

        /// <summary>
        /// URL to the sign-up lists section of event page.
        /// </summary>
        public const string SignUpListsUrl = "SignUpListsUrl";

        /// <summary>
        /// Event description text.
        /// </summary>
        public const string EventDescription = "EventDescription";

        /// <summary>
        /// Event image URL.
        /// </summary>
        public const string EventImageUrl = "EventImageUrl";

        /// <summary>
        /// Maximum attendee capacity.
        /// </summary>
        public const string MaxAttendees = "MaxAttendees";
    }

    #endregion

    #region Organizer Contact Parameters

    /// <summary>
    /// Parameters for event organizer contact information.
    /// Used with {{#if HasOrganizerContact}} conditionals.
    /// </summary>
    public static class OrganizerContact
    {
        /// <summary>
        /// Boolean flag for Handlebars {{#if HasOrganizerContact}} conditional.
        /// </summary>
        public const string HasOrganizerContact = "HasOrganizerContact";

        /// <summary>
        /// Organizer's display name.
        /// </summary>
        public const string OrganizerContactName = "OrganizerContactName";

        /// <summary>
        /// Organizer's email address.
        /// </summary>
        public const string OrganizerContactEmail = "OrganizerContactEmail";

        /// <summary>
        /// Organizer's phone number.
        /// </summary>
        public const string OrganizerContactPhone = "OrganizerContactPhone";
    }

    #endregion

    #region Refund Parameters

    /// <summary>
    /// Parameters specific to refund-related emails.
    /// </summary>
    public static class Refund
    {
        /// <summary>
        /// Refund amount (formatted without $ symbol as templates have it hardcoded).
        /// </summary>
        public const string RefundAmount = "RefundAmount";

        /// <summary>
        /// Original payment amount.
        /// </summary>
        public const string OriginalAmount = "OriginalAmount";

        /// <summary>
        /// Stripe refund ID (e.g., "re_xxx123").
        /// </summary>
        public const string StripeRefundId = "StripeRefundId";

        /// <summary>
        /// Reference ID - StripeRefundId or PaymentIntentId fallback.
        /// </summary>
        public const string ReferenceId = "ReferenceId";

        /// <summary>
        /// Reason for refund request.
        /// </summary>
        public const string RefundReason = "RefundReason";

        /// <summary>
        /// Refund status (e.g., "Pending", "Completed").
        /// </summary>
        public const string RefundStatus = "RefundStatus";

        /// <summary>
        /// Timestamp when refund was requested.
        /// </summary>
        public const string RequestedAt = "RequestedAt";

        /// <summary>
        /// Timestamp when refund was completed.
        /// </summary>
        public const string CompletedAt = "CompletedAt";

        /// <summary>
        /// Processing method (e.g., "Original Payment Method").
        /// </summary>
        public const string ProcessingMethod = "ProcessingMethod";

        /// <summary>
        /// Currency code (e.g., "USD").
        /// </summary>
        public const string Currency = "Currency";

        /// <summary>
        /// URL to refund details page.
        /// </summary>
        public const string RefundDetailsUrl = "RefundDetailsUrl";
    }

    #endregion

    #region Registration Parameters

    /// <summary>
    /// Parameters for registration-related emails.
    /// </summary>
    public static class Registration
    {
        /// <summary>
        /// Reason for cancellation.
        /// </summary>
        public const string CancellationReason = "CancellationReason";

        /// <summary>
        /// Date/time of cancellation.
        /// </summary>
        public const string CancelledAt = "CancelledAt";

        /// <summary>
        /// Alias for CancelledAt (some templates use this).
        /// </summary>
        public const string CancellationDate = "CancellationDate";

        /// <summary>
        /// Registration confirmation number.
        /// </summary>
        public const string ConfirmationNumber = "ConfirmationNumber";

        /// <summary>
        /// Ticket type name.
        /// </summary>
        public const string TicketType = "TicketType";

        /// <summary>
        /// Ticket price.
        /// </summary>
        public const string TicketPrice = "TicketPrice";

        /// <summary>
        /// Number of tickets purchased.
        /// </summary>
        public const string TicketQuantity = "TicketQuantity";

        /// <summary>
        /// Total amount paid.
        /// </summary>
        public const string TotalAmount = "TotalAmount";

        /// <summary>
        /// URL to download/view ticket.
        /// </summary>
        public const string TicketUrl = "TicketUrl";
    }

    #endregion

    #region Sign-up List Parameters

    /// <summary>
    /// Parameters for sign-up list commitment emails.
    /// </summary>
    public static class SignupList
    {
        /// <summary>
        /// Name of the sign-up list.
        /// </summary>
        public const string ListName = "ListName";

        /// <summary>
        /// Item(s) committed to bring.
        /// </summary>
        public const string CommitmentItem = "CommitmentItem";

        /// <summary>
        /// Quantity committed.
        /// </summary>
        public const string CommitmentQuantity = "CommitmentQuantity";

        /// <summary>
        /// Total number of slots available.
        /// </summary>
        public const string TotalSlots = "TotalSlots";

        /// <summary>
        /// Number of slots taken.
        /// </summary>
        public const string SlotsTaken = "SlotsTaken";
    }

    #endregion

    #region Password/Auth Parameters

    /// <summary>
    /// Parameters for password reset and auth emails.
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// User's email address.
        /// </summary>
        public const string UserEmail = "UserEmail";

        /// <summary>
        /// Password reset token.
        /// </summary>
        public const string ResetToken = "ResetToken";

        /// <summary>
        /// Full password reset link.
        /// </summary>
        public const string ResetLink = "ResetLink";

        /// <summary>
        /// Token expiration timestamp.
        /// </summary>
        public const string ExpiresAt = "ExpiresAt";

        /// <summary>
        /// Timestamp when password was changed.
        /// </summary>
        public const string ChangedAt = "ChangedAt";

        /// <summary>
        /// Login page URL.
        /// </summary>
        public const string LoginUrl = "LoginUrl";

        /// <summary>
        /// Email verification link.
        /// </summary>
        public const string VerificationLink = "VerificationLink";
    }

    #endregion

    #region Support Ticket Parameters

    /// <summary>
    /// Parameters for support ticket emails.
    /// </summary>
    public static class Support
    {
        /// <summary>
        /// Support ticket ID/number.
        /// </summary>
        public const string TicketId = "TicketId";

        /// <summary>
        /// Ticket subject/title.
        /// </summary>
        public const string TicketSubject = "TicketSubject";

        /// <summary>
        /// Ticket message/description.
        /// </summary>
        public const string TicketMessage = "TicketMessage";

        /// <summary>
        /// Reply message from support.
        /// </summary>
        public const string ReplyMessage = "ReplyMessage";

        /// <summary>
        /// Ticket category.
        /// </summary>
        public const string Category = "Category";

        /// <summary>
        /// Ticket status.
        /// </summary>
        public const string TicketStatus = "TicketStatus";
    }

    #endregion

    #region Admin User Parameters

    /// <summary>
    /// Parameters for admin user management emails.
    /// </summary>
    public static class AdminUser
    {
        /// <summary>
        /// Reason for account activation/deactivation.
        /// </summary>
        public const string Reason = "Reason";

        /// <summary>
        /// Admin who took the action.
        /// </summary>
        public const string AdminName = "AdminName";

        /// <summary>
        /// Timestamp of the action.
        /// </summary>
        public const string ActionTimestamp = "ActionTimestamp";
    }

    #endregion
}

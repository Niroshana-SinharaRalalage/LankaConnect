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
    /// Phase 6A.113: Updated all constants to match EXACT database template names.
    /// These must match the 'name' column in communications.email_templates table.
    ///
    /// CRITICAL: These values are NOW synchronized with database. Do NOT change without migration!
    /// </summary>
    public static class TemplateNames
    {
        // Authentication Templates
        public const string PasswordReset = "template-password-reset";
        public const string PasswordChangeConfirmation = "template-password-change-confirmation";
        public const string EmailVerification = "template-membership-email-verification"; // Phase 6A.113: Corrected
        public const string WelcomeEmail = "template-welcome"; // Phase 6A.113: Corrected (removed -email suffix)

        // Event Publication Templates
        public const string NewEventPublication = "template-new-event-publication";
        public const string EventDetailsPublication = "template-event-details-publication";

        // Registration Templates
        public const string PaidEventRegistration = "template-paid-event-registration-confirmation-with-ticket"; // Phase 6A.113: Corrected
        public const string FreeEventRegistration = "template-free-event-registration-confirmation"; // Phase 6A.113: Corrected
        public const string EventRegistrationCancellation = "template-event-registration-cancellation";
        public const string PreliminaryRegistrationPayment = "template-preliminary-registration-payment-pending"; // Phase 6A.113: Added missing constant

        // Refund Templates
        public const string RefundRequested = "template-refund-requested";
        public const string RefundCompleted = "template-refund-completed";

        // Event Management Templates
        public const string EventApproval = "template-event-approval"; // Phase 6A.113: Renamed from EventApproved, corrected value
        public const string EventRejected = "template-event-rejected";
        public const string EventPostponed = "template-event-postponed";
        public const string EventCancellation = "template-event-cancellation-notifications"; // Phase 6A.113: Corrected
        public const string EventReminder = "template-event-reminder";
        public const string AttendeesAdded = "template-attendees-added-confirmation"; // Phase 6A.113: Corrected

        // Sign-up List Templates
        public const string SignupCommitmentConfirmation = "template-signup-list-commitment-confirmation";
        public const string SignupCommitmentUpdate = "template-signup-list-commitment-update";
        public const string SignupCommitmentCancellation = "template-signup-list-commitment-cancellation";

        // Support Templates
        public const string SupportTicketConfirmation = "template-support-ticket-confirmation"; // Phase 6A.113: Renamed from SupportTicketReceived, corrected value
        public const string SupportTicketReply = "template-support-ticket-reply";

        // Admin Templates
        public const string AccountActivatedByAdmin = "template-account-activated-by-admin"; // Phase 6A.113: Renamed from AdminUserActivation, corrected value
        public const string AccountDeactivatedByAdmin = "template-account-deactivated-by-admin"; // Phase 6A.113: Renamed from AdminUserDeactivation, corrected value

        // Organizer Templates
        public const string OrganizerRoleApproval = "template-organizer-role-approval";

        // Newsletter Templates
        public const string NewsletterSubscriptionConfirmation = "template-newsletter-subscription-confirmation";
        public const string NewsletterNotification = "template-newsletter-notification";

        // Form Response Templates (Phase 6A.107)
        public const string FormResponseConfirmation = "template-form-response-confirmation";
        public const string FormResponseUpdate = "template-form-response-update";
        public const string FormResponseCancellation = "template-form-response-cancellation";
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
        /// User's first name (used in welcome/business emails).
        /// </summary>
        public const string FirstName = "FirstName";

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

        /// <summary>
        /// URL to the user dashboard.
        /// </summary>
        public const string DashboardUrl = "DashboardUrl";

        /// <summary>
        /// Site URL for template linking.
        /// </summary>
        public const string SiteUrl = "SiteUrl";
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
        /// Alias for SignUpListsUrl - templates use singular form {{SignupListUrl}}.
        /// </summary>
        public const string SignupListUrl = "SignupListUrl";

        /// <summary>
        /// Boolean flag for {{#HasSignUpLists}} conditional blocks in templates.
        /// </summary>
        public const string HasSignUpLists = "HasSignUpLists";

        /// <summary>
        /// URL to the signup forms section of event page.
        /// Phase 6A.112: Added for "View Signup Forms" button in event emails.
        /// </summary>
        public const string SignupFormsUrl = "SignupFormsUrl";

        /// <summary>
        /// Boolean flag for {{#HasSignupForms}} conditional blocks in templates.
        /// Phase 6A.112: Added for "View Signup Forms" button in event emails.
        /// </summary>
        public const string HasSignupForms = "HasSignupForms";

        /// <summary>
        /// Alias for EventStartDate - some templates use {{EventDate}}.
        /// </summary>
        public const string EventDate = "EventDate";

        /// <summary>
        /// Organizer's display name (used in cancellation, approval, rejection emails).
        /// </summary>
        public const string OrganizerName = "OrganizerName";

        /// <summary>
        /// URL to manage the event (organizer-facing).
        /// </summary>
        public const string EventManageUrl = "EventManageUrl";

        /// <summary>
        /// URL to browse other events.
        /// </summary>
        public const string BrowseEventsUrl = "BrowseEventsUrl";

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

    #region Event Cancellation Parameters

    /// <summary>
    /// Parameters specific to event cancellation emails.
    /// </summary>
    public static class EventCancellation
    {
        /// <summary>
        /// Whether refunds will be processed for cancelled event.
        /// </summary>
        public const string RefundsWillBeProcessed = "RefundsWillBeProcessed";

        /// <summary>
        /// Refund status message for cancelled event.
        /// </summary>
        public const string RefundMessage = "RefundMessage";
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

        /// <summary>
        /// Registration date.
        /// </summary>
        public const string RegistrationDate = "RegistrationDate";

        /// <summary>
        /// Number of registrations/quantity.
        /// </summary>
        public const string Quantity = "Quantity";

        /// <summary>
        /// Flag for whether attendee details are available.
        /// </summary>
        public const string HasAttendeeDetails = "HasAttendeeDetails";

        /// <summary>
        /// Attendee details (JSON or formatted string).
        /// </summary>
        public const string Attendees = "Attendees";

        /// <summary>
        /// Flag for whether contact info is available.
        /// </summary>
        public const string HasContactInfo = "HasContactInfo";

        /// <summary>
        /// Contact email address.
        /// </summary>
        public const string ContactEmail = "ContactEmail";

        /// <summary>
        /// Contact phone number.
        /// </summary>
        public const string ContactPhone = "ContactPhone";
    }

    #endregion

    #region Payment Parameters

    /// <summary>
    /// Parameters for payment-related emails.
    /// </summary>
    public static class Payment
    {
        /// <summary>
        /// Amount paid (formatted).
        /// </summary>
        public const string AmountPaid = "AmountPaid";

        /// <summary>
        /// Total amount for the order.
        /// </summary>
        public const string TotalAmount = "TotalAmount";

        /// <summary>
        /// Stripe Payment Intent ID.
        /// </summary>
        public const string PaymentIntentId = "PaymentIntentId";

        /// <summary>
        /// Order confirmation number.
        /// </summary>
        public const string OrderNumber = "OrderNumber";

        /// <summary>
        /// Date of payment.
        /// </summary>
        public const string PaymentDate = "PaymentDate";

        /// <summary>
        /// URL for making a payment (e.g., preliminary registration payment).
        /// </summary>
        public const string PaymentLink = "PaymentLink";

        /// <summary>
        /// Alias for PaymentLink - some templates use {{PaymentUrl}}.
        /// </summary>
        public const string PaymentUrl = "PaymentUrl";

        /// <summary>
        /// Payment expiration time (alias for ExpiresAt in payment contexts).
        /// </summary>
        public const string ExpirationTime = "ExpirationTime";
    }

    #endregion

    #region Ticket Parameters

    /// <summary>
    /// Parameters for ticket-related emails.
    /// </summary>
    public static class Ticket
    {
        /// <summary>
        /// Flag for whether a ticket is attached/included.
        /// </summary>
        public const string HasTicket = "HasTicket";

        /// <summary>
        /// Ticket code/number.
        /// </summary>
        public const string TicketCode = "TicketCode";

        /// <summary>
        /// Ticket expiry date.
        /// </summary>
        public const string TicketExpiryDate = "TicketExpiryDate";

        /// <summary>
        /// URL to view/download ticket.
        /// </summary>
        public const string TicketUrl = "TicketUrl";

        /// <summary>
        /// Ticket type name (e.g., "General Admission", "VIP").
        /// </summary>
        public const string TicketType = "TicketType";
    }

    #endregion

    #region Event Image Parameters

    /// <summary>
    /// Parameters for event image display.
    /// </summary>
    public static class EventImage
    {
        /// <summary>
        /// Flag for whether an event image should be displayed.
        /// </summary>
        public const string HasEventImage = "HasEventImage";

        /// <summary>
        /// URL to the event image.
        /// </summary>
        public const string EventImageUrl = "EventImageUrl";
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
        /// Item description (alias for SignupItem in templates).
        /// </summary>
        public const string ItemDescription = "ItemDescription";

        /// <summary>
        /// Signup item name.
        /// </summary>
        public const string SignupItem = "SignupItem";

        /// <summary>
        /// Type of commitment (e.g., "Item Contribution").
        /// </summary>
        public const string CommitmentType = "CommitmentType";

        /// <summary>
        /// Instructions for pickup/delivery.
        /// </summary>
        public const string PickupInstructions = "PickupInstructions";

        /// <summary>
        /// Quantity committed.
        /// </summary>
        public const string CommitmentQuantity = "CommitmentQuantity";

        /// <summary>
        /// New quantity (for commitment update emails).
        /// </summary>
        public const string NewQuantity = "NewQuantity";

        /// <summary>
        /// Previous quantity (for commitment update emails).
        /// </summary>
        public const string OldQuantity = "OldQuantity";

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

    #region Newsletter Parameters

    /// <summary>
    /// Parameters for newsletter-related emails.
    /// </summary>
    public static class Newsletter
    {
        /// <summary>
        /// Newsletter title/subject.
        /// </summary>
        public const string NewsletterTitle = "NewsletterTitle";

        /// <summary>
        /// Newsletter HTML content.
        /// </summary>
        public const string NewsletterContent = "NewsletterContent";

        /// <summary>
        /// URL to unsubscribe from newsletter.
        /// </summary>
        public const string UnsubscribeLink = "UnsubscribeLink";

        /// <summary>
        /// Whether this newsletter is event-specific.
        /// </summary>
        public const string IsEventNewsletter = "IsEventNewsletter";
    }

    /// <summary>
    /// Phase 6A.107: Parameters for form response emails.
    /// Architect Review: Approved - includes all required parameters with timestamp and deadline support.
    /// </summary>
    public static class FormResponse
    {
        /// <summary>
        /// Form title/name.
        /// </summary>
        public const string FormTitle = "FormTitle";

        /// <summary>
        /// Form description (optional).
        /// </summary>
        public const string FormDescription = "FormDescription";

        /// <summary>
        /// Summary of user's responses (e.g., "Vegetarian preference, Arriving Friday").
        /// </summary>
        public const string ResponseSummary = "ResponseSummary";

        /// <summary>
        /// URL to edit the form response (includes access token for anonymous users).
        /// </summary>
        public const string EditFormUrl = "EditFormUrl";

        /// <summary>
        /// When the response was submitted.
        /// </summary>
        public const string SubmittedAt = "SubmittedAt";

        /// <summary>
        /// When the response was last updated (for update emails).
        /// </summary>
        public const string UpdatedAt = "UpdatedAt";

        /// <summary>
        /// When the response was cancelled (for cancellation emails).
        /// </summary>
        public const string CancelledAt = "CancelledAt";

        /// <summary>
        /// Response deadline (if set by organizer).
        /// </summary>
        public const string ResponseDeadline = "ResponseDeadline";

        /// <summary>
        /// Whether the form has a description.
        /// </summary>
        public const string HasFormDescription = "HasFormDescription";

        /// <summary>
        /// Whether the form has a response deadline.
        /// </summary>
        public const string HasResponseDeadline = "HasResponseDeadline";
    }

    #endregion

    #region Business Notification Parameters

    /// <summary>
    /// Phase 6A.100: Parameters for business notification emails.
    /// Supports 15+ notification types: approval, rejection, suspension, reviews, payments, etc.
    /// </summary>
    public static class Business
    {
        /// <summary>
        /// Business name.
        /// </summary>
        public const string BusinessName = "BusinessName";

        /// <summary>
        /// Business identifier.
        /// </summary>
        public const string BusinessId = "BusinessId";

        /// <summary>
        /// Business category (e.g., "Restaurant", "Retail").
        /// </summary>
        public const string BusinessCategory = "BusinessCategory";

        /// <summary>
        /// Business status (e.g., "Active", "Pending", "Suspended").
        /// </summary>
        public const string BusinessStatus = "BusinessStatus";

        /// <summary>
        /// Notification type (e.g., "BusinessApproved", "NewReview").
        /// </summary>
        public const string NotificationType = "NotificationType";

        /// <summary>
        /// Date/time of notification.
        /// </summary>
        public const string NotificationDate = "NotificationDate";

        /// <summary>
        /// URL to the business page.
        /// </summary>
        public const string BusinessUrl = "BusinessUrl";

        /// <summary>
        /// Whether this is an approval notification.
        /// </summary>
        public const string IsApproval = "IsApproval";

        /// <summary>
        /// Whether this is a rejection notification.
        /// </summary>
        public const string IsRejection = "IsRejection";

        /// <summary>
        /// Whether this is a suspension notification.
        /// </summary>
        public const string IsSuspension = "IsSuspension";

        /// <summary>
        /// Whether this is a critical notification (high priority).
        /// </summary>
        public const string IsCritical = "IsCritical";

        /// <summary>
        /// Whether this is a report notification.
        /// </summary>
        public const string IsReport = "IsReport";

        /// <summary>
        /// Whether this notification requires user action.
        /// </summary>
        public const string RequiresAction = "RequiresAction";

        /// <summary>
        /// Next steps instructions for the user.
        /// </summary>
        public const string NextSteps = "NextSteps";

        /// <summary>
        /// Action URL (e.g., review page, payment page).
        /// </summary>
        public const string ActionUrl = "ActionUrl";

        /// <summary>
        /// Report URL (e.g., analytics page).
        /// </summary>
        public const string ReportUrl = "ReportUrl";
    }

    #endregion
}

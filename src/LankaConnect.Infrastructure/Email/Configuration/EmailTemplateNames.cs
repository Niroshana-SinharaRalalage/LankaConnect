namespace LankaConnect.Infrastructure.Email.Configuration;

/// <summary>
/// Type-safe constants for email template names.
/// Eliminates magic strings and provides compile-time checking.
/// Templates are stored in database and referenced by these names.
/// Phase 0 - Email System Configuration Infrastructure
/// Phase 6A.76 - Updated to use descriptive 'template-*' naming convention
/// </summary>
public static class EmailTemplateNames
{
    /// <summary>
    /// Free event registration confirmation email.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string FreeEventRegistration = "template-free-event-registration-confirmation";

    /// <summary>
    /// Paid event registration confirmation email (sent after payment).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {Amount}, {Currency}, {TicketUrl}, {QRCode}
    /// </summary>
    public const string PaidEventRegistration = "template-paid-event-registration-confirmation-with-ticket";

    /// <summary>
    /// Event reminder email (sent before event starts).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string EventReminder = "template-event-reminder";

    /// <summary>
    /// Member email verification email.
    /// Variables: {UserName}, {VerificationUrl}, {TokenExpiry}
    /// </summary>
    public const string MemberEmailVerification = "template-membership-email-verification";

    /// <summary>
    /// Signup commitment confirmation email (when user confirms "I Will Attend").
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {SignupItem}, {CommitmentType}
    /// </summary>
    public const string SignupCommitmentConfirmation = "template-signup-list-commitment-confirmation";

    /// <summary>
    /// Signup commitment update email (when user updates their commitment).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {SignupItem}, {CommitmentType}
    /// </summary>
    public const string SignupCommitmentUpdate = "template-signup-list-commitment-update";

    /// <summary>
    /// Signup commitment cancellation email (when user cancels their commitment).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {SignupItem}
    /// </summary>
    public const string SignupCommitmentCancellation = "template-signup-list-commitment-cancellation";

    /// <summary>
    /// Registration cancellation confirmation email.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {RefundAmount}, {RefundCurrency}
    /// </summary>
    public const string RegistrationCancellation = "template-event-registration-cancellation";

    /// <summary>
    /// Event published notification email.
    /// Variables: {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string EventPublished = "template-new-event-publication";

    /// <summary>
    /// Event details publication email.
    /// Variables: {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string EventDetails = "template-event-details-publication";

    /// <summary>
    /// Event cancellation notification email.
    /// Variables: {EventTitle}, {EventDateTime}, {CancellationReason}
    /// </summary>
    public const string EventCancellation = "template-event-cancellation-notifications";

    /// <summary>
    /// Event approval notification email.
    /// Variables: {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string EventApproval = "template-event-approval";

    /// <summary>
    /// Newsletter notification email.
    /// Variables: {NewsletterTitle}, {NewsletterContent}, {UnsubscribeUrl}
    /// </summary>
    public const string Newsletter = "template-newsletter-notification";

    /// <summary>
    /// Newsletter subscription confirmation email.
    /// Variables: {UserName}, {UnsubscribeUrl}
    /// </summary>
    public const string NewsletterSubscriptionConfirmation = "template-newsletter-subscription-confirmation";

    /// <summary>
    /// Custom organizer email to event attendees/signups.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {CustomSubject}, {CustomBody}, {OrganizerName}, {OrganizerEmail}
    /// </summary>
    public const string OrganizerCustomEmail = "OrganizerCustomEmail";

    /// <summary>
    /// Password reset email with reset link.
    /// Variables: {UserName}, {UserEmail}, {ResetToken}, {ResetLink}, {ExpiresAt}, {CompanyName}, {SupportEmail}
    /// </summary>
    public const string PasswordReset = "template-password-reset";

    /// <summary>
    /// Password change confirmation email (sent after successful password reset).
    /// Variables: {UserName}, {UserEmail}, {ChangeDate}, {CompanyName}, {SupportEmail}, {LoginUrl}
    /// </summary>
    public const string PasswordChangeConfirmation = "template-password-change-confirmation";

    /// <summary>
    /// Welcome email sent after user verifies their email (different from verification email).
    /// Variables: {UserName}, {UserEmail}, {CompanyName}, {LoginUrl}
    /// </summary>
    public const string Welcome = "template-welcome";

    /// <summary>
    /// RSVP confirmation for anonymous (non-registered) event attendees.
    /// Variables: {UserName}, {EventTitle}, {EventStartDate}, {EventStartTime}, {EventEndDate},
    ///           {EventLocation}, {Quantity}, {RegistrationDate}, {Attendees}, {HasAttendeeDetails},
    ///           {ContactEmail}, {ContactPhone}, {HasContactInfo}
    /// </summary>
    public const string AnonymousRsvpConfirmation = "template-anonymous-rsvp-confirmation";

    /// <summary>
    /// Organizer role approval notification email.
    /// Variables: {UserName}, {ApprovedAt}, {DashboardUrl}
    /// </summary>
    public const string OrganizerRoleApproval = "template-organizer-role-approval";

    /// <summary>
    /// Gets all template names as a collection.
    /// Useful for validation, seeding, and documentation.
    /// </summary>
    public static IReadOnlyCollection<string> All { get; } = new[]
    {
        FreeEventRegistration,
        PaidEventRegistration,
        EventReminder,
        MemberEmailVerification,
        SignupCommitmentConfirmation,
        SignupCommitmentUpdate,
        SignupCommitmentCancellation,
        RegistrationCancellation,
        EventPublished,
        EventDetails,
        EventCancellation,
        EventApproval,
        Newsletter,
        NewsletterSubscriptionConfirmation,
        OrganizerCustomEmail,
        PasswordReset,
        PasswordChangeConfirmation,
        Welcome,
        AnonymousRsvpConfirmation,
        OrganizerRoleApproval
    };

    /// <summary>
    /// Validates that a template name is known/supported.
    /// </summary>
    /// <param name="templateName">Template name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string templateName)
    {
        return All.Contains(templateName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets template description for documentation purposes.
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <returns>Human-readable description</returns>
    public static string GetDescription(string templateName)
    {
        return templateName switch
        {
            FreeEventRegistration => "Free event registration confirmation email",
            PaidEventRegistration => "Paid event registration confirmation email (with ticket)",
            EventReminder => "Event reminder sent before event starts",
            MemberEmailVerification => "Member email verification email",
            SignupCommitmentConfirmation => "Signup commitment confirmation (I Will Attend)",
            SignupCommitmentUpdate => "Signup commitment update notification",
            SignupCommitmentCancellation => "Signup commitment cancellation notification",
            RegistrationCancellation => "Registration cancellation confirmation",
            EventPublished => "New event publication notification",
            EventDetails => "Event details publication notification",
            EventCancellation => "Event cancellation notification",
            EventApproval => "Event approval notification to organizer",
            Newsletter => "Newsletter notification email",
            NewsletterSubscriptionConfirmation => "Newsletter subscription confirmation",
            OrganizerCustomEmail => "Custom organizer email to attendees/signups",
            PasswordReset => "Password reset email with reset link",
            PasswordChangeConfirmation => "Password change confirmation email",
            Welcome => "Welcome email after email verification",
            AnonymousRsvpConfirmation => "RSVP confirmation for anonymous attendees",
            OrganizerRoleApproval => "Organizer role approval notification",
            _ => "Unknown template"
        };
    }
}

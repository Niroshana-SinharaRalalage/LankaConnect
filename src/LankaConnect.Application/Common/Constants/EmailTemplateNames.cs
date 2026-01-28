namespace LankaConnect.Application.Common.Constants;

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
    /// Free event registration confirmation email (member and anonymous users).
    /// Phase 6A.80: Updated to support both member and anonymous registrations.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// </summary>
    public const string FreeEventRegistration = "template-free-event-registration-confirmation";

    /// <summary>
    /// Paid event registration confirmation email (sent after payment, member and anonymous users).
    /// Phase 6A.80: Updated to support both member and anonymous registrations.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {Amount}, {Currency}, {TicketUrl}, {QRCode}
    /// </summary>
    public const string PaidEventRegistration = "template-paid-event-registration-confirmation-with-ticket";

    /// <summary>
    /// Phase 6A.81 Part 3: Email sent when Preliminary registration created (payment pending).
    /// Notifies user to complete payment within 24 hours.
    /// User decision: Immediate sending (not delayed).
    /// Variables: {UserName}, {EventTitle}, {EventStartDate}, {EventStartTime}, {EventLocation},
    ///           {AttendeeCount}, {TotalAmount}, {Currency}, {PaymentLink}, {ExpiresAt},
    ///           {HoursRemaining}, {RegistrationId}, {SupportEmail}, {Year}
    /// </summary>
    public const string PreliminaryRegistrationPayment = "template-preliminary-registration-payment-pending";

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

    // ❌ Phase 6A.80: REMOVED AnonymousRsvpConfirmation
    // Anonymous users now use FreeEventRegistration template (same as members)
    // This eliminates duplication and ensures consistent experience

    /// <summary>
    /// Organizer role approval notification email.
    /// Variables: {UserName}, {ApprovedAt}, {DashboardUrl}
    /// </summary>
    public const string OrganizerRoleApproval = "template-organizer-role-approval";

    // ============================================
    // Phase 6A.90: Admin User Management & Support/Feedback System Templates
    // ============================================

    /// <summary>
    /// Support ticket received confirmation email (auto-sent when contact form submitted).
    /// Phase 6A.90: Admin Support/Feedback System
    /// Variables: {Name}, {ReferenceId}, {Subject}, {Message}, {SupportEmail}, {Year}
    /// </summary>
    public const string SupportTicketConfirmation = "template-support-ticket-confirmation";

    /// <summary>
    /// Support ticket reply notification email (sent when admin replies to ticket).
    /// Phase 6A.90: Admin Support/Feedback System
    /// Variables: {Name}, {ReferenceId}, {Subject}, {ReplyContent}, {SupportEmail}, {Year}
    /// </summary>
    public const string SupportTicketReply = "template-support-ticket-reply";

    /// <summary>
    /// Account locked by admin notification email.
    /// Phase 6A.90: Admin User Management
    /// Variables: {UserName}, {LockUntil}, {Reason}, {SupportEmail}, {Year}
    /// </summary>
    public const string AccountLockedByAdmin = "template-account-locked-by-admin";

    /// <summary>
    /// Account unlocked by admin notification email.
    /// Phase 6A.90: Admin User Management
    /// Variables: {UserName}, {LoginUrl}, {SupportEmail}, {Year}
    /// </summary>
    public const string AccountUnlockedByAdmin = "template-account-unlocked-by-admin";

    /// <summary>
    /// Account activated by admin notification email.
    /// Phase 6A.90: Admin User Management
    /// Variables: {UserName}, {LoginUrl}, {SupportEmail}, {Year}
    /// </summary>
    public const string AccountActivatedByAdmin = "template-account-activated-by-admin";

    /// <summary>
    /// Account deactivated by admin notification email.
    /// Phase 6A.90: Admin User Management
    /// Variables: {UserName}, {SupportEmail}, {Year}
    /// </summary>
    public const string AccountDeactivatedByAdmin = "template-account-deactivated-by-admin";

    /// <summary>
    /// Gets all template names as a collection.
    /// Useful for validation, seeding, and documentation.
    /// Phase 6A.80: Removed AnonymousRsvpConfirmation - now using FreeEventRegistration for anonymous users.
    /// </summary>
    public static IReadOnlyCollection<string> All { get; } = new[]
    {
        FreeEventRegistration,
        PaidEventRegistration,
        PreliminaryRegistrationPayment, // Phase 6A.81 Part 3
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
        // AnonymousRsvpConfirmation, // ❌ Phase 6A.80: REMOVED - using FreeEventRegistration instead
        OrganizerRoleApproval,
        // Phase 6A.90: Admin User Management & Support/Feedback System
        SupportTicketConfirmation,
        SupportTicketReply,
        AccountLockedByAdmin,
        AccountUnlockedByAdmin,
        AccountActivatedByAdmin,
        AccountDeactivatedByAdmin
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
            FreeEventRegistration => "Free event registration confirmation email (member and anonymous)",
            PaidEventRegistration => "Paid event registration confirmation email with ticket (member and anonymous)",
            PreliminaryRegistrationPayment => "Preliminary registration email for pending payment (Phase 6A.81 Part 3)",
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
            // AnonymousRsvpConfirmation => "RSVP confirmation for anonymous attendees", // ❌ Phase 6A.80: REMOVED
            OrganizerRoleApproval => "Organizer role approval notification",
            // Phase 6A.90: Admin User Management & Support/Feedback System
            SupportTicketConfirmation => "Support ticket received confirmation email",
            SupportTicketReply => "Support ticket reply notification email",
            AccountLockedByAdmin => "Account locked by admin notification email",
            AccountUnlockedByAdmin => "Account unlocked by admin notification email",
            AccountActivatedByAdmin => "Account activated by admin notification email",
            AccountDeactivatedByAdmin => "Account deactivated by admin notification email",
            _ => "Unknown template"
        };
    }
}

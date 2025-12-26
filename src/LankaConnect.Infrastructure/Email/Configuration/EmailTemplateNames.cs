namespace LankaConnect.Infrastructure.Email.Configuration;

/// <summary>
/// Type-safe constants for email template names.
/// Eliminates magic strings and provides compile-time checking.
/// Templates are stored in database and referenced by these names.
/// Phase 0 - Email System Configuration Infrastructure
/// </summary>
public static class EmailTemplateNames
{
    /// <summary>
    /// Free event registration confirmation email.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// Status: ✅ Existing (working)
    /// </summary>
    public const string FreeEventRegistration = "FreeEventRegistration";

    /// <summary>
    /// Paid event registration confirmation email (sent after payment).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {Amount}, {Currency}, {TicketUrl}, {QRCode}
    /// Status: 🔧 Broken (Phase 6A.49 - PaymentCompletedEvent not dispatched)
    /// </summary>
    public const string PaidEventRegistration = "PaidEventRegistration";

    /// <summary>
    /// Event reminder email (sent before event starts).
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl}
    /// Status: ✅ Existing (working)
    /// </summary>
    public const string EventReminder = "EventReminder";

    /// <summary>
    /// Member email verification email.
    /// Variables: {UserName}, {VerificationUrl}, {TokenExpiry}
    /// Status: 🆕 New (Phase 6A.53)
    /// </summary>
    public const string MemberEmailVerification = "MemberEmailVerification";

    /// <summary>
    /// Signup commitment confirmation email (when user confirms "I Will Attend").
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {SignupItem}, {CommitmentType}
    /// Status: 🆕 New (Phase 6A.51)
    /// </summary>
    public const string SignupCommitmentConfirmation = "SignupCommitmentConfirmation";

    /// <summary>
    /// Registration cancellation confirmation email.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {RefundAmount}, {RefundCurrency}
    /// Status: 🆕 New (Phase 6A.52)
    /// </summary>
    public const string RegistrationCancellation = "RegistrationCancellation";

    /// <summary>
    /// Custom organizer email to event attendees/signups.
    /// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation}, {EventDetailsUrl},
    ///           {CustomSubject}, {CustomBody}, {OrganizerName}, {OrganizerEmail}
    /// Status: 🆕 New (Phase 6A.50)
    /// </summary>
    public const string OrganizerCustomEmail = "OrganizerCustomEmail";

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
        RegistrationCancellation,
        OrganizerCustomEmail
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
            RegistrationCancellation => "Registration cancellation confirmation",
            OrganizerCustomEmail => "Custom organizer email to attendees/signups",
            _ => "Unknown template"
        };
    }
}

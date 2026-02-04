using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for signup commitment emails.
/// Templates: template-signup-list-commitment-confirmation, template-signup-list-commitment-update, template-signup-list-commitment-cancellation
///
/// This replaces Dictionary&lt;string, object&gt; in UserCommittedToSignUpEventHandler,
/// CommitmentUpdatedEventHandler, and CommitmentCancelledEmailHandler with
/// compile-time type-safe parameters.
///
/// Parameters match exactly what the templates expect:
/// - UserName, EventTitle, SignupItem, Quantity, EventDateTime, EventLocation, EventDetailsUrl, CommitmentType, PickupInstructions
///
/// Phase 6A.87 Fix: Corrected template names to include "list" (template-signup-list-commitment-*).
/// </summary>
public class SignupCommitmentEmailParams : IEmailParameters
{
    private string _templateName = "template-signup-list-commitment-confirmation";

    /// <summary>
    /// The template name for signup commitment.
    /// </summary>
    public string TemplateName => _templateName;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => UserEmail;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #region Core Properties

    /// <summary>
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// The item description for the signup commitment.
    /// </summary>
    public string SignupItem { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of items committed.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Event start date/time.
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event timezone ID for proper date/time formatting.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// URL to view event details.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Type of commitment (e.g., "Item Contribution").
    /// </summary>
    public string CommitmentType { get; set; } = "Item Contribution";

    /// <summary>
    /// Instructions for pickup/delivery.
    /// </summary>
    public string PickupInstructions { get; set; } = "Please coordinate pickup/delivery details with the event organizer.";

    #endregion

    #region Template Type

    /// <summary>
    /// Sets the template to use for confirmation emails.
    /// Phase 6A.87 Fix: Corrected template name to include "list".
    /// </summary>
    public SignupCommitmentEmailParams AsConfirmation()
    {
        _templateName = "template-signup-list-commitment-confirmation";
        return this;
    }

    /// <summary>
    /// Sets the template to use for update emails.
    /// Phase 6A.87 Fix: Corrected template name to include "list".
    /// </summary>
    public SignupCommitmentEmailParams AsUpdate()
    {
        _templateName = "template-signup-list-commitment-update";
        return this;
    }

    /// <summary>
    /// Sets the template to use for cancellation emails.
    /// Phase 6A.87 Fix: Corrected template name to include "list".
    /// </summary>
    public SignupCommitmentEmailParams AsCancellation()
    {
        _templateName = "template-signup-list-commitment-cancellation";
        return this;
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "SignupItem", SignupItem },
            { "Quantity", Quantity },
            { "EventDateTime", EmailDateTimeHelper.FormatDateTimeWithTz(EventStartDate, TimeZoneId) },
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "CommitmentType", CommitmentType },
            { "PickupInstructions", PickupInstructions }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(SignupItem))
            errors.Add("SignupItem is required");

        if (Quantity <= 0)
            errors.Add("Quantity must be greater than 0");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new SignupCommitmentEmailParams for a confirmation email.
    /// </summary>
    public static SignupCommitmentEmailParams CreateConfirmation(
        Guid userId,
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string signupItem,
        int quantity,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl)
    {
        return new SignupCommitmentEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            SignupItem = signupItem,
            Quantity = quantity,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl
        }.AsConfirmation();
    }

    /// <summary>
    /// Creates a new SignupCommitmentEmailParams for an update email.
    /// </summary>
    public static SignupCommitmentEmailParams CreateUpdate(
        Guid userId,
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string signupItem,
        int quantity,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl)
    {
        return new SignupCommitmentEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            SignupItem = signupItem,
            Quantity = quantity,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl
        }.AsUpdate();
    }

    /// <summary>
    /// Creates a new SignupCommitmentEmailParams for a cancellation email.
    /// </summary>
    public static SignupCommitmentEmailParams CreateCancellation(
        Guid userId,
        string userName,
        string userEmail,
        Guid eventId,
        string eventTitle,
        string signupItem,
        int quantity,
        DateTime eventStartDate,
        string? timeZoneId,
        string eventLocation,
        string eventDetailsUrl)
    {
        return new SignupCommitmentEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            EventId = eventId,
            EventTitle = eventTitle,
            SignupItem = signupItem,
            Quantity = quantity,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl
        }.AsCancellation();
    }

    #endregion
}

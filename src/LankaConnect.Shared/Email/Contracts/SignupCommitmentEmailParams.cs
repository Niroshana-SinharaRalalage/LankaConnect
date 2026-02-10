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

    /// <summary>
    /// New quantity (for update template).
    /// </summary>
    public int NewQuantity { get; set; }

    /// <summary>
    /// Old/previous quantity (for update template).
    /// </summary>
    public int OldQuantity { get; set; }

    #endregion

    #region Signup Lists Properties

    /// <summary>
    /// Whether the event has signup lists (controls {{#HasSignUpLists}} conditional).
    /// </summary>
    public bool HasSignUpLists { get; set; } = false;

    /// <summary>
    /// URL to view signup lists for the event.
    /// </summary>
    public string SignUpListsUrl { get; set; } = string.Empty;

    #endregion

    #region Organizer Contact Properties

    /// <summary>
    /// Whether organizer contact info is available (controls {{#HasOrganizerContact}} conditional).
    /// </summary>
    public bool HasOrganizerContact { get; set; } = false;

    /// <summary>
    /// Organizer's name.
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's email.
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's phone.
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

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
    /// Phase 6A.87+ Fix: Added ItemDescription alias, organizer contact, and signup list params.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            // Core params
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "ItemDescription", SignupItem },  // Template expects ItemDescription
            { "SignupItem", SignupItem },       // Keep for backward compatibility
            { "Quantity", Quantity },
            { "EventDateTime", EmailDateTimeHelper.FormatDateTimeWithTz(EventStartDate, TimeZoneId) },
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "CommitmentType", CommitmentType },
            { "PickupInstructions", PickupInstructions },

            // Signup list params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular

            // Update template params
            { "EventDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
            { "NewQuantity", NewQuantity },
            { "OldQuantity", OldQuantity },

            // Organizer contact params (for {{#HasOrganizerContact}} conditional)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },

            // Footer params
            { "Year", DateTime.UtcNow.Year }
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

    #region Fluent Setters

    /// <summary>
    /// Sets organizer contact information.
    /// </summary>
    public SignupCommitmentEmailParams WithOrganizerContact(
        string? name,
        string? email = null,
        string? phone = null)
    {
        HasOrganizerContact = !string.IsNullOrWhiteSpace(name);
        OrganizerContactName = name ?? string.Empty;
        OrganizerContactEmail = email ?? string.Empty;
        OrganizerContactPhone = phone ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets signup lists URL (if event has signup lists).
    /// </summary>
    public SignupCommitmentEmailParams WithSignUpLists(string signUpListsUrl)
    {
        HasSignUpLists = !string.IsNullOrWhiteSpace(signUpListsUrl);
        SignUpListsUrl = signUpListsUrl ?? string.Empty;
        return this;
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

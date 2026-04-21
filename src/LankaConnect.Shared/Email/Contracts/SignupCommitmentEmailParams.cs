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
    /// Event location (legacy flat fallback — "Street, City" or "Online Event").
    /// Phase 7C.2: Prefer <see cref="WithLocationDetails"/> to populate the 8
    /// decomposed keys. This string is kept so un-migrated templates that still
    /// reference {{EventLocation}} continue to render.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7C.2: Full projection of the event's primary + optional secondary
    /// location. When non-null, <see cref="ToDictionary"/> emits the 8 decomposed
    /// keys (LocationName, LocationAddress, HasLocationName, HasSecondaryLocation,
    /// SecondaryLocationLabel, SecondaryLocationName, HasSecondaryLocationName,
    /// SecondaryLocationAddress) and overwrites EventLocation with the projection's
    /// LegacyFlatString. Populated via <see cref="WithLocationDetails"/>.
    /// </summary>
    public LocationEmailProjection? LocationDetails { get; set; }

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

    /// <summary>
    /// Whether the event has signup forms (controls {{#HasSignupForms}} conditional).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public bool HasSignupForms { get; set; } = false;

    /// <summary>
    /// URL to signup forms section of event (if event has signup forms).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public string SignupFormsUrl { get; set; } = string.Empty;

    #endregion

    #region Event Image Properties

    /// <summary>
    /// Whether the event has an image (controls {{#HasEventImage}} conditional).
    /// </summary>
    public bool HasEventImage { get; set; } = false;

    /// <summary>
    /// URL to the event's primary image.
    /// </summary>
    public string EventImageUrl { get; set; } = string.Empty;

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

    /// <summary>
    /// Phase 6A.133 Email: Pre-formatted HTML for all organizer contacts.
    /// </summary>
    public string OrganizerContactsHtml { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Dynamic header text ("EVENT ORGANIZER" or "EVENT ORGANIZERS").
    /// </summary>
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";

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

    /// <summary>
    /// Phase 7D.1: Switches template to the volunteer-commitment confirmation template.
    /// Handlebars parameter shape is identical to the signup-list variant, so all
    /// existing ToDictionary() population continues to work unchanged.
    /// </summary>
    public SignupCommitmentEmailParams AsVolunteerConfirmation()
    {
        _templateName = EmailTemplateContract.TemplateNames.VolunteerCommitmentConfirmation;
        return this;
    }

    /// <summary>
    /// Phase 7D.1: Switches template to the volunteer-commitment cancellation template.
    /// </summary>
    public SignupCommitmentEmailParams AsVolunteerCancellation()
    {
        _templateName = EmailTemplateContract.TemplateNames.VolunteerCommitmentCancellation;
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
        var dict = new Dictionary<string, object>
        {
            // Core params
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "ItemDescription", SignupItem },  // Template expects ItemDescription
            { "SignupItem", SignupItem },       // Keep for backward compatibility
            { "Quantity", Quantity },
            { "EventDateTime", EmailDateTimeHelper.FormatDateTimeWithTz(EventStartDate, TimeZoneId) },
            // Phase 7C.2: LocationDetails (when set) overwrites EventLocation with its
            // legacy flat string AND adds the 8 decomposed keys via the writer below.
            { "EventLocation", EventLocation },
            { "EventDetailsUrl", EventDetailsUrl },
            { "CommitmentType", CommitmentType },
            { "PickupInstructions", PickupInstructions },

            // Signup list params (for {{#HasSignUpLists}} conditional)
            { "HasSignUpLists", HasSignUpLists },
            { "SignUpListsUrl", SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112

            // Update template params
            { "EventDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
            { "NewQuantity", NewQuantity },
            { "OldQuantity", OldQuantity },

            // Organizer contact params (for {{#HasOrganizerContact}} conditional)
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader },

            // Event image params (for {{#HasEventImage}} conditional)
            { EmailTemplateContract.EventImage.HasEventImage, HasEventImage },
            { EmailTemplateContract.EventImage.EventImageUrl, EventImageUrl },

            // Footer params
            { "Year", DateTime.UtcNow.Year }
        };

        // Phase 7C.2: Emit 8 decomposed location keys (+ legacy EventLocation override).
        // When no projection was supplied we still emit the keys as empty/false so
        // templates containing {{#if HasSecondaryLocation}} don't see an "undefined".
        LocationEmailDictionaryWriter.WriteTo(
            dict,
            LocationDetails ?? LocationEmailProjection.Online with { LegacyFlatString = EventLocation });

        return dict;
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

        // Phase 6A.127: Cancellation emails must fire even if quantity resolves to 0
        // (pre-Phase 6A.121 commitments may have null PhysicalQuantity AND null SlotsClaimed)
        var isCancellation = _templateName == "template-signup-list-commitment-cancellation";
        if (!isCancellation && Quantity <= 0)
            errors.Add("Quantity must be greater than 0");

        if (EventStartDate == default)
            errors.Add("EventStartDate is required");

        return errors.Count == 0;
    }

    #endregion

    #region Fluent Setters

    /// <summary>
    /// Sets the event image URL. If a non-empty URL is provided, HasEventImage is set to true.
    /// </summary>
    public SignupCommitmentEmailParams WithEventImage(string imageUrl)
    {
        HasEventImage = !string.IsNullOrEmpty(imageUrl);
        EventImageUrl = imageUrl ?? string.Empty;
        return this;
    }

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
    /// Phase 6A.133 Email: Sets all organizer contacts with pre-formatted HTML.
    /// </summary>
    public SignupCommitmentEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
    {
        if (contacts.Count > 0)
        {
            HasOrganizerContact = true;
            var primary = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts[0];
            OrganizerContactName = primary.Name;
            OrganizerContactEmail = primary.Email ?? string.Empty;
            OrganizerContactPhone = primary.Phone ?? string.Empty;
        }
        OrganizerContactsHtml = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);
        OrganizerContactHeader = OrganizerContactHtmlBuilder.BuildHeaderText(contacts.Count);
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

    
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public SignupCommitmentEmailParams WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Phase 7C.2: Copies the full location projection into this params instance
    /// and overwrites <see cref="EventLocation"/> with <c>projection.LegacyFlatString</c>
    /// so legacy and Phase-7C.2 templates both render correctly. Throws when
    /// <paramref name="projection"/> is null (caller bug — projections are always
    /// non-null since <c>EventExtensions.ProjectEmailLocation</c> returns the
    /// <see cref="LocationEmailProjection.Online"/> sentinel for address-less events).
    /// </summary>
    public SignupCommitmentEmailParams WithLocationDetails(LocationEmailProjection projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        LocationDetails = projection;
        EventLocation = projection.LegacyFlatString;
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

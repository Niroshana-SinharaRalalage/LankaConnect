using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for newsletter emails.
/// Template: template-newsletter-notification
///
/// This replaces Dictionary&lt;string, object&gt; in NewsletterEmailJob with
/// compile-time type-safe parameters.
///
/// Sent to newsletter subscribers when a new newsletter is published.
/// Can be linked to an event or standalone.
/// </summary>
public class NewsletterEmailParams : IEmailParameters, IUnsubscribeableEmail
{
    #region Template Selection

    /// <summary>
    /// The template name for newsletter notifications.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.NewsletterNotification;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name (uses email for newsletter subscriptions).
    /// </summary>
    public string RecipientName => RecipientEmail;

    #endregion

    #region Newsletter Properties

    /// <summary>
    /// Newsletter title.
    /// </summary>
    public string NewsletterTitle { get; set; } = string.Empty;

    /// <summary>
    /// Newsletter content/body.
    /// </summary>
    public string NewsletterContent { get; set; } = string.Empty;

    /// <summary>
    /// URL to the dashboard.
    /// </summary>
    public string DashboardUrl { get; set; } = string.Empty;

    #endregion

    #region Event Properties (Optional)

    /// <summary>
    /// Whether this newsletter is linked to an event.
    /// </summary>
    public bool IsEventNewsletter { get; set; }

    /// <summary>
    /// Event ID if linked (use false for Handlebars {{#if EventId}} when not linked).
    /// </summary>
    public object EventId { get; set; } = false;

    /// <summary>
    /// Event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event date and time (formatted).
    /// </summary>
    public string EventDateTime { get; set; } = string.Empty;

    /// <summary>
    /// Event description.
    /// </summary>
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>
    /// Event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// URL to event details page.
    /// </summary>
    public string EventDetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event has sign-up lists.
    /// </summary>
    public bool HasSignUpLists { get; set; }

    /// <summary>
    /// URL to the sign-up lists section.
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

    /// <summary>
    /// URL for unsubscribing from newsletter.
    /// </summary>
    public string UnsubscribeLink { get; set; } = string.Empty;

    /// <summary>
    /// IUnsubscribeableEmail implementation — aliases UnsubscribeLink for header generation.
    /// </summary>
    public string? UnsubscribeUrl => string.IsNullOrWhiteSpace(UnsubscribeLink) ? null : UnsubscribeLink;

    #endregion

    #region Organizer Contact Properties

    /// <summary>
    /// Whether event has organizer contact information.
    /// Phase 6A.133 Email: Controls {{#if HasOrganizerContact}} conditional in template.
    /// Only relevant for event-linked newsletters.
    /// </summary>
    public bool HasOrganizerContact { get; set; } = false;

    /// <summary>
    /// Primary organizer's name (backward compatibility).
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Primary organizer's email (backward compatibility).
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Primary organizer's phone (backward compatibility).
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Pre-formatted HTML table for all organizer contacts.
    /// Rendered via triple-brace {{{OrganizerContactsHtml}}} for unescaped HTML.
    /// </summary>
    public string OrganizerContactsHtml { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.133 Email: Dynamic header text ("EVENT ORGANIZER" or "EVENT ORGANIZERS").
    /// </summary>
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Uses EmailTemplateContract constants for all parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "NewsletterTitle", NewsletterTitle },
            { "NewsletterContent", NewsletterContent },
            { "DashboardUrl", DashboardUrl },
            { "IsEventNewsletter", IsEventNewsletter },
            { "EventId", EventId },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.EventDateTime, EventDateTime },
            { EmailTemplateContract.Event.EventDescription, EventDescription },
            { EmailTemplateContract.Event.EventLocation, EventLocation },
            { EmailTemplateContract.Event.EventDetailsUrl, EventDetailsUrl },
            { "HasSignUpLists", HasSignUpLists },
            { EmailTemplateContract.Event.SignUpListsUrl, SignUpListsUrl },
            { "SignupListUrl", SignUpListsUrl },  // Alias: template uses {{SignupListUrl}} singular
            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112
            { "UnsubscribeLink", UnsubscribeLink },

            // Phase 6A.133 Email: Organizer contact parameters (for event-linked newsletters)
            { EmailTemplateContract.OrganizerContact.HasOrganizerContact, HasOrganizerContact },
            { EmailTemplateContract.OrganizerContact.OrganizerContactName, OrganizerContactName },
            { EmailTemplateContract.OrganizerContact.OrganizerContactEmail, OrganizerContactEmail },
            { EmailTemplateContract.OrganizerContact.OrganizerContactPhone, OrganizerContactPhone },
            { EmailTemplateContract.OrganizerContact.OrganizerContactsHtml, OrganizerContactsHtml },
            { EmailTemplateContract.OrganizerContact.OrganizerContactHeader, OrganizerContactHeader }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(NewsletterTitle))
            errors.Add("NewsletterTitle is required");

        if (string.IsNullOrWhiteSpace(NewsletterContent))
            errors.Add("NewsletterContent is required");

        if (string.IsNullOrWhiteSpace(DashboardUrl))
            errors.Add("DashboardUrl is required");

        // If event newsletter, validate event fields
        if (IsEventNewsletter)
        {
            if (string.IsNullOrWhiteSpace(EventTitle))
                errors.Add("EventTitle is required for event newsletters");

            if (string.IsNullOrWhiteSpace(EventDetailsUrl))
                errors.Add("EventDetailsUrl is required for event newsletters");
        }

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new NewsletterEmailParams for a standalone newsletter.
    /// </summary>
    public static NewsletterEmailParams CreateStandalone(
        string recipientEmail,
        string newsletterTitle,
        string newsletterContent,
        string dashboardUrl)
    {
        return new NewsletterEmailParams
        {
            RecipientEmail = recipientEmail,
            NewsletterTitle = newsletterTitle,
            NewsletterContent = newsletterContent,
            DashboardUrl = dashboardUrl,
            IsEventNewsletter = false,
            EventId = false
        };
    }

    /// <summary>
    /// Creates a new NewsletterEmailParams linked to an event.
    /// </summary>
    public static NewsletterEmailParams CreateForEvent(
        string recipientEmail,
        string newsletterTitle,
        string newsletterContent,
        string dashboardUrl,
        Guid eventId,
        string eventTitle,
        string eventDateTime,
        string eventDescription,
        string eventLocation,
        string eventDetailsUrl,
        bool hasSignUpLists,
        string signUpListsUrl)
    {
        return new NewsletterEmailParams
        {
            RecipientEmail = recipientEmail,
            NewsletterTitle = newsletterTitle,
            NewsletterContent = newsletterContent,
            DashboardUrl = dashboardUrl,
            IsEventNewsletter = true,
            EventId = eventId,
            EventTitle = eventTitle,
            EventDateTime = eventDateTime,
            EventDescription = eventDescription,
            EventLocation = eventLocation,
            EventDetailsUrl = eventDetailsUrl,
            HasSignUpLists = hasSignUpLists,
            SignUpListsUrl = signUpListsUrl
        };
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Phase 6A.133 Email: Sets all organizer contacts with pre-formatted HTML table.
    /// Only applicable for event-linked newsletters where the event has organizer contacts.
    /// </summary>
    public NewsletterEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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

    #endregion
}

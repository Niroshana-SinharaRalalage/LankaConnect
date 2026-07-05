using System.Globalization;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;

namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Parameters for the "your refund decision" email.
/// Template: template-refund-decision (header: "Refund Decision").
///
/// Fires AFTER organizer approves an attendee-initiated request, OR at organizer-initiated
/// request creation (D8b — handler subscribes to OrganizerInitiatedRefundCreatedEvent).
///
/// Operator UAT (E3) showed attendees couldn't tell apart "the standalone $125 sponsor email"
/// vs "the consolidated $255 decision email." This single template carries the full per-line
/// breakdown so it's unambiguously the authoritative summary; the per-Sponsor standalone email
/// is suppressed for workflow-owned refunds in D9.
/// </summary>
public class RefundDecisionEmailParams : IEmailParameters, IDispatchLoggable
{
    public string TemplateName => EmailTemplateContract.TemplateNames.RefundDecision;
    public string RecipientEmail => UserEmail;
    public string RecipientName => UserName;

    // Phase 6A.148.W5.6.B.OBS2 — dispatch-log threading.
    Guid? IDispatchLoggable.DispatchRefundRequestId => RefundRequestId == Guid.Empty ? null : RefundRequestId;
    string? IDispatchLoggable.DispatchEntityType => null;
    Guid? IDispatchLoggable.DispatchEntityId => null;

    #region Core Identity
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid RegistrationId { get; set; }
    public Guid RefundRequestId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public string? TimeZoneId { get; set; }
    public string EventDetailsUrl { get; set; } = string.Empty;
    #endregion

    #region Refund-Specific
    public IReadOnlyList<RefundLineItemView> LineItems { get; set; } = Array.Empty<RefundLineItemView>();
    public decimal ApprovedTotal { get; set; }
    public decimal RequestedTotal { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// True when the organizer created the refund on the attendee's behalf (D2/D8b path).
    /// Drives a body copy variant: "Your organizer has initiated a refund on your behalf"
    /// instead of "Your organizer has decided on your refund request."
    /// </summary>
    public bool IsOrganizerInitiated { get; set; }

    public DateTime DecidedAt { get; set; }
    #endregion

    #region Organizer Contacts
    public bool HasOrganizerContact { get; set; }
    public string OrganizerContactName { get; set; } = string.Empty;
    public string OrganizerContactEmail { get; set; } = string.Empty;
    public string OrganizerContactPhone { get; set; } = string.Empty;
    public string OrganizerContactsHtml { get; set; } = string.Empty;
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";
    #endregion

    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    public RefundDecisionEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
    {
        if (contacts == null || contacts.Count == 0)
            return this;

        HasOrganizerContact = true;
        var primary = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts[0];
        OrganizerContactName = primary.Name;
        OrganizerContactEmail = primary.Email ?? string.Empty;
        OrganizerContactPhone = primary.Phone ?? string.Empty;
        OrganizerContactsHtml = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);
        OrganizerContactHeader = OrganizerContactHtmlBuilder.BuildHeaderText(contacts.Count);
        return this;
    }

    public Dictionary<string, object> ToDictionary()
    {
        var formattedDate = EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(EventStartDate, TimeZoneId);
        var lineItemsHtml = RefundLineItemsHtmlBuilder.BuildDecisionListHtml(LineItems, Currency);

        return new Dictionary<string, object>
        {
            // Common
            { "UserName", UserName },
            { "EventTitle", EventTitle },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },
            { "EventDetailsUrl", EventDetailsUrl },
            { "SupportEmail", SupportEmail },
            { "Year", DateTime.UtcNow.Year },

            // Refund-specific
            { "LineItemsHtml", lineItemsHtml },
            { "ApprovedTotal", ApprovedTotal.ToString("F2", CultureInfo.InvariantCulture) },
            { "RequestedTotal", RequestedTotal.ToString("F2", CultureInfo.InvariantCulture) },
            { "Currency", Currency },
            { "IsOrganizerInitiated", IsOrganizerInitiated },
            { "DecidedAt", DecidedAt.ToString("MMMM dd, yyyy h:mm tt") },

            // Organizer contacts
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone },
            { "OrganizerContactsHtml", OrganizerContactsHtml },
            { "OrganizerContactHeader", OrganizerContactHeader }
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (UserId == Guid.Empty) errors.Add("UserId is required");
        if (string.IsNullOrWhiteSpace(UserName)) errors.Add("UserName is required");
        if (string.IsNullOrWhiteSpace(UserEmail)) errors.Add("UserEmail is required");
        if (RefundRequestId == Guid.Empty) errors.Add("RefundRequestId is required");
        if (EventId == Guid.Empty) errors.Add("EventId is required");
        if (string.IsNullOrWhiteSpace(EventTitle)) errors.Add("EventTitle is required");
        if (LineItems == null || LineItems.Count == 0) errors.Add("LineItems must have at least one entry");
        if (ApprovedTotal < 0) errors.Add("ApprovedTotal cannot be negative");
        if (RequestedTotal <= 0) errors.Add("RequestedTotal must be greater than zero");
        return errors.Count == 0;
    }

    public static RefundDecisionEmailParams Create(
        Guid userId,
        string userName,
        string userEmail,
        Guid registrationId,
        Guid refundRequestId,
        Guid eventId,
        string eventTitle,
        DateTime eventStartDate,
        string? timeZoneId,
        IReadOnlyList<RefundLineItemView> lineItems,
        string currency,
        bool isOrganizerInitiated,
        DateTime decidedAt,
        string eventDetailsUrl)
    {
        var safeLines = lineItems ?? Array.Empty<RefundLineItemView>();
        return new RefundDecisionEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            RegistrationId = registrationId,
            RefundRequestId = refundRequestId,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartDate = eventStartDate,
            TimeZoneId = timeZoneId,
            LineItems = safeLines,
            RequestedTotal = safeLines.Sum(li => li.RequestedAmount),
            ApprovedTotal = safeLines.Sum(li => li.ApprovedAmount ?? 0m),
            Currency = currency ?? "USD",
            IsOrganizerInitiated = isOrganizerInitiated,
            DecidedAt = decidedAt,
            EventDetailsUrl = eventDetailsUrl ?? string.Empty
        };
    }
}

using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Parameters for the "your refund request was declined" email.
/// Template: template-refund-rejected (header: "Refund Request Declined").
///
/// Fires when the organizer rejects the entire request. The customer-facing
/// <see cref="RejectionReason"/> is a first-class top-level field — no body-stuffing
/// like the 148.c handler used (which jammed the reason into RefundReason text).
///
/// Per product decision Q4 (locked in MASTER_TODO_PHASE_6A_148): this is the END state.
/// No "Contact Organizer" CTA, no escalation path. Organizer contact block still renders
/// for cases where the attendee wants to reach out manually.
/// </summary>
public class RefundRejectedEmailParams : IEmailParameters, IDispatchLoggable
{
    public string TemplateName => EmailTemplateContract.TemplateNames.RefundRejected;
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
    public decimal RequestedTotal { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Customer-facing reason for declining the refund. MANDATORY — Validate() fails when empty.
    /// </summary>
    public string RejectionReason { get; set; } = string.Empty;

    public DateTime RejectedAt { get; set; }
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

    public RefundRejectedEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
        var lineItemsHtml = RefundLineItemsHtmlBuilder.BuildRequestedListHtml(LineItems, Currency);

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
            { "RequestedTotal", RequestedTotal.ToString("F2", CultureInfo.InvariantCulture) },
            { "Currency", Currency },
            { "RejectionReason", RejectionReason },
            { "RejectedAt", RejectedAt.ToString("MMMM dd, yyyy h:mm tt") },

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
        if (RequestedTotal <= 0) errors.Add("RequestedTotal must be greater than zero");
        if (string.IsNullOrWhiteSpace(RejectionReason)) errors.Add("RejectionReason is required");
        return errors.Count == 0;
    }

    public static RefundRejectedEmailParams Create(
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
        string rejectionReason,
        DateTime rejectedAt,
        string eventDetailsUrl)
    {
        var safeLines = lineItems ?? Array.Empty<RefundLineItemView>();
        return new RefundRejectedEmailParams
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
            Currency = currency ?? "USD",
            RejectionReason = rejectionReason ?? string.Empty,
            RejectedAt = rejectedAt,
            EventDetailsUrl = eventDetailsUrl ?? string.Empty
        };
    }
}

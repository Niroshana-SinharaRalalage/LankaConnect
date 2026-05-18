using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Parameters for the "your refund request is pending organizer review" email.
/// Template: template-refund-pending-review (header: "Refund Request Received").
///
/// Fires once at attendee-initiated request creation. Replaces the misleading
/// template-refund-requested reuse from 148.c (operator UAT E1/E2 — "Refund In Progress"
/// header gave the false impression Stripe was already running).
///
/// Lifecycle position: this is the FIRST email in the new approval workflow. The decision
/// email (<see cref="RefundDecisionEmailParams"/>) fires next on approve, OR the rejected
/// email (<see cref="RefundRejectedEmailParams"/>) fires on reject.
/// </summary>
public class RefundPendingReviewEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.RefundPendingReview;
    public string RecipientEmail => UserEmail;
    public string RecipientName => UserName;

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
    public string RequesterReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    #endregion

    #region Organizer Contacts (parity with RefundEmailParams)
    public bool HasOrganizerContact { get; set; }
    public string OrganizerContactName { get; set; } = string.Empty;
    public string OrganizerContactEmail { get; set; } = string.Empty;
    public string OrganizerContactPhone { get; set; } = string.Empty;
    public string OrganizerContactsHtml { get; set; } = string.Empty;
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";
    #endregion

    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    public RefundPendingReviewEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
        var hasReason = !string.IsNullOrWhiteSpace(RequesterReason);

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
            { "RequesterReason", RequesterReason },
            { "HasRequesterReason", hasReason },
            { "RequestedAt", RequestedAt.ToString("MMMM dd, yyyy h:mm tt") },

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
        return errors.Count == 0;
    }

    public static RefundPendingReviewEmailParams Create(
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
        string? requesterReason,
        DateTime requestedAt,
        string eventDetailsUrl)
    {
        return new RefundPendingReviewEmailParams
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
            LineItems = lineItems ?? Array.Empty<RefundLineItemView>(),
            RequestedTotal = lineItems?.Sum(li => li.RequestedAmount) ?? 0m,
            Currency = currency ?? "USD",
            RequesterReason = requesterReason ?? string.Empty,
            RequestedAt = requestedAt,
            EventDetailsUrl = eventDetailsUrl ?? string.Empty
        };
    }
}

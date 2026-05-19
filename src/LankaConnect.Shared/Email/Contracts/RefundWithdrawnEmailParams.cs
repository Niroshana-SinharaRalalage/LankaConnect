using System.Globalization;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.148.W4.D13: Parameters for the "you withdrew your refund request" email.
/// Template: template-refund-withdrawn (header: "Refund Request Withdrawn").
///
/// Fires once when an attendee uses the in-app Withdraw button on the pending-review
/// status banner (RefundRequestStatusBanner.tsx:106-114). Confirms to the attendee
/// that the request was withdrawn, the registration is back to Confirmed, and no
/// money moved. Per Q2 product decision, organizer is NOT notified — the queue item
/// just disappears from their dashboard.
///
/// Mirrors <see cref="RefundPendingReviewEmailParams"/> shape — same lifecycle
/// position (request-side, not money-side), so the email body uses the same
/// requested-items table without an "approved/declined" decision column.
/// </summary>
public class RefundWithdrawnEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.RefundWithdrawn;
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
    public DateTime WithdrawnAt { get; set; }
    #endregion

    #region Organizer Contacts (parity with other lifecycle params)
    public bool HasOrganizerContact { get; set; }
    public string OrganizerContactName { get; set; } = string.Empty;
    public string OrganizerContactEmail { get; set; } = string.Empty;
    public string OrganizerContactPhone { get; set; } = string.Empty;
    public string OrganizerContactsHtml { get; set; } = string.Empty;
    public string OrganizerContactHeader { get; set; } = "EVENT ORGANIZER";
    #endregion

    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    public RefundWithdrawnEmailParams WithOrganizerContacts(IReadOnlyList<OrganizerContactInfo> contacts)
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
            { "WithdrawnAt", WithdrawnAt.ToString("MMMM dd, yyyy h:mm tt") },

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

    public static RefundWithdrawnEmailParams Create(
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
        DateTime withdrawnAt,
        string eventDetailsUrl)
    {
        return new RefundWithdrawnEmailParams
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
            WithdrawnAt = withdrawnAt,
            EventDetailsUrl = eventDetailsUrl ?? string.Empty
        };
    }
}

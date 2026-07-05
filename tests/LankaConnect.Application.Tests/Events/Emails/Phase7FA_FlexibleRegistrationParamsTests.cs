using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Emails;

/// <summary>
/// Phase 7F-A: assert the three lifecycle EmailParams classes (cancellation broadcast,
/// reminder, attendees-added) carry the <see cref="EmailTemplateContract.FlexibleRegistration"/>
/// keys in their <c>ToDictionary()</c> output, true OR false, never omitted (architect rule
/// from Phase 7E.4 — Handlebars must evaluate <c>{{#if HasHeadCount}}</c> predictably and
/// <c>EmailTemplateValidationService</c> must pass at startup).
///
/// These tests are the thin contract guard. The full handler-side population path is
/// exercised by integration smokes against staging on a Mode-B2 event.
/// </summary>
public class Phase7FA_FlexibleRegistrationParamsTests
{
    private static void AssertFlexibleKeysEmitted(System.Collections.Generic.Dictionary<string, object> dict)
    {
        // Every Flexible* boolean must be present (true OR false, never missing).
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HasDetailedAttendees);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HasHeadCount);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HasHeadCountBreakdown);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HasTierBreakdown);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HeadCountTotal);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.HeadCountBreakdownLine);
        dict.Should().ContainKey(EmailTemplateContract.FlexibleRegistration.TierBreakdownLine);
        dict.Should().ContainKey("LeadAttendeeName");
    }

    [Fact]
    public void EventCancellationEmailParams_ToDictionary_EmitsFlexibleKeys_DefaultsModeA()
    {
        var p = EventCancellationEmailParams.Create(
            userId: System.Guid.NewGuid(),
            userName: "User",
            userEmail: "u@example.com",
            eventId: System.Guid.NewGuid(),
            eventTitle: "T",
            eventStartDate: System.DateTime.UtcNow.AddDays(7),
            timeZoneId: null,
            eventLocation: "L",
            cancellationReason: "R",
            cancelledAt: System.DateTime.UtcNow,
            organizerName: "O",
            refundsWillBeProcessed: false,
            refundMessage: "M");

        var dict = p.ToDictionary();

        AssertFlexibleKeysEmitted(dict);
        dict[EmailTemplateContract.FlexibleRegistration.HasDetailedAttendees].Should().Be(false);
        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCount].Should().Be(false);
    }

    [Fact]
    public void EventCancellationEmailParams_ToDictionary_EmitsFlexibleKeys_ModeBPopulated()
    {
        var p = EventCancellationEmailParams.Create(
            userId: System.Guid.NewGuid(), userName: "U", userEmail: "u@e.com",
            eventId: System.Guid.NewGuid(), eventTitle: "T",
            eventStartDate: System.DateTime.UtcNow.AddDays(1), timeZoneId: null,
            eventLocation: "L", cancellationReason: "R",
            cancelledAt: System.DateTime.UtcNow,
            organizerName: "O", refundsWillBeProcessed: false, refundMessage: "M");

        p.HasHeadCount = true;
        p.HasHeadCountBreakdown = true;
        p.HeadCountTotal = "5";
        p.HeadCountBreakdownLine = "3 adults · 2 children";
        p.LeadAttendeeName = "Lead";

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCount].Should().Be(true);
        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCountBreakdown].Should().Be(true);
        dict[EmailTemplateContract.FlexibleRegistration.HeadCountTotal].Should().Be("5");
        dict[EmailTemplateContract.FlexibleRegistration.HeadCountBreakdownLine].Should().Be("3 adults · 2 children");
        dict["LeadAttendeeName"].Should().Be("Lead");
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_EmitsFlexibleKeys_DefaultsModeA()
    {
        var p = EventReminderEmailParams.Create(
            eventId: System.Guid.NewGuid(),
            registrationId: System.Guid.NewGuid(),
            attendeeName: "A", attendeeEmail: "a@e.com",
            eventTitle: "T",
            eventStartDate: System.DateTime.UtcNow.AddDays(1),
            eventStartTime: "10:00 AM",
            eventLocation: "L",
            quantity: 2, hoursUntilEvent: 24, reminderTimeframe: "tomorrow",
            reminderMessage: "Reminder", eventDetailsUrl: "https://e/d");

        var dict = p.ToDictionary();

        AssertFlexibleKeysEmitted(dict);
        dict[EmailTemplateContract.FlexibleRegistration.HasDetailedAttendees].Should().Be(false);
        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCount].Should().Be(false);
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_EmitsFlexibleKeys_ModeBPopulated()
    {
        var p = EventReminderEmailParams.Create(
            eventId: System.Guid.NewGuid(), registrationId: System.Guid.NewGuid(),
            attendeeName: "A", attendeeEmail: "a@e.com", eventTitle: "T",
            eventStartDate: System.DateTime.UtcNow.AddDays(1), eventStartTime: "10",
            eventLocation: "L", quantity: 5, hoursUntilEvent: 24,
            reminderTimeframe: "tomorrow", reminderMessage: "R", eventDetailsUrl: "https://e");

        p.HasHeadCount = true;
        p.HasTierBreakdown = true;
        p.HeadCountTotal = "5";
        p.TierBreakdownLine = "VIP × 2, General × 3";
        p.LeadAttendeeName = "B Lead";

        var dict = p.ToDictionary();

        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCount].Should().Be(true);
        dict[EmailTemplateContract.FlexibleRegistration.HasTierBreakdown].Should().Be(true);
        dict[EmailTemplateContract.FlexibleRegistration.HeadCountTotal].Should().Be("5");
        dict[EmailTemplateContract.FlexibleRegistration.TierBreakdownLine].Should().Be("VIP × 2, General × 3");
        dict["LeadAttendeeName"].Should().Be("B Lead");
    }

    [Fact]
    public void AttendeesAddedEmailParams_ToDictionary_EmitsFlexibleKeys_DefaultsModeA()
    {
        var p = AttendeesAddedEmailParams.Create(
            userId: System.Guid.NewGuid(), registrationId: System.Guid.NewGuid(),
            eventId: System.Guid.NewGuid(),
            userName: "U", userEmail: "u@e.com",
            eventTitle: "T",
            eventStartDate: System.DateTime.UtcNow.AddDays(1), timeZoneId: null,
            eventLocation: "L",
            previousCount: 1, addedCount: 1, newTotalCount: 2,
            additionalAmount: 10m, totalPaid: 20m,
            newAttendees: "X", newAttendeesHtml: "<p>X</p>",
            allAttendees: "X, Y", allAttendeesHtml: "<p>X, Y</p>",
            eventDetailsUrl: "https://e", ticketUrl: null, ticketCode: null);

        var dict = p.ToDictionary();

        AssertFlexibleKeysEmitted(dict);
        dict[EmailTemplateContract.FlexibleRegistration.HasDetailedAttendees].Should().Be(false);
        dict[EmailTemplateContract.FlexibleRegistration.HasHeadCount].Should().Be(false);
    }
}

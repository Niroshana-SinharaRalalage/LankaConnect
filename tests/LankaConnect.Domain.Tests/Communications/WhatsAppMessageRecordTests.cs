using FluentAssertions;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications;

/// <summary>
/// TDD tests for WhatsAppMessageRecord entity.
/// Tests creation, status transitions, retry logic, and scheduling.
/// </summary>
public class WhatsAppMessageRecordTests
{
    private const string FromPhone = "+14155551234";
    private const string ToPhone = "+94771234567";

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Record_With_Draft_Status()
    {
        var record = WhatsAppMessageRecord.Create(
            FromPhone, ToPhone, WhatsAppMessageType.Template,
            templateName: "event_confirmation",
            parameters: new Dictionary<string, string> { { "UserName", "John" } },
            language: "en");

        record.Should().NotBeNull();
        record.FromPhoneNumber.Should().Be(FromPhone);
        record.ToPhoneNumber.Should().Be(ToPhone);
        record.MessageType.Should().Be(WhatsAppMessageType.Template);
        record.Status.Should().Be(WhatsAppMessageStatus.Draft);
        record.TemplateName.Should().Be("event_confirmation");
        record.TemplateParameters.Should().Contain("UserName");
        record.TemplateParameters.Should().Contain("John");
        record.Language.Should().Be("en");
        record.RetryCount.Should().Be(0);
        record.MaxRetries.Should().Be(3);
        record.SentAt.Should().BeNull();
        record.DeliveredAt.Should().BeNull();
        record.ReadAt.Should().BeNull();
        record.FailedAt.Should().BeNull();
        record.ErrorMessage.Should().BeNull();
        record.AcsMessageId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullParameters_Should_Set_TemplateParameters_To_Null()
    {
        var record = WhatsAppMessageRecord.Create(
            FromPhone, ToPhone, WhatsAppMessageType.Text,
            templateName: null, parameters: null);

        record.TemplateParameters.Should().BeNull();
    }

    [Fact]
    public void Create_With_Optional_ForeignKeys_Should_Set_All_Ids()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var newsletterId = Guid.NewGuid();

        var record = WhatsAppMessageRecord.Create(
            FromPhone, ToPhone, WhatsAppMessageType.Template,
            templateName: "test", parameters: null,
            userId: userId, eventId: eventId,
            registrationId: registrationId, newsletterId: newsletterId);

        record.UserId.Should().Be(userId);
        record.EventId.Should().Be(eventId);
        record.RegistrationId.Should().Be(registrationId);
        record.NewsletterId.Should().Be(newsletterId);
    }

    [Fact]
    public void Create_With_Default_Language_Should_Be_English()
    {
        var record = WhatsAppMessageRecord.Create(
            FromPhone, ToPhone, WhatsAppMessageType.Text,
            templateName: null, parameters: null);

        record.Language.Should().Be("en");
    }

    #endregion

    #region MarkAsSent Tests

    [Fact]
    public void MarkAsSent_Should_Set_Status_To_Sent_And_Record_AcsMessageId()
    {
        var record = CreateDraftRecord();
        var acsId = "acs-msg-12345";

        record.MarkAsSent(acsId);

        record.Status.Should().Be(WhatsAppMessageStatus.Sent);
        record.SentAt.Should().NotBeNull();
        record.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        record.AcsMessageId.Should().Be(acsId);
    }

    #endregion

    #region MarkAsDelivered Tests

    [Fact]
    public void MarkAsDelivered_Should_Set_Status_To_Delivered_And_Record_Timestamp()
    {
        var record = CreateDraftRecord();
        record.MarkAsSent("acs-123");

        record.MarkAsDelivered();

        record.Status.Should().Be(WhatsAppMessageStatus.Delivered);
        record.DeliveredAt.Should().NotBeNull();
        record.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        record.IsDelivered.Should().BeTrue();
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public void MarkAsRead_Should_Set_Status_To_Read_And_Record_Timestamp()
    {
        var record = CreateDraftRecord();
        record.MarkAsSent("acs-123");
        record.MarkAsDelivered();

        record.MarkAsRead();

        record.Status.Should().Be(WhatsAppMessageStatus.Read);
        record.ReadAt.Should().NotBeNull();
        record.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        record.IsRead.Should().BeTrue();
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_Should_Set_Status_To_Failed_And_Record_Error()
    {
        var record = CreateDraftRecord();
        var errorMsg = "Connection timeout to WhatsApp Business API";

        record.MarkAsFailed(errorMsg);

        record.Status.Should().Be(WhatsAppMessageStatus.Failed);
        record.FailedAt.Should().NotBeNull();
        record.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        record.ErrorMessage.Should().Be(errorMsg);
        record.IsFailed.Should().BeTrue();
    }

    #endregion

    #region IncrementRetry Tests

    [Fact]
    public void IncrementRetry_Should_Increment_RetryCount_And_Reset_To_Sending()
    {
        var record = CreateDraftRecord();
        record.MarkAsFailed("Temporary failure");

        record.IncrementRetry();

        record.RetryCount.Should().Be(1);
        record.Status.Should().Be(WhatsAppMessageStatus.Sending);
        record.FailedAt.Should().BeNull();
        record.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_Called_Multiple_Times_Should_Accumulate_Count()
    {
        var record = CreateDraftRecord();

        record.IncrementRetry();
        record.IncrementRetry();
        record.IncrementRetry();

        record.RetryCount.Should().Be(3);
    }

    #endregion

    #region CanRetry Tests

    [Fact]
    public void CanRetry_When_Failed_And_Under_MaxRetries_Should_Return_True()
    {
        var record = CreateDraftRecord();
        record.MarkAsFailed("Temporary failure");

        record.CanRetry.Should().BeTrue();
        record.RetryCount.Should().BeLessThan(record.MaxRetries);
    }

    [Fact]
    public void CanRetry_When_At_MaxRetries_Should_Return_False()
    {
        var record = CreateDraftRecord();
        // Exhaust all retries (MaxRetries = 3)
        record.IncrementRetry(); // 1
        record.IncrementRetry(); // 2
        record.IncrementRetry(); // 3
        record.MarkAsFailed("Final failure");

        record.CanRetry.Should().BeFalse();
        record.RetryCount.Should().Be(record.MaxRetries);
    }

    [Fact]
    public void CanRetry_When_Not_Failed_Should_Return_False()
    {
        var record = CreateDraftRecord();
        record.MarkAsSent("acs-123");

        record.CanRetry.Should().BeFalse();
    }

    #endregion

    #region ScheduleFor Tests

    [Fact]
    public void ScheduleFor_Should_Set_Status_To_Scheduled_And_Record_Time()
    {
        var record = CreateDraftRecord();
        var scheduledTime = DateTime.UtcNow.AddHours(2);

        record.ScheduleFor(scheduledTime);

        record.Status.Should().Be(WhatsAppMessageStatus.Scheduled);
        record.ScheduledFor.Should().Be(scheduledTime);
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void IsRead_Should_Return_False_For_New_Record()
    {
        var record = CreateDraftRecord();
        record.IsRead.Should().BeFalse();
    }

    [Fact]
    public void IsFailed_Should_Return_False_For_New_Record()
    {
        var record = CreateDraftRecord();
        record.IsFailed.Should().BeFalse();
    }

    [Fact]
    public void IsDelivered_Should_Return_False_For_New_Record()
    {
        var record = CreateDraftRecord();
        record.IsDelivered.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static WhatsAppMessageRecord CreateDraftRecord()
    {
        return WhatsAppMessageRecord.Create(
            FromPhone, ToPhone, WhatsAppMessageType.Template,
            templateName: "test_template",
            parameters: new Dictionary<string, string> { { "Name", "Test" } });
    }

    #endregion
}

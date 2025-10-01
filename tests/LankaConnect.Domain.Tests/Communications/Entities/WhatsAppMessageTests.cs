using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications.Entities;

/// <summary>
/// Comprehensive TDD tests for WhatsApp Business API integration with cultural intelligence
/// Tests Buddhist/Hindu calendar awareness, diaspora targeting, and cultural appropriateness validation
/// </summary>
public class WhatsAppMessageTests
{
    #region Create Method Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567", "+447911123456" };
        var content = "Welcome to our cultural community event!";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FromPhoneNumber.Should().Be(fromPhone);
        result.Value.ToPhoneNumbers.Should().BeEquivalentTo(toPhones);
        result.Value.MessageContent.Should().Be(content);
        result.Value.Status.Should().Be(WhatsAppMessageStatus.Draft);
        result.Value.Language.Should().Be("en");
    }

    [Fact]
    public void Create_WithEmptyFromPhoneNumber_ShouldFail()
    {
        // Arrange
        var fromPhone = "";
        var toPhones = new[] { "+15551234567" };
        var content = "Test message";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("From phone number is required");
    }

    [Fact]
    public void Create_WithNoRecipients_ShouldFail()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new string[] { };
        var content = "Test message";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("At least one recipient is required");
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldFail()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var content = "";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Message content is required");
    }

    [Fact]
    public void Create_WithNullCulturalContext_ShouldFail()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var content = "Test message";
        WhatsAppCulturalContext? culturalContext = null;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cultural context is required");
    }

    #endregion

    #region CreateFromTemplate Method Tests

    [Fact]
    public void CreateFromTemplate_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var templateName = "vesak_greeting";
        var templateParams = new Dictionary<string, object>
        {
            { "recipientName", "Samantha" },
            { "festivalDate", "May 16, 2024" }
        };
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));

        // Act
        var result = WhatsAppMessage.CreateFromTemplate(fromPhone, toPhones, templateName, templateParams, culturalContext, "si");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MessageType.Should().Be(WhatsAppMessageType.Template);
        result.Value.TemplateName.Should().Be(templateName);
        result.Value.TemplateParameters.Should().BeEquivalentTo(templateParams);
        result.Value.Language.Should().Be("si");
        result.Value.RequiresCulturalValidation.Should().BeTrue();
    }

    [Fact]
    public void CreateFromTemplate_WithEmptyTemplateName_ShouldFail()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var templateName = "";
        var templateParams = new Dictionary<string, object>();
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.CreateFromTemplate(fromPhone, toPhones, templateName, templateParams, culturalContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Template name is required");
    }

    #endregion

    #region Cultural Intelligence Tests

    [Fact]
    public void Create_WithBuddhistFestivalContext_ShouldRequireCulturalValidation()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var content = "May this Vesak Day bring you peace and enlightenment";
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.EventNotification, culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresCulturalValidation.Should().BeTrue();
        result.Value.CulturalContext.HasReligiousContent.Should().BeTrue();
        result.Value.CulturalContext.IsFestivalRelated.Should().BeTrue();
        result.Value.CulturalContext.PrimaryReligion.Should().Be("Buddhism");
        result.Value.CulturalContext.RequiresBuddhistCalendarAwareness.Should().BeTrue();
    }

    [Fact]
    public void Create_WithHinduFestivalContext_ShouldRequireCulturalValidation()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var content = "Wishing you a blessed Deepavali filled with light and prosperity";
        var culturalContext = WhatsAppCulturalContext.ForHinduFestival("Deepavali", new DateTime(2024, 11, 1));

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.FestivalGreeting, culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresCulturalValidation.Should().BeTrue();
        result.Value.CulturalContext.HasReligiousContent.Should().BeTrue();
        result.Value.CulturalContext.PrimaryReligion.Should().Be("Hinduism");
        result.Value.CulturalContext.RequiresHinduCalendarAwareness.Should().BeTrue();
    }

    [Fact]
    public void Create_WithBroadcastMessageType_ShouldRequireCulturalValidation()
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567", "+447911123456", "+16471234567" };
        var content = "Important community announcement for all diaspora members";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Broadcast, culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresCulturalValidation.Should().BeTrue();
    }

    #endregion

    #region Cultural Appropriateness Scoring Tests

    [Fact]
    public void SetCulturalAppropriatnessScore_WithValidScore_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        var score = 0.85;

        // Act
        var result = message.SetCulturalAppropriatnessScore(score);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.CulturalAppropriatnessScore.Should().Be(score);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void SetCulturalAppropriatnessScore_WithInvalidScore_ShouldFail(double invalidScore)
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();

        // Act
        var result = message.SetCulturalAppropriatnessScore(invalidScore);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cultural appropriateness score must be between 0 and 1");
    }

    [Fact]
    public void Send_WithCulturalValidationRequiredAndLowScore_ShouldFail()
    {
        // Arrange
        var culturalContext = CulturalContext.ForBuddhistFestival("Vesak", DateTime.UtcNow.AddDays(7));
        var message = WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567" },
            "Religious message content",
            WhatsAppMessageType.EventNotification,
            culturalContext).Value;
        
        message.SetCulturalAppropriatnessScore(0.6); // Below threshold of 0.7

        // Act
        var result = message.Send();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Message failed cultural appropriateness validation");
    }

    [Fact]
    public void Send_WithCulturalValidationRequiredAndHighScore_ShouldSucceed()
    {
        // Arrange
        var culturalContext = CulturalContext.ForBuddhistFestival("Vesak", DateTime.UtcNow.AddDays(7));
        var message = WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567" },
            "May this Vesak bring you inner peace and wisdom",
            WhatsAppMessageType.EventNotification,
            culturalContext).Value;
        
        message.SetCulturalAppropriatnessScore(0.9); // Above threshold

        // Act
        var result = message.Send();

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(WhatsAppMessageStatus.Sending);
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Diaspora Region and Cultural Metadata Tests

    [Fact]
    public void SetDiasporaRegion_WithValidData_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        var region = "Bay Area";
        var timeZone = "America/Los_Angeles";

        // Act
        var result = message.SetDiasporaRegion(region, timeZone);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.DiasporaRegion.Should().Be(region);
        message.TimeZone.Should().Be(timeZone);
    }

    [Fact]
    public void AddCulturalMetadata_WithValidData_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        var key = "festival_type";
        var value = "religious_observance";

        // Act
        var result = message.AddCulturalMetadata(key, value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.CulturalMetadata[key].Should().Be(value);
    }

    [Fact]
    public void AddCulturalMetadata_WithEmptyKey_ShouldFail()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();

        // Act
        var result = message.AddCulturalMetadata("", "value");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cultural metadata key is required");
    }

    #endregion

    #region Scheduling and Timing Tests

    [Fact]
    public void ScheduleForOptimalCulturalTiming_WithFutureTime_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        var scheduledTime = DateTime.UtcNow.AddHours(2);

        // Act
        var result = message.ScheduleForOptimalCulturalTiming(scheduledTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.ScheduledFor.Should().Be(scheduledTime);
        message.Status.Should().Be(WhatsAppMessageStatus.Scheduled);
        message.IsScheduled.Should().BeTrue();
    }

    [Fact]
    public void ScheduleForOptimalCulturalTiming_WithPastTime_ShouldFail()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        var pastTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = message.ScheduleForOptimalCulturalTiming(pastTime);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot schedule message in the past");
    }

    #endregion

    #region Delivery Status Tests

    [Fact]
    public void MarkAsDelivered_WithValidState_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        message.Send();

        // Act
        var result = message.MarkAsDelivered();

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(WhatsAppMessageStatus.Delivered);
        message.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.IsDelivered.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_WithDeliveredMessage_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        message.Send();
        message.MarkAsDelivered();

        // Act
        var result = message.MarkAsRead();

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsFailed_WithErrorMessage_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        message.Send();
        var errorMessage = "WhatsApp API rate limit exceeded";

        // Act
        var result = message.MarkAsFailed(errorMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(WhatsAppMessageStatus.Failed);
        message.ErrorMessage.Should().Be(errorMessage);
        message.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.IsFailed.Should().BeTrue();
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public void Retry_WithFailedMessageUnderMaxRetries_ShouldSucceed()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        message.Send();
        message.MarkAsFailed("Temporary network error");
        var initialRetryCount = message.RetryCount;

        // Act
        var result = message.Retry();

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(WhatsAppMessageStatus.Draft);
        message.RetryCount.Should().Be(initialRetryCount + 1);
        message.ErrorMessage.Should().BeNull();
        message.FailedAt.Should().BeNull();
        message.CanRetry.Should().BeTrue();
    }

    [Fact]
    public void Retry_WithMaxRetriesReached_ShouldFail()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();
        
        // Simulate reaching max retries
        for (int i = 0; i < 3; i++)
        {
            message.Send();
            message.MarkAsFailed($"Error attempt {i + 1}");
            if (i < 2) message.Retry();
        }

        // Act
        var result = message.Retry();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Message cannot be retried - either max retries reached or not in failed state");
        message.CanRetry.Should().BeFalse();
    }

    #endregion

    #region Cultural Context Tests

    [Fact]
    public void CulturalContext_ForBuddhistFestival_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var context = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));

        // Assert
        context.HasReligiousContent.Should().BeTrue();
        context.IsFestivalRelated.Should().BeTrue();
        context.PrimaryReligion.Should().Be("Buddhism");
        context.FestivalName.Should().Be("Vesak");
        context.RequiresBuddhistCalendarAwareness.Should().BeTrue();
        context.RequiresHinduCalendarAwareness.Should().BeFalse();
    }

    [Fact]
    public void CulturalContext_ForHinduFestival_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var context = WhatsAppCulturalContext.ForHinduFestival("Deepavali", new DateTime(2024, 11, 1));

        // Assert
        context.HasReligiousContent.Should().BeTrue();
        context.IsFestivalRelated.Should().BeTrue();
        context.PrimaryReligion.Should().Be("Hinduism");
        context.FestivalName.Should().Be("Deepavali");
        context.RequiresHinduCalendarAwareness.Should().BeTrue();
        context.RequiresBuddhistCalendarAwareness.Should().BeFalse();
    }

    [Fact]
    public void CulturalContext_None_ShouldHaveNoSpecialRequirements()
    {
        // Arrange & Act
        var context = WhatsAppCulturalContext.None;

        // Assert
        context.HasReligiousContent.Should().BeFalse();
        context.IsFestivalRelated.Should().BeFalse();
        context.PrimaryReligion.Should().BeNull();
        context.RequiresBuddhistCalendarAwareness.Should().BeFalse();
        context.RequiresHinduCalendarAwareness.Should().BeFalse();
    }

    #endregion

    #region Multi-Language Support Tests

    [Theory]
    [InlineData("en", "English")]
    [InlineData("si", "Sinhala")]
    [InlineData("ta", "Tamil")]
    public void Create_WithDifferentLanguages_ShouldSetCorrectLanguage(string languageCode, string languageName)
    {
        // Arrange
        var fromPhone = "+94771234567";
        var toPhones = new[] { "+15551234567" };
        var content = $"Message in {languageName}";
        var culturalContext = WhatsAppCulturalContext.None;

        // Act
        var result = WhatsAppMessage.Create(fromPhone, toPhones, content, WhatsAppMessageType.Text, culturalContext, languageCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be(languageCode);
    }

    #endregion

    #region Business Logic Properties Tests

    [Fact]
    public void BusinessProperties_ShouldReturnCorrectStates()
    {
        // Arrange
        var message = CreateValidWhatsAppMessage();

        // Act & Assert - Initial state
        message.IsRead.Should().BeFalse();
        message.IsDelivered.Should().BeFalse();
        message.IsFailed.Should().BeFalse();
        message.IsScheduled.Should().BeFalse();

        // Schedule message
        message.ScheduleForOptimalCulturalTiming(DateTime.UtcNow.AddHours(1));
        message.IsScheduled.Should().BeTrue();

        // Send and deliver
        message.Send();
        message.MarkAsDelivered();
        message.IsDelivered.Should().BeTrue();

        // Mark as read
        message.MarkAsRead();
        message.IsRead.Should().BeTrue();
    }

    #endregion

    #region Integration Scenario Tests - Real World Cultural Use Cases

    [Fact]
    public void VesakDayGreeting_DiasporaCommunityBroadcast_ShouldHaveCorrectProperties()
    {
        // Arrange - Vesak Day greeting for Bay Area Sri Lankan diaspora
        var fromPhone = "+94771234567";
        var bayAreaRecipients = new[] { "+15551234567", "+16501234567", "+14081234567" };
        var vesakGreeting = "May this sacred Vesak Day bring you inner peace, wisdom, and compassion. May the Buddha's teachings illuminate your path to enlightenment.";
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));

        // Act
        var result = WhatsAppMessage.Create(fromPhone, bayAreaRecipients, vesakGreeting, WhatsAppMessageType.FestivalGreeting, culturalContext, "si");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var message = result.Value;
        
        message.RequiresCulturalValidation.Should().BeTrue();
        message.Language.Should().Be("si");
        message.CulturalContext.RequiresBuddhistCalendarAwareness.Should().BeTrue();
        message.ToPhoneNumbers.Should().HaveCount(3);
        
        // Should be ready for diaspora region setting
        message.SetDiasporaRegion("Bay Area", "America/Los_Angeles").IsSuccess.Should().BeTrue();
        
        // Should be ready for cultural metadata
        message.AddCulturalMetadata("festival_significance", "buddha_birth_enlightenment_death").IsSuccess.Should().BeTrue();
        message.AddCulturalMetadata("religious_observance", "meditation_dana_precepts").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DeepavaliCelebration_MultiRegionDiaspora_ShouldSupportGlobalTiming()
    {
        // Arrange - Deepavali greeting for global diaspora communities
        var fromPhone = "+94771234567";
        var globalRecipients = new[] { 
            "+15551234567", // US West Coast
            "+16471234567", // Toronto
            "+447911123456"  // London
        };
        var deepavaliMessage = "Wishing you and your family a very Happy Deepavali! May the festival of lights bring prosperity, happiness, and success to your home.";
        var culturalContext = WhatsAppCulturalContext.ForHinduFestival("Deepavali", new DateTime(2024, 11, 1));

        // Act
        var result = WhatsAppMessage.Create(fromPhone, globalRecipients, deepavaliMessage, WhatsAppMessageType.FestivalGreeting, culturalContext, "ta");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var message = result.Value;
        
        message.RequiresCulturalValidation.Should().BeTrue();
        message.Language.Should().Be("ta");
        message.CulturalContext.RequiresHinduCalendarAwareness.Should().BeTrue();
        
        // Should support multiple cultural metadata for Hindu festival
        message.AddCulturalMetadata("festival_type", "lights_victory_good_over_evil").IsSuccess.Should().BeTrue();
        message.AddCulturalMetadata("traditions", "oil_lamps_rangoli_sweets").IsSuccess.Should().BeTrue();
        message.AddCulturalMetadata("timing_preference", "evening_lakshmi_puja").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PoyaDayReminder_BuddhistObservance_ShouldHandleQuietPeriods()
    {
        // Arrange - Poyaday observance reminder with timing sensitivity
        var fromPhone = "+94771234567";
        var recipients = new[] { "+94711234567", "+94721234567" }; // Sri Lankan numbers
        var poyaReminder = "Today is Poyaday. A time for meditation, dana, and observing the eight precepts. May you find peace in your practice.";
        var culturalContext = new WhatsAppCulturalContext(
            hasReligiousContent: true,
            isFestivalRelated: false,
            primaryReligion: "Buddhism",
            requiresBuddhistCalendarAwareness: true);

        // Act
        var result = WhatsAppMessage.Create(fromPhone, recipients, poyaReminder, WhatsAppMessageType.Reminder, culturalContext, "si");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var message = result.Value;
        
        message.RequiresCulturalValidation.Should().BeTrue();
        message.CulturalContext.RequiresBuddhistCalendarAwareness.Should().BeTrue();
        
        // Should support scheduling around Buddhist quiet periods
        var schedulingResult = message.ScheduleForOptimalCulturalTiming(DateTime.UtcNow.AddHours(4));
        schedulingResult.IsSuccess.Should().BeTrue();
        
        // Should support cultural timing metadata
        message.AddCulturalMetadata("observance_type", "buddhist_uposatha").IsSuccess.Should().BeTrue();
        message.AddCulturalMetadata("timing_sensitivity", "avoid_evening_meditation_hours").IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static WhatsAppMessage CreateValidWhatsAppMessage()
    {
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567" },
            "Test message content",
            WhatsAppMessageType.Text,
            CulturalContext.None).Value;
    }

    #endregion
}
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications;

/// <summary>
/// Validation tests for Cultural Intelligence error resolution
/// Tests that Priority 1 and Priority 2 errors have been fixed
/// </summary>
public class CulturalIntelligenceValidationTest
{
    [Fact]
    public void CulturalAppropriateness_ShouldHaveValueProperty()
    {
        // Arrange
        var culturalAppropriateness = new CulturalAppropriateness(
            0.8, 
            AppropriatenessLevel.Appropriate, 
            "Test context");

        // Act & Assert
        culturalAppropriateness.Value.Should().Be(0.8);
        culturalAppropriateness.Level.Should().Be(AppropriatenessLevel.Appropriate);
        culturalAppropriateness.CulturalContext.Should().Be("Test context");
        culturalAppropriateness.IsAppropriate.Should().BeTrue();
        culturalAppropriateness.IsHighlyAppropriate.Should().BeFalse();
    }

    [Fact]
    public void CulturalAppropriateness_ShouldCreateStaticInstances()
    {
        // Act
        var highlyAppropriate = CulturalAppropriateness.HighlyAppropriate("High context");
        var appropriate = CulturalAppropriateness.Appropriate("Good context");
        var mildConcern = CulturalAppropriateness.MildConcern("Mild concern context");
        var inappropriate = CulturalAppropriateness.Inappropriate("Bad context");

        // Assert
        highlyAppropriate.Value.Should().Be(0.9);
        highlyAppropriate.IsHighlyAppropriate.Should().BeTrue();
        
        appropriate.Value.Should().Be(0.7);
        appropriate.IsAppropriate.Should().BeTrue();
        
        mildConcern.Value.Should().Be(0.6);
        mildConcern.IsAppropriate.Should().BeTrue();
        
        inappropriate.Value.Should().Be(0.1);
        inappropriate.IsInappropriate.Should().BeTrue();
    }

    [Fact]
    public void CulturalEvent_ShouldHaveBuddhistCalendarProperties()
    {
        // Arrange & Act
        var culturalEvent = new CulturalEvent(
            date: DateTime.Now,
            englishName: "Vesak Poya Day",
            nativeName: "වෙසක් පෝය දිනය",
            secondaryName: "Buddha Day",
            primaryCommunity: LankaConnect.Domain.Common.Enums.CulturalCommunity.SriLankanBuddhist,
            isMajorPoya: true,
            isPoyaday: true,
            eventType: LankaConnect.Domain.Common.Enums.CulturalEventType.Religious,
            isReligiousObservance: true
        );

        // Assert
        culturalEvent.IsMajorPoya.Should().BeTrue();
        culturalEvent.IsPoyaday.Should().BeTrue();
        culturalEvent.IsReligiousObservance.Should().BeTrue();
        culturalEvent.EnglishName.Should().Be("Vesak Poya Day");
        culturalEvent.PrimaryCommunity.Should().Be(LankaConnect.Domain.Common.Enums.CulturalCommunity.SriLankanBuddhist);
    }

    [Fact]
    public void Error_TypeShouldWork()
    {
        // Arrange & Act
        var error = new Error("TestCode", "Test message");
        var culturalError = Error.CulturalConflict;
        var calendarError = Error.CalendarError;

        // Assert
        error.Code.Should().Be("TestCode");
        error.Message.Should().Be("Test message");
        error.HasError.Should().BeTrue();
        error.IsEmpty.Should().BeFalse();
        
        culturalError.Code.Should().Be("Error.CulturalConflict");
        calendarError.Code.Should().Be("Error.CalendarError");
    }

    [Theory]
    [InlineData(0.9, true, true, false)]
    [InlineData(0.7, true, false, false)]
    [InlineData(0.5, false, false, false)]
    [InlineData(0.2, false, false, true)]
    public void CulturalAppropriateness_ShouldCalculateAppropriatenessCorrectly(
        double value, bool expectedAppropriate, bool expectedHighlyAppropriate, bool expectedInappropriate)
    {
        // Arrange & Act
        var appropriateness = new CulturalAppropriateness(
            value, 
            AppropriatenessLevel.Appropriate);

        // Assert
        appropriateness.IsAppropriate.Should().Be(expectedAppropriate);
        appropriateness.IsHighlyAppropriate.Should().Be(expectedHighlyAppropriate);
        appropriateness.IsInappropriate.Should().Be(expectedInappropriate);
    }
}
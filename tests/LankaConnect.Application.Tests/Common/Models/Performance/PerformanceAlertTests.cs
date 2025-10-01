using FluentAssertions;
using LankaConnect.Application.Common.Models.Performance;

namespace LankaConnect.Application.Tests.Common.Models.Performance;

public class PerformanceAlertTests
{
    [Fact]
    public void PerformanceAlert_Creation_ShouldInitializeWithValidProperties()
    {
        // Arrange
        var alertType = "CPU_HIGH";
        var message = "High CPU usage detected";
        var severity = "High";
        
        // Act
        var alert = new PerformanceAlert(alertType, message, severity);
        
        // Assert
        alert.Should().NotBeNull();
        alert.AlertType.Should().Be(alertType);
        alert.Message.Should().Be(message);
        alert.Severity.Should().Be(severity);
        alert.Id.Should().NotBeEmpty();
        alert.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PerformanceAlert_WithCulturalContext_ShouldStoreCulturalInformation()
    {
        // Arrange
        var culturalEvent = "Vesak_Day";
        var culturalRegion = "Sri_Lanka";
        
        // Act
        var alert = new PerformanceAlert("CULTURAL_LOAD", "Cultural event load spike", "Medium")
        {
            CulturalEventContext = culturalEvent,
            AffectedRegion = culturalRegion
        };
        
        // Assert
        alert.CulturalEventContext.Should().Be(culturalEvent);
        alert.AffectedRegion.Should().Be(culturalRegion);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PerformanceAlert_WithInvalidAlertType_ShouldThrowException(string invalidAlertType)
    {
        // Arrange & Act & Assert
        var act = () => new PerformanceAlert(invalidAlertType, "Test message", "High");
        act.Should().Throw<ArgumentException>();
    }
}
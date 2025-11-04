using FluentAssertions;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Analytics.DomainEvents;
using Xunit;

namespace LankaConnect.Domain.Tests.Analytics;

/// <summary>
/// TDD RED Phase: EventAnalytics aggregate tests
/// Tests for analytics tracking functionality
/// </summary>
public class EventAnalyticsTests
{
    #region Creation Tests

    [Fact]
    public void Create_ShouldInitializeWithZeroCounts()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var analytics = EventAnalytics.Create(eventId);

        // Assert
        analytics.Should().NotBeNull();
        analytics.EventId.Should().Be(eventId);
        analytics.TotalViews.Should().Be(0);
        analytics.UniqueViewers.Should().Be(0);
        analytics.RegistrationCount.Should().Be(0);
        analytics.ConversionRate.Should().Be(0);
        analytics.LastViewedAt.Should().BeNull();
        analytics.Id.Should().NotBeEmpty();
        analytics.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        // Arrange
        var emptyEventId = Guid.Empty;

        // Act
        var act = () => EventAnalytics.Create(emptyEventId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*eventId*");
    }

    #endregion

    #region RecordView Tests

    [Fact]
    public void RecordView_ShouldIncrementTotalViews()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        analytics.RecordView(Guid.NewGuid(), "192.168.1.1");

        // Assert
        analytics.TotalViews.Should().Be(1);
        analytics.LastViewedAt.Should().NotBeNull();
        analytics.LastViewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analytics.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordView_MultipleTimes_ShouldIncrementCount()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        analytics.RecordView(Guid.NewGuid(), "192.168.1.1");
        analytics.RecordView(Guid.NewGuid(), "192.168.1.2");
        analytics.RecordView(Guid.NewGuid(), "192.168.1.3");

        // Assert
        analytics.TotalViews.Should().Be(3);
    }

    [Fact]
    public void RecordView_WithNullUserId_ShouldAcceptAnonymousView()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        analytics.RecordView(null, "192.168.1.1");

        // Assert
        analytics.TotalViews.Should().Be(1);
    }

    [Fact]
    public void RecordView_WithNullIpAddress_ShouldFail()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        var act = () => analytics.RecordView(Guid.NewGuid(), null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ipAddress*");
    }

    [Fact]
    public void RecordView_WithEmptyIpAddress_ShouldFail()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        var act = () => analytics.RecordView(Guid.NewGuid(), "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ipAddress*");
    }

    #endregion

    #region UpdateUniqueViewers Tests

    [Fact]
    public void UpdateUniqueViewers_ShouldUpdateCount()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        analytics.UpdateUniqueViewers(5);

        // Assert
        analytics.UniqueViewers.Should().Be(5);
        analytics.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateUniqueViewers_WithNegativeValue_ShouldFail()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        var act = () => analytics.UpdateUniqueViewers(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*count*");
    }

    #endregion

    #region UpdateRegistrationCount Tests

    [Fact]
    public void UpdateRegistrationCount_ShouldUpdateCount()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        analytics.UpdateRegistrationCount(10);

        // Assert
        analytics.RegistrationCount.Should().Be(10);
        analytics.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateRegistrationCount_WithNegativeValue_ShouldFail()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());

        // Act
        var act = () => analytics.UpdateRegistrationCount(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*count*");
    }

    #endregion

    #region ConversionRate Tests

    [Fact]
    public void ConversionRate_WithNoViews_ShouldBeZero()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());
        analytics.UpdateRegistrationCount(5);

        // Act
        var conversionRate = analytics.ConversionRate;

        // Assert
        conversionRate.Should().Be(0);
    }

    [Fact]
    public void ConversionRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());
        analytics.RecordView(null, "192.168.1.1");
        analytics.RecordView(null, "192.168.1.2");
        analytics.RecordView(null, "192.168.1.3");
        analytics.RecordView(null, "192.168.1.4");
        analytics.UpdateRegistrationCount(1);

        // Act
        var conversionRate = analytics.ConversionRate;

        // Assert
        conversionRate.Should().Be(25m); // 1/4 * 100 = 25%
    }

    [Fact]
    public void ConversionRate_WithHalfRegistrations_ShouldBeFiftyPercent()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());
        analytics.RecordView(null, "192.168.1.1");
        analytics.RecordView(null, "192.168.1.2");
        analytics.UpdateRegistrationCount(1);

        // Act
        var conversionRate = analytics.ConversionRate;

        // Assert
        conversionRate.Should().Be(50m); // 1/2 * 100 = 50%
    }

    [Fact]
    public void ConversionRate_With100PercentConversion_ShouldBe100()
    {
        // Arrange
        var analytics = EventAnalytics.Create(Guid.NewGuid());
        analytics.RecordView(null, "192.168.1.1");
        analytics.RecordView(null, "192.168.1.2");
        analytics.UpdateRegistrationCount(2);

        // Act
        var conversionRate = analytics.ConversionRate;

        // Assert
        conversionRate.Should().Be(100m); // 2/2 * 100 = 100%
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void RecordView_ShouldRaiseDomainEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var analytics = EventAnalytics.Create(eventId);

        // Act
        analytics.RecordView(userId, ipAddress);

        // Assert
        analytics.DomainEvents.Should().HaveCount(1);
        var domainEvent = analytics.DomainEvents.First();
        domainEvent.Should().BeOfType<EventViewRecordedDomainEvent>();

        var viewEvent = domainEvent as EventViewRecordedDomainEvent;
        viewEvent!.EventId.Should().Be(eventId);
        viewEvent.UserId.Should().Be(userId);
        viewEvent.IpAddress.Should().Be(ipAddress);
    }

    #endregion
}

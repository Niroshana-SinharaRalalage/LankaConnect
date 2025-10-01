using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;
using Xunit;
using System.Text.Json;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED PHASE: Integration tests for AlertSeverity with cultural intelligence
/// London School approach focusing on interaction with other domain components
/// </summary>
public class AlertSeverityIntegrationTests
{
    [Fact]
    public void AlertSeverity_ShouldSerializeToJson()
    {
        // RED: Testing JSON serialization for API responses
        var severity = AlertSeverity.Sacred;

        var json = JsonSerializer.Serialize(severity);
        var deserialized = JsonSerializer.Deserialize<AlertSeverity>(json);

        deserialized.Should().Be(severity);
        deserialized.Value.Should().Be(5);
        deserialized.Name.Should().Be("Sacred");
    }

    [Fact]
    public void AlertSeverity_ShouldWorkWithPerformanceThreshold()
    {
        // RED: Testing integration with PerformanceThreshold
        var threshold = new PerformanceThreshold("CPU", 80.0, 90.0, 95.0, 98.0);

        var lowSeverity = threshold.GetSeverityForValue(75.0);
        var mediumSeverity = threshold.GetSeverityForValue(85.0);
        var highSeverity = threshold.GetSeverityForValue(92.0);
        var criticalSeverity = threshold.GetSeverityForValue(96.0);
        var sacredSeverity = threshold.GetSeverityForValue(99.0);

        lowSeverity.Should().Be(AlertSeverity.Low);
        mediumSeverity.Should().Be(AlertSeverity.Medium);
        highSeverity.Should().Be(AlertSeverity.High);
        criticalSeverity.Should().Be(AlertSeverity.Critical);
        sacredSeverity.Should().Be(AlertSeverity.Sacred);
    }

    [Fact]
    public void AlertSeverity_ShouldSupportCulturalEventContext()
    {
        // RED: Testing cultural event context integration
        var culturalContext = new CulturalEventContext("Vesak Poya", AlertSeverity.Sacred);

        culturalContext.Severity.Should().Be(AlertSeverity.Sacred);
        culturalContext.RequiresSpecialHandling().Should().BeTrue();
        culturalContext.GetEscalationTimeframe().Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void AlertSeverity_ShouldIntegrateWithMonitoringSystem()
    {
        // RED: Testing monitoring system integration
        var monitoringAlert = new MonitoringAlert
        {
            Id = Guid.NewGuid(),
            Severity = AlertSeverity.Critical,
            Message = "Database connection pool exhausted",
            Timestamp = DateTimeOffset.UtcNow
        };

        monitoringAlert.ShouldEscalate().Should().BeTrue();
        monitoringAlert.GetRetryCount().Should().Be(3);
        monitoringAlert.GetTimeoutDuration().Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AlertSeverity_ShouldSupportDiasporaNotificationRouting()
    {
        // RED: Testing diaspora community notification routing
        var notifications = new List<DiasporaNotification>
        {
            new("Community Center Event", AlertSeverity.High, "australia-melbourne"),
            new("Temple Festival", AlertSeverity.Sacred, "uk-london"),
            new("Business Directory Update", AlertSeverity.Low, "canada-toronto")
        };

        var sacredNotifications = notifications
            .Where(n => n.Severity == AlertSeverity.Sacred)
            .ToList();

        var criticalNotifications = notifications
            .Where(n => n.Severity.Value >= AlertSeverity.Critical.Value)
            .ToList();

        sacredNotifications.Should().HaveCount(1);
        sacredNotifications.First().Location.Should().Be("uk-london");

        criticalNotifications.Should().HaveCount(1); // Sacred is > Critical
        criticalNotifications.First().Severity.Should().Be(AlertSeverity.Sacred);
    }

    [Fact]
    public void AlertSeverity_ShouldCalculateEscalationMatrix()
    {
        // RED: Testing escalation matrix calculation
        var escalationMatrix = AlertSeverity.CreateEscalationMatrix();

        escalationMatrix[AlertSeverity.Low].Should().Be(TimeSpan.FromHours(4));
        escalationMatrix[AlertSeverity.Medium].Should().Be(TimeSpan.FromHours(2));
        escalationMatrix[AlertSeverity.High].Should().Be(TimeSpan.FromHours(1));
        escalationMatrix[AlertSeverity.Critical].Should().Be(TimeSpan.FromMinutes(30));
        escalationMatrix[AlertSeverity.Sacred].Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AlertSeverity_ShouldSupportBatchProcessing()
    {
        // RED: Testing batch processing scenarios
        var alerts = GenerateTestAlerts(1000);

        var groupedBySeverity = alerts.GroupBy(a => a.Severity).ToList();

        groupedBySeverity.Should().HaveCount(5);

        var sacredAlerts = groupedBySeverity.First(g => g.Key == AlertSeverity.Sacred);
        sacredAlerts.Should().NotBeEmpty();

        // Sacred alerts should be processed first
        var processingOrder = AlertSeverity.GetProcessingOrder();
        processingOrder.First().Should().Be(AlertSeverity.Sacred);
        processingOrder.Last().Should().Be(AlertSeverity.Low);
    }

    private static IEnumerable<TestAlert> GenerateTestAlerts(int count)
    {
        var random = new Random();
        var severities = AlertSeverity.GetAll().ToArray();

        for (int i = 0; i < count; i++)
        {
            yield return new TestAlert
            {
                Id = i,
                Severity = severities[random.Next(severities.Length)],
                Message = $"Test alert {i}"
            };
        }
    }
}

/// <summary>
/// Supporting types for RED phase testing
/// These will integrate with existing domain models
/// </summary>
public record CulturalEventContext(string EventName, AlertSeverity Severity)
{
    public bool RequiresSpecialHandling() => Severity == AlertSeverity.Sacred;

    public TimeSpan GetEscalationTimeframe() => Severity switch
    {
        var s when s == AlertSeverity.Sacred => TimeSpan.FromMinutes(1),
        var s when s == AlertSeverity.Critical => TimeSpan.FromMinutes(15),
        var s when s == AlertSeverity.High => TimeSpan.FromHours(1),
        var s when s == AlertSeverity.Medium => TimeSpan.FromHours(4),
        var s when s == AlertSeverity.Low => TimeSpan.FromHours(24),
        _ => TimeSpan.FromDays(1)
    };
}

public class MonitoringAlert
{
    public Guid Id { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Low;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    public bool ShouldEscalate() => Severity.Value >= AlertSeverity.High.Value;

    public int GetRetryCount() => Severity switch
    {
        var s when s == AlertSeverity.Sacred => 5,
        var s when s == AlertSeverity.Critical => 3,
        var s when s == AlertSeverity.High => 2,
        _ => 1
    };

    public TimeSpan GetTimeoutDuration() => Severity switch
    {
        var s when s == AlertSeverity.Sacred => TimeSpan.FromMinutes(1),
        var s when s == AlertSeverity.Critical => TimeSpan.FromMinutes(5),
        var s when s == AlertSeverity.High => TimeSpan.FromMinutes(15),
        var s when s == AlertSeverity.Medium => TimeSpan.FromHours(1),
        var s when s == AlertSeverity.Low => TimeSpan.FromHours(4),
        _ => TimeSpan.FromDays(1)
    };
}

public record DiasporaNotification(string Title, AlertSeverity Severity, string Location);

public class TestAlert
{
    public int Id { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Low;
    public string Message { get; set; } = string.Empty;
}
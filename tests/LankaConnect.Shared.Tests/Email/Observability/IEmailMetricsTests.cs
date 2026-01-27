using FluentAssertions;
using LankaConnect.Shared.Email.Observability;

namespace LankaConnect.Shared.Tests.Email.Observability;

/// <summary>
/// Phase 6A.86: Tests for IEmailMetrics interface (TDD - RED phase)
/// Ensures email metrics collection for dashboard and alerting
/// </summary>
public class IEmailMetricsTests
{
    private readonly IEmailMetrics _metrics;

    public IEmailMetricsTests()
    {
        _metrics = new EmailMetrics();
    }

    [Fact]
    public void RecordEmailSent_ShouldIncrementTotalCount()
    {
        // Arrange
        var templateName = "template-event-reminder";
        var durationMs = 250;

        // Act
        _metrics.RecordEmailSent(templateName, durationMs, success: true);
        _metrics.RecordEmailSent(templateName, 300, success: true);

        // Assert
        var stats = _metrics.GetStatsByTemplate(templateName);
        stats.TotalSent.Should().Be(2);
    }

    [Fact]
    public void RecordEmailSent_ShouldIncrementSuccessCount()
    {
        // Arrange
        var templateName = "template-event-reminder";

        // Act
        _metrics.RecordEmailSent(templateName, 250, success: true);
        _metrics.RecordEmailSent(templateName, 300, success: true);
        _metrics.RecordEmailSent(templateName, 100, success: false);

        // Assert
        var stats = _metrics.GetStatsByTemplate(templateName);
        stats.SuccessCount.Should().Be(2);
        stats.FailureCount.Should().Be(1);
    }

    [Fact]
    public void RecordEmailSent_ShouldTrackAverageDuration()
    {
        // Arrange
        var templateName = "template-event-reminder";

        // Act
        _metrics.RecordEmailSent(templateName, 100, success: true);
        _metrics.RecordEmailSent(templateName, 200, success: true);
        _metrics.RecordEmailSent(templateName, 300, success: true);

        // Assert
        var stats = _metrics.GetStatsByTemplate(templateName);
        stats.AverageDurationMs.Should().Be(200); // (100 + 200 + 300) / 3
    }

    [Fact]
    public void RecordParameterValidationFailure_ShouldIncrementCount()
    {
        // Arrange
        var templateName = "template-event-reminder";

        // Act
        _metrics.RecordParameterValidationFailure(templateName);
        _metrics.RecordParameterValidationFailure(templateName);

        // Assert
        var stats = _metrics.GetStatsByTemplate(templateName);
        stats.ValidationFailures.Should().Be(2);
    }

    [Fact]
    public void RecordTemplateNotFound_ShouldIncrementCount()
    {
        // Arrange
        var templateName = "template-nonexistent";

        // Act
        _metrics.RecordTemplateNotFound(templateName);
        _metrics.RecordTemplateNotFound(templateName);
        _metrics.RecordTemplateNotFound(templateName);

        // Assert
        var stats = _metrics.GetStatsByTemplate(templateName);
        stats.TemplateNotFoundCount.Should().Be(3);
    }

    [Fact]
    public void RecordHandlerUsage_ShouldTrackTypedVsDictionary()
    {
        // Arrange
        var handlerName = "EventReminderJob";

        // Act
        _metrics.RecordHandlerUsage(handlerName, usedTypedParameters: true);
        _metrics.RecordHandlerUsage(handlerName, usedTypedParameters: true);
        _metrics.RecordHandlerUsage(handlerName, usedTypedParameters: false);

        // Assert
        var stats = _metrics.GetStatsByHandler(handlerName);
        stats.TypedParameterUsageCount.Should().Be(2);
        stats.DictionaryParameterUsageCount.Should().Be(1);
    }

    [Fact]
    public void GetStatsByTemplate_ShouldReturnZeroStatsForNewTemplate()
    {
        // Arrange
        var templateName = "template-new";

        // Act
        var stats = _metrics.GetStatsByTemplate(templateName);

        // Assert
        stats.TotalSent.Should().Be(0);
        stats.SuccessCount.Should().Be(0);
        stats.FailureCount.Should().Be(0);
        stats.ValidationFailures.Should().Be(0);
        stats.TemplateNotFoundCount.Should().Be(0);
        stats.AverageDurationMs.Should().Be(0);
    }

    [Fact]
    public void GetStatsByHandler_ShouldReturnZeroStatsForNewHandler()
    {
        // Arrange
        var handlerName = "NewHandler";

        // Act
        var stats = _metrics.GetStatsByHandler(handlerName);

        // Assert
        stats.TypedParameterUsageCount.Should().Be(0);
        stats.DictionaryParameterUsageCount.Should().Be(0);
        stats.TotalEmailsSent.Should().Be(0);
    }

    [Fact]
    public void GetGlobalStats_ShouldAggregateAllTemplates()
    {
        // Arrange & Act
        _metrics.RecordEmailSent("template-1", 100, success: true);
        _metrics.RecordEmailSent("template-2", 200, success: true);
        _metrics.RecordEmailSent("template-3", 300, success: false);

        var globalStats = _metrics.GetGlobalStats();

        // Assert
        globalStats.TotalEmailsSent.Should().Be(3);
        globalStats.TotalSuccesses.Should().Be(2);
        globalStats.TotalFailures.Should().Be(1);
    }

    [Fact]
    public void GetSuccessRate_ShouldCalculatePercentage()
    {
        // Arrange
        var templateName = "template-event-reminder";

        // Act
        _metrics.RecordEmailSent(templateName, 100, success: true);
        _metrics.RecordEmailSent(templateName, 100, success: true);
        _metrics.RecordEmailSent(templateName, 100, success: true);
        _metrics.RecordEmailSent(templateName, 100, success: false);

        var stats = _metrics.GetStatsByTemplate(templateName);

        // Assert
        stats.SuccessRate.Should().Be(75.0); // 3/4 = 75%
    }

    [Fact]
    public void GetSuccessRate_ShouldReturnZeroWhenNoEmailsSent()
    {
        // Arrange
        var templateName = "template-new";

        // Act
        var stats = _metrics.GetStatsByTemplate(templateName);

        // Assert
        stats.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void ResetMetrics_ShouldClearAllStats()
    {
        // Arrange
        _metrics.RecordEmailSent("template-1", 100, success: true);
        _metrics.RecordEmailSent("template-2", 200, success: true);

        // Act
        _metrics.ResetMetrics();

        // Assert
        var stats1 = _metrics.GetStatsByTemplate("template-1");
        var stats2 = _metrics.GetStatsByTemplate("template-2");
        var globalStats = _metrics.GetGlobalStats();

        stats1.TotalSent.Should().Be(0);
        stats2.TotalSent.Should().Be(0);
        globalStats.TotalEmailsSent.Should().Be(0);
    }
}

/// <summary>
/// Concrete implementation of IEmailMetrics for testing
/// </summary>
public class EmailMetrics : IEmailMetrics
{
    private readonly Dictionary<string, TemplateMetrics> _templateMetrics = new();
    private readonly Dictionary<string, HandlerMetrics> _handlerMetrics = new();

    public void RecordEmailSent(string templateName, int durationMs, bool success)
    {
        var metrics = GetOrCreateTemplateMetrics(templateName);
        metrics.TotalSent++;

        if (success)
            metrics.SuccessCount++;
        else
            metrics.FailureCount++;

        // Update average duration
        metrics.TotalDurationMs += durationMs;
        metrics.AverageDurationMs = metrics.TotalDurationMs / metrics.TotalSent;
    }

    public void RecordParameterValidationFailure(string templateName)
    {
        var metrics = GetOrCreateTemplateMetrics(templateName);
        metrics.ValidationFailures++;
    }

    public void RecordTemplateNotFound(string templateName)
    {
        var metrics = GetOrCreateTemplateMetrics(templateName);
        metrics.TemplateNotFoundCount++;
    }

    public void RecordHandlerUsage(string handlerName, bool usedTypedParameters)
    {
        var metrics = GetOrCreateHandlerMetrics(handlerName);
        metrics.TotalEmailsSent++;

        if (usedTypedParameters)
            metrics.TypedParameterUsageCount++;
        else
            metrics.DictionaryParameterUsageCount++;
    }

    public TemplateMetrics GetStatsByTemplate(string templateName)
    {
        return _templateMetrics.GetValueOrDefault(templateName) ?? new TemplateMetrics();
    }

    public HandlerMetrics GetStatsByHandler(string handlerName)
    {
        return _handlerMetrics.GetValueOrDefault(handlerName) ?? new HandlerMetrics();
    }

    public GlobalMetrics GetGlobalStats()
    {
        var totalSent = _templateMetrics.Values.Sum(m => m.TotalSent);
        var totalSuccesses = _templateMetrics.Values.Sum(m => m.SuccessCount);
        var totalFailures = _templateMetrics.Values.Sum(m => m.FailureCount);

        return new GlobalMetrics
        {
            TotalEmailsSent = totalSent,
            TotalSuccesses = totalSuccesses,
            TotalFailures = totalFailures
        };
    }

    public void ResetMetrics()
    {
        _templateMetrics.Clear();
        _handlerMetrics.Clear();
    }

    private TemplateMetrics GetOrCreateTemplateMetrics(string templateName)
    {
        if (!_templateMetrics.ContainsKey(templateName))
        {
            _templateMetrics[templateName] = new TemplateMetrics();
        }
        return _templateMetrics[templateName];
    }

    private HandlerMetrics GetOrCreateHandlerMetrics(string handlerName)
    {
        if (!_handlerMetrics.ContainsKey(handlerName))
        {
            _handlerMetrics[handlerName] = new HandlerMetrics();
        }
        return _handlerMetrics[handlerName];
    }
}

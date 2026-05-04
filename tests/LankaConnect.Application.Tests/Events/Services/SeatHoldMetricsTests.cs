using FluentAssertions;
using LankaConnect.Application.Events.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 7H — verifies that <see cref="SeatHoldMetrics"/> emits the
/// architect-spec'd structured Serilog log entries. The dashboard query
/// matches on the <c>MetricName</c> property — these tests pin that
/// contract so a future refactor can't silently change the wire format.
/// </summary>
public class SeatHoldMetricsTests
{
    private readonly Mock<ILogger<SeatHoldMetrics>> _logger = new();
    private readonly SeatHoldMetrics _sut;

    public SeatHoldMetricsTests()
    {
        _sut = new SeatHoldMetrics(_logger.Object);
    }

    [Fact]
    public void SeatHoldCreated_EmitsStructuredLog_WithExpectedMetricName()
    {
        var eventId = Guid.NewGuid();

        _sut.SeatHoldCreated(eventId, seatCount: 3);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("seat_hold.created")
                    && state.ToString()!.Contains(eventId.ToString())
                    && state.ToString()!.Contains("3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SeatHoldExpired_EmitsStructuredLog_WithExpectedMetricName()
    {
        _sut.SeatHoldExpired(expiredCount: 7);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("seat_hold.expired")
                    && state.ToString()!.Contains("7")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SeatHoldExpired_WithZeroCount_StillEmits_ButCountIsZero()
    {
        // Cleanup-pass-with-nothing-to-do is the common path — emitting at
        // count=0 lets the dashboard prove the cleanup service is alive.
        _sut.SeatHoldExpired(expiredCount: 0);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("seat_hold.expired")
                    && state.ToString()!.Contains("0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

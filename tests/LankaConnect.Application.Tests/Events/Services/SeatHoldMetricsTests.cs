using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Services;
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

    [Fact]
    public void SeatHoldConvertedToReservation_EmitsStructuredLog_WithExpectedMetricName()
    {
        // S8.2.C — closes the Phase 7H deferred metric. Fires once per
        // successful webhook conversion of pending seats → reservation rows.
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _sut.SeatHoldConvertedToReservation(eventId, registrationId, seatCount: 4);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("seat_hold.converted_to_reservation")
                    && state.ToString()!.Contains(eventId.ToString())
                    && state.ToString()!.Contains(registrationId.ToString())
                    && state.ToString()!.Contains("4")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SeatConversionRaceLost_EmitsStructuredWarning_PerSeat()
    {
        // S8.2.C — emitted once per seat lost to a concurrent buyer.
        // Logged at Warning level (not Information) because it represents
        // a confirmed-but-unseated registration that ops needs to handle.
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        _sut.SeatConversionRaceLost(eventId, registrationId, seatId);

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("seat_conversion.race_lost")
                    && state.ToString()!.Contains(eventId.ToString())
                    && state.ToString()!.Contains(registrationId.ToString())
                    && state.ToString()!.Contains(seatId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

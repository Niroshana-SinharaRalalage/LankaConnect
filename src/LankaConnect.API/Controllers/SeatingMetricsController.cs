using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Events.Services;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Slice 7 S7.8: receives client-reported seating UX metrics.
/// Today only the <c>seatpicker.selection_completed</c> metric lands here
/// (attendee-side wall-clock measurement of SeatPicker mount → confirm).
/// </summary>
[ApiController]
[Route("api/seating-metrics")]
[Produces("application/json")]
public class SeatingMetricsController : BaseController<SeatingMetricsController>
{
    private readonly ILayoutMetrics _layoutMetrics;

    public SeatingMetricsController(
        IMediator mediator,
        ILogger<SeatingMetricsController> logger,
        ILayoutMetrics layoutMetrics)
        : base(mediator, logger)
    {
        _layoutMetrics = layoutMetrics;
    }

    /// <summary>
    /// Record that a registrant finished picking seats and confirmed. Body carries
    /// the client-measured elapsed time since SeatPicker mount.
    /// AllowAnonymous so anonymous registrations can report the metric too.
    /// </summary>
    [HttpPost("selection-completed")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult RecordSelectionCompleted([FromBody] SeatPickerSelectionCompletedRequest request)
    {
        if (request.EventId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "EventId is required" });
        }

        if (request.AttendeeCount <= 0)
        {
            return BadRequest(new ProblemDetails { Title = "AttendeeCount must be positive" });
        }

        if (request.TimeToCompleteMs < 0)
        {
            return BadRequest(new ProblemDetails { Title = "TimeToCompleteMs must be non-negative" });
        }

        _layoutMetrics.SeatPickerSelectionCompleted(
            request.EventId,
            request.AttendeeCount,
            request.TimeToCompleteMs);

        return NoContent();
    }
}

public record SeatPickerSelectionCompletedRequest(
    Guid EventId,
    int AttendeeCount,
    long TimeToCompleteMs);

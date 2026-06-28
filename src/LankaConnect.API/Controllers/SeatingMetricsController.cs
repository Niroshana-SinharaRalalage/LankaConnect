using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Products.LankaEvents.Application.Services;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Slice 7 S7.8 + Slice 8 S8.1/S8.8: receives client-reported seating UX metrics.
/// <list type="bullet">
///   <item><c>POST selection-completed</c> — attendee-side SeatPicker mount → confirm (anon allowed).</item>
///   <item><c>POST canvas-editor-opened</c> — organizer opened the canvas editor (auth required).</item>
///   <item><c>POST canvas-editor-saved</c> — organizer saved a canvas-editor session (auth required).</item>
/// </list>
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

    /// <summary>
    /// Slice 8 S8.1: record that an organizer opened the canvas editor modal.
    /// Requires auth because only organizers can open the editor.
    /// </summary>
    [HttpPost("canvas-editor-opened")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult RecordCanvasEditorOpened([FromBody] CanvasEditorOpenedRequest request)
    {
        if (request.LayoutId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "LayoutId is required" });
        }

        _layoutMetrics.LayoutCanvasEditorOpened(request.LayoutId);

        return NoContent();
    }

    /// <summary>
    /// Slice 8 S8.8: record that an organizer saved a canvas-editor session.
    /// <c>ChangesCount</c> is the number of mutations packed into the PUT /batch payload.
    /// </summary>
    [HttpPost("canvas-editor-saved")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult RecordCanvasEditorSaved([FromBody] CanvasEditorSavedRequest request)
    {
        if (request.LayoutId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "LayoutId is required" });
        }

        if (request.ChangesCount < 0)
        {
            return BadRequest(new ProblemDetails { Title = "ChangesCount must be non-negative" });
        }

        _layoutMetrics.LayoutCanvasEditorSaved(request.LayoutId, request.ChangesCount);

        return NoContent();
    }
}

public record SeatPickerSelectionCompletedRequest(
    Guid EventId,
    int AttendeeCount,
    long TimeToCompleteMs);

public record CanvasEditorOpenedRequest(Guid LayoutId);

public record CanvasEditorSavedRequest(Guid LayoutId, int ChangesCount);

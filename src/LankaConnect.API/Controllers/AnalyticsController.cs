using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Analytics.Queries.GetEventAnalytics;
using LankaConnect.Application.Analytics.Queries.GetOrganizerDashboard;
using LankaConnect.Application.Analytics.Common;
using LankaConnect.API.Extensions;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Controller for Event Analytics endpoints
/// Provides analytics data for events and organizers
/// </summary>
public class AnalyticsController : BaseController<AnalyticsController>
{
    public AnalyticsController(IMediator mediator, ILogger<AnalyticsController> logger) : base(mediator, logger)
    {
    }

    // ==================== PUBLIC ENDPOINTS ====================

    /// <summary>
    /// Get analytics data for a specific event
    /// Returns view counts, unique viewers, registrations, and conversion rate
    /// </summary>
    /// <param name="eventId">Event ID</param>
    [HttpGet("events/{eventId:guid}")]
    [ProducesResponseType(typeof(EventAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventAnalytics(Guid eventId)
    {
        Logger.LogInformation("Getting analytics for event: {EventId}", eventId);

        var query = new GetEventAnalyticsQuery(eventId);
        var result = await Mediator.Send(query);

        if (result.IsSuccess && result.Value == null)
        {
            Logger.LogInformation("No analytics found for event: {EventId}", eventId);
            return NotFound(new { message = "No analytics data found for this event" });
        }

        return HandleResult(result);
    }

    // ==================== ORGANIZER ENDPOINTS (Authenticated) ====================

    /// <summary>
    /// Get aggregated analytics dashboard for an organizer
    /// Returns total views, registrations, conversion rates, and top/upcoming events
    /// Requires authentication
    /// </summary>
    [HttpGet("organizer/dashboard")]
    [Authorize]
    [ProducesResponseType(typeof(OrganizerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrganizerDashboard()
    {
        var organizerId = User.GetUserId();

        Logger.LogInformation("Getting organizer dashboard for: {OrganizerId}", organizerId);

        var query = new GetOrganizerDashboardQuery(organizerId);
        var result = await Mediator.Send(query);

        if (result.IsSuccess && result.Value == null)
        {
            Logger.LogInformation("No analytics found for organizer: {OrganizerId}", organizerId);
            return NotFound(new { message = "No analytics data found. Create some events to see your dashboard!" });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get aggregated analytics dashboard for a specific organizer (Admin only)
    /// </summary>
    /// <param name="organizerId">Organizer ID</param>
    [HttpGet("organizer/{organizerId:guid}/dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrganizerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrganizerDashboardByIdForAdmin(Guid organizerId)
    {
        Logger.LogInformation("Admin getting organizer dashboard for: {OrganizerId}", organizerId);

        var query = new GetOrganizerDashboardQuery(organizerId);
        var result = await Mediator.Send(query);

        if (result.IsSuccess && result.Value == null)
        {
            Logger.LogInformation("No analytics found for organizer: {OrganizerId}", organizerId);
            return NotFound(new { message = "No analytics data found for this organizer" });
        }

        return HandleResult(result);
    }
}

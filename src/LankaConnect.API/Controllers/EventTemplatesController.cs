using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Events.Queries.GetEventTemplates;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 6A.8: Event Template System
/// Controller for event template endpoints
/// </summary>
public class EventTemplatesController : BaseController<EventTemplatesController>
{
    public EventTemplatesController(IMediator mediator, ILogger<EventTemplatesController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get all active event templates with optional filtering by category
    /// </summary>
    /// <param name="category">Optional category filter (e.g., Religious, Cultural, Community)</param>
    /// <param name="isActive">Optional active status filter (defaults to true if not specified)</param>
    /// <returns>List of event templates ordered by display order</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventTemplates(
        [FromQuery] EventCategory? category = null,
        [FromQuery] bool? isActive = null)
    {
        Logger.LogInformation("Getting event templates with filters: category={Category}, isActive={IsActive}",
            category, isActive);

        var query = new GetEventTemplatesQuery(category, isActive);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }
}

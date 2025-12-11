using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Application.MetroAreas.Queries.GetMetroAreas;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Metro Areas API endpoints
/// Phase 5C: Metro Areas System
/// </summary>
[ApiController]
[Route("api/metro-areas")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class MetroAreasController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MetroAreasController> _logger;

    public MetroAreasController(IMediator mediator, ILogger<MetroAreasController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all active metro areas
    /// </summary>
    /// <param name="stateFilter">Optional state filter (e.g., "OH", "NY")</param>
    /// <param name="activeOnly">Return only active metro areas (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of metro areas</returns>
    /// <response code="200">Returns list of metro areas</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ResponseCache(Duration = 900)] // Cache for 15 minutes
    [ProducesResponseType(typeof(IReadOnlyList<MetroAreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMetroAreas(
        [FromQuery] string? stateFilter = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET /api/metro-areas - StateFilter: {StateFilter}, ActiveOnly: {ActiveOnly}",
                stateFilter, activeOnly);

            var query = new GetMetroAreasQuery
            {
                StateFilter = stateFilter,
                ActiveOnly = activeOnly
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch metro areas: {Error}", result.Error);
                return BadRequest(new
                {
                    error = result.Error,
                    message = "Failed to fetch metro areas"
                });
            }

            _logger.LogInformation("Successfully fetched {Count} metro areas", result.Value.Count);
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching metro areas");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while fetching metro areas"
            });
        }
    }
}

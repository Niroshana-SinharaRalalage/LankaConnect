using LankaConnect.Application.ReferenceData.DTOs;
using LankaConnect.Application.ReferenceData.Services;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Reference Data API endpoints
/// Phase 6A.47: Database-driven reference data (enums, lookups, etc.)
/// </summary>
[ApiController]
[Route("api/reference-data")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;
    private readonly ILogger<ReferenceDataController> _logger;

    public ReferenceDataController(
        IReferenceDataService referenceDataService,
        ILogger<ReferenceDataController> logger)
    {
        _referenceDataService = referenceDataService;
        _logger = logger;
    }

    /// <summary>
    /// Get all event categories
    /// </summary>
    /// <param name="activeOnly">Return only active categories (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of event categories</returns>
    /// <response code="200">Returns list of event categories</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("event-categories")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour (matches service cache TTL)
    [ProducesResponseType(typeof(IReadOnlyList<EventCategoryRefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventCategories(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET /api/reference-data/event-categories - ActiveOnly: {ActiveOnly}", activeOnly);

            var categories = await _referenceDataService.GetEventCategoriesAsync(activeOnly, cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} event categories", categories.Count);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching event categories");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while fetching event categories"
            });
        }
    }

    /// <summary>
    /// Get all event statuses
    /// </summary>
    /// <param name="activeOnly">Return only active statuses (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of event statuses</returns>
    /// <response code="200">Returns list of event statuses</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("event-statuses")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    [ProducesResponseType(typeof(IReadOnlyList<EventStatusRefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventStatuses(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET /api/reference-data/event-statuses - ActiveOnly: {ActiveOnly}", activeOnly);

            var statuses = await _referenceDataService.GetEventStatusesAsync(activeOnly, cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} event statuses", statuses.Count);
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching event statuses");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while fetching event statuses"
            });
        }
    }

    /// <summary>
    /// Get all user roles
    /// </summary>
    /// <param name="activeOnly">Return only active roles (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user roles with permissions</returns>
    /// <response code="200">Returns list of user roles</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("user-roles")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    [ProducesResponseType(typeof(IReadOnlyList<UserRoleRefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET /api/reference-data/user-roles - ActiveOnly: {ActiveOnly}", activeOnly);

            var roles = await _referenceDataService.GetUserRolesAsync(activeOnly, cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} user roles", roles.Count);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching user roles");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while fetching user roles"
            });
        }
    }

    /// <summary>
    /// Invalidate cache for specific reference type
    /// Admin-only endpoint for cache management
    /// </summary>
    /// <param name="referenceType">Type of reference data to invalidate (eventcategories, eventstatuses, userroles)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Cache invalidated successfully</response>
    /// <response code="400">Invalid reference type</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("invalidate-cache/{referenceType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InvalidateCache(
        string referenceType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("POST /api/reference-data/invalidate-cache/{ReferenceType}", referenceType);

            await _referenceDataService.InvalidateCacheAsync(referenceType, cancellationToken);

            _logger.LogInformation("Successfully invalidated cache for {ReferenceType}", referenceType);
            return Ok(new
            {
                message = $"Cache invalidated successfully for {referenceType}"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reference type: {ReferenceType}", referenceType);
            return BadRequest(new
            {
                error = "INVALID_REFERENCE_TYPE",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error invalidating cache for {ReferenceType}", referenceType);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while invalidating cache"
            });
        }
    }

    /// <summary>
    /// Invalidate all reference data caches
    /// Admin-only endpoint for cache management
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">All caches invalidated successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("invalidate-all-caches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateAllCaches(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("POST /api/reference-data/invalidate-all-caches");

            await _referenceDataService.InvalidateAllCachesAsync(cancellationToken);

            _logger.LogInformation("Successfully invalidated all reference data caches");
            return Ok(new
            {
                message = "All reference data caches invalidated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error invalidating all caches");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while invalidating all caches"
            });
        }
    }
}

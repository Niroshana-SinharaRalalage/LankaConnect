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
    /// Get reference values by enum type(s) - UNIFIED ENDPOINT
    /// Supports multiple enum types in a single request for optimal performance
    /// Examples: ?types=EventCategory,EventStatus,UserRole
    /// </summary>
    /// <param name="types">Comma-separated list of enum types (e.g., EventCategory,EventStatus,UserRole)</param>
    /// <param name="activeOnly">Return only active values (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reference values grouped by enum type</returns>
    /// <response code="200">Returns reference values for requested types</response>
    /// <response code="400">Invalid or missing types parameter</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour (matches service cache TTL)
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReferenceData(
        [FromQuery] string? types = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(types))
            {
                _logger.LogWarning("GET /api/reference-data - Missing types parameter");
                return BadRequest(new
                {
                    error = "MISSING_TYPES",
                    message = "The 'types' query parameter is required. Example: ?types=EventCategory,EventStatus,UserRole"
                });
            }

            var enumTypes = types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (enumTypes.Length == 0)
            {
                _logger.LogWarning("GET /api/reference-data - Empty types parameter");
                return BadRequest(new
                {
                    error = "EMPTY_TYPES",
                    message = "The 'types' parameter cannot be empty. Example: ?types=EventCategory,EventStatus,UserRole"
                });
            }

            _logger.LogInformation("GET /api/reference-data - Types: {Types}, ActiveOnly: {ActiveOnly}",
                string.Join(",", enumTypes), activeOnly);

            var referenceData = await _referenceDataService.GetByTypesAsync(enumTypes, activeOnly, cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} reference values for {TypeCount} types",
                referenceData.Count, enumTypes.Length);

            return Ok(referenceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching reference data");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "INTERNAL_ERROR",
                message = "An unexpected error occurred while fetching reference data"
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

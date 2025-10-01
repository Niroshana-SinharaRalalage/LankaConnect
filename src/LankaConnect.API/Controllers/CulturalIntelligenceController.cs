using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using LankaConnect.Application.Common.Queries;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Cultural Intelligence API Controller demonstrating cache-aside pattern
/// Provides cached cultural calendar and appropriateness scoring endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CulturalIntelligenceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICulturalIntelligenceCacheService _cacheService;
    private readonly ILogger<CulturalIntelligenceController> _logger;

    public CulturalIntelligenceController(
        IMediator mediator,
        ICulturalIntelligenceCacheService cacheService,
        ILogger<CulturalIntelligenceController> logger)
    {
        _mediator = mediator;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets cultural calendar information with automatic caching
    /// </summary>
    /// <param name="calendarType">Type of calendar (Buddhist/Hindu)</param>
    /// <param name="date">Date to get calendar information for</param>
    /// <param name="region">Geographic region (sri_lanka, thailand, india, etc.)</param>
    /// <param name="language">Language code (si, th, hi, en, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cultural calendar information with events and observances</returns>
    [HttpGet("calendar/{calendarType}/{date:datetime}")]
    [ProducesResponseType(typeof(CulturalCalendarResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<CulturalCalendarResponse>> GetCulturalCalendar(
        [FromRoute] CalendarType calendarType,
        [FromRoute] DateTime date,
        [FromQuery] string region = "sri_lanka",
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
            
            var query = new CulturalCalendarQuery
            {
                CalendarType = calendarType,
                Date = date,
                GeographicRegion = region,
                Language = language,
                UserId = userId
            };

            _logger.LogInformation(
                "Processing cultural calendar request: {CalendarType} for {Date} in {Region} ({Language})",
                calendarType, date, region, language);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Cultural calendar query failed: {Errors}", string.Join(", ", result.Errors));
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cultural calendar request for {CalendarType} on {Date}", 
                calendarType, date);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Evaluates cultural appropriateness of content with ML-powered scoring
    /// </summary>
    /// <param name="request">Content appropriateness evaluation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cultural appropriateness score and recommendations</returns>
    [HttpPost("appropriateness")]
    [ProducesResponseType(typeof(CulturalAppropriatenessResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<CulturalAppropriatenessResponse>> EvaluateCulturalAppropriateness(
        [FromBody] CulturalAppropriatenessRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content is required for appropriateness evaluation" });
            }

            var userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

            var query = new CulturalAppropriatenessQuery
            {
                Content = request.Content,
                ContentType = request.ContentType ?? "text",
                CommunityId = request.CommunityId ?? "multi_cultural",
                GeographicRegion = request.GeographicRegion ?? "global",
                UserId = userId,
                AdditionalContext = request.AdditionalContext ?? new Dictionary<string, string>()
            };

            _logger.LogInformation(
                "Processing cultural appropriateness evaluation for {CommunityId} in {Region}",
                query.CommunityId, query.GeographicRegion);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Cultural appropriateness query failed: {Errors}", string.Join(", ", result.Errors));
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cultural appropriateness evaluation");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Gets cache performance metrics for cultural intelligence endpoints
    /// </summary>
    /// <param name="endpoint">Cultural intelligence endpoint type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache performance metrics</returns>
    [HttpGet("cache/metrics/{endpoint}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CacheMetrics), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<CacheMetrics>> GetCacheMetrics(
        [FromRoute] CulturalIntelligenceEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _cacheService.GetCacheMetricsAsync(endpoint, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache metrics for endpoint {Endpoint}", endpoint);
            return StatusCode(500, new { error = "Failed to retrieve cache metrics" });
        }
    }

    /// <summary>
    /// Gets cache health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache health status</returns>
    [HttpGet("cache/health")]
    [ProducesResponseType(typeof(CacheHealthStatus), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<CacheHealthStatus>> GetCacheHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _cacheService.GetHealthStatusAsync(cancellationToken);
            
            var statusCode = healthStatus.IsHealthy ? 200 : 503;
            return StatusCode(statusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache health");
            return StatusCode(500, new { error = "Failed to check cache health" });
        }
    }

    /// <summary>
    /// Invalidates cache for specific cultural context (Admin only)
    /// </summary>
    /// <param name="request">Cache invalidation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invalidation result</returns>
    [HttpPost("cache/invalidate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> InvalidateCache(
        [FromBody] CacheInvalidationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var context = new CulturalCacheContext
            {
                CommunityId = request.CommunityId ?? string.Empty,
                GeographicRegion = request.GeographicRegion ?? string.Empty,
                Language = request.Language ?? string.Empty,
                DataType = request.DataType ?? string.Empty
            };

            var result = await _cacheService.InvalidateCulturalCacheAsync(context, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            _logger.LogInformation(
                "Cache invalidated for context: Community={Community}, Region={Region}, Language={Language}, DataType={DataType}",
                context.CommunityId, context.GeographicRegion, context.Language, context.DataType);

            return Ok(new { message = "Cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache");
            return StatusCode(500, new { error = "Failed to invalidate cache" });
        }
    }

    /// <summary>
    /// Triggers cache warming for a cultural community (Admin only)
    /// </summary>
    /// <param name="request">Cache warming request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Warming initiation result</returns>
    [HttpPost("cache/warm")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> WarmCache(
        [FromBody] CacheWarmingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<CulturalCommunity>(request.Community, true, out var community))
            {
                return BadRequest(new { error = "Invalid cultural community specified" });
            }

            if (!Enum.TryParse<CacheWarmingStrategy>(request.Strategy, true, out var strategy))
            {
                return BadRequest(new { error = "Invalid cache warming strategy specified" });
            }

            _logger.LogInformation("Initiating cache warming for {Community} using {Strategy} strategy", 
                community, strategy);

            // Start cache warming in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cacheService.WarmCulturalCacheAsync(community, strategy, cancellationToken);
                    _logger.LogInformation("Cache warming completed for {Community}", community);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cache warming failed for {Community}", community);
                }
            }, cancellationToken);

            return Accepted(new { message = $"Cache warming initiated for {community} community" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating cache warming");
            return StatusCode(500, new { error = "Failed to initiate cache warming" });
        }
    }
}

// Request/Response DTOs
public class CulturalAppropriatenessRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? CommunityId { get; set; }
    public string? GeographicRegion { get; set; }
    public Dictionary<string, string>? AdditionalContext { get; set; }
}

public class CacheInvalidationRequest
{
    public string? CommunityId { get; set; }
    public string? GeographicRegion { get; set; }
    public string? Language { get; set; }
    public string? DataType { get; set; }
}

public class CacheWarmingRequest
{
    public string Community { get; set; } = string.Empty;
    public string Strategy { get; set; } = "Background";
}
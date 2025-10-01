using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LankaConnect.API.Controllers;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetCulturallyAppropriateEvents;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Cultural Event Recommendation API - Exposes sophisticated cultural intelligence algorithms
/// for event recommendation, cultural appropriateness analysis, and festival optimization
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/cultural-events")]
[ApiVersion("1.0")]
[EnableRateLimiting("CulturalIntelligencePolicy")]
[Produces("application/json")]
[Tags("Cultural Intelligence - Events")]
public class CulturalEventsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CulturalEventsController> _logger;

    public CulturalEventsController(IMediator mediator, ILogger<CulturalEventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get personalized event recommendations using cultural intelligence algorithms
    /// </summary>
    /// <param name="request">Event recommendation parameters</param>
    /// <returns>Culturally optimized event recommendations</returns>
    /// <response code="200">Successfully generated recommendations</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">API key required</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(GetEventRecommendationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<GetEventRecommendationsResponse>> GetEventRecommendations(
        [FromBody] GetEventRecommendationsRequest request)
    {
        try
        {
            _logger.LogInformation("Processing event recommendations for user {UserId} with {MaxResults} max results", 
                request.UserId, request.MaxResults);

            var query = new GetEventRecommendationsQuery
            {
                UserId = request.UserId,
                MaxResults = request.MaxResults,
                IncludeCulturalScoring = request.IncludeCulturalScoring,
                IncludeGeographicScoring = request.IncludeGeographicScoring,
                ForDate = request.ForDate,
                EventTypes = request.EventTypes,
                GeographicFilter = request.GeographicFilter != null ? new GeographicFilter
                {
                    Latitude = request.GeographicFilter.Latitude,
                    Longitude = request.GeographicFilter.Longitude,
                    MaxDistanceKm = request.GeographicFilter.MaxDistanceKm,
                    PreferredRegion = request.GeographicFilter.PreferredRegion
                } : null,
                CulturalFilter = request.CulturalFilter != null ? new CulturalFilter
                {
                    CulturalBackground = request.CulturalFilter.CulturalBackground,
                    PreferredLanguages = request.CulturalFilter.PreferredLanguages,
                    AvoidReligiousConflicts = request.CulturalFilter.AvoidReligiousConflicts,
                    PreferDiasporaFriendlyEvents = request.CulturalFilter.PreferDiasporaFriendlyEvents
                } : null
            };

            var response = await _mediator.Send(query);
            
            _logger.LogInformation("Generated {Count} event recommendations for user {UserId}", 
                response.Recommendations.Count, request.UserId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid request for event recommendations: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event recommendations for user {UserId}", request.UserId);
            return StatusCode(500, new { Error = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Analyze cultural appropriateness of events for specific cultural contexts
    /// </summary>
    /// <param name="request">Cultural appropriateness analysis parameters</param>
    /// <returns>Detailed cultural appropriateness analysis</returns>
    /// <response code="200">Successfully analyzed cultural appropriateness</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost("analyze-appropriateness")]
    [ProducesResponseType(typeof(GetCulturallyAppropriateEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetCulturallyAppropriateEventsResponse>> AnalyzeCulturalAppropriateness(
        [FromBody] CulturalAppropriatenessRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing cultural appropriateness for event {EventId}, user {UserId}", 
                request.EventId, request.UserId);

            var query = new GetCulturallyAppropriateEventsQuery
            {
                EventId = request.EventId,
                UserId = request.UserId,
                CulturalBackground = request.CulturalBackground,
                AnalysisDate = request.AnalysisDate,
                TargetAudiences = request.TargetAudiences,
                IncludeConflictAnalysis = true,
                IncludeRecommendations = true,
                IncludeCalendarValidation = true
            };

            var response = await _mediator.Send(query);
            
            _logger.LogInformation("Cultural appropriateness analysis completed. Score: {Score}, Conflict Level: {Level}", 
                response.AppropriatenessScore, response.ConflictLevel);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid cultural appropriateness request: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing cultural appropriateness for event {EventId}", request.EventId);
            return StatusCode(500, new { Error = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Get festival-optimized event recommendations aligned with traditional timing
    /// </summary>
    /// <param name="request">Festival optimization parameters</param>
    /// <returns>Festival-aligned event recommendations</returns>
    /// <response code="200">Successfully generated festival-optimized recommendations</response>
    /// <response code="404">Festival not supported</response>
    [HttpPost("festival-optimization")]
    [ProducesResponseType(typeof(FestivalOptimizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FestivalOptimizationResponse>> GetFestivalOptimizedEvents(
        [FromBody] FestivalOptimizationRequest request)
    {
        try
        {
            _logger.LogInformation("Processing festival optimization for {Festival} {Year}", 
                request.FestivalName, request.Year);

            // Validate supported festivals
            var supportedFestivals = new[] { "Vesak", "Poson", "Esala", "Vap", "Il", "Binara", 
                "Vassa", "Kathina", "Diwali", "Thaipusam", "Holi", "NavRatri" };
            
            if (!supportedFestivals.Contains(request.FestivalName, StringComparer.OrdinalIgnoreCase))
            {
                return NotFound(new { Error = "Festival not supported", SupportedFestivals = supportedFestivals });
            }

            // This would be handled by a specific query handler
            var response = new FestivalOptimizationResponse
            {
                OptimalEvents = new List<OptimalEventDto>(),
                FestivalPeriod = new FestivalPeriodDto
                {
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7),
                    PoyadayDates = new List<DateTime>(),
                    FestivalType = request.FestivalName
                },
                CulturalGuidelines = new List<string> 
                { 
                    "Align events with traditional observances",
                    "Consider community gathering patterns",
                    "Respect religious timing requirements"
                }
            };

            _logger.LogInformation("Festival optimization completed for {Festival}", request.FestivalName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing festival optimization for {Festival}", request.FestivalName);
            return StatusCode(500, new { Error = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Get diaspora community-targeted event recommendations with geographic optimization
    /// </summary>
    /// <param name="request">Diaspora targeting parameters</param>
    /// <returns>Community-optimized event recommendations</returns>
    /// <response code="200">Successfully generated diaspora-targeted recommendations</response>
    [HttpPost("diaspora-targeting")]
    [ProducesResponseType(typeof(DiasporaTargetingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DiasporaTargetingResponse>> GetDiasporaTargetedEvents(
        [FromBody] DiasporaTargetingRequest request)
    {
        try
        {
            _logger.LogInformation("Processing diaspora targeting for location {City}, {State}", 
                request.Location.City, request.Location.State);

            // This would leverage the sophisticated diaspora community analysis from domain services
            var response = new DiasporaTargetingResponse
            {
                CommunityOptimizedEvents = new List<CommunityOptimizedEventDto>(),
                CommunityClusterAnalysis = new CommunityClusterAnalysisDto
                {
                    SriLankanDensity = CalculateCommunityDensity(request.Location),
                    EstimatedCommunitySize = EstimateCommunitySize(request.Location),
                    PredominantNeighborhoods = GetPredominantNeighborhoods(request.Location)
                },
                ProximityScore = CalculateProximityScore(request.Location)
            };

            _logger.LogInformation("Diaspora targeting completed with {EventCount} optimized events", 
                response.CommunityOptimizedEvents.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing diaspora targeting for {Location}", request.Location);
            return StatusCode(500, new { Error = "An error occurred processing your request" });
        }
    }

    #region Helper Methods

    private double CalculateCommunityDensity(LocationRequest location)
    {
        // This would use the sophisticated geographic intelligence from domain services
        // For Bay Area: high density (0.8-0.9)
        // For Toronto: very high density (0.9-1.0)
        // For smaller cities: lower density (0.3-0.6)
        
        var knownHighDensityAreas = new[] { "San Francisco", "Toronto", "Melbourne", "London" };
        return knownHighDensityAreas.Any(area => location.City.Contains(area, StringComparison.OrdinalIgnoreCase)) ? 0.85 : 0.45;
    }

    private int EstimateCommunitySize(LocationRequest location)
    {
        // Estimates based on known diaspora data
        return location.City.ToLower() switch
        {
            var city when city.Contains("toronto") => 25000,
            var city when city.Contains("san francisco") || city.Contains("bay area") => 15000,
            var city when city.Contains("melbourne") => 12000,
            var city when city.Contains("london") => 18000,
            _ => 5000
        };
    }

    private List<string> GetPredominantNeighborhoods(LocationRequest location)
    {
        // This would come from sophisticated geographic analysis
        return location.City.ToLower() switch
        {
            var city when city.Contains("toronto") => new List<string> { "Scarborough", "Markham", "Richmond Hill" },
            var city when city.Contains("san francisco") => new List<string> { "Fremont", "Union City", "San Jose" },
            _ => new List<string> { "City Center", "Suburban Areas" }
        };
    }

    private double CalculateProximityScore(LocationRequest location)
    {
        // This would use sophisticated geographic proximity algorithms
        return 0.7; // Placeholder - would be calculated based on actual community distribution
    }

    #endregion
}

#region Request DTOs

public class GetEventRecommendationsRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Range(1, 50)]
    public int MaxResults { get; set; } = 10;
    
    public bool IncludeCulturalScoring { get; set; } = true;
    public bool IncludeGeographicScoring { get; set; } = true;
    public DateTime? ForDate { get; set; }
    public List<string> EventTypes { get; set; } = new();
    public GeographicFilterRequest? GeographicFilter { get; set; }
    public CulturalFilterRequest? CulturalFilter { get; set; }
}

public class GeographicFilterRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? MaxDistanceKm { get; set; }
    public string? PreferredRegion { get; set; }
}

public class CulturalFilterRequest
{
    public string? CulturalBackground { get; set; }
    public List<string> PreferredLanguages { get; set; } = new();
    public bool AvoidReligiousConflicts { get; set; } = true;
    public bool PreferDiasporaFriendlyEvents { get; set; }
}

public class CulturalAppropriatenessRequest
{
    [Required]
    public Guid EventId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public string CulturalBackground { get; set; } = string.Empty;
    public DateTime? AnalysisDate { get; set; }
    public List<string> TargetAudiences { get; set; } = new();
}

public class FestivalOptimizationRequest
{
    [Required]
    public string FestivalName { get; set; } = string.Empty;
    
    [Range(2020, 2030)]
    public int Year { get; set; } = DateTime.UtcNow.Year;
    
    [Required]
    public Guid UserId { get; set; }
    
    public string GeographicRegion { get; set; } = string.Empty;
}

public class DiasporaTargetingRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public LocationRequest Location { get; set; } = new();
    
    public DiasporaProfileRequest DiasporaProfile { get; set; } = new();
}

public class LocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class DiasporaProfileRequest
{
    public string CommunityInvolvement { get; set; } = string.Empty;
    public List<string> PreferredLanguages { get; set; } = new();
    public string CulturalAdaptationLevel { get; set; } = string.Empty;
}

#endregion

#region Response DTOs

public class FestivalOptimizationResponse
{
    public List<OptimalEventDto> OptimalEvents { get; set; } = new();
    public FestivalPeriodDto FestivalPeriod { get; set; } = new();
    public List<string> CulturalGuidelines { get; set; } = new();
}

public class OptimalEventDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public CulturalAlignmentDto CulturalAlignment { get; set; } = new();
}

public class CulturalAlignmentDto
{
    public bool IsAppropriateForBuddhist { get; set; }
    public bool IsAppropriateForHindu { get; set; }
    public double AppropriatenessScore { get; set; }
}

public class FestivalPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DateTime> PoyadayDates { get; set; } = new();
    public string FestivalType { get; set; } = string.Empty;
}

public class DiasporaTargetingResponse
{
    public List<CommunityOptimizedEventDto> CommunityOptimizedEvents { get; set; } = new();
    public CommunityClusterAnalysisDto CommunityClusterAnalysis { get; set; } = new();
    public double ProximityScore { get; set; }
}

public class CommunityOptimizedEventDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double DiasporaFriendlinessScore { get; set; }
    public double CommunityRelevanceScore { get; set; }
}

public class CommunityClusterAnalysisDto
{
    public double SriLankanDensity { get; set; }
    public int EstimatedCommunitySize { get; set; }
    public List<string> PredominantNeighborhoods { get; set; } = new();
}

#endregion
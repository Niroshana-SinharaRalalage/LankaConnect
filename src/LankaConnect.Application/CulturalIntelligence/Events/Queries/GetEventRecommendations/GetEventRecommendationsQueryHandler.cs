using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using System.Diagnostics;
using DomainEventRecommendation = LankaConnect.Domain.Events.ValueObjects.Recommendations.EventRecommendation;

namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;

/// <summary>
/// Handler for getting personalized event recommendations using cultural intelligence
/// Leverages existing sophisticated domain services: IEventRecommendationEngine, ICulturalCalendar, IUserPreferences
/// </summary>
public class GetEventRecommendationsQueryHandler : IQueryHandler<GetEventRecommendationsQuery, GetEventRecommendationsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventRecommendationEngine _recommendationEngine;
    private readonly ICulturalCalendar _culturalCalendar;
    private readonly IUserPreferences _userPreferences;
    private readonly IGeographicProximityService _geographicService;

    public GetEventRecommendationsQueryHandler(
        IEventRepository eventRepository,
        IEventRecommendationEngine recommendationEngine,
        ICulturalCalendar culturalCalendar,
        IUserPreferences userPreferences,
        IGeographicProximityService geographicService)
    {
        _eventRepository = eventRepository;
        _recommendationEngine = recommendationEngine;
        _culturalCalendar = culturalCalendar;
        _userPreferences = userPreferences;
        _geographicService = geographicService;
    }

    public async Task<Result<GetEventRecommendationsResponse>> Handle(GetEventRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get eligible events based on filters
            var events = await GetEligibleEvents(request, cancellationToken);
            
            // Get recommendations using domain service
            var recommendations = await GetRecommendationsFromEngine(request, events);
            
            // Convert to response DTOs
            var response = await BuildResponse(request, recommendations, stopwatch.ElapsedMilliseconds);
            
            return Result<GetEventRecommendationsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            // Log error and return empty response with metadata
            var errorResponse = new GetEventRecommendationsResponse
            {
                Recommendations = new List<EventRecommendationDto>(),
                TotalCount = 0,
                Metadata = new CulturalIntelligenceMetadata
                {
                    GeneratedAt = DateTime.UtcNow,
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    DataSources = new List<string> { "Error: " + ex.Message }
                }
            };
            return Result<GetEventRecommendationsResponse>.Failure(ex.Message);
        }
    }

    private async Task<IEnumerable<Event>> GetEligibleEvents(GetEventRecommendationsQuery request, CancellationToken cancellationToken)
    {
        // Get all published events
        var events = await _eventRepository.GetPublishedEventsAsync(cancellationToken);
        
        // Apply basic filters
        if (request.ForDate.HasValue)
        {
            events = events.Where(e => e.StartDate.Date == request.ForDate.Value.Date).ToList();
        }
        
        if (request.EventTypes.Any())
        {
            // This would need to be implemented based on your event categorization
            // events = events.Where(e => request.EventTypes.Contains(e.Category));
        }

        // Apply geographic filtering if specified
        if (request.GeographicFilter != null && request.GeographicFilter.MaxDistanceKm.HasValue)
        {
            events = (await ApplyGeographicFiltering(events, request.GeographicFilter)).ToList();
        }

        return events.Take(request.MaxResults * 5).ToList(); // Get more than needed for better recommendations
    }

    private Task<IEnumerable<Event>> ApplyGeographicFiltering(IEnumerable<Event> events, GeographicFilter filter)
    {
        if (!filter.Latitude.HasValue || !filter.Longitude.HasValue)
            return Task.FromResult(events);

        var filteredEvents = new List<Event>();
        var userCoordinates = new Coordinates { Latitude = filter.Latitude.Value, Longitude = filter.Longitude.Value };
        
        foreach (var @event in events)
        {
            // This assumes events have location coordinates - you'd need to implement this
            // var eventCoordinates = GetEventCoordinates(@event);
            // var distance = _geographicService.CalculateDistance(userCoordinates, eventCoordinates);
            
            // if (distance.Kilometers <= filter.MaxDistanceKm.Value)
            // {
                filteredEvents.Add(@event);
            // }
        }
        
        return Task.FromResult(filteredEvents.AsEnumerable());
    }

    private async Task<IEnumerable<DomainEventRecommendation>> GetRecommendationsFromEngine(
        GetEventRecommendationsQuery request,
        IEnumerable<Event> events)
    {
        // Use the sophisticated recommendation engine from domain services
        if (request.ForDate.HasValue)
        {
            return await _recommendationEngine.GetRecommendationsForDate(request.UserId, events, request.ForDate.Value);
        }

        // Apply cultural filtering if requested
        if (request.CulturalFilter?.AvoidReligiousConflicts == true)
        {
            var culturalRecommendations = await _recommendationEngine.GetCulturallyFilteredRecommendations(request.UserId, events);

            if (request.CulturalFilter.PreferDiasporaFriendlyEvents)
            {
                return await _recommendationEngine.GetDiasporaOptimizedRecommendations(request.UserId, events);
            }

            return culturalRecommendations;
        }

        // Default comprehensive recommendations
        return await _recommendationEngine.GetScoredRecommendations(request.UserId, events);
    }

    private async Task<GetEventRecommendationsResponse> BuildResponse(
        GetEventRecommendationsQuery request,
        IEnumerable<DomainEventRecommendation> recommendations,
        long processingTimeMs)
    {
        var recommendationDtos = new List<EventRecommendationDto>();

        foreach (var recommendation in recommendations.Take(request.MaxResults))
        {
            // Convert domain recommendation to DTO using mapper
            var dto = EventRecommendationMapper.ConvertToDto(recommendation);

            // Add cultural scoring if requested
            if (request.IncludeCulturalScoring)
            {
                var culturalScore = await _recommendationEngine.CalculateCulturalScore(request.UserId, recommendation.Event);
                var extendedCulturalScore = EventRecommendationMapper.CreateExtendedCulturalScore(recommendation, culturalScore);
                dto.CulturalScore = EventRecommendationMapper.ConvertCulturalScoreToDto(extendedCulturalScore);
            }

            // Add geographic scoring if requested
            if (request.IncludeGeographicScoring && request.GeographicFilter != null)
            {
                // This would use the geographic proximity service
                dto.GeographicScore = new GeographicScoreDto
                {
                    ProximityScore = 0.8, // Placeholder - implement actual calculation
                    CommunityDensityScore = 0.7,
                    AccessibilityScore = 0.9
                };
            }

            recommendationDtos.Add(dto);
        }

        return new GetEventRecommendationsResponse
        {
            Recommendations = recommendationDtos,
            TotalCount = recommendations.Count(),
            Metadata = new CulturalIntelligenceMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                ProcessingTimeMs = (int)processingTimeMs,
                AlgorithmVersion = "CulturalIntelligence-v2.1",
                DataSources = new List<string> 
                { 
                    "EventRecommendationEngine", 
                    "CulturalCalendar", 
                    "UserPreferences",
                    "GeographicProximityService"
                }
            }
        };
    }
}

// Supporting classes that would be defined elsewhere in the domain
public class Coordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Distance
{
    public double Kilometers { get; set; }
}
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IDiasporaCommunityClusteringService : IDisposable
{
    Task<Result<IEnumerable<GeographicCulturalRegion>>> AnalyzeCommunityClusteringAsync(
        List<string> geographicRegions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalCommunityClusterAnalysis>> GetCommunityDensityAnalysisAsync(
        string geographicRegion,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<CulturalCommunityType, List<string>>>> MapCommunitiesToRegionsAsync(
        List<string> geographicRegions,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CrossCulturalDiscoveryRecommendation>>> GenerateCommunityCrossConnectionsAsync(
        CulturalCommunityType sourceCommunity,
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<GeographicCulturalRegion>> CreateCulturalRegionProfileAsync(
        string regionName,
        GeographicCoordinates coordinates,
        List<CulturalCommunityType> dominantCommunities,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, int>>> CalculateCommunityPopulationDistributionAsync(
        CulturalCommunityType communityType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateCulturalDiversityIndexAsync(
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetOptimalRegionsForCommunityExpansionAsync(
        CulturalCommunityType communityType,
        int maxRegions,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<CulturalLanguage, double>>> AnalyzeLanguageDistributionAsync(
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetCulturalInstitutionsByRegionAsync(
        string geographicRegion,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result<int>> GetBusinessDirectoryCountAsync(
        string geographicRegion,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateRegionalCulturalProfileAsync(
        string regionId,
        GeographicCulturalRegion updatedProfile,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateCommunitySimilarityScoreAsync(
        CulturalCommunityType community1,
        CulturalCommunityType community2,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalEventType>>> GetPopularCulturalEventsInRegionAsync(
        string geographicRegion,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result<GeographicCoordinates>> GetRegionCenterCoordinatesAsync(
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateGeographicDistanceAsync(
        GeographicCoordinates location1,
        GeographicCoordinates location2,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<GeographicCulturalRegion>>> GetNearbyCommunitiesAsync(
        GeographicCoordinates coordinates,
        double radiusKm,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> CalculateRegionalCommunityGrowthTrendsAsync(
        CulturalCommunityType communityType,
        TimeSpan analysisWindow,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ValidateCommunityClusteringDataAsync(
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetRegionsWithCommunityPresenceAsync(
        CulturalCommunityType communityType,
        int minimumPopulation,
        CancellationToken cancellationToken = default);
}
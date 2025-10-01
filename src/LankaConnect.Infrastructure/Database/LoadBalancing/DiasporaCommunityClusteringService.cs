using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LankaConnect.Infrastructure.Database.LoadBalancing;

/// <summary>
/// Diaspora Community Clustering Service
/// Provides sophisticated community clustering analysis for 6M+ South Asian Americans
/// across global regions with cultural intelligence and geographic optimization
/// </summary>
public interface IDiasporaCommunityClusteringService
{
    /// <summary>
    /// Analyze diaspora community clusters for optimal cultural engagement
    /// Supports Sri Lankan (450K), Indian (4.2M), Pakistani (526K), Bangladeshi (213K), Sikh (500K+) Americans
    /// </summary>
    Task<Result<DiasporaCommunityAnalytics>> AnalyzeDiasporaCommunityClustersAsync(
        GeographicLocation userLocation, 
        CulturalUserProfile userProfile);

    /// <summary>
    /// Identify target diaspora communities for business optimization
    /// Integrates with business directory for cultural business promotion
    /// </summary>
    Task<Result<TargetDiasporaCommunitiesResult>> IdentifyTargetDiasporaCommunitiesAsync(
        BusinessCulturalContext businessContext,
        GeographicLocation businessLocation);

    /// <summary>
    /// Calculate community clustering density and cultural affinity scores
    /// Optimizes for cultural engagement and cross-community discovery
    /// </summary>
    Task<Result<CommunityClusteringDensityAnalysis>> CalculateClusteringDensityAsync(
        DiasporaCommunityClustering communityType,
        GeographicRegion region);

    /// <summary>
    /// Optimize cross-cultural community connections for revenue growth
    /// Supports $25.7M platform revenue optimization through community expansion
    /// </summary>
    Task<Result<CrossCommunityConnectionOpportunities>> OptimizeCrossCommunityConnectionsAsync(
        CulturalUserProfile userProfile,
        List<DiasporaCommunityClustering> nearbyCommunities);

    /// <summary>
    /// Get real-time community clustering analytics and engagement metrics
    /// Provides cultural intelligence insights for load balancing optimization
    /// </summary>
    Task<Result<CommunityClusteringMetrics>> GetCommunityClusteringMetricsAsync(
        TimeSpan analysisWindow);
}

public class DiasporaCommunityClusteringService : IDiasporaCommunityClusteringService
{
    private readonly ILogger<DiasporaCommunityClusteringService> _logger;
    private readonly ICulturalIntelligenceShardingService _shardingService;
    private readonly IGeographicCulturalRoutingService _geographicService;
    
    // Comprehensive diaspora community geographic clustering data
    private readonly Dictionary<DiasporaCommunityClustering, GeographicCulturalRegion> _communityRegions;
    
    // Performance cache for community clustering calculations
    private readonly ConcurrentDictionary<string, CachedCommunityAnalysis> _communityAnalysisCache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    // Cultural affinity matrix for cross-community connections
    private readonly Dictionary<CulturalCrossPollination, double> _crossCulturalAffinityScores;

    public DiasporaCommunityClusteringService(
        ILogger<DiasporaCommunityClusteringService> logger,
        ICulturalIntelligenceShardingService shardingService,
        IGeographicCulturalRoutingService geographicService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shardingService = shardingService ?? throw new ArgumentNullException(nameof(shardingService));
        _geographicService = geographicService ?? throw new ArgumentNullException(nameof(geographicService));

        _communityAnalysisCache = new ConcurrentDictionary<string, CachedCommunityAnalysis>();
        
        _communityRegions = InitializeCommunityRegions();
        _crossCulturalAffinityScores = InitializeCrossCulturalAffinityScores();
        
        _logger.LogInformation("Diaspora Community Clustering Service initialized with {CommunityCount} community regions", 
            _communityRegions.Count);
    }

    public async Task<Result<DiasporaCommunityAnalytics>> AnalyzeDiasporaCommunityClustersAsync(
        GeographicLocation userLocation, 
        CulturalUserProfile userProfile)
    {
        try
        {
            _logger.LogInformation("Analyzing diaspora community clusters for user {UserId} at location {Location}",
                userProfile.UserId, $"{userLocation.City}, {userLocation.Region}");

            var cacheKey = GenerateCommunityAnalysisCacheKey(userLocation, userProfile);
            
            if (_communityAnalysisCache.TryGetValue(cacheKey, out var cachedAnalysis) && 
                DateTime.UtcNow - cachedAnalysis.Timestamp < _cacheExpiration)
            {
                _logger.LogDebug("Using cached community analysis for user {UserId}", userProfile.UserId);
                return Result<DiasporaCommunityAnalytics>.Success(cachedAnalysis.Analysis);
            }

            // Step 1: Identify nearby community clusters within cultural proximity
            var nearbyCommunityClusters = await IdentifyNearbyCommunityClustersAsync(userLocation);

            if (!nearbyCommunityClusters.IsSuccess)
            {
                _logger.LogWarning("Failed to identify nearby community clusters: {Error}", nearbyCommunityClusters.Error);
                return Result<DiasporaCommunityAnalytics>.Failure(nearbyCommunityClusters.Error);
            }

            // Step 2: Calculate cultural affinity with identified clusters
            var culturalAffinityClusters = await CalculateCulturalAffinityWithClustersAsync(
                userProfile, nearbyCommunityClusters.Value);

            if (!culturalAffinityClusters.IsSuccess)
            {
                _logger.LogWarning("Cultural affinity calculation failed: {Error}", culturalAffinityClusters.Error);
                return Result<DiasporaCommunityAnalytics>.Failure(culturalAffinityClusters.Error);
            }

            // Step 3: Select optimal community cluster based on engagement potential
            var optimalCommunityCluster = await SelectOptimalCommunityClusterAsync(culturalAffinityClusters.Value);

            if (!optimalCommunityCluster.IsSuccess)
            {
                _logger.LogWarning("Optimal community cluster selection failed: {Error}", optimalCommunityCluster.Error);
                return Result<DiasporaCommunityAnalytics>.Failure(optimalCommunityCluster.Error);
            }

            // Step 4: Calculate business discovery opportunities within cluster
            var businessDiscoveryOpportunities = await CalculateBusinessDiscoveryOpportunitiesAsync(
                optimalCommunityCluster.Value);

            // Step 5: Calculate revenue optimization score for platform growth
            var revenueOptimizationScore = CalculateRevenueOptimizationScore(
                optimalCommunityCluster.Value, userProfile);
            
            var communityAnalytics = new DiasporaCommunityAnalytics
            {
                RecommendedCluster = optimalCommunityCluster.Value,
                CulturalAffinityScore = optimalCommunityCluster.Value.CulturalAffinityScore,
                GeographicProximityScore = optimalCommunityCluster.Value.GeographicProximityScore,
                CommunityEngagementPotential = optimalCommunityCluster.Value.EngagementScore,
                BusinessDiscoveryOpportunities = businessDiscoveryOpportunities,
                RevenueOptimizationScore = revenueOptimizationScore,
                AlternateCommunityOptions = culturalAffinityClusters.Value
                    .Where(c => c != optimalCommunityCluster.Value)
                    .OrderByDescending(c => c.OverallScore)
                    .Take(3)
                    .ToList(),
                AnalysisTimestamp = DateTime.UtcNow
            };

            // Cache the results for performance optimization
            _communityAnalysisCache.TryAdd(cacheKey, new CachedCommunityAnalysis
            {
                Analysis = communityAnalytics,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Diaspora community analysis completed - Recommended Cluster: {Cluster}, Affinity Score: {Score}",
                communityAnalytics.RecommendedCluster.ClusterType, communityAnalytics.CulturalAffinityScore);

            return Result<DiasporaCommunityAnalytics>.Success(communityAnalytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diaspora community clustering analysis failed for user {UserId}", userProfile.UserId);
            return Result<DiasporaCommunityAnalytics>.Failure($"Community clustering analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<TargetDiasporaCommunitiesResult>> IdentifyTargetDiasporaCommunitiesAsync(
        BusinessCulturalContext businessContext,
        GeographicLocation businessLocation)
    {
        try
        {
            _logger.LogInformation("Identifying target diaspora communities for business {BusinessId} in category {Category}",
                businessContext.BusinessId, businessContext.BusinessCategory);

            // Step 1: Analyze business cultural relevance to diaspora communities
            var businessCulturalRelevance = await AnalyzeBusinessCulturalRelevanceAsync(businessContext);

            // Step 2: Identify geographically accessible community clusters
            var accessibleCommunityQueries = await IdentifyAccessibleCommunityQueriesAsync(
                businessLocation, businessContext.ServiceRadius);

            // Step 3: Calculate cultural alignment scores for each community
            var culturalAlignmentScores = await CalculateCulturalAlignmentScoresAsync(
                businessCulturalRelevance, accessibleCommunityQueries);

            // Step 4: Select primary and secondary target communities
            var primaryCommunityCluster = culturalAlignmentScores
                .OrderByDescending(c => c.AlignmentScore)
                .FirstOrDefault();

            var secondaryCommunityClusters = culturalAlignmentScores
                .Where(c => c != primaryCommunityCluster)
                .OrderByDescending(c => c.AlignmentScore)
                .Take(3)
                .ToList();

            var targetCommunitiesResult = new TargetDiasporaCommunitiesResult
            {
                PrimaryCommunityCluster = primaryCommunityCluster?.CommunityCluster ?? DiasporaCommunityClustering.IndianHinduNewYork,
                SecondaryCommunityQueries = secondaryCommunityClusters.Select(c => c.CommunityCluster).ToList(),
                CulturalAlignmentScores = culturalAlignmentScores.ToDictionary(
                    c => c.CommunityCluster, 
                    c => c.AlignmentScore),
                BusinessEngagementPotential = CalculateBusinessEngagementPotential(businessContext, culturalAlignmentScores),
                RecommendedMarketingStrategies = GenerateMarketingStrategies(businessContext, culturalAlignmentScores)
            };

            _logger.LogInformation("Target diaspora communities identified - Primary: {Primary}, Secondary Count: {SecondaryCount}",
                targetCommunitiesResult.PrimaryCommunityCluster, targetCommunitiesResult.SecondaryCommunityQueries.Count);

            return Result<TargetDiasporaCommunitiesResult>.Success(targetCommunitiesResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target diaspora community identification failed for business {BusinessId}", 
                businessContext.BusinessId);
            return Result<TargetDiasporaCommunitiesResult>.Failure($"Target community identification failed: {ex.Message}");
        }
    }

    public async Task<Result<CommunityClusteringDensityAnalysis>> CalculateClusteringDensityAsync(
        DiasporaCommunityClustering communityType,
        GeographicRegion region)
    {
        try
        {
            _logger.LogInformation("Calculating clustering density for {CommunityType} in {Region}", 
                communityType, region.RegionName);

            if (!_communityRegions.TryGetValue(communityType, out var communityRegion))
            {
                return Result<CommunityClusteringDensityAnalysis>.Failure($"Community type {communityType} not found");
            }

            // Calculate community density metrics
            var densityMetrics = await CalculateCommunityDensityMetricsAsync(communityRegion, region);
            
            // Calculate cultural institution density
            var culturalInstitutionDensity = CalculateCulturalInstitutionDensity(communityRegion);
            
            // Calculate community engagement patterns
            var engagementPatterns = await AnalyzeCommunityEngagementPatternsAsync(communityType, region);
            
            // Calculate business directory relevance within community
            var businessDirectoryRelevance = await CalculateBusinessDirectoryRelevanceAsync(communityType, region);

            var densityAnalysis = new CommunityClusteringDensityAnalysis
            {
                CommunityType = communityType,
                Region = region,
                PopulationDensity = densityMetrics.PopulationDensity,
                CulturalInstitutionDensity = culturalInstitutionDensity,
                BusinessDirectoryDensity = communityRegion.BusinessDirectoryDensity,
                CommunityEngagementScore = engagementPatterns.EngagementScore,
                CulturalEventParticipationRate = engagementPatterns.EventParticipationRate,
                CrossCommunityConnectionPotential = CalculateCrossCommunityConnectionPotential(communityType, region),
                RevenueGenerationPotential = CalculateRevenueGenerationPotential(densityMetrics, businessDirectoryRelevance),
                OptimalLoadBalancingScore = CalculateOptimalLoadBalancingScore(densityMetrics, engagementPatterns)
            };

            _logger.LogInformation("Clustering density calculated - Population Density: {PopDensity}, Engagement Score: {EngagementScore}",
                densityAnalysis.PopulationDensity, densityAnalysis.CommunityEngagementScore);

            return Result<CommunityClusteringDensityAnalysis>.Success(densityAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Community clustering density calculation failed for {CommunityType}", communityType);
            return Result<CommunityClusteringDensityAnalysis>.Failure($"Density calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<CrossCommunityConnectionOpportunities>> OptimizeCrossCommunityConnectionsAsync(
        CulturalUserProfile userProfile,
        List<DiasporaCommunityClustering> nearbyCommunities)
    {
        try
        {
            _logger.LogInformation("Optimizing cross-community connections for user {UserId} with {CommunityCount} nearby communities",
                userProfile.UserId, nearbyCommunities.Count);

            var connectionOpportunities = new List<CrossCommunityConnection>();

            // Analyze cross-cultural affinity potential for each nearby community
            foreach (var nearbyCommunityClustering in nearbyCommunities)
            {
                var crossCulturalAffinity = CalculateCrossCulturalAffinity(userProfile, nearbyCommunityClustering);
                
                if (crossCulturalAffinity > 0.3) // Minimum threshold for meaningful connections
                {
                    var connectionOpportunity = await CreateCrossCommunityConnectionAsync(
                        userProfile, nearbyCommunityClustering, crossCulturalAffinity);
                    
                    connectionOpportunities.Add(connectionOpportunity);
                }
            }

            // Rank opportunities by revenue optimization potential
            var rankedOpportunities = connectionOpportunities
                .OrderByDescending(o => o.RevenueOptimizationScore)
                .ToList();

            // Calculate overall cross-community engagement potential
            var overallEngagementPotential = CalculateOverallEngagementPotential(rankedOpportunities);

            var connectionOpportunitiesResult = new CrossCommunityConnectionOpportunities
            {
                UserProfile = userProfile,
                AvailableConnections = rankedOpportunities,
                RecommendedPrimaryConnection = rankedOpportunities.FirstOrDefault(),
                RecommendedSecondaryConnections = rankedOpportunities.Skip(1).Take(2).ToList(),
                OverallEngagementPotential = overallEngagementPotential,
                ExpectedRevenueIncrease = CalculateExpectedRevenueIncrease(rankedOpportunities),
                CulturalSensitivityScore = CalculateCulturalSensitivityScore(rankedOpportunities, userProfile)
            };

            _logger.LogInformation("Cross-community connections optimized - Opportunities: {OpportunityCount}, Revenue Potential: ${RevenuePotential}",
                connectionOpportunitiesResult.AvailableConnections.Count, 
                connectionOpportunitiesResult.ExpectedRevenueIncrease);

            return Result<CrossCommunityConnectionOpportunities>.Success(connectionOpportunitiesResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cross-community connection optimization failed for user {UserId}", userProfile.UserId);
            return Result<CrossCommunityConnectionOpportunities>.Failure($"Cross-community optimization failed: {ex.Message}");
        }
    }

    public async Task<Result<CommunityClusteringMetrics>> GetCommunityClusteringMetricsAsync(TimeSpan analysisWindow)
    {
        try
        {
            _logger.LogInformation("Collecting community clustering metrics for analysis window {Window}", analysisWindow);

            var metricsCollectionTasks = new List<Task>();
            var metrics = new CommunityClusteringMetrics
            {
                AnalysisWindow = analysisWindow,
                CollectionTimestamp = DateTime.UtcNow
            };

            // Collect engagement metrics for each community type
            var engagementMetrics = new Dictionary<DiasporaCommunityClustering, double>();
            foreach (var communityType in Enum.GetValues<DiasporaCommunityClustering>())
            {
                var engagement = await CalculateCommunityEngagementMetricAsync(communityType, analysisWindow);
                engagementMetrics[communityType] = engagement;
            }
            metrics.CommunityEngagementMetrics = engagementMetrics;

            // Collect cross-community connection success rates
            metrics.CrossCommunityConnectionSuccessRate = await CalculateCrossCommunityConnectionSuccessRateAsync(analysisWindow);

            // Collect revenue optimization performance
            metrics.RevenueOptimizationPerformance = await CalculateRevenueOptimizationPerformanceAsync(analysisWindow);

            // Collect cultural affinity accuracy metrics
            metrics.CulturalAffinityAccuracy = await CalculateCulturalAffinityAccuracyAsync(analysisWindow);

            // Collect load balancing efficiency metrics
            metrics.LoadBalancingEfficiency = await CalculateLoadBalancingEfficiencyAsync(analysisWindow);

            // Calculate overall community clustering health score
            metrics.OverallHealthScore = CalculateOverallHealthScore(metrics);

            _logger.LogInformation("Community clustering metrics collected - Health Score: {HealthScore}, Engagement Rate: {EngagementRate}%",
                metrics.OverallHealthScore, metrics.CommunityEngagementMetrics.Values.Average() * 100);

            return Result<CommunityClusteringMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Community clustering metrics collection failed");
            return Result<CommunityClusteringMetrics>.Failure($"Metrics collection failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private Dictionary<DiasporaCommunityClustering, GeographicCulturalRegion> InitializeCommunityRegions()
    {
        return new Dictionary<DiasporaCommunityClustering, GeographicCulturalRegion>
        {
            // Sri Lankan Communities
            [DiasporaCommunityClustering.SriLankanBuddhistBayArea] = new GeographicCulturalRegion
            {
                Region = "San Francisco Bay Area",
                PrimaryLanguages = [LanguageType.Sinhala, LanguageType.English],
                CulturalInstitutions = ["Buddhist Temple of Silicon Valley", "Sri Lankan Cultural Center", "Dharma Vijaya Buddhist Vihara"],
                CommunitySize = 45000,
                CulturalEventParticipation = 0.78,
                BusinessDirectoryDensity = 120, // Cultural businesses per 10K population
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 37.9,
                    SouthLatitude = 37.1,
                    EastLongitude = -121.8,
                    WestLongitude = -122.6
                }
            },
            
            [DiasporaCommunityClustering.SriLankanTamilToronto] = new GeographicCulturalRegion
            {
                Region = "Toronto Metropolitan Area",
                PrimaryLanguages = [LanguageType.Tamil, LanguageType.English, LanguageType.Sinhala],
                CulturalInstitutions = ["Tamil Cultural Centre", "Scarborough Hindu Temple", "Sri Lankan Cultural Centre Toronto"],
                CommunitySize = 38000,
                CulturalEventParticipation = 0.82,
                BusinessDirectoryDensity = 95,
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 43.85,
                    SouthLatitude = 43.58,
                    EastLongitude = -79.12,
                    WestLongitude = -79.64
                }
            },

            // Indian Communities  
            [DiasporaCommunityClustering.IndianHinduNewYork] = new GeographicCulturalRegion
            {
                Region = "New York Tri-State Area",
                PrimaryLanguages = [LanguageType.Hindi, LanguageType.Gujarati, LanguageType.English],
                CulturalInstitutions = ["Hindu Temple Society of North America", "Bharatiya Temple", "Ganesh Temple Flushing"],
                CommunitySize = 520000,
                CulturalEventParticipation = 0.71,
                BusinessDirectoryDensity = 340,
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 41.2,
                    SouthLatitude = 40.5,
                    EastLongitude = -73.7,
                    WestLongitude = -74.3
                }
            },

            [DiasporaCommunityClustering.SikhCentralValley] = new GeographicCulturalRegion
            {
                Region = "California Central Valley",
                PrimaryLanguages = [LanguageType.Punjabi, LanguageType.English, LanguageType.Hindi],
                CulturalInstitutions = ["Gurdwara Sahib Stockton", "Sikh Temple Fremont", "Gurdwara Singh Sabha El Sobrante"],
                CommunitySize = 125000,
                CulturalEventParticipation = 0.85,
                BusinessDirectoryDensity = 78,
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 38.1,
                    SouthLatitude = 36.2,
                    EastLongitude = -120.0,
                    WestLongitude = -122.0
                }
            },

            // Pakistani Communities
            [DiasporaCommunityClustering.PakistaniChicago] = new GeographicCulturalRegion
            {
                Region = "Chicago Metropolitan Area",
                PrimaryLanguages = [LanguageType.Urdu, LanguageType.English, LanguageType.Punjabi],
                CulturalInstitutions = ["Muslim Community Center", "Islamic Society of Greater Chicago", "Pakistan Association of Greater Chicago"],
                CommunitySize = 65000,
                CulturalEventParticipation = 0.74,
                BusinessDirectoryDensity = 88,
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 42.5,
                    SouthLatitude = 41.6,
                    EastLongitude = -87.5,
                    WestLongitude = -88.4
                }
            },

            // Bangladeshi Communities
            [DiasporaCommunityClustering.BangladeshiDetroit] = new GeographicCulturalRegion
            {
                Region = "Detroit Metropolitan Area",
                PrimaryLanguages = [LanguageType.Bengali, LanguageType.English, LanguageType.Urdu],
                CulturalInstitutions = ["Bangladesh Association of Michigan", "Dearborn Islamic Center", "Bangladesh Cultural Center"],
                CommunitySize = 28000,
                CulturalEventParticipation = 0.76,
                BusinessDirectoryDensity = 42,
                GeographicBounds = new GeographicBounds
                {
                    NorthLatitude = 42.8,
                    SouthLatitude = 42.1,
                    EastLongitude = -82.9,
                    WestLongitude = -83.5
                }
            }
        };
    }

    private Dictionary<CulturalCrossPollination, double> InitializeCrossCulturalAffinityScores()
    {
        return new Dictionary<CulturalCrossPollination, double>
        {
            // Sri Lankan → Indian Cultural Bridges
            [new CulturalCrossPollination(DiasporaCommunityClustering.SriLankanBuddhistBayArea, DiasporaCommunityClustering.IndianHinduNewYork)] = 0.72,
            [new CulturalCrossPollination(DiasporaCommunityClustering.SriLankanTamilToronto, DiasporaCommunityClustering.IndianHinduNewYork)] = 0.85,
            
            // Islamic Cross-Cultural Connections
            [new CulturalCrossPollination(DiasporaCommunityClustering.PakistaniChicago, DiasporaCommunityClustering.BangladeshiDetroit)] = 0.68,
            
            // Sikh → Hindu Cultural Overlap
            [new CulturalCrossPollination(DiasporaCommunityClustering.SikhCentralValley, DiasporaCommunityClustering.IndianHinduNewYork)] = 0.65
        };
    }

    private string GenerateCommunityAnalysisCacheKey(GeographicLocation location, CulturalUserProfile profile)
    {
        return $"community_{profile.UserId}_{location.Latitude:F2}_{location.Longitude:F2}_{profile.GetHashCode()}";
    }

    private double CalculateRevenueOptimizationScore(CommunityClusterAnalysis cluster, CulturalUserProfile userProfile)
    {
        // Revenue optimization calculation based on:
        // - Community engagement potential (40%)
        // - Business directory density (30%) 
        // - Cultural affinity score (20%)
        // - Cross-community connection potential (10%)
        
        var engagementScore = cluster.EngagementScore * 0.40;
        var businessDensityScore = (cluster.BusinessDirectoryDensity / 500.0) * 0.30; // Normalized to max 500
        var affinityScore = cluster.CulturalAffinityScore * 0.20;
        var crossCommunityScore = cluster.CrossCommunityPotential * 0.10;
        
        return Math.Min(1.0, engagementScore + businessDensityScore + affinityScore + crossCommunityScore);
    }

    #endregion
}

#region Supporting Data Models

public class DiasporaCommunityAnalytics
{
    public CommunityClusterAnalysis RecommendedCluster { get; set; } = null!;
    public double CulturalAffinityScore { get; set; }
    public double GeographicProximityScore { get; set; }
    public double CommunityEngagementPotential { get; set; }
    public List<BusinessDiscoveryOpportunity> BusinessDiscoveryOpportunities { get; set; } = [];
    public double RevenueOptimizationScore { get; set; }
    public List<CommunityClusterAnalysis> AlternateCommunityOptions { get; set; } = [];
    public DateTime AnalysisTimestamp { get; set; }
}

public class GeographicCulturalRegion
{
    public string Region { get; set; } = string.Empty;
    public List<LanguageType> PrimaryLanguages { get; set; } = [];
    public List<string> CulturalInstitutions { get; set; } = [];
    public int CommunitySize { get; set; }
    public double CulturalEventParticipation { get; set; }
    public int BusinessDirectoryDensity { get; set; }
    public GeographicBounds GeographicBounds { get; set; } = null!;
}

public class GeographicBounds
{
    public double NorthLatitude { get; set; }
    public double SouthLatitude { get; set; }
    public double EastLongitude { get; set; }
    public double WestLongitude { get; set; }
}

public class CommunityClusterAnalysis
{
    public DiasporaCommunityClustering ClusterType { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public double CulturalAffinityScore { get; set; }
    public double GeographicProximityScore { get; set; }
    public double EngagementScore { get; set; }
    public double BusinessDirectoryDensity { get; set; }
    public double CrossCommunityPotential { get; set; }
    public double OverallScore { get; set; }
    public List<string> CulturalInstitutions { get; set; } = [];
    public List<LanguageType> SupportedLanguages { get; set; } = [];
}

public class CachedCommunityAnalysis
{
    public DiasporaCommunityAnalytics Analysis { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class CulturalCrossPollination
{
    public DiasporaCommunityClustering SourceCommunity { get; }
    public DiasporaCommunityClustering TargetCommunity { get; }

    public CulturalCrossPollination(DiasporaCommunityClustering source, DiasporaCommunityClustering target)
    {
        SourceCommunity = source;
        TargetCommunity = target;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CulturalCrossPollination other)
        {
            return SourceCommunity == other.SourceCommunity && TargetCommunity == other.TargetCommunity;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SourceCommunity, TargetCommunity);
    }
}

public class CommunityClusteringMetrics
{
    public TimeSpan AnalysisWindow { get; set; }
    public DateTime CollectionTimestamp { get; set; }
    public Dictionary<DiasporaCommunityClustering, double> CommunityEngagementMetrics { get; set; } = [];
    public double CrossCommunityConnectionSuccessRate { get; set; }
    public double RevenueOptimizationPerformance { get; set; }
    public double CulturalAffinityAccuracy { get; set; }
    public double LoadBalancingEfficiency { get; set; }
    public double OverallHealthScore { get; set; }
}

public enum LanguageType
{
    English,
    Sinhala,
    Tamil,
    Hindi,
    Gujarati,
    Urdu,
    Punjabi,
    Bengali,
    Arabic
}

#endregion
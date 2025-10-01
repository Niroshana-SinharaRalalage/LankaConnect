using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Diaspora Notification Service - Geographic Community Targeting Implementation
/// Provides sophisticated geographic clustering, community analytics, and diaspora engagement optimization
/// for WhatsApp Business API broadcasting to Sri Lankan communities worldwide
/// </summary>
public class DiasporaNotificationService : IDiasporaNotificationService
{
    private readonly IGeographicTimeZoneService _timeZoneService;
    private readonly ICulturalCalendarService _culturalCalendarService;
    private readonly IDiasporaAnalyticsService _analyticsService;

    // Pre-defined diaspora clusters based on known Sri Lankan community concentrations
    private static readonly Dictionary<string, DiasporaCluster> _knownClusters = new()
    {
        {
            "Bay Area", new DiasporaCluster
            {
                Region = "Bay Area",
                City = "San Jose",
                Country = "USA",
                TimeZone = "America/Los_Angeles",
                EstimatedPopulation = 45000,
                EngagementScore = 0.82,
                PreferredLanguages = new[] { "si", "en", "ta" },
                DominantReligions = new[] { "Buddhism", "Hinduism", "Christianity" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "buddhist_events", 0.65 },
                    { "hindu_events", 0.45 },
                    { "cultural_festivals", 0.78 },
                    { "business_networking", 0.71 }
                }
            }
        },
        {
            "Toronto", new DiasporaCluster
            {
                Region = "Toronto",
                City = "Toronto",
                Country = "Canada",
                TimeZone = "America/Toronto",
                EstimatedPopulation = 38000,
                EngagementScore = 0.78,
                PreferredLanguages = new[] { "en", "ta", "si" },
                DominantReligions = new[] { "Hinduism", "Buddhism", "Christianity" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "tamil_cultural_events", 0.72 },
                    { "hindu_events", 0.68 },
                    { "buddhist_events", 0.42 },
                    { "community_services", 0.81 }
                }
            }
        },
        {
            "London", new DiasporaCluster
            {
                Region = "London",
                City = "London",
                Country = "United Kingdom",
                TimeZone = "Europe/London",
                EstimatedPopulation = 42000,
                EngagementScore = 0.75,
                PreferredLanguages = new[] { "en", "ta", "si" },
                DominantReligions = new[] { "Buddhism", "Hinduism", "Christianity" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "vesak_celebrations", 0.69 },
                    { "cultural_heritage", 0.76 },
                    { "educational_events", 0.83 },
                    { "professional_networking", 0.67 }
                }
            }
        },
        {
            "Sydney", new DiasporaCluster
            {
                Region = "Sydney",
                City = "Sydney",
                Country = "Australia",
                TimeZone = "Australia/Sydney",
                EstimatedPopulation = 28000,
                EngagementScore = 0.73,
                PreferredLanguages = new[] { "en", "si", "ta" },
                DominantReligions = new[] { "Buddhism", "Christianity", "Hinduism" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "buddhist_events", 0.71 },
                    { "sports_events", 0.65 },
                    { "family_gatherings", 0.79 },
                    { "cultural_preservation", 0.74 }
                }
            }
        },
        {
            "Melbourne", new DiasporaCluster
            {
                Region = "Melbourne",
                City = "Melbourne",
                Country = "Australia",
                TimeZone = "Australia/Melbourne",
                EstimatedPopulation = 22000,
                EngagementScore = 0.71,
                PreferredLanguages = new[] { "en", "si", "ta" },
                DominantReligions = new[] { "Buddhism", "Christianity", "Hinduism" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "arts_culture", 0.82 },
                    { "food_festivals", 0.78 },
                    { "religious_observances", 0.66 }
                }
            }
        },
        {
            "New York", new DiasporaCluster
            {
                Region = "New York",
                City = "New York",
                Country = "USA",
                TimeZone = "America/New_York",
                EstimatedPopulation = 35000,
                EngagementScore = 0.69,
                PreferredLanguages = new[] { "en", "ta", "si" },
                DominantReligions = new[] { "Hinduism", "Christianity", "Buddhism" },
                CulturalPreferences = new Dictionary<string, double>
                {
                    { "professional_events", 0.88 },
                    { "cultural_shows", 0.73 },
                    { "religious_festivals", 0.61 }
                }
            }
        }
    };

    public DiasporaNotificationService(
        IGeographicTimeZoneService timeZoneService,
        ICulturalCalendarService culturalCalendarService,
        IDiasporaAnalyticsService analyticsService)
    {
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _culturalCalendarService = culturalCalendarService ?? throw new ArgumentNullException(nameof(culturalCalendarService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    public async Task<Result<IEnumerable<DiasporaCluster>>> GetTargetDiasporaCommunitiesAsync(WhatsAppMessage message)
    {
        try
        {
            var relevantClusters = new List<DiasporaCluster>();

            // Filter clusters based on message cultural context
            foreach (var cluster in _knownClusters.Values)
            {
                var relevanceScore = await CalculateClusterRelevance(cluster, message);
                
                if (relevanceScore > 0.5) // Threshold for relevance
                {
                    relevantClusters.Add(cluster);
                }
            }

            // Sort by combined relevance and engagement score
            var sortedClusters = relevantClusters
                .OrderByDescending(c => c.EngagementScore)
                .ThenByDescending(c => CalculateClusterRelevanceSync(c, message))
                .ToList();

            // Apply message type specific filtering
            var filteredClusters = await ApplyMessageTypeFiltering(sortedClusters, message);

            return Result<IEnumerable<DiasporaCluster>>.Success(filteredClusters);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<DiasporaCluster>>.Failure($"Failed to get target diaspora communities: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, DateTime>>> OptimizeBroadcastTimingByRegionAsync(
        WhatsAppMessage message, 
        IEnumerable<DiasporaCluster> targetClusters)
    {
        try
        {
            var optimizedTimings = new Dictionary<string, DateTime>();
            var baseTime = DateTime.UtcNow.AddHours(1); // Start 1 hour from now

            foreach (var cluster in targetClusters)
            {
                // Convert to local time zone
                var localTime = await _timeZoneService.ConvertToTimeZoneAsync(baseTime, cluster.TimeZone);

                // Optimize based on cultural context and local preferences
                var culturallyOptimizedTime = await OptimizeTimeForCulturalContext(localTime, message.CulturalContext, cluster);

                // Ensure time is within appropriate hours (8 AM - 9 PM local time)
                var finalOptimizedTime = await EnsureAppropriateLocalHours(culturallyOptimizedTime, cluster.TimeZone);

                // Convert back to UTC for storage
                var utcTime = await _timeZoneService.ConvertFromTimeZoneAsync(finalOptimizedTime, cluster.TimeZone);
                
                optimizedTimings[cluster.Region] = utcTime;
            }

            // Ensure staggered delivery to avoid API rate limits
            var staggeredTimings = await ApplyStaggeredDelivery(optimizedTimings);

            return Result<Dictionary<string, DateTime>>.Success(staggeredTimings);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, DateTime>>.Failure($"Failed to optimize broadcast timing: {ex.Message}");
        }
    }

    public async Task<Result<DiasporaReachEstimate>> EstimateMessageReachAsync(
        WhatsAppMessage message, 
        IEnumerable<DiasporaCluster> targetClusters)
    {
        try
        {
            var clusters = targetClusters.ToList();
            var totalReach = 0;
            var reachByRegion = new Dictionary<string, int>();
            var engagementByLanguage = new Dictionary<string, double>();

            // Calculate reach metrics
            foreach (var cluster in clusters)
            {
                var clusterReach = await EstimateClusterReach(cluster, message);
                totalReach += clusterReach;
                reachByRegion[cluster.Region] = clusterReach;

                // Calculate engagement by language
                foreach (var language in cluster.PreferredLanguages)
                {
                    if (!engagementByLanguage.ContainsKey(language))
                        engagementByLanguage[language] = 0;
                    
                    engagementByLanguage[language] = Math.Max(
                        engagementByLanguage[language], 
                        cluster.EngagementScore);
                }
            }

            // Calculate overall engagement rate
            var expectedEngagementRate = await CalculateOverallEngagementRate(clusters, message);

            // Calculate cultural relevance score
            var culturalRelevanceScore = await CalculateCulturalRelevanceScore(clusters, message);

            var estimate = new DiasporaReachEstimate
            {
                TotalReach = totalReach,
                ExpectedEngagementRate = expectedEngagementRate,
                ReachByRegion = reachByRegion,
                EngagementByLanguage = engagementByLanguage,
                CulturalRelevanceScore = culturalRelevanceScore
            };

            return Result<DiasporaReachEstimate>.Success(estimate);
        }
        catch (Exception ex)
        {
            return Result<DiasporaReachEstimate>.Failure($"Failed to estimate message reach: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, string>>> GetLocalizedContentByRegionAsync(
        string baseContent, 
        IEnumerable<DiasporaCluster> targetClusters)
    {
        try
        {
            var localizedContent = new Dictionary<string, string>();

            foreach (var cluster in targetClusters)
            {
                // Determine primary language for this cluster
                var primaryLanguage = cluster.PreferredLanguages.First();
                
                // Localize content based on cultural preferences
                var culturallyAdaptedContent = await AdaptContentForCluster(baseContent, cluster);

                // Apply language-specific formatting
                var languageFormattedContent = await ApplyLanguageFormatting(culturallyAdaptedContent, primaryLanguage);

                localizedContent[cluster.Region] = languageFormattedContent;
            }

            return Result<Dictionary<string, string>>.Success(localizedContent);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, string>>.Failure($"Failed to get localized content: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<double> CalculateClusterRelevance(DiasporaCluster cluster, WhatsAppMessage message)
    {
        var relevanceScore = 0.5; // Base relevance

        // Religious context matching
        if (message.CulturalContext.HasReligiousContent)
        {
            var religion = message.CulturalContext.PrimaryReligion?.ToLower();
            if (religion != null && cluster.DominantReligions.Any(r => r.ToLower().Contains(religion)))
            {
                relevanceScore += 0.3;
            }
        }

        // Festival context matching
        if (message.CulturalContext.IsFestivalRelated)
        {
            var festival = message.CulturalContext.FestivalName?.ToLower();
            if (festival == "vesak" && cluster.CulturalPreferences.ContainsKey("buddhist_events"))
                relevanceScore += cluster.CulturalPreferences["buddhist_events"] * 0.4;
            
            if (festival == "deepavali" && cluster.CulturalPreferences.ContainsKey("hindu_events"))
                relevanceScore += cluster.CulturalPreferences["hindu_events"] * 0.4;
        }

        // Language matching
        if (cluster.PreferredLanguages.Contains(message.Language))
        {
            relevanceScore += 0.2;
        }

        return await Task.FromResult(Math.Min(1.0, relevanceScore));
    }

    private double CalculateClusterRelevanceSync(DiasporaCluster cluster, WhatsAppMessage message)
    {
        // Synchronous version for LINQ operations
        return CalculateClusterRelevance(cluster, message).GetAwaiter().GetResult();
    }

    private async Task<IEnumerable<DiasporaCluster>> ApplyMessageTypeFiltering(
        List<DiasporaCluster> clusters, 
        WhatsAppMessage message)
    {
        var messageType = message.MessageType;

        // Broadcast messages go to all relevant clusters
        if (messageType == Communications.Enums.WhatsAppMessageType.Broadcast)
        {
            return clusters;
        }

        // Event notifications prioritize high-engagement clusters
        if (messageType == Communications.Enums.WhatsAppMessageType.EventNotification)
        {
            return clusters.Where(c => c.EngagementScore > 0.7).Take(4);
        }

        // Festival greetings go to culturally relevant clusters
        if (messageType == Communications.Enums.WhatsAppMessageType.FestivalGreeting)
        {
            return clusters.Take(6); // Top 6 most relevant
        }

        // Default: top 3 clusters
        return await Task.FromResult(clusters.Take(3));
    }

    private async Task<DateTime> OptimizeTimeForCulturalContext(
        DateTime localTime, 
        WhatsAppCulturalContext culturalContext, 
        DiasporaCluster cluster)
    {
        var optimizedTime = localTime;

        // Buddhist festivals - morning preference
        if (culturalContext.PrimaryReligion == "Buddhism")
        {
            optimizedTime = localTime.Date.AddHours(10); // 10 AM local
            if (localTime.Hour > 12) optimizedTime = optimizedTime.AddDays(1);
        }

        // Hindu festivals - evening preference for celebrations like Deepavali
        if (culturalContext.PrimaryReligion == "Hinduism" && culturalContext.IsFestivalRelated)
        {
            optimizedTime = localTime.Date.AddHours(18); // 6 PM local
            if (localTime.Hour > 20) optimizedTime = optimizedTime.AddDays(1);
        }

        // Community announcements - general optimal hours
        if (culturalContext.PrimaryReligion == null)
        {
            if (localTime.Hour < 9 || localTime.Hour > 20)
            {
                optimizedTime = localTime.Date.AddHours(10); // 10 AM local
                if (localTime.Hour > 20) optimizedTime = optimizedTime.AddDays(1);
            }
        }

        return await Task.FromResult(optimizedTime);
    }

    private async Task<DateTime> EnsureAppropriateLocalHours(DateTime localTime, string timeZone)
    {
        // Ensure message is sent between 8 AM and 9 PM local time
        if (localTime.Hour < 8)
        {
            return await Task.FromResult(localTime.Date.AddHours(9)); // 9 AM
        }
        
        if (localTime.Hour >= 21)
        {
            return await Task.FromResult(localTime.Date.AddDays(1).AddHours(9)); // Next day 9 AM
        }

        return await Task.FromResult(localTime);
    }

    private async Task<Dictionary<string, DateTime>> ApplyStaggeredDelivery(Dictionary<string, DateTime> timings)
    {
        // Stagger deliveries by 2-3 minutes to avoid API rate limits
        var staggeredTimings = new Dictionary<string, DateTime>();
        var delayMinutes = 0;

        foreach (var timing in timings.OrderBy(t => t.Value))
        {
            staggeredTimings[timing.Key] = timing.Value.AddMinutes(delayMinutes);
            delayMinutes += 2; // 2-minute intervals
        }

        return await Task.FromResult(staggeredTimings);
    }

    private async Task<int> EstimateClusterReach(DiasporaCluster cluster, WhatsAppMessage message)
    {
        // Estimate based on population, engagement score, and message relevance
        var baseReach = (int)(cluster.EstimatedPopulation * 0.15); // 15% reach assumption
        
        // Apply engagement multiplier
        var engagementMultiplier = cluster.EngagementScore;
        
        // Apply cultural relevance multiplier
        var relevanceMultiplier = await CalculateClusterRelevance(cluster, message);
        
        var estimatedReach = (int)(baseReach * engagementMultiplier * relevanceMultiplier);
        
        return await Task.FromResult(Math.Max(100, estimatedReach)); // Minimum 100 reach
    }

    private async Task<double> CalculateOverallEngagementRate(List<DiasporaCluster> clusters, WhatsAppMessage message)
    {
        if (!clusters.Any()) return 0.3; // Default 30% engagement

        var weightedEngagement = clusters.Sum(c => c.EngagementScore * c.EstimatedPopulation);
        var totalPopulation = clusters.Sum(c => c.EstimatedPopulation);
        
        var baseEngagement = weightedEngagement / totalPopulation;

        // Apply message type multipliers
        var messageTypeMultiplier = message.MessageType switch
        {
            Communications.Enums.WhatsAppMessageType.FestivalGreeting => 1.2,
            Communications.Enums.WhatsAppMessageType.EventNotification => 1.1,
            Communications.Enums.WhatsAppMessageType.CommunityAnnouncement => 0.9,
            _ => 1.0
        };

        return await Task.FromResult(Math.Min(0.8, baseEngagement * messageTypeMultiplier)); // Cap at 80% engagement
    }

    private async Task<double> CalculateCulturalRelevanceScore(List<DiasporaCluster> clusters, WhatsAppMessage message)
    {
        if (!clusters.Any()) return 0.5;

        var relevanceScores = new List<double>();
        
        foreach (var cluster in clusters)
        {
            var clusterRelevance = await CalculateClusterRelevance(cluster, message);
            relevanceScores.Add(clusterRelevance);
        }

        return relevanceScores.Average();
    }

    private async Task<string> AdaptContentForCluster(string baseContent, DiasporaCluster cluster)
    {
        var adaptedContent = baseContent;

        // Add region-specific cultural references if appropriate
        if (cluster.CulturalPreferences.ContainsKey("cultural_heritage") && 
            cluster.CulturalPreferences["cultural_heritage"] > 0.7)
        {
            adaptedContent = $"Celebrating our rich Sri Lankan heritage together. {adaptedContent}";
        }

        return await Task.FromResult(adaptedContent);
    }

    private async Task<string> ApplyLanguageFormatting(string content, string language)
    {
        // Apply language-specific formatting (placeholder implementation)
        var formatted = language switch
        {
            "si" => $"🙏 {content} 🙏", // Sinhala with traditional greeting symbols
            "ta" => $"வணக்கம்! {content}", // Tamil greeting
            _ => content
        };
        
        return await Task.FromResult(formatted);
    }

    #endregion
}

/// <summary>
/// Supporting interface for diaspora analytics
/// </summary>
public interface IDiasporaAnalyticsService
{
    Task<DiasporaEngagementMetrics> GetEngagementMetricsAsync(string region);
    Task<IEnumerable<string>> GetTopPerformingContentTypesAsync(string region);
}

/// <summary>
/// Diaspora engagement metrics
/// </summary>
public class DiasporaEngagementMetrics
{
    public string Region { get; set; } = string.Empty;
    public double AverageEngagementRate { get; set; }
    public Dictionary<string, double> ContentTypeEngagement { get; set; } = new();
    public Dictionary<string, double> LanguageEngagement { get; set; } = new();
}
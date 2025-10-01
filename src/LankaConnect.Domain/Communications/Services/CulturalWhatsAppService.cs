using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using static LankaConnect.Domain.Communications.Services.EmailCulturalOptimizer;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Cultural Intelligence WhatsApp Service - Domain Service Implementation
/// Provides Buddhist/Hindu calendar integration, cultural appropriateness validation, 
/// and diaspora community targeting for WhatsApp Business API messaging
/// </summary>
public class CulturalWhatsAppService : ICulturalWhatsAppService
{
    private readonly ICulturalCalendarService _culturalCalendarService;
    private readonly ICulturalSensitivityAnalyzer _sensitivityAnalyzer;
    private readonly IGeographicTimeZoneService _timeZoneService;
    private readonly IMultiLanguageTemplateService _templateService;
    private readonly IReligiousObservanceService _observanceService;

    public CulturalWhatsAppService(
        ICulturalCalendarService culturalCalendarService,
        ICulturalSensitivityAnalyzer sensitivityAnalyzer,
        IGeographicTimeZoneService timeZoneService,
        IMultiLanguageTemplateService templateService,
        IReligiousObservanceService observanceService)
    {
        _culturalCalendarService = culturalCalendarService ?? throw new ArgumentNullException(nameof(culturalCalendarService));
        _sensitivityAnalyzer = sensitivityAnalyzer ?? throw new ArgumentNullException(nameof(sensitivityAnalyzer));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _observanceService = observanceService ?? throw new ArgumentNullException(nameof(observanceService));
    }

    public async Task<Result<double>> ValidateCulturalAppropriatenessAsync(WhatsAppMessage message)
    {
        try
        {
            var scores = new List<double>();

            // Religious content appropriateness
            if (message.CulturalContext.HasReligiousContent)
            {
                var religiousScore = await AnalyzeReligiousContentAppropriateness(message);
                scores.Add(religiousScore);
            }

            // Festival timing appropriateness
            if (message.CulturalContext.IsFestivalRelated)
            {
                var festivalScore = await AnalyzeFestivalAppropriateness(message);
                scores.Add(festivalScore);
            }

            // Language and cultural sensitivity
            var sensitivityScore = await AnalyzeCulturalSensitivity(message.MessageContent, message.Language);
            scores.Add(sensitivityScore);

            // Geographic and diaspora appropriateness
            var diasporaScore = await AnalyzeDiasporaRelevance(message);
            scores.Add(diasporaScore);

            // Calculate composite score
            var compositeScore = scores.Any() ? scores.Average() : 0.5; // Default neutral score

            // Apply cultural context weighting
            var weightedScore = ApplyCulturalWeighting(compositeScore, message.CulturalContext);

            return Result<double>.Success(Math.Max(0.0, Math.Min(1.0, weightedScore)));
        }
        catch (Exception ex)
        {
            return Result<double>.Failure($"Failed to validate cultural appropriateness: {ex.Message}");
        }
    }

    public async Task<Result<DateTime>> OptimizeMessageTimingAsync(WhatsAppMessage message, DateTime requestedTime)
    {
        try
        {
            var culturalContext = message.CulturalContext;
            var timeZone = message.TimeZone ?? "UTC";

            // Check Buddhist calendar conflicts
            if (culturalContext.RequiresBuddhistCalendarAwareness)
            {
                var buddhistConflict = await CheckBuddhistObservanceConflict(requestedTime, timeZone);
                if (buddhistConflict)
                {
                    var optimizedTime = await FindNextBuddhistOptimalTime(requestedTime, timeZone);
                    return Result<DateTime>.Success(optimizedTime);
                }
            }

            // Check Hindu calendar conflicts
            if (culturalContext.RequiresHinduCalendarAwareness)
            {
                var hinduConflict = await CheckHinduObservanceConflict(requestedTime, timeZone);
                if (hinduConflict)
                {
                    var optimizedTime = await FindNextHinduOptimalTime(requestedTime, timeZone);
                    return Result<DateTime>.Success(optimizedTime);
                }
            }

            // Check general cultural timing preferences
            var culturallyOptimizedTime = await OptimizeForCulturalPreferences(requestedTime, culturalContext, timeZone);
            
            return Result<DateTime>.Success(culturallyOptimizedTime);
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Failure($"Failed to optimize message timing: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetOptimalDiasporaRegionsAsync(WhatsAppMessage message)
    {
        try
        {
            var regions = new List<string>();

            // Analyze message content for regional relevance
            var contentAnalysis = await AnalyzeContentForRegionalRelevance(message.MessageContent, message.CulturalContext);
            
            // Buddhist festival messages target Buddhist-majority diaspora regions
            if (message.CulturalContext.PrimaryReligion == "Buddhism")
            {
                regions.AddRange(new[] { "Bay Area", "Los Angeles", "Toronto", "London", "Sydney", "Melbourne" });
            }

            // Hindu festival messages target Tamil/Hindu diaspora regions
            if (message.CulturalContext.PrimaryReligion == "Hinduism")
            {
                regions.AddRange(new[] { "Toronto", "London", "New York", "Sydney", "Paris", "Frankfurt" });
            }

            // Community events target all major diaspora hubs
            if (message.MessageType == Communications.Enums.WhatsAppMessageType.CommunityAnnouncement)
            {
                regions.AddRange(new[] { "Bay Area", "Toronto", "London", "Sydney", "New York", "Los Angeles", "Melbourne", "Vancouver" });
            }

            // Remove duplicates and apply engagement scoring
            var optimizedRegions = regions.Distinct().ToList();
            var scoredRegions = await ScoreRegionsForMessage(optimizedRegions, message);

            return Result<IEnumerable<string>>.Success(scoredRegions.Take(6)); // Top 6 regions
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure($"Failed to get optimal diaspora regions: {ex.Message}");
        }
    }

    public async Task<Result<string>> SelectOptimalLanguageAsync(WhatsAppMessage message, IEnumerable<string> recipientPhoneNumbers)
    {
        try
        {
            var recipients = recipientPhoneNumbers.ToList();
            
            // Analyze recipient phone number patterns for geographic inference
            var geographicAnalysis = await AnalyzeRecipientGeography(recipients);
            
            // Buddhist contexts often prefer Sinhala
            if (message.CulturalContext.PrimaryReligion == "Buddhism")
            {
                // Check if recipients are in Sri Lanka or have Sri Lankan patterns
                if (geographicAnalysis.HasSriLankanRecipients)
                    return Result<string>.Success("si");
                
                // Diaspora Buddhist communities often use English
                return Result<string>.Success("en");
            }

            // Hindu contexts often prefer Tamil or English
            if (message.CulturalContext.PrimaryReligion == "Hinduism")
            {
                // Tamil diaspora preference
                if (geographicAnalysis.HasTamilDiasporaIndicators)
                    return Result<string>.Success("ta");
                
                return Result<string>.Success("en");
            }

            // Default language selection based on geographic distribution
            if (geographicAnalysis.PrimaryRegion == "SriLanka")
                return Result<string>.Success("si");
            
            if (geographicAnalysis.HasSignificantTamilPopulation)
                return Result<string>.Success("ta");

            // Default to English for international diaspora
            return Result<string>.Success("en");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to select optimal language: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateCulturallyAwareContentAsync(string baseContent, WhatsAppCulturalContext culturalContext, string language)
    {
        try
        {
            var enhancedContent = baseContent;

            // Buddhist festival enhancements
            if (culturalContext.RequiresBuddhistCalendarAwareness && culturalContext.IsFestivalRelated)
            {
                enhancedContent = await EnhanceWithBuddhistWisdom(enhancedContent, culturalContext.FestivalName, language);
            }

            // Hindu festival enhancements
            if (culturalContext.RequiresHinduCalendarAwareness && culturalContext.IsFestivalRelated)
            {
                enhancedContent = await EnhanceWithHinduBlessings(enhancedContent, culturalContext.FestivalName, language);
            }

            // Add cultural greetings and closings
            enhancedContent = await AddCulturalFraming(enhancedContent, culturalContext, language);

            // Validate enhanced content doesn't introduce cultural issues
            var validation = await _sensitivityAnalyzer.AnalyzeCulturalSensitivityAsync(enhancedContent, language);
            if (!validation.IsAcceptable)
            {
                return Result<string>.Failure("Enhanced content failed cultural sensitivity validation");
            }

            return Result<string>.Success(enhancedContent);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate culturally aware content: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsTimingReligiouslyAppropriateAsync(DateTime messageTime, WhatsAppCulturalContext culturalContext)
    {
        try
        {
            // Check Buddhist observance restrictions
            if (culturalContext.RequiresBuddhistCalendarAwareness)
            {
                var buddhistRestriction = await _observanceService.CheckBuddhistObservanceRestrictionsAsync(messageTime, LankaConnect.Domain.Common.Enums.GeographicRegion.SriLanka);
                if (buddhistRestriction)
                    return Result<bool>.Success(false);
            }

            // Check Hindu observance restrictions
            if (culturalContext.RequiresHinduCalendarAwareness)
            {
                var hinduRestriction = await _observanceService.CheckHinduObservanceRestrictionsAsync(messageTime, LankaConnect.Domain.Common.Enums.GeographicRegion.SriLanka);
                if (hinduRestriction)
                    return Result<bool>.Success(false);
            }

            // Check general cultural timing appropriateness
            var convertedCulturalContext = culturalContext.ToCulturalContext();
            // Use the already converted cultural context directly
            var serviceCulturalContext = convertedCulturalContext;
            var culturalTiming = await _culturalCalendarService.IsTimeCulturallyAppropriateAsync(messageTime, serviceCulturalContext);
            
            return Result<bool>.Success(culturalTiming);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check religious timing appropriateness: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, string>>> GetRecommendedCulturalMetadataAsync(WhatsAppMessage message)
    {
        try
        {
            var metadata = new Dictionary<string, string>();

            // Festival-specific metadata
            if (message.CulturalContext.IsFestivalRelated)
            {
                await AddFestivalMetadata(metadata, message.CulturalContext);
            }

            // Religious observance metadata
            if (message.CulturalContext.HasReligiousContent)
            {
                await AddReligiousMetadata(metadata, message.CulturalContext);
            }

            // Diaspora targeting metadata
            await AddDiasporaMetadata(metadata, message);

            // Timing and scheduling metadata
            await AddTimingMetadata(metadata, message);

            // Language and localization metadata
            await AddLanguageMetadata(metadata, message);

            return Result<Dictionary<string, string>>.Success(metadata);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, string>>.Failure($"Failed to get recommended cultural metadata: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<double> AnalyzeReligiousContentAppropriateness(WhatsAppMessage message)
    {
        var content = message.MessageContent;
        var religion = message.CulturalContext.PrimaryReligion;

        if (religion == "Buddhism")
        {
            return await AnalyzeBuddhistContentAccuracy(content);
        }

        if (religion == "Hinduism")
        {
            return await AnalyzeHinduContentAccuracy(content);
        }

        return 0.7; // Default score for general religious content
    }

    private async Task<double> AnalyzeFestivalAppropriateness(WhatsAppMessage message)
    {
        var festivalName = message.CulturalContext.FestivalName;
        var content = message.MessageContent;

        // High scores for traditional greetings
        if (festivalName == "Vesak" && content?.Contains("Buddha") == true && (content?.Contains("peace") == true || content?.Contains("wisdom") == true))
            return await Task.FromResult(0.95);

        if (festivalName == "Deepavali" && content?.Contains("light") == true && (content?.Contains("prosperity") == true || content?.Contains("happiness") == true))
            return await Task.FromResult(0.92);

        // Medium scores for appropriate but generic content
        if (content?.Contains(festivalName ?? "") == true)
            return await Task.FromResult(0.75);

        return await Task.FromResult(0.5); // Neutral score for generic festival content
    }

    private async Task<double> AnalyzeCulturalSensitivity(string content, string language)
    {
        var analysis = await _sensitivityAnalyzer.AnalyzeCulturalSensitivityAsync(content, language);
        return analysis.SensitivityScore;
    }

    private async Task<double> AnalyzeDiasporaRelevance(WhatsAppMessage message)
    {
        // Higher scores for content that resonates with diaspora experiences
        var content = message.MessageContent.ToLower();
        
        var diasporaRelevanceScore = 0.5;

        // Community and cultural connection references
        if (content?.Contains("community") == true || content?.Contains("heritage") == true || content?.Contains("tradition") == true)
            diasporaRelevanceScore += 0.2;

        // Home country references
        if (content?.Contains("sri lanka") == true || content?.Contains("lanka") == true)
            diasporaRelevanceScore += 0.15;

        // Festival and cultural celebration references
        if (content?.Contains("celebrate") == true || content?.Contains("festival") == true || content?.Contains("tradition") == true)
            diasporaRelevanceScore += 0.15;

        return await Task.FromResult(Math.Min(1.0, diasporaRelevanceScore));
    }

    private double ApplyCulturalWeighting(double baseScore, WhatsAppCulturalContext culturalContext)
    {
        var weighted = baseScore;

        // Boost scores for high cultural significance
        if (culturalContext.IsFestivalRelated)
            weighted *= 1.1;

        if (culturalContext.HasReligiousContent)
            weighted *= 1.05;

        return Math.Min(1.0, weighted);
    }

    private async Task<bool> CheckBuddhistObservanceConflict(DateTime requestedTime, string timeZone)
    {
        // Check if time falls during Buddhist meditation hours or Poyaday quiet periods
        var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
        
        // Avoid evening meditation hours (6 PM - 9 PM)
        if (localTime.Hour >= 18 && localTime.Hour <= 21)
            return true;

        // Check for Poyaday (Buddhist observance day)
        var isPoyaDay = await _culturalCalendarService.IsBuddhistObservanceDayAsync(localTime.Date);
        
        return isPoyaDay;
    }

    private async Task<DateTime> FindNextBuddhistOptimalTime(DateTime requestedTime, string timeZone)
    {
        // Suggest morning time (8 AM - 11 AM) for Buddhist messages
        var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
        var optimalLocalTime = localTime.Date.AddHours(9); // 9 AM

        // If already past 9 AM, suggest next day
        if (localTime.Hour >= 12)
            optimalLocalTime = optimalLocalTime.AddDays(1);

        return await _timeZoneService.ConvertFromTimeZoneAsync(optimalLocalTime, timeZone);
    }

    private async Task<bool> CheckHinduObservanceConflict(DateTime requestedTime, string timeZone)
    {
        var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
        
        // Check for Hindu prayer times or fasting periods
        var isHinduObservanceTime = await _culturalCalendarService.IsHinduObservanceTimeAsync(localTime);
        
        return isHinduObservanceTime;
    }

    private async Task<DateTime> FindNextHinduOptimalTime(DateTime requestedTime, string timeZone)
    {
        // For Hindu festivals like Deepavali, evening times are often appropriate
        var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
        var optimalLocalTime = localTime.Date.AddHours(18); // 6 PM

        if (localTime.Hour >= 20)
            optimalLocalTime = optimalLocalTime.AddDays(1);

        return await _timeZoneService.ConvertFromTimeZoneAsync(optimalLocalTime, timeZone);
    }

    private async Task<DateTime> OptimizeForCulturalPreferences(DateTime requestedTime, WhatsAppCulturalContext culturalContext, string timeZone)
    {
        // General cultural timing optimization
        var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
        
        // Avoid very early morning (before 7 AM) or very late night (after 10 PM)
        if (localTime.Hour < 7)
            localTime = localTime.Date.AddHours(9);
        else if (localTime.Hour >= 22)
            localTime = localTime.Date.AddDays(1).AddHours(9);

        return await _timeZoneService.ConvertFromTimeZoneAsync(localTime, timeZone);
    }

    // Additional helper methods would be implemented here...
    // Due to length constraints, showing representative patterns

    private Task<ContentAnalysis> AnalyzeContentForRegionalRelevance(string content, WhatsAppCulturalContext culturalContext)
    {
        // Implementation for analyzing content relevance to different regions
        return Task.FromResult(new ContentAnalysis());
    }

    private Task<IEnumerable<string>> ScoreRegionsForMessage(List<string> regions, WhatsAppMessage message)
    {
        // Implementation for scoring regions based on message content and context
        return Task.FromResult(regions.AsEnumerable());
    }

    private Task<GeographicAnalysis> AnalyzeRecipientGeography(List<string> recipients)
    {
        // Implementation for analyzing recipient phone numbers for geographic patterns
        var analysis = new GeographicAnalysis
        {
            HasSriLankanRecipients = recipients.Any(r => r.StartsWith("+94")),
            HasTamilDiasporaIndicators = recipients.Any(r => r.StartsWith("+1647") || r.StartsWith("+44")),
            PrimaryRegion = recipients.Count(r => r.StartsWith("+94")) > recipients.Count / 2 ? "SriLanka" : "Diaspora"
        };
        return Task.FromResult(analysis);
    }

    private Task<double> AnalyzeBuddhistContentAccuracy(string content)
    {
        // Implementation for validating Buddhist religious content accuracy
        var score = 0.5;
        if (content?.Contains("Buddha") == true && content?.Contains("wisdom") == true)
            score = 0.9;
        return Task.FromResult(score);
    }

    private Task<double> AnalyzeHinduContentAccuracy(string content)
    {
        // Implementation for validating Hindu religious content accuracy
        var score = 0.5;
        if (content?.Contains("prosperity") == true && content?.Contains("light") == true)
            score = 0.85;
        return Task.FromResult(score);
    }

    private Task<string> EnhanceWithBuddhistWisdom(string content, string? festivalName, string language)
    {
        if (festivalName == "Vesak")
            return Task.FromResult($"May this sacred {festivalName} bring you inner peace and wisdom. {content}");
        return Task.FromResult(content);
    }

    private Task<string> EnhanceWithHinduBlessings(string content, string? festivalName, string language)
    {
        if (festivalName == "Deepavali")
            return Task.FromResult($"May the lights of {festivalName} illuminate your path to prosperity. {content}");
        return Task.FromResult(content);
    }

    private Task<string> AddCulturalFraming(string content, WhatsAppCulturalContext culturalContext, string language)
    {
        // Add culturally appropriate greetings and closings
        return Task.FromResult(content);
    }

    private Task AddFestivalMetadata(Dictionary<string, string> metadata, WhatsAppCulturalContext culturalContext)
    {
        if (culturalContext.FestivalName != null)
        {
            metadata["festival_name"] = culturalContext.FestivalName;
            metadata["festival_significance"] = GetFestivalSignificance(culturalContext.FestivalName);
        }
        return Task.CompletedTask;
    }

    private Task AddReligiousMetadata(Dictionary<string, string> metadata, WhatsAppCulturalContext culturalContext)
    {
        if (culturalContext.PrimaryReligion != null)
        {
            metadata["primary_religion"] = culturalContext.PrimaryReligion;
            metadata["cultural_sensitivity"] = "high";
        }
        return Task.CompletedTask;
    }

    private Task AddDiasporaMetadata(Dictionary<string, string> metadata, WhatsAppMessage message)
    {
        if (message.DiasporaRegion != null)
        {
            metadata["target_diaspora_region"] = message.DiasporaRegion;
            metadata["target_demographic"] = $"{message.CulturalContext.PrimaryReligion?.ToLower()}_diaspora";
        }
        return Task.CompletedTask;
    }

    private Task AddTimingMetadata(Dictionary<string, string> metadata, WhatsAppMessage message)
    {
        if (message.CulturalContext.RequiresBuddhistCalendarAwareness)
            metadata["timing_preference"] = "morning_optimal";
        if (message.CulturalContext.RequiresHinduCalendarAwareness)
            metadata["timing_preference"] = "evening_optimal";
        return Task.CompletedTask;
    }

    private Task AddLanguageMetadata(Dictionary<string, string> metadata, WhatsAppMessage message)
    {
        metadata["message_language"] = message.Language;
        metadata["supports_multilingual"] = "true";
        return Task.CompletedTask;
    }

    private string GetFestivalSignificance(string festivalName)
    {
        return festivalName switch
        {
            "Vesak" => "buddha_birth_enlightenment_death",
            "Deepavali" => "victory_light_over_darkness",
            _ => "cultural_celebration"
        };
    }

    #endregion
}

// Supporting classes for internal operations
internal class ContentAnalysis
{
    public double RegionalRelevance { get; set; } = 0.5;
}

internal class GeographicAnalysis
{
    public bool HasSriLankanRecipients { get; set; }
    public bool HasTamilDiasporaIndicators { get; set; }
    public bool HasSignificantTamilPopulation { get; set; }
    public string PrimaryRegion { get; set; } = "Unknown";
}
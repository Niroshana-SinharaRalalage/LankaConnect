using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Cultural Message Validator - Content Appropriateness Scoring Service
/// Provides sophisticated cultural sensitivity analysis, religious content validation,
/// and appropriateness scoring for WhatsApp messages targeting Sri Lankan diaspora communities
/// </summary>
public class CulturalMessageValidator : ICulturalMessageValidator
{
    private readonly ICulturalSensitivityAnalyzer _sensitivityAnalyzer;
    private readonly IReligiousContentValidator _religiousValidator;
    private readonly IMultiLanguageContentAnalyzer _languageAnalyzer;

    // Cultural appropriateness keywords and their scores
    private static readonly Dictionary<string, Dictionary<string, double>> _culturalKeywords = new()
    {
        {
            "buddhist_positive", new Dictionary<string, double>
            {
                { "buddha", 0.9 }, { "dharma", 0.85 }, { "sangha", 0.85 }, { "peace", 0.8 },
                { "wisdom", 0.85 }, { "compassion", 0.9 }, { "meditation", 0.8 }, { "mindfulness", 0.8 },
                { "enlightenment", 0.9 }, { "nirvana", 0.85 }, { "karma", 0.75 }, { "vesak", 0.9 },
                { "poson", 0.85 }, { "triple gem", 0.95 }, { "bodhi", 0.85 }, { "samsara", 0.75 }
            }
        },
        {
            "buddhist_negative", new Dictionary<string, double>
            {
                { "party", -0.6 }, { "alcohol", -0.9 }, { "loud music", -0.7 }, { "drinking", -0.9 },
                { "wild", -0.5 }, { "crazy", -0.4 }, { "disco", -0.6 }, { "nightclub", -0.8 }
            }
        },
        {
            "hindu_positive", new Dictionary<string, double>
            {
                { "deepavali", 0.9 }, { "diwali", 0.9 }, { "lakshmi", 0.85 }, { "ganesh", 0.85 },
                { "prosperity", 0.8 }, { "light", 0.85 }, { "happiness", 0.8 }, { "success", 0.75 },
                { "blessed", 0.9 }, { "divine", 0.85 }, { "auspicious", 0.85 }, { "puja", 0.85 },
                { "rangoli", 0.8 }, { "oil lamp", 0.85 }, { "thaipusam", 0.85 }, { "murugan", 0.85 }
            }
        },
        {
            "cultural_positive", new Dictionary<string, double>
            {
                { "heritage", 0.8 }, { "tradition", 0.8 }, { "community", 0.85 }, { "family", 0.8 },
                { "respect", 0.85 }, { "honor", 0.8 }, { "celebrate", 0.75 }, { "sri lanka", 0.9 },
                { "lanka", 0.8 }, { "sinhala", 0.8 }, { "tamil", 0.8 }, { "culture", 0.85 },
                { "ancestors", 0.75 }, { "motherland", 0.8 }, { "homeland", 0.8 }
            }
        },
        {
            "inappropriate_general", new Dictionary<string, double>
            {
                { "hate", -0.9 }, { "violence", -0.9 }, { "discrimination", -0.9 }, { "racism", -1.0 },
                { "extremism", -1.0 }, { "terrorist", -1.0 }, { "war", -0.7 }, { "conflict", -0.6 },
                { "politics", -0.4 }, { "controversy", -0.5 }, { "scandal", -0.6 }
            }
        }
    };

    // Festival-specific validation patterns
    private static readonly Dictionary<string, FestivalValidationPattern> _festivalPatterns = new()
    {
        {
            "vesak", new FestivalValidationPattern
            {
                RequiredElements = new[] { "peace", "wisdom", "buddha" },
                ProhibitedElements = new[] { "party", "alcohol", "loud" },
                OptimalGreetings = new[] { "may this vesak", "blessed vesak", "sacred vesak" },
                CulturalScore = 0.95
            }
        },
        {
            "deepavali", new FestivalValidationPattern
            {
                RequiredElements = new[] { "light", "prosperity", "happiness" },
                ProhibitedElements = new[] { "darkness", "sadness" },
                OptimalGreetings = new[] { "happy deepavali", "blessed deepavali", "festival of lights" },
                CulturalScore = 0.92
            }
        },
        {
            "poson", new FestivalValidationPattern
            {
                RequiredElements = new[] { "dharma", "sri lanka", "buddhism" },
                ProhibitedElements = new[] { "celebration", "party" }, // Poson is more contemplative
                OptimalGreetings = new[] { "blessed poson", "may the dharma" },
                CulturalScore = 0.88
            }
        }
    };

    public CulturalMessageValidator(
        ICulturalSensitivityAnalyzer sensitivityAnalyzer,
        IReligiousContentValidator religiousValidator,
        IMultiLanguageContentAnalyzer languageAnalyzer)
    {
        _sensitivityAnalyzer = sensitivityAnalyzer ?? throw new ArgumentNullException(nameof(sensitivityAnalyzer));
        _religiousValidator = religiousValidator ?? throw new ArgumentNullException(nameof(religiousValidator));
        _languageAnalyzer = languageAnalyzer ?? throw new ArgumentNullException(nameof(languageAnalyzer));
    }

    public async Task<Result<CulturalValidationResult>> ValidateMessageContentAsync(
        string content, 
        WhatsAppCulturalContext culturalContext, 
        string language)
    {
        try
        {
            var validationResult = new CulturalValidationResult();
            var issues = new List<string>();
            var suggestions = new List<string>();
            var detailedScores = new Dictionary<string, double>();

            // 1. Religious content accuracy validation
            var religiousScore = await ValidateReligiousAccuracy(content, culturalContext);
            detailedScores["religious_accuracy"] = religiousScore.Score;
            issues.AddRange(religiousScore.Issues);
            suggestions.AddRange(religiousScore.Suggestions);

            // 2. Cultural sensitivity analysis
            var sensitivityScore = await AnalyzeCulturalSensitivity(content, language);
            detailedScores["cultural_sensitivity"] = sensitivityScore;

            // 3. Language appropriateness validation
            var languageScore = await ValidateLanguageAppropriateness(content, language, culturalContext);
            detailedScores["language_appropriateness"] = languageScore.Score;
            issues.AddRange(languageScore.Issues);
            suggestions.AddRange(languageScore.Suggestions);

            // 4. Festival-specific validation
            if (culturalContext.IsFestivalRelated)
            {
                var festivalScore = await ValidateFestivalAppropriateness(content, culturalContext);
                detailedScores["festival_appropriateness"] = festivalScore.Score;
                issues.AddRange(festivalScore.Issues);
                suggestions.AddRange(festivalScore.Suggestions);
            }

            // 5. Diaspora relevance validation
            var diasporaScore = await ValidateDiasporaRelevance(content);
            detailedScores["diaspora_relevance"] = diasporaScore;

            // Calculate composite appropriateness score
            var compositeScore = CalculateCompositeScore(detailedScores, culturalContext);
            
            validationResult = new CulturalValidationResult
            {
                AppropriatnessScore = compositeScore,
                IsAcceptable = compositeScore >= 0.7, // 70% threshold
                Issues = issues,
                Suggestions = suggestions,
                DetailedScores = detailedScores
            };

            return Result<CulturalValidationResult>.Success(validationResult);
        }
        catch (Exception ex)
        {
            return Result<CulturalValidationResult>.Failure($"Failed to validate message content: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<string>>> DetectCulturalSensitivityIssuesAsync(string content, string language)
    {
        try
        {
            var issues = new List<string>();
            var contentLower = content.ToLower();

            // Check for inappropriate religious references
            foreach (var negativeKeyword in _culturalKeywords["inappropriate_general"])
            {
                if (contentLower.Contains(negativeKeyword.Key))
                {
                    issues.Add($"Contains potentially offensive term: '{negativeKeyword.Key}'");
                }
            }

            // Check for Buddhist context violations
            if (contentLower.Contains("vesak") || contentLower.Contains("buddha"))
            {
                foreach (var negativeKeyword in _culturalKeywords["buddhist_negative"])
                {
                    if (contentLower.Contains(negativeKeyword.Key))
                    {
                        issues.Add($"Inappropriate for Buddhist context: '{negativeKeyword.Key}' conflicts with religious observance");
                    }
                }
            }

            // Check for cultural insensitivity patterns
            var culturalIssues = await DetectCulturalInsensitivityPatterns(content, language);
            issues.AddRange(culturalIssues);

            // Check for religious mixing inappropriately
            var religiousMixingIssues = await DetectInappropriateReligiousMixing(content);
            issues.AddRange(religiousMixingIssues);

            return Result<IEnumerable<string>>.Success(issues);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure($"Failed to detect cultural sensitivity issues: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetCulturalImprovementSuggestionsAsync(
        string content, 
        WhatsAppCulturalContext culturalContext, 
        string language)
    {
        try
        {
            var suggestions = new List<string>();

            // Buddhist context suggestions
            if (culturalContext.RequiresBuddhistCalendarAwareness)
            {
                suggestions.AddRange(await GetBuddhistContextSuggestions(content, culturalContext));
            }

            // Hindu context suggestions
            if (culturalContext.RequiresHinduCalendarAwareness)
            {
                suggestions.AddRange(await GetHinduContextSuggestions(content, culturalContext));
            }

            // Festival-specific suggestions
            if (culturalContext.IsFestivalRelated && culturalContext.FestivalName != null)
            {
                suggestions.AddRange(await GetFestivalSpecificSuggestions(content, culturalContext.FestivalName));
            }

            // Language-specific suggestions
            suggestions.AddRange(await GetLanguageSpecificSuggestions(content, language));

            // Diaspora engagement suggestions
            suggestions.AddRange(await GetDiasporaEngagementSuggestions(content));

            return Result<IEnumerable<string>>.Success(suggestions.Distinct());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure($"Failed to get improvement suggestions: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ValidateFestivalGreetingAppropriatnessAsync(
        string greeting, 
        string festivalName, 
        string religion)
    {
        try
        {
            var festivalKey = festivalName.ToLower();
            var greetingLower = greeting.ToLower();

            // Check if festival pattern exists
            if (!_festivalPatterns.ContainsKey(festivalKey))
            {
                // Generic validation for unknown festivals
                return Result<bool>.Success(true);
            }

            var pattern = _festivalPatterns[festivalKey];

            // Check for prohibited elements
            foreach (var prohibited in pattern.ProhibitedElements)
            {
                if (greetingLower.Contains(prohibited))
                {
                    return Result<bool>.Success(false);
                }
            }

            // Check for at least one required element
            var hasRequiredElement = pattern.RequiredElements.Any(required => greetingLower.Contains(required));
            
            // Check for optimal greeting patterns
            var hasOptimalPattern = pattern.OptimalGreetings.Any(optimal => greetingLower.Contains(optimal));

            // Validate religion matching
            var religionMatches = await ValidateReligionFestivalMatching(festivalName, religion);

            var isAppropriate = hasRequiredElement && religionMatches && !pattern.ProhibitedElements.Any(p => greetingLower.Contains(p));

            return Result<bool>.Success(isAppropriate);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to validate festival greeting appropriateness: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<(double Score, List<string> Issues, List<string> Suggestions)> ValidateReligiousAccuracy(
        string content, 
        WhatsAppCulturalContext culturalContext)
    {
        var score = 0.5; // Neutral base score
        var issues = new List<string>();
        var suggestions = new List<string>();
        var contentLower = content.ToLower();

        if (culturalContext.PrimaryReligion == "Buddhism")
        {
            // Check for positive Buddhist elements
            foreach (var keyword in _culturalKeywords["buddhist_positive"])
            {
                if (contentLower.Contains(keyword.Key))
                {
                    score += keyword.Value * 0.3; // Weight positive keywords
                }
            }

            // Check for negative Buddhist elements
            foreach (var keyword in _culturalKeywords["buddhist_negative"])
            {
                if (contentLower.Contains(keyword.Key))
                {
                    score += keyword.Value * 0.5; // Negative values reduce score
                    issues.Add($"'{keyword.Key}' is inappropriate for Buddhist context");
                }
            }

            // Buddhist-specific suggestions
            if (score < 0.7)
            {
                suggestions.Add("Consider adding references to peace, wisdom, or compassion");
                suggestions.Add("Include Buddha's teachings or mindfulness concepts");
            }
        }

        if (culturalContext.PrimaryReligion == "Hinduism")
        {
            // Check for positive Hindu elements
            foreach (var keyword in _culturalKeywords["hindu_positive"])
            {
                if (contentLower.Contains(keyword.Key))
                {
                    score += keyword.Value * 0.3;
                }
            }

            if (score < 0.7)
            {
                suggestions.Add("Consider adding references to prosperity, light, or divine blessings");
                suggestions.Add("Include appropriate deity names or festival significance");
            }
        }

        return await Task.FromResult((Math.Max(0.0, Math.Min(1.0, score)), issues, suggestions));
    }

    private async Task<double> AnalyzeCulturalSensitivity(string content, string language)
    {
        // Delegate to cultural sensitivity analyzer service
        var analysis = await _sensitivityAnalyzer.AnalyzeCulturalSensitivityAsync(content, language);
        return analysis.SensitivityScore;
    }

    private async Task<(double Score, List<string> Issues, List<string> Suggestions)> ValidateLanguageAppropriateness(
        string content, 
        string language, 
        WhatsAppCulturalContext culturalContext)
    {
        var score = 0.7; // Default good score for language appropriateness
        var issues = new List<string>();
        var suggestions = new List<string>();

        // Validate language choice for cultural context
        if (culturalContext.PrimaryReligion == "Buddhism" && language == "si")
        {
            score += 0.2; // Bonus for using Sinhala for Buddhist content
        }

        if (culturalContext.PrimaryReligion == "Hinduism" && language == "ta")
        {
            score += 0.2; // Bonus for using Tamil for Hindu content
        }

        // Check for language mixing issues
        var languageAnalysis = await _languageAnalyzer.AnalyzeContentLanguageAsync(content, language);
        if (!languageAnalysis.IsConsistent)
        {
            issues.Add("Mixed languages detected - ensure consistency");
            score -= 0.2;
        }

        return (Math.Max(0.0, Math.Min(1.0, score)), issues, suggestions);
    }

    private async Task<(double Score, List<string> Issues, List<string> Suggestions)> ValidateFestivalAppropriateness(
        string content, 
        WhatsAppCulturalContext culturalContext)
    {
        var score = 0.5;
        var issues = new List<string>();
        var suggestions = new List<string>();

        var festivalName = culturalContext.FestivalName?.ToLower();
        if (festivalName != null && _festivalPatterns.ContainsKey(festivalName))
        {
            var pattern = _festivalPatterns[festivalName];
            var contentLower = content.ToLower();

            // Check required elements
            var hasRequiredElements = pattern.RequiredElements.Count(req => contentLower.Contains(req));
            score += (hasRequiredElements / (double)pattern.RequiredElements.Length) * 0.4;

            // Check prohibited elements
            foreach (var prohibited in pattern.ProhibitedElements)
            {
                if (contentLower.Contains(prohibited))
                {
                    issues.Add($"'{prohibited}' is inappropriate for {festivalName} observance");
                    score -= 0.3;
                }
            }

            // Check optimal greetings
            var hasOptimalGreeting = pattern.OptimalGreetings.Any(opt => contentLower.Contains(opt));
            if (hasOptimalGreeting)
            {
                score += 0.2;
            }
            else
            {
                suggestions.Add($"Consider using traditional {festivalName} greetings");
            }
        }

        return await Task.FromResult((Math.Max(0.0, Math.Min(1.0, score)), issues, suggestions));
    }

    private async Task<double> ValidateDiasporaRelevance(string content)
    {
        var score = 0.5;
        var contentLower = content.ToLower();

        // Check for cultural connection keywords
        foreach (var keyword in _culturalKeywords["cultural_positive"])
        {
            if (contentLower.Contains(keyword.Key))
            {
                score += keyword.Value * 0.2;
            }
        }

        return await Task.FromResult(Math.Max(0.0, Math.Min(1.0, score)));
    }

    private double CalculateCompositeScore(Dictionary<string, double> detailedScores, WhatsAppCulturalContext culturalContext)
    {
        var weights = new Dictionary<string, double>
        {
            { "religious_accuracy", culturalContext.HasReligiousContent ? 0.3 : 0.1 },
            { "cultural_sensitivity", 0.3 },
            { "language_appropriateness", 0.2 },
            { "festival_appropriateness", culturalContext.IsFestivalRelated ? 0.3 : 0.0 },
            { "diaspora_relevance", 0.2 }
        };

        // Normalize weights to sum to 1.0
        var totalWeight = weights.Values.Sum();
        var normalizedWeights = weights.ToDictionary(w => w.Key, w => w.Value / totalWeight);

        var weightedScore = 0.0;
        foreach (var score in detailedScores)
        {
            if (normalizedWeights.ContainsKey(score.Key))
            {
                weightedScore += score.Value * normalizedWeights[score.Key];
            }
        }

        return Math.Max(0.0, Math.Min(1.0, weightedScore));
    }

    private async Task<IEnumerable<string>> DetectCulturalInsensitivityPatterns(string content, string language)
    {
        var issues = new List<string>();
        var contentLower = content.ToLower();

        // Check for common cultural insensitivity patterns
        if (contentLower.Contains("curry") && contentLower.Contains("stereotype"))
        {
            issues.Add("Avoid stereotypical cultural references");
        }

        if (contentLower.Contains("exotic") || contentLower.Contains("foreign"))
        {
            issues.Add("Terms like 'exotic' or 'foreign' can be culturally insensitive");
        }

        return await Task.FromResult(issues.AsEnumerable());
    }

    private async Task<IEnumerable<string>> DetectInappropriateReligiousMixing(string content)
    {
        var issues = new List<string>();
        var contentLower = content.ToLower();

        // Check for inappropriate mixing of religious concepts
        if (contentLower.Contains("buddha") && contentLower.Contains("ganesh"))
        {
            issues.Add("Mixing Buddhist and Hindu religious figures may be culturally inappropriate");
        }

        return await Task.FromResult(issues.AsEnumerable());
    }

    private async Task<IEnumerable<string>> GetBuddhistContextSuggestions(string content, WhatsAppCulturalContext culturalContext)
    {
        var suggestions = new List<string>();
        var contentLower = content.ToLower();

        if (!contentLower.Contains("peace") && !contentLower.Contains("wisdom"))
        {
            suggestions.Add("Consider adding references to inner peace or wisdom");
        }

        if (culturalContext.FestivalName?.ToLower() == "vesak" && !contentLower.Contains("buddha"))
        {
            suggestions.Add("Vesak messages traditionally reference Buddha's teachings");
        }

        return await Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<IEnumerable<string>> GetHinduContextSuggestions(string content, WhatsAppCulturalContext culturalContext)
    {
        var suggestions = new List<string>();
        var contentLower = content.ToLower();

        if (culturalContext.FestivalName?.ToLower() == "deepavali")
        {
            if (!contentLower.Contains("light") && !contentLower.Contains("prosperity"))
            {
                suggestions.Add("Deepavali messages traditionally reference light and prosperity");
            }
        }

        return await Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<IEnumerable<string>> GetFestivalSpecificSuggestions(string content, string festivalName)
    {
        var suggestions = new List<string>();
        var festivalKey = festivalName.ToLower();

        if (_festivalPatterns.ContainsKey(festivalKey))
        {
            var pattern = _festivalPatterns[festivalKey];
            suggestions.Add($"Consider using one of these traditional greetings: {string.Join(", ", pattern.OptimalGreetings)}");
        }

        return await Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<IEnumerable<string>> GetLanguageSpecificSuggestions(string content, string language)
    {
        var suggestions = new List<string>();

        if (language == "en")
        {
            suggestions.Add("Consider localizing to Sinhala or Tamil for stronger cultural connection");
        }

        return await Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<IEnumerable<string>> GetDiasporaEngagementSuggestions(string content)
    {
        var suggestions = new List<string>();
        var contentLower = content.ToLower();

        if (!contentLower.Contains("community") && !contentLower.Contains("heritage"))
        {
            suggestions.Add("Consider adding references to community or cultural heritage");
        }

        return await Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<bool> ValidateReligionFestivalMatching(string festivalName, string religion)
    {
        var festivalReligionMap = new Dictionary<string, string[]>
        {
            { "vesak", new[] { "Buddhism" } },
            { "poson", new[] { "Buddhism" } },
            { "deepavali", new[] { "Hinduism" } },
            { "thai pusam", new[] { "Hinduism" } }
        };

        var festivalKey = festivalName.ToLower();
        if (festivalReligionMap.ContainsKey(festivalKey))
        {
            return await Task.FromResult(festivalReligionMap[festivalKey].Contains(religion));
        }

        return await Task.FromResult(true); // Allow unknown festivals
    }

    #endregion
}

/// <summary>
/// Festival validation pattern for cultural appropriateness
/// </summary>
internal class FestivalValidationPattern
{
    public string[] RequiredElements { get; set; } = Array.Empty<string>();
    public string[] ProhibitedElements { get; set; } = Array.Empty<string>();
    public string[] OptimalGreetings { get; set; } = Array.Empty<string>();
    public double CulturalScore { get; set; }
}

/// <summary>
/// Supporting interfaces for validation services
/// </summary>
public interface IReligiousContentValidator
{
    Task<ValidationResult> ValidateBuddhistContentAsync(string content);
    Task<ValidationResult> ValidateHinduContentAsync(string content);
}

public interface IMultiLanguageContentAnalyzer
{
    Task<LanguageAnalysisResult> AnalyzeContentLanguageAsync(string content, string expectedLanguage);
}

/// <summary>
/// Supporting result classes
/// </summary>
public class ValidationResult
{
    public double Score { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class LanguageAnalysisResult
{
    public bool IsConsistent { get; set; }
    public string DetectedLanguage { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}
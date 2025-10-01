using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service interface for cultural sensitivity analysis of email content
/// </summary>
public interface ICulturalSensitivityAnalyzer
{
    SensitivityAnalysisResult AnalyzeContent(string content, RecipientAudience audience);
    bool IsContentAppropriate(string content, CulturalBackground culturalBackground);
    string[] GetCulturalViolations(string content, CulturalBackground[] backgrounds);
    string SuggestNeutralAlternative(string content, CulturalBackground[] backgrounds);
    
    // Async methods for cultural sensitivity analysis
    Task<SensitivityAnalysisResult> AnalyzeCulturalSensitivityAsync(string content, string language);
    Task<bool> IsContentCulturallySensitiveAsync(string content, CulturalBackground culturalBackground);
}

/// <summary>
/// Recipient audience with multiple cultural backgrounds
/// </summary>
public record RecipientAudience(CulturalBackground[] CulturalBackgrounds);

/// <summary>
/// Result of cultural sensitivity analysis
/// </summary>
public record SensitivityAnalysisResult(
    bool IsAppropriate,
    string[] Violations,
    string[] NeutralAlternatives)
{
    // Additional properties for backward compatibility
    public double SensitivityScore => IsAppropriate ? 1.0 : Math.Max(0.0, 1.0 - (Violations?.Length ?? 0) * 0.2);
    public bool IsAcceptable => IsAppropriate;
}
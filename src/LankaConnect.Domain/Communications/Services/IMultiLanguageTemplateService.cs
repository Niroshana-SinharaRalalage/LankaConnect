using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service interface for multi-language template management and cultural content adaptation
/// </summary>
public interface IMultiLanguageTemplateService
{
    LocalizedEmailTemplate GetLocalizedTemplate(EmailType emailType, SriLankanLanguage language);
    CulturallyAdaptedContent GetCulturallyAdaptedContent(string originalContent, CulturalBackground culturalBackground);
    MultilingualContent CreateMultilingualContent(string primaryContent, SriLankanLanguage primaryLanguage);
    bool HasTemplateForLanguage(EmailType emailType, SriLankanLanguage language);
}

/// <summary>
/// Localized email template with cultural adaptations
/// </summary>
public record LocalizedEmailTemplate(
    string Subject,
    string Body,
    SriLankanLanguage Language,
    string[] CulturalAdaptations);

/// <summary>
/// Culturally adapted content with explanation of adaptations
/// </summary>
public record CulturallyAdaptedContent(
    string AdaptedText,
    string[] CulturalElements,
    string[] Adaptations);
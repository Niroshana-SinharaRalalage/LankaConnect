using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Multi-language email content supporting Sinhala, Tamil, and English for Sri Lankan diaspora
/// </summary>
public class MultilingualContent : ValueObject
{
    public string PrimaryContent { get; }
    public SriLankanLanguage PrimaryLanguage { get; }
    public IReadOnlyDictionary<SriLankanLanguage, string> Translations { get; }
    public bool HasMultipleLanguages => Translations.Count > 1;
    public IReadOnlyList<SriLankanLanguage> SupportedLanguages { get; }

    private MultilingualContent(
        string primaryContent,
        SriLankanLanguage primaryLanguage,
        IReadOnlyDictionary<SriLankanLanguage, string> translations)
    {
        PrimaryContent = primaryContent;
        PrimaryLanguage = primaryLanguage;
        Translations = translations;
        SupportedLanguages = translations.Keys.ToList().AsReadOnly();
    }

    public static Result<MultilingualContent> Create(
        string primaryContent,
        SriLankanLanguage primaryLanguage,
        Dictionary<SriLankanLanguage, string>? additionalTranslations = null)
    {
        if (string.IsNullOrWhiteSpace(primaryContent))
            return Result<MultilingualContent>.Failure("Primary content is required");

        var translations = new Dictionary<SriLankanLanguage, string>
        {
            [primaryLanguage] = primaryContent
        };

        if (additionalTranslations != null)
        {
            foreach (var translation in additionalTranslations)
            {
                if (!string.IsNullOrWhiteSpace(translation.Value))
                {
                    translations[translation.Key] = translation.Value;
                }
            }
        }

        return Result<MultilingualContent>.Success(new MultilingualContent(
            primaryContent,
            primaryLanguage,
            translations.AsReadOnly()));
    }

    public static MultilingualContent CreateEnglishOnly(string content)
    {
        return new MultilingualContent(
            content,
            SriLankanLanguage.English,
            new Dictionary<SriLankanLanguage, string>
            {
                [SriLankanLanguage.English] = content
            }.AsReadOnly());
    }

    public static MultilingualContent CreateTriLingual(string english, string sinhala, string tamil)
    {
        var translations = new Dictionary<SriLankanLanguage, string>
        {
            [SriLankanLanguage.English] = english,
            [SriLankanLanguage.Sinhala] = sinhala,
            [SriLankanLanguage.Tamil] = tamil
        };

        return new MultilingualContent(
            english,
            SriLankanLanguage.English,
            translations.AsReadOnly());
    }

    public string GetContentInLanguage(SriLankanLanguage language)
    {
        return Translations.TryGetValue(language, out var content) ? content : PrimaryContent;
    }

    public bool HasTranslationFor(SriLankanLanguage language)
    {
        return Translations.ContainsKey(language);
    }

    public MultilingualContent AddTranslation(SriLankanLanguage language, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return this;

        var newTranslations = new Dictionary<SriLankanLanguage, string>(Translations)
        {
            [language] = content
        };

        return new MultilingualContent(PrimaryContent, PrimaryLanguage, newTranslations.AsReadOnly());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrimaryContent;
        yield return PrimaryLanguage;
        
        foreach (var translation in Translations.OrderBy(t => t.Key))
        {
            yield return translation.Key;
            yield return translation.Value;
        }
    }
}
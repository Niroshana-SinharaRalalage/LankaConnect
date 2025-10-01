using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing multilingual descriptions for cultural events
/// Supports Sinhala, Tamil, and English for comprehensive diaspora communication
/// </summary>
public sealed class MultilingualDescription : ValueObject
{
    public string English { get; }
    public string? Sinhala { get; }
    public string? Tamil { get; }
    public LanguagePreference PrimaryLanguage { get; }
    public bool HasSinhala => !string.IsNullOrWhiteSpace(Sinhala);
    public bool HasTamil => !string.IsNullOrWhiteSpace(Tamil);
    public bool IsMultilingual => HasSinhala || HasTamil;

    private MultilingualDescription(
        string english,
        string? sinhala,
        string? tamil,
        LanguagePreference primaryLanguage)
    {
        English = english;
        Sinhala = sinhala;
        Tamil = tamil;
        PrimaryLanguage = primaryLanguage;
    }

    /// <summary>
    /// Creates multilingual description with all three languages
    /// </summary>
    public static Result<MultilingualDescription> Create(
        string english,
        string? sinhala = null,
        string? tamil = null,
        LanguagePreference primaryLanguage = LanguagePreference.English)
    {
        if (string.IsNullOrWhiteSpace(english))
            return Result<MultilingualDescription>.Failure("English description is required");

        // Validate Sinhala text if provided
        if (!string.IsNullOrWhiteSpace(sinhala) && !IsValidSinhalaText(sinhala))
            return Result<MultilingualDescription>.Failure("Invalid Sinhala text format");

        // Validate Tamil text if provided
        if (!string.IsNullOrWhiteSpace(tamil) && !IsValidTamilText(tamil))
            return Result<MultilingualDescription>.Failure("Invalid Tamil text format");

        return Result<MultilingualDescription>.Success(
            new MultilingualDescription(english, sinhala, tamil, primaryLanguage));
    }

    /// <summary>
    /// Creates English-only description
    /// </summary>
    public static MultilingualDescription CreateEnglishOnly(string english)
    {
        return new MultilingualDescription(english, null, null, LanguagePreference.English);
    }

    /// <summary>
    /// Creates Buddhist cultural event description
    /// </summary>
    public static MultilingualDescription CreateBuddhistEvent(
        string englishDescription,
        string sinhalaDescription,
        LanguagePreference primaryLanguage = LanguagePreference.English)
    {
        return new MultilingualDescription(
            englishDescription, sinhalaDescription, null, primaryLanguage);
    }

    /// <summary>
    /// Creates Hindu cultural event description
    /// </summary>
    public static MultilingualDescription CreateHinduEvent(
        string englishDescription,
        string tamilDescription,
        LanguagePreference primaryLanguage = LanguagePreference.English)
    {
        return new MultilingualDescription(
            englishDescription, null, tamilDescription, primaryLanguage);
    }

    /// <summary>
    /// Creates comprehensive Sri Lankan cultural event description
    /// </summary>
    public static MultilingualDescription CreateSriLankanEvent(
        string englishDescription,
        string sinhalaDescription,
        string tamilDescription,
        LanguagePreference primaryLanguage = LanguagePreference.English)
    {
        return new MultilingualDescription(
            englishDescription, sinhalaDescription, tamilDescription, primaryLanguage);
    }

    /// <summary>
    /// Gets description in preferred language with fallback
    /// </summary>
    public string GetPreferredDescription(LanguagePreference preference)
    {
        return preference switch
        {
            LanguagePreference.Sinhala when HasSinhala => Sinhala!,
            LanguagePreference.Tamil when HasTamil => Tamil!,
            _ => English
        };
    }

    /// <summary>
    /// Gets description in user's primary language
    /// </summary>
    public string GetPrimaryDescription()
    {
        return GetPreferredDescription(PrimaryLanguage);
    }

    /// <summary>
    /// Adds cultural context to existing description
    /// </summary>
    public MultilingualDescription WithCulturalContext(string culturalContext)
    {
        var enhancedEnglish = $"{English}. {culturalContext}";
        var enhancedSinhala = HasSinhala ? $"{Sinhala}. {TranslateCulturalContext(culturalContext, LanguagePreference.Sinhala)}" : null;
        var enhancedTamil = HasTamil ? $"{Tamil}. {TranslateCulturalContext(culturalContext, LanguagePreference.Tamil)}" : null;

        return new MultilingualDescription(enhancedEnglish, enhancedSinhala, enhancedTamil, PrimaryLanguage);
    }

    /// <summary>
    /// Creates formatted description for calendar events
    /// </summary>
    public string GetCalendarDescription(LanguagePreference language, bool includeTranslations = true)
    {
        var primaryDesc = GetPreferredDescription(language);
        
        if (!includeTranslations || !IsMultilingual)
            return primaryDesc;

        var translations = new List<string>();
        
        if (language != LanguagePreference.English)
            translations.Add($"English: {English}");
            
        if (language != LanguagePreference.Sinhala && HasSinhala)
            translations.Add($"සිංහල: {Sinhala}");
            
        if (language != LanguagePreference.Tamil && HasTamil)
            translations.Add($"தமிழ்: {Tamil}");

        return translations.Any() 
            ? $"{primaryDesc}\n\n{string.Join("\n", translations)}"
            : primaryDesc;
    }

    /// <summary>
    /// Gets language availability summary
    /// </summary>
    public string GetLanguageAvailability()
    {
        var languages = new List<string> { "English" };
        
        if (HasSinhala) languages.Add("Sinhala");
        if (HasTamil) languages.Add("Tamil");
        
        return string.Join(", ", languages);
    }

    private static bool IsValidSinhalaText(string text)
    {
        // Basic validation for Sinhala Unicode range (U+0D80–U+0DFF)
        // This is a simplified check - in production, you might want more sophisticated validation
        return text.Any(c => c >= 0x0D80 && c <= 0x0DFF) || 
               text.All(c => char.IsWhiteSpace(c) || char.IsPunctuation(c) || (c >= 0x0D80 && c <= 0x0DFF));
    }

    private static bool IsValidTamilText(string text)
    {
        // Basic validation for Tamil Unicode range (U+0B80–U+0BFF)
        // This is a simplified check - in production, you might want more sophisticated validation
        return text.Any(c => c >= 0x0B80 && c <= 0x0BFF) || 
               text.All(c => char.IsWhiteSpace(c) || char.IsPunctuation(c) || (c >= 0x0B80 && c <= 0x0BFF));
    }

    private static string TranslateCulturalContext(string context, LanguagePreference targetLanguage)
    {
        // In a real implementation, this would use a translation service
        // For now, return the English context as fallback
        return targetLanguage switch
        {
            LanguagePreference.Sinhala => GetSinhalaCulturalTranslation(context),
            LanguagePreference.Tamil => GetTamilCulturalTranslation(context),
            _ => context
        };
    }

    private static string GetSinhalaCulturalTranslation(string context)
    {
        // Common cultural translations for Sinhala
        return context.ToLowerInvariant() switch
        {
            var c when c.Contains("temple") => "විහාරස්ථානය",
            var c when c.Contains("prayer") => "ප්‍රාර්ථනා",
            var c when c.Contains("meditation") => "භාවනා",
            var c when c.Contains("festival") => "උත්සවය",
            var c when c.Contains("celebration") => "සැමරුම",
            _ => context // Fallback to English
        };
    }

    private static string GetTamilCulturalTranslation(string context)
    {
        // Common cultural translations for Tamil
        return context.ToLowerInvariant() switch
        {
            var c when c.Contains("temple") => "கோயில்",
            var c when c.Contains("prayer") => "பிரார்த்தனை",
            var c when c.Contains("devotion") => "பக்தி",
            var c when c.Contains("festival") => "திருவிழா",
            var c when c.Contains("celebration") => "கொண்டாட்டம்",
            _ => context // Fallback to English
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return English;
        yield return Sinhala ?? string.Empty;
        yield return Tamil ?? string.Empty;
        yield return PrimaryLanguage;
    }
}

/// <summary>
/// Language preference for diaspora communities
/// </summary>
public enum LanguagePreference
{
    English,
    Sinhala,
    Tamil
}


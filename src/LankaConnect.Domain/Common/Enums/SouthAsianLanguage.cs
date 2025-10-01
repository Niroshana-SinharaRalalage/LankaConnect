namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// South Asian languages supported by the cultural intelligence system
/// </summary>
public enum SouthAsianLanguage
{
    /// <summary>
    /// Sinhala - Primary language of Sri Lanka
    /// </summary>
    Sinhala = 1,

    /// <summary>
    /// Tamil - Spoken in Sri Lanka, India, and Tamil diaspora
    /// </summary>
    Tamil = 2,

    /// <summary>
    /// Hindi - Primary language of India
    /// </summary>
    Hindi = 3,

    /// <summary>
    /// Bengali - Spoken in Bangladesh and West Bengal
    /// </summary>
    Bengali = 4,

    /// <summary>
    /// Urdu - National language of Pakistan
    /// </summary>
    Urdu = 5,

    /// <summary>
    /// Punjabi - Spoken in Punjab regions of India and Pakistan
    /// </summary>
    Punjabi = 6,

    /// <summary>
    /// Malayalam - Spoken in Kerala, India
    /// </summary>
    Malayalam = 7,

    /// <summary>
    /// Telugu - Spoken in Andhra Pradesh and Telangana
    /// </summary>
    Telugu = 8,

    /// <summary>
    /// Gujarati - Spoken in Gujarat, India
    /// </summary>
    Gujarati = 9,

    /// <summary>
    /// Marathi - Spoken in Maharashtra, India
    /// </summary>
    Marathi = 10,

    /// <summary>
    /// Kannada - Spoken in Karnataka, India
    /// </summary>
    Kannada = 11,

    /// <summary>
    /// Odia - Spoken in Odisha, India
    /// </summary>
    Odia = 12,

    /// <summary>
    /// Nepali - National language of Nepal
    /// </summary>
    Nepali = 13,

    /// <summary>
    /// Dhivehi - National language of Maldives
    /// </summary>
    Dhivehi = 14,

    /// <summary>
    /// Bhutanese (Dzongkha) - National language of Bhutan
    /// </summary>
    Dzongkha = 15,

    /// <summary>
    /// Sanskrit - Classical language for religious texts
    /// </summary>
    Sanskrit = 16,

    /// <summary>
    /// Pali - Language of Buddhist scriptures
    /// </summary>
    Pali = 17,

    /// <summary>
    /// English - Widely used lingua franca
    /// </summary>
    English = 18,

    /// <summary>
    /// Multi-language support
    /// </summary>
    MultiLanguage = 19,

    /// <summary>
    /// Unknown or other South Asian language
    /// </summary>
    Other = 99
}

/// <summary>
/// Extensions for SouthAsianLanguage enum
/// </summary>
public static class SouthAsianLanguageExtensions
{
    /// <summary>
    /// Gets the ISO 639-1 language code for the South Asian language
    /// </summary>
    /// <param name="language">The South Asian language</param>
    /// <returns>ISO language code</returns>
    public static string GetLanguageCode(this SouthAsianLanguage language)
    {
        return language switch
        {
            SouthAsianLanguage.Sinhala => "si",
            SouthAsianLanguage.Tamil => "ta",
            SouthAsianLanguage.Hindi => "hi",
            SouthAsianLanguage.Bengali => "bn",
            SouthAsianLanguage.Urdu => "ur",
            SouthAsianLanguage.Punjabi => "pa",
            SouthAsianLanguage.Malayalam => "ml",
            SouthAsianLanguage.Telugu => "te",
            SouthAsianLanguage.Gujarati => "gu",
            SouthAsianLanguage.Marathi => "mr",
            SouthAsianLanguage.Kannada => "kn",
            SouthAsianLanguage.Odia => "or",
            SouthAsianLanguage.Nepali => "ne",
            SouthAsianLanguage.Dhivehi => "dv",
            SouthAsianLanguage.Dzongkha => "dz",
            SouthAsianLanguage.Sanskrit => "sa",
            SouthAsianLanguage.Pali => "pi",
            SouthAsianLanguage.English => "en",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Gets the display name for the South Asian language
    /// </summary>
    /// <param name="language">The South Asian language</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this SouthAsianLanguage language)
    {
        return language switch
        {
            SouthAsianLanguage.Sinhala => "Sinhala (සිංහල)",
            SouthAsianLanguage.Tamil => "Tamil (தமிழ்)",
            SouthAsianLanguage.Hindi => "Hindi (हिन्दी)",
            SouthAsianLanguage.Bengali => "Bengali (বাংলা)",
            SouthAsianLanguage.Urdu => "Urdu (اردو)",
            SouthAsianLanguage.Punjabi => "Punjabi (ਪੰਜਾਬੀ)",
            SouthAsianLanguage.Malayalam => "Malayalam (മലയാളം)",
            SouthAsianLanguage.Telugu => "Telugu (తెలుగు)",
            SouthAsianLanguage.Gujarati => "Gujarati (ગુજરાતી)",
            SouthAsianLanguage.Marathi => "Marathi (मराठी)",
            SouthAsianLanguage.Kannada => "Kannada (ಕನ್ನಡ)",
            SouthAsianLanguage.Odia => "Odia (ଓଡ଼ିଆ)",
            SouthAsianLanguage.Nepali => "Nepali (नेपाली)",
            SouthAsianLanguage.Dhivehi => "Dhivehi (ދިވެހި)",
            SouthAsianLanguage.Dzongkha => "Dzongkha (རྫོང་ཁ)",
            SouthAsianLanguage.Sanskrit => "Sanskrit (संस्कृतम्)",
            SouthAsianLanguage.Pali => "Pali (पालि)",
            SouthAsianLanguage.English => "English",
            SouthAsianLanguage.MultiLanguage => "Multiple Languages",
            _ => "Other"
        };
    }

    /// <summary>
    /// Determines if the language is primarily used for religious purposes
    /// </summary>
    /// <param name="language">The South Asian language</param>
    /// <returns>True if primarily religious</returns>
    public static bool IsReligiousLanguage(this SouthAsianLanguage language)
    {
        return language is SouthAsianLanguage.Sanskrit or SouthAsianLanguage.Pali;
    }

    /// <summary>
    /// Determines if the language has right-to-left script
    /// </summary>
    /// <param name="language">The South Asian language</param>
    /// <returns>True if right-to-left</returns>
    public static bool IsRightToLeft(this SouthAsianLanguage language)
    {
        return language is SouthAsianLanguage.Urdu or SouthAsianLanguage.Dhivehi;
    }
}
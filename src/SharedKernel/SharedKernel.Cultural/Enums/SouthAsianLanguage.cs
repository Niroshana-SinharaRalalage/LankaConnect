namespace LankaConnect.SharedKernel.Cultural.Enums;

/// <summary>
/// Broad South Asian language set used by the Phase 6 cultural intelligence
/// routing engine for diaspora language preference detection.
/// </summary>
/// <remarks>
/// Distinct from <c>SriLankanLanguage</c> (narrow 3-value Sri Lankan operations
/// enum) — see ADR-008 + cultural-type-inventory.md §B.6 for the narrow-vs-broad
/// rationale. Both live in SharedKernel.Cultural.
///
/// Regional variants (SriLankanTamil, IndianTamil, PakistaniUrdu, IndianUrdu,
/// Arabic, Persian) absorbed in W2C.2 (2026-06-04) from the duplicate enum
/// previously in <c>LankaConnect.BuildingBlocks.Domain.Database.MultiLanguageRoutingModels</c>.
/// </remarks>
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

    // Regional variants — absorbed from the routing-models duplicate enum in W2C.2 (2026-06-04).

    /// <summary>Sri Lankan Tamil dialect (distinct from Indian Tamil)</summary>
    SriLankanTamil = 20,

    /// <summary>Indian Tamil dialect (distinct from Sri Lankan Tamil)</summary>
    IndianTamil = 21,

    /// <summary>Pakistani Urdu dialect</summary>
    PakistaniUrdu = 22,

    /// <summary>Indian Urdu dialect</summary>
    IndianUrdu = 23,

    /// <summary>Arabic — Middle Eastern diaspora liturgical/communication language</summary>
    Arabic = 24,

    /// <summary>Persian (Farsi) — Iranian/Afghan diaspora language</summary>
    Persian = 25,

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
            SouthAsianLanguage.SriLankanTamil => "ta-LK",
            SouthAsianLanguage.IndianTamil => "ta-IN",
            SouthAsianLanguage.PakistaniUrdu => "ur-PK",
            SouthAsianLanguage.IndianUrdu => "ur-IN",
            SouthAsianLanguage.Arabic => "ar",
            SouthAsianLanguage.Persian => "fa",
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
            SouthAsianLanguage.SriLankanTamil => "Sri Lankan Tamil",
            SouthAsianLanguage.IndianTamil => "Indian Tamil",
            SouthAsianLanguage.PakistaniUrdu => "Pakistani Urdu",
            SouthAsianLanguage.IndianUrdu => "Indian Urdu",
            SouthAsianLanguage.Arabic => "Arabic (العربية)",
            SouthAsianLanguage.Persian => "Persian (فارسی)",
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
        return language is SouthAsianLanguage.Sanskrit
            or SouthAsianLanguage.Pali
            or SouthAsianLanguage.Arabic; // Quranic / Islamic liturgical
    }

    /// <summary>
    /// Determines if the language has right-to-left script
    /// </summary>
    /// <param name="language">The South Asian language</param>
    /// <returns>True if right-to-left</returns>
    public static bool IsRightToLeft(this SouthAsianLanguage language)
    {
        return language is SouthAsianLanguage.Urdu
            or SouthAsianLanguage.Dhivehi
            or SouthAsianLanguage.PakistaniUrdu
            or SouthAsianLanguage.IndianUrdu
            or SouthAsianLanguage.Arabic
            or SouthAsianLanguage.Persian;
    }
}
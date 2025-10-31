using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Language code value object using Enumeration Pattern with ISO 639 codes
/// Represents supported languages for Sri Lankan diaspora community (ISO 639-1 / 639-3 codes)
/// Architecture: Strongly-typed value object with predefined list of South Asian + international languages
/// </summary>
public sealed class LanguageCode : ValueObject
{
    public string Code { get; }
    public string Name { get; }
    public string NativeName { get; }

    private LanguageCode(string code, string name, string nativeName)
    {
        Code = code;
        Name = name;
        NativeName = nativeName;
    }

    // Primary Sri Lankan Languages (ordered by priority)
    public static readonly LanguageCode Sinhala = new("si", "Sinhala", "සිංහල");
    public static readonly LanguageCode Tamil = new("ta", "Tamil", "தமிழ்");
    public static readonly LanguageCode English = new("en", "English", "English");

    // Major South Asian Languages
    public static readonly LanguageCode Hindi = new("hi", "Hindi", "हिन्दी");
    public static readonly LanguageCode Bengali = new("bn", "Bengali", "বাংলা");
    public static readonly LanguageCode Urdu = new("ur", "Urdu", "اردو");
    public static readonly LanguageCode Punjabi = new("pa", "Punjabi", "ਪੰਜਾਬੀ");
    public static readonly LanguageCode Gujarati = new("gu", "Gujarati", "ગુજરાતી");
    public static readonly LanguageCode Malayalam = new("ml", "Malayalam", "മലയാളം");
    public static readonly LanguageCode Kannada = new("kn", "Kannada", "ಕನ್ನಡ");
    public static readonly LanguageCode Telugu = new("te", "Telugu", "తెలుగు");
    public static readonly LanguageCode Marathi = new("mr", "Marathi", "मराठी");

    // Additional International Languages (for diaspora communities)
    public static readonly LanguageCode Arabic = new("ar", "Arabic", "العربية");
    public static readonly LanguageCode French = new("fr", "French", "Français");
    public static readonly LanguageCode German = new("de", "German", "Deutsch");
    public static readonly LanguageCode Spanish = new("es", "Spanish", "Español");
    public static readonly LanguageCode Italian = new("it", "Italian", "Italiano");
    public static readonly LanguageCode Portuguese = new("pt", "Portuguese", "Português");
    public static readonly LanguageCode Dutch = new("nl", "Dutch", "Nederlands");
    public static readonly LanguageCode Swedish = new("sv", "Swedish", "Svenska");

    /// <summary>
    /// All available language codes (immutable list, ordered by priority)
    /// </summary>
    public static IReadOnlyList<LanguageCode> All { get; } = new List<LanguageCode>
    {
        // Sri Lankan languages first
        Sinhala,
        Tamil,
        English,

        // Major South Asian languages
        Hindi,
        Bengali,
        Urdu,
        Punjabi,
        Gujarati,
        Malayalam,
        Kannada,
        Telugu,
        Marathi,

        // International languages
        Arabic,
        French,
        German,
        Spanish,
        Italian,
        Portuguese,
        Dutch,
        Swedish
    }.AsReadOnly();

    /// <summary>
    /// Creates a LanguageCode from an ISO 639 code string (case-insensitive)
    /// </summary>
    /// <param name="code">ISO 639 language code (e.g., "si", "ta", "en")</param>
    /// <returns>Result with LanguageCode or error</returns>
    public static Result<LanguageCode> FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<LanguageCode>.Failure("Language code cannot be null or empty");
        }

        // Case-insensitive lookup (ISO codes are case-insensitive)
        var language = All.FirstOrDefault(l =>
            string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

        if (language == null)
        {
            return Result<LanguageCode>.Failure(
                $"Language code '{code}' is not recognized. Supported codes: {string.Join(", ", All.Select(l => l.Code))}");
        }

        return Result<LanguageCode>.Success(language);
    }

    /// <summary>
    /// Value object equality based on Code (case-insensitive)
    /// </summary>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code.ToLowerInvariant();
    }

    /// <summary>
    /// Returns the name for display purposes
    /// </summary>
    public override string ToString() => Name;
}

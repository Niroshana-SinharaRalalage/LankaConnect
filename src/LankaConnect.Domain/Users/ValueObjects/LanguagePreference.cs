using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Language preference value object combining language code and proficiency level
/// Architecture: Composite value object for user language preferences
/// </summary>
public sealed class LanguagePreference : ValueObject
{
    public LanguageCode Language { get; internal set; } = null!;
    public ProficiencyLevel Proficiency { get; internal set; }

    // Parameterless constructor for EF Core
    private LanguagePreference()
    {
    }

    private LanguagePreference(LanguageCode language, ProficiencyLevel proficiency)
    {
        Language = language;
        Proficiency = proficiency;
    }

    /// <summary>
    /// Creates a LanguagePreference from language code and proficiency level
    /// </summary>
    public static Result<LanguagePreference> Create(LanguageCode? language, ProficiencyLevel proficiency)
    {
        if (language == null)
        {
            return Result<LanguagePreference>.Failure("Language code is required");
        }

        return Result<LanguagePreference>.Success(new LanguagePreference(language, proficiency));
    }

    /// <summary>
    /// Value object equality based on Language and Proficiency
    /// </summary>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return Proficiency;
    }

    /// <summary>
    /// Returns string representation for display
    /// </summary>
    public override string ToString() => $"{Language.Name} ({Proficiency})";
}

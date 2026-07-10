using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Cultural.Enums;

namespace LankaConnect.Modules.Communications.Domain.ValueObjects;

// Sprint Day 5 (2026-07-06) Consult #13.3 PASS A: minimal-stub VOs restored after
// LankaConnect.BuildingBlocks.Domain.Shared wipe. Same rationale as
// LankaEvents.Domain/ValueObjects/ContactInfoPrimitives.cs — post-sprint (Consult
// #13's tracked backlog item) these promote to a proper SharedKernel.ContactInfo
// project. For now: local stubs.

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) { Value = value; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email is required");
        var trimmed = value.Trim();
        if (!trimmed.Contains('@'))
            return Result<Email>.Failure("Email must contain '@'");
        return Result<Email>.Success(new Email(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
}

// UserEmail was an alias for BB.Domain.Shared.ValueObjects.Email — same shape.
public sealed class UserEmail : ValueObject
{
    public string Value { get; }

    private UserEmail(string value) { Value = value; }

    public static Result<UserEmail> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<UserEmail>.Failure("Email is required");
        var trimmed = value.Trim();
        if (!trimmed.Contains('@'))
            return Result<UserEmail>.Failure("Email must contain '@'");
        return Result<UserEmail>.Success(new UserEmail(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
}

public sealed class CulturalContext : ValueObject
{
    public string? CulturalBackground { get; }
    public string? PrimaryLanguage { get; }
    public string? ReligiousContext { get; }

    private CulturalContext() { }

    public CulturalContext(string? culturalBackground, string? primaryLanguage, string? religiousContext)
    {
        CulturalBackground = culturalBackground;
        PrimaryLanguage = primaryLanguage;
        ReligiousContext = religiousContext;
    }

    public static CulturalContext Create(string? culturalBackground = null, string? primaryLanguage = null, string? religiousContext = null)
        => new CulturalContext(culturalBackground, primaryLanguage, religiousContext);

    // Enum-shaped overloads matching pre-wipe caller signatures.
    public static Result<CulturalContext> Create(
        LankaConnect.SharedKernel.Cultural.Enums.SriLankanLanguage language,
        LankaConnect.SharedKernel.Cultural.Enums.CulturalBackground background,
        LankaConnect.SharedKernel.Cultural.Enums.GeographicRegion region)
        => Result<CulturalContext>.Success(new CulturalContext(background.ToString(), language.ToString(), region.ToString()));

    // 6-arg pre-wipe caller signature (WhatsAppMessage.cs). Same output as 3-arg.
    public static Result<CulturalContext> Create(
        LankaConnect.SharedKernel.Cultural.Enums.SriLankanLanguage language,
        LankaConnect.SharedKernel.Cultural.Enums.CulturalBackground background,
        LankaConnect.SharedKernel.Cultural.Enums.GeographicRegion region,
        LankaConnect.SharedKernel.Cultural.Enums.ReligiousContext religious,
        bool hasReligiousContent,
        bool hasReligiousObservances)
        => Result<CulturalContext>.Success(new CulturalContext(background.ToString(), language.ToString(), religious.ToString()));

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalBackground ?? string.Empty;
        yield return PrimaryLanguage ?? string.Empty;
        yield return ReligiousContext ?? string.Empty;
    }
}

public sealed class CulturalConflict : ValueObject
{
    public string ConflictType { get; }
    public string Description { get; }

    public CulturalConflict(string conflictType, string description)
    {
        ConflictType = conflictType ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ConflictType;
        yield return Description;
    }
}


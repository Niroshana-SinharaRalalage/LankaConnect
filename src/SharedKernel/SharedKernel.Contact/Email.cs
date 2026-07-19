using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Contact;

/// <summary>
/// Email address value object. Pan-platform primitive per ADR-006 — consumed by
/// Identity (user account email), LankaEvents (attendee contact + organizer contact),
/// Communications (newsletter subscribers, support tickets, verification recipients),
/// and any future product needing email as a first-class type.
/// </summary>
/// <remarks>
/// <para>
/// Promoted from <c>LankaConnect.Products.LankaEvents.Domain.ValueObjects</c> in
/// Wave 8.5-cleanup (2026-07-18) per ExtractabilityAudit GAP-6. Behavior-preserving
/// move: same trimming rule, same `@` presence check, same `Value` accessor.
/// </para>
/// <para>
/// Note: <c>LankaConnect.Modules.Communications.Domain.ValueObjects</c> holds a
/// twin <c>Email</c>/<c>UserEmail</c> stub used inside the Communications aggregate.
/// That aggregate-local pair is out of scope for this promotion (Consult #13 tracks
/// it as a follow-up). This class is the platform-wide primitive.
/// </para>
/// </remarks>
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

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

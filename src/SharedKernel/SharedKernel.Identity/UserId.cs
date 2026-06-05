using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Identity;

/// <summary>
/// Typed user identifier. Wraps a <see cref="Guid"/> so callers can't
/// accidentally pass a <c>UserId</c> where a <c>StorefrontId</c> is expected
/// (or any other Guid). Equality is by underlying value.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a value object, not a record struct</b>: consistency with the rest
/// of the codebase's <see cref="ValueObject"/>-derived primitives and to keep
/// the door open for invariants (e.g. forbid <c>Guid.Empty</c>) without
/// breaking equality semantics. Record struct works too but mixing patterns
/// hurts readability.
/// </para>
/// <para>
/// <b>Phase A scope</b>: this typed ID coexists with the legacy
/// <c>Guid userId</c> direct passing throughout LankaConnect.* code.
/// Wave 3 entity migration introduces typed IDs at the domain boundary;
/// legacy controllers continue to accept Guid and call <see cref="From"/>
/// to wrap.
/// </para>
/// </remarks>
public sealed class UserId : ValueObject
{
    /// <summary>The underlying GUID value. Use <see cref="Value"/> for serialization.</summary>
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    /// <summary>Wraps an existing Guid. Throws on <see cref="Guid.Empty"/>.</summary>
    /// <exception cref="ArgumentException">If <paramref name="value"/> is the empty GUID.</exception>
    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be Guid.Empty.", nameof(value));
        }
        return new UserId(value);
    }

    /// <summary>Generates a new random UserId.</summary>
    public static UserId NewId() => new(Guid.NewGuid());

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <summary>Implicit-cast-free Guid extraction for persistence boundaries.</summary>
    public static implicit operator Guid(UserId id) => id.Value;
}

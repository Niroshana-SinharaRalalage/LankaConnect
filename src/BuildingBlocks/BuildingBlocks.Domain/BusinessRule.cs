namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A business invariant a domain operation must satisfy. Concrete rules
/// implement <see cref="IsBroken"/> and <see cref="BrokenError"/>; the
/// <see cref="Check"/> helper converts a rule check into a <see cref="Result"/>.
/// </summary>
/// <remarks>
/// <para>
/// Pattern:
/// </para>
/// <code>
/// public sealed class CannotRsvpToCancelledEvent : BusinessRule
/// {
///     public CannotRsvpToCancelledEvent(EventStatus status) { _status = status; }
///     public override bool IsBroken() =&gt; _status == EventStatus.Cancelled;
///     public override Error BrokenError =&gt; new("Event.Rsvp.Cancelled", "Cannot RSVP to a cancelled event.");
/// }
///
/// var result = BusinessRule.Check(new CannotRsvpToCancelledEvent(event.Status));
/// if (result.IsFailure) return result;
/// </code>
/// <para>
/// Why a class hierarchy and not just a <see cref="Func{TResult}"/>?
/// Named rules surface as types in stack traces, test names, and
/// architecture diagrams — they document the domain. A delegate doesn't.
/// </para>
/// </remarks>
public abstract class BusinessRule
{
    /// <summary>True when the rule is violated.</summary>
    public abstract bool IsBroken();

    /// <summary>The error to return when <see cref="IsBroken"/> is true.</summary>
    public abstract Error BrokenError { get; }

    /// <summary>
    /// Converts a single rule into a <see cref="Result"/>. Use at the start of
    /// state-mutating methods on aggregates.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="rule"/> is null.</exception>
    public static Result Check(BusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.IsBroken() ? Result.Failure(rule.BrokenError) : Result.Success();
    }

    /// <summary>
    /// Converts an array of rules into a single <see cref="Result"/>. Returns
    /// the FIRST broken rule's error so the caller sees the most-specific
    /// failure. Empty input is treated as success.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="rules"/> is null.</exception>
    public static Result CheckAll(params BusinessRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);
            if (rule.IsBroken())
            {
                return Result.Failure(rule.BrokenError);
            }
        }
        return Result.Success();
    }
}

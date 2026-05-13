namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A typed error that can flow through a <see cref="Result"/> or <see cref="Result{T}"/>.
/// </summary>
/// <param name="Code">Stable machine-readable error code (e.g. <c>Event.Cancellation.AlreadyCancelled</c>).</param>
/// <param name="Message">Human-readable description, suitable for logs and UI surfaces.</param>
/// <remarks>
/// <para>
/// Codes should follow a hierarchical dotted convention (<c>Module.Subject.Specifics</c>)
/// so they remain stable across refactors and i18n message changes. Per ADR-001
/// the message string is intentionally English-only at the domain layer; i18n
/// happens at the presentation boundary using the code as the lookup key.
/// </para>
/// <para>
/// Compared to <c>System.Exception</c>, an <see cref="Error"/> is cheap, allocation-friendly,
/// and forces callers to handle failure explicitly via the <see cref="Result"/> API
/// instead of relying on unchecked exception propagation. Throw is reserved for
/// programmer errors (invariant violations); domain failures return errors.
/// </para>
/// </remarks>
public sealed record Error(string Code, string Message)
{
    /// <summary>Sentinel "no error" instance returned by successful <see cref="Result"/> values.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Generic "null value supplied where one was required" error.</summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided where a value is required.");

    /// <summary>Generic "resource not found" error.</summary>
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found.");

    /// <summary>Generic "input validation failed" error.</summary>
    public static readonly Error Validation = new("Error.Validation", "Input failed validation.");

    /// <summary>Generic "operation conflicts with current state" error.</summary>
    public static readonly Error Conflict = new("Error.Conflict", "The operation conflicts with the current state.");

    /// <summary>Generic "caller is not allowed to perform this operation" error.</summary>
    public static readonly Error Forbidden = new("Error.Forbidden", "The caller is not permitted to perform this operation.");

    /// <summary>
    /// Returns <c>true</c> when this error represents the success sentinel <see cref="None"/>.
    /// </summary>
    public bool IsNone => string.IsNullOrEmpty(Code);

    /// <summary>
    /// Formatted view used in logs: <c>[Code] Message</c>.
    /// </summary>
    public override string ToString() => IsNone ? "(none)" : $"[{Code}] {Message}";
}

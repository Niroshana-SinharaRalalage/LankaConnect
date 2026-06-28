namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30, plan §3.2): per-registration audit row
/// recorded for every registration touched by a conversion (migrated, skipped, OR failed).
/// Joined to <see cref="RegistrationModeConversion"/> via <see cref="AggregateConversionId"/>.
/// </summary>
public class RegistrationModeConversionRow : LankaConnect.Domain.Common.LegacyBaseEntity
{
    public Guid AggregateConversionId { get; private set; }
    public Guid RegistrationId { get; private set; }
    public ConversionOutcome ConversionOutcome { get; private set; }

    /// <summary>
    /// Stable code (e.g. <c>GenderOtherNotSupportedByMode</c>) when the row was skipped or
    /// failed. Null on Migrated rows.
    /// </summary>
    public string? OutcomeReason { get; private set; }

    /// <summary>
    /// Snapshot of the registration's <c>RowVersion</c> at conversion time — useful when
    /// support replays the audit trail and needs to know whether the registration was edited
    /// later.
    /// </summary>
    public byte[]? RegistrationRowVersionSnapshot { get; private set; }

    /// <summary>
    /// JSONB snapshot of the pre-conversion shape (Attendees array OR HeadCountBreakdown).
    /// Null on rows where the registration wasn't actually mutated (skipped / failed).
    /// </summary>
    public string? BeforeShape { get; private set; }

    /// <summary>
    /// JSONB snapshot of the post-conversion shape. Null on skipped / failed rows.
    /// </summary>
    public string? AfterShape { get; private set; }

    public DateTime ConvertedAt { get; private set; }

    private RegistrationModeConversionRow() { /* EF Core */ }

    private RegistrationModeConversionRow(
        Guid aggregateConversionId, Guid registrationId,
        ConversionOutcome outcome, string? outcomeReason,
        byte[]? registrationRowVersionSnapshot,
        string? beforeShape, string? afterShape,
        DateTime convertedAt)
    {
        AggregateConversionId = aggregateConversionId;
        RegistrationId = registrationId;
        ConversionOutcome = outcome;
        OutcomeReason = outcomeReason;
        RegistrationRowVersionSnapshot = registrationRowVersionSnapshot;
        BeforeShape = beforeShape;
        AfterShape = afterShape;
        ConvertedAt = convertedAt;
    }

    public static RegistrationModeConversionRow ForMigrated(
        Guid aggregateConversionId, Guid registrationId,
        string beforeShape, string afterShape, DateTime convertedAt,
        byte[]? registrationRowVersionSnapshot = null) =>
        new(aggregateConversionId, registrationId,
            ConversionOutcome.Migrated, outcomeReason: null,
            registrationRowVersionSnapshot, beforeShape, afterShape, convertedAt);

    public static RegistrationModeConversionRow ForSkipped(
        Guid aggregateConversionId, Guid registrationId,
        string reasonCode, DateTime convertedAt,
        byte[]? registrationRowVersionSnapshot = null) =>
        new(aggregateConversionId, registrationId,
            ConversionOutcome.Skipped, outcomeReason: reasonCode,
            registrationRowVersionSnapshot, beforeShape: null, afterShape: null, convertedAt);

    public static RegistrationModeConversionRow ForFailed(
        Guid aggregateConversionId, Guid registrationId,
        string failureReason, DateTime convertedAt,
        byte[]? registrationRowVersionSnapshot = null) =>
        new(aggregateConversionId, registrationId,
            ConversionOutcome.Failed, outcomeReason: failureReason,
            registrationRowVersionSnapshot, beforeShape: null, afterShape: null, convertedAt);
}

public enum ConversionOutcome : short
{
    Migrated = 0,
    Skipped = 1,
    Failed = 2,
}

using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain.Services;

/// <summary>
/// Phase 7F-B: result of <see cref="Event.ConvertRegistrationMode"/>.
///
/// Carries one row per registration that was successfully migrated AND one row per
/// registration that was skipped with a reason. The shape is rich enough for both:
/// (a) the UI diff-preview (architect plan §3 7F-B.5), and
/// (b) the audit table inserts (architect plan §3 split into aggregate + per-row tables).
/// </summary>
public sealed class ConversionReport
{
    public IReadOnlyList<MigratedRow> Migrated { get; }
    public IReadOnlyList<SkippedRow> Skipped { get; }

    public int TotalProcessed => Migrated.Count + Skipped.Count;

    public ConversionReport(IReadOnlyList<MigratedRow> migrated, IReadOnlyList<SkippedRow> skipped)
    {
        Migrated = migrated;
        Skipped = skipped;
    }

    public static ConversionReport Empty => new(Array.Empty<MigratedRow>(), Array.Empty<SkippedRow>());
}

/// <summary>
/// One row per migrated registration. Carries before + after shapes so the audit table
/// snapshot can be persisted as JSONB.
/// </summary>
public sealed record MigratedRow(
    Guid RegistrationId,
    /// <summary>Mode A source — null when source is Mode B.</summary>
    IReadOnlyList<AttendeeDetails>? BeforeAttendees,
    /// <summary>Mode B source — null when source is Mode A.</summary>
    HeadCountBreakdown? BeforeHeadCount,
    /// <summary>Mode A target — null when target is Mode B.</summary>
    IReadOnlyList<AttendeeDetails>? AfterAttendees,
    /// <summary>Mode B target — null when target is Mode A.</summary>
    HeadCountBreakdown? AfterHeadCount,
    string? AfterLeadAttendeeName);

/// <summary>
/// One row per registration that was NOT migrated, with a stable reason code the audit
/// + UI can switch on.
/// </summary>
public sealed record SkippedRow(
    Guid RegistrationId,
    string ReasonCode,
    string Reason);

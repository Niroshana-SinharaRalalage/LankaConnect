using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ConvertRegistrationMode;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30, plan §3 7F-B.3): convert all *active*
/// registrations on an event from one <see cref="RegistrationMode"/> to another, performing
/// the appropriate per-registration backfill.
///
/// When <see cref="DryRun"/> is true the handler computes the conversion report (and writes
/// the audit aggregate row marking it as a preview) WITHOUT mutating any registration. The
/// UI uses this to render the diff-preview confirmation dialog before the user commits.
///
/// When <see cref="NotifyAttendees"/> is true (default false), the handler raises a domain
/// event the email pipeline picks up to send each affected registrant a "your registration
/// format changed" mail. Default-off avoids surprise inbox traffic during operator testing.
/// </summary>
public record ConvertRegistrationModeCommand(
    Guid EventId,
    RegistrationMode TargetMode,
    bool DryRun = false,
    bool NotifyAttendees = false
) : ICommand<ConvertRegistrationModeResult>;

/// <summary>
/// Result envelope. Carries enough info for the UI to render the confirmation dialog
/// (Migrated count, Skipped count + reasons) plus the aggregate audit ID for follow-up
/// support traces.
/// </summary>
public record ConvertRegistrationModeResult(
    Guid? AggregateConversionId,
    int TotalProcessed,
    int MigratedCount,
    int SkippedCount,
    IReadOnlyList<ConvertedRegistrationRow> Migrated,
    IReadOnlyList<SkippedRegistrationRow> Skipped,
    bool WasDryRun);

public record ConvertedRegistrationRow(
    Guid RegistrationId,
    int BeforeAttendeeCount,
    int AfterAttendeeCount);

public record SkippedRegistrationRow(
    Guid RegistrationId,
    string ReasonCode,
    string Reason);

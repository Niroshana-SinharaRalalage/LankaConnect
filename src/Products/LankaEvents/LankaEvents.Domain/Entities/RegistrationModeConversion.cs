using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30, plan §3): aggregate audit row recorded
/// once per organiser conversion action. Cheap dashboard joins; the per-registration
/// detail rows live in <see cref="RegistrationModeConversionRow"/>.
/// </summary>
public class RegistrationModeConversion : LegacyBaseEntity
{
    public Guid EventId { get; private set; }
    public Guid OrganiserId { get; private set; }
    public RegistrationMode FromMode { get; private set; }
    public RegistrationMode ToMode { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public int TotalCount { get; private set; }
    public int MigratedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public int FailedCount { get; private set; }

    /// <summary>
    /// EventRowVersion snapshot at the time of conversion. Stored for forensic replay
    /// (architect §3.1) — supports answering "did the event change between preview and
    /// commit?" after the fact.
    /// </summary>
    public byte[]? EventRowVersionSnapshot { get; private set; }

    private RegistrationModeConversion() { /* EF Core */ }

    private RegistrationModeConversion(
        Guid eventId, Guid organiserId,
        RegistrationMode fromMode, RegistrationMode toMode,
        DateTime startedAt, DateTime completedAt,
        int totalCount, int migratedCount, int skippedCount, int failedCount,
        byte[]? eventRowVersionSnapshot)
    {
        EventId = eventId;
        OrganiserId = organiserId;
        FromMode = fromMode;
        ToMode = toMode;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        TotalCount = totalCount;
        MigratedCount = migratedCount;
        SkippedCount = skippedCount;
        FailedCount = failedCount;
        EventRowVersionSnapshot = eventRowVersionSnapshot;
    }

    public static RegistrationModeConversion Create(
        Guid eventId, Guid organiserId,
        RegistrationMode fromMode, RegistrationMode toMode,
        DateTime startedAt, DateTime completedAt,
        int totalCount, int migratedCount, int skippedCount, int failedCount = 0,
        byte[]? eventRowVersionSnapshot = null) =>
        new(eventId, organiserId, fromMode, toMode, startedAt, completedAt,
            totalCount, migratedCount, skippedCount, failedCount, eventRowVersionSnapshot);
}

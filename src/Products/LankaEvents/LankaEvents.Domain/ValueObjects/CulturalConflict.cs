using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Events-specific cultural conflict record. Simpler shape than the canonical
/// <see cref="LankaConnect.SharedKernel.Cultural.CulturalConflict"/> class —
/// used by Events context for lightweight conflict-level reporting.
/// </summary>
/// <remarks>
/// W2D.1b (2026-06-05): RENAMED from `CulturalConflict` to `EventCulturalConflict`
/// per architect Q3 ruling — distinct concept that shared a name with the
/// canonical Communications class. See cultural-type-inventory.md §B.2.
/// </remarks>
public record EventCulturalConflict
{
    public bool HasConflict { get; init; }
    public CulturalConflictLevel ConflictLevel { get; init; }
    public string Reason { get; init; }
    public string Suggestion { get; init; }
    public DateTime ConflictDate { get; init; }
    public string ConflictingObservance { get; init; }

    public EventCulturalConflict(bool hasConflict, CulturalConflictLevel conflictLevel,
        string reason, string suggestion, DateTime conflictDate = default, string conflictingObservance = "")
    {
        HasConflict = hasConflict;
        ConflictLevel = conflictLevel;
        Reason = reason ?? string.Empty;
        Suggestion = suggestion ?? string.Empty;
        ConflictDate = conflictDate;
        ConflictingObservance = conflictingObservance ?? string.Empty;
    }

    public static EventCulturalConflict None() =>
        new(false, CulturalConflictLevel.None, string.Empty, string.Empty);

    public static EventCulturalConflict Create(CulturalConflictLevel level, string reason, string suggestion,
        DateTime conflictDate = default, string conflictingObservance = "") =>
        new(level != CulturalConflictLevel.None, level, reason, suggestion, conflictDate, conflictingObservance);
}
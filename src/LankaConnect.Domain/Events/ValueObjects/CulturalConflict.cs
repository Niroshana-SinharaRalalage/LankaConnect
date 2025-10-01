using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.ValueObjects;

public record CulturalConflict
{
    public bool HasConflict { get; init; }
    public CulturalConflictLevel ConflictLevel { get; init; }
    public string Reason { get; init; }
    public string Suggestion { get; init; }
    public DateTime ConflictDate { get; init; }
    public string ConflictingObservance { get; init; }

    public CulturalConflict(bool hasConflict, CulturalConflictLevel conflictLevel, 
        string reason, string suggestion, DateTime conflictDate = default, string conflictingObservance = "")
    {
        HasConflict = hasConflict;
        ConflictLevel = conflictLevel;
        Reason = reason ?? string.Empty;
        Suggestion = suggestion ?? string.Empty;
        ConflictDate = conflictDate;
        ConflictingObservance = conflictingObservance ?? string.Empty;
    }

    public static CulturalConflict None() => 
        new(false, CulturalConflictLevel.None, string.Empty, string.Empty);

    public static CulturalConflict Create(CulturalConflictLevel level, string reason, string suggestion, 
        DateTime conflictDate = default, string conflictingObservance = "") =>
        new(level != CulturalConflictLevel.None, level, reason, suggestion, conflictDate, conflictingObservance);
}
namespace LankaConnect.SharedKernel.Cultural;

/// <summary>
/// Religious observance severity levels affecting scheduling-conflict resolution.
/// Used by <see cref="CulturalConflict"/> (W2D.1b) and other cultural conflict
/// detection to grade how strictly an observance period must be respected.
/// </summary>
/// <remarks>
/// Extracted from a nested declaration in
/// <c>LankaConnect.Domain.Communications.ValueObjects.GoogleCalendarCulturalEvent.cs</c>
/// in W2C.6 (2026-06-05) per ADR-008. Promoted to its own file in
/// SharedKernel.Cultural alongside <see cref="CulturalEventType"/> so the
/// cultural-conflict and cultural-event value objects can move cleanly in W2D.1b.
/// </remarks>
public enum ReligiousObservanceLevel
{
    None,
    Low,
    Medium,
    High,
    Highest
}

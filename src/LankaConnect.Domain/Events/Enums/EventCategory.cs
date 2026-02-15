namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Event category classification
/// Phase 6A.109: Added Workshop, Festival, Ceremony, Celebration to sync with database
/// </summary>
public enum EventCategory
{
    Religious,      // 0
    Cultural,       // 1
    Community,      // 2
    Educational,    // 3
    Social,         // 4
    Business,       // 5
    Charity,        // 6
    Entertainment,  // 7
    Workshop,       // 8 - Phase 6A.109: Added to sync with database (Issue #78)
    Festival,       // 9 - Phase 6A.109: Added to sync with database (Issue #78)
    Ceremony,       // 10 - Phase 6A.109: Added to sync with database (Issue #78)
    Celebration     // 11 - Phase 6A.109: Added to sync with database (Issue #78)
}
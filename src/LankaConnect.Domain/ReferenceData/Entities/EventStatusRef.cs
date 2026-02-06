namespace LankaConnect.Domain.ReferenceData.Entities;

/// <summary>
/// Event Status reference data
/// Phase 6A.47: Replaces EventStatus enum with database-driven reference data
/// </summary>
public class EventStatusRef : ReferenceDataBase
{
    /// <summary>
    /// Whether registrations are allowed in this status
    /// </summary>
    public bool AllowsRegistration { get; private set; }

    /// <summary>
    /// Whether this is a final state (no further transitions allowed)
    /// </summary>
    public bool IsFinalState { get; private set; }

    private EventStatusRef(
        Guid id,
        string code,
        string name,
        int displayOrder,
        bool allowsRegistration,
        bool isFinalState,
        string? description = null)
        : base(id, code, name, displayOrder, description)
    {
        AllowsRegistration = allowsRegistration;
        IsFinalState = isFinalState;
    }

    private EventStatusRef() : base()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Factory method to create a new EventStatusRef
    /// </summary>
    public static EventStatusRef Create(
        Guid id,
        string code,
        string name,
        int displayOrder,
        bool allowsRegistration,
        bool isFinalState,
        string? description = null)
    {
        return new EventStatusRef(id, code, name, displayOrder, allowsRegistration, isFinalState, description) { Id = id };
    }
}

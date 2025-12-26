namespace LankaConnect.Domain.ReferenceData.Entities;

/// <summary>
/// Event Category reference data
/// Phase 6A.47: Replaces EventCategory enum + expands with Cultural Interests
/// </summary>
public class EventCategoryRef : ReferenceDataBase
{
    /// <summary>
    /// Optional icon URL or icon name for UI display
    /// </summary>
    public string? IconUrl { get; private set; }

    private EventCategoryRef(
        Guid id,
        string code,
        string name,
        int displayOrder,
        string? description = null,
        string? iconUrl = null)
        : base(id, code, name, displayOrder, description)
    {
        IconUrl = iconUrl;
    }

    private EventCategoryRef() : base()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Factory method to create a new EventCategoryRef
    /// </summary>
    public static EventCategoryRef Create(
        Guid id,
        string code,
        string name,
        int displayOrder,
        string? description = null,
        string? iconUrl = null)
    {
        return new EventCategoryRef(id, code, name, displayOrder, description, iconUrl) { Id = id };
    }

    /// <summary>
    /// Update icon URL
    /// </summary>
    public void UpdateIcon(string? iconUrl)
    {
        IconUrl = iconUrl;
        MarkAsUpdated();
    }
}

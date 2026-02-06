using LankaConnect.Domain.Common.Entities;

namespace LankaConnect.Domain.ReferenceData.Entities;

/// <summary>
/// Base class for all reference data entities
/// Phase 6A.47: Centralized reference data management
/// </summary>
public abstract class ReferenceDataBase : EntityBase
{
    /// <summary>
    /// Unique code for the reference data item (used for lookups and migrations)
    /// </summary>
    public string Code { get; protected set; } = null!;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Optional detailed description
    /// </summary>
    public string? Description { get; protected set; }

    /// <summary>
    /// Display order for sorting in UI
    /// </summary>
    public int DisplayOrder { get; protected set; }

    /// <summary>
    /// Soft delete flag - inactive items should not be shown in UI
    /// </summary>
    public bool IsActive { get; protected set; } = true;

    protected ReferenceDataBase(Guid id, string code, string name, int displayOrder, string? description = null)
        : base(id)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayOrder = displayOrder;
        Description = description;
    }

    protected ReferenceDataBase() : base()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Deactivate this reference data item (soft delete)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reactivate this reference data item
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update display order
    /// </summary>
    public void UpdateDisplayOrder(int newDisplayOrder)
    {
        DisplayOrder = newDisplayOrder;
        MarkAsUpdated();
    }
}

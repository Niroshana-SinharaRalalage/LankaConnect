using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Organizer-defined purchasable add-on item for an event.
/// Examples: snacks, meals, beverages, parking passes, merchandise.
///
/// Stock management is handled via atomic SQL UPDATE (not EF Core tracking)
/// to prevent race conditions under concurrent purchases.
///
/// QuantitySold is managed by AddOnDefinitionRepository.TryReserveStockAsync()
/// and should NOT be modified through EF Core change tracking.
/// </summary>
public class AddOnDefinition : BaseEntity
{
    public const int MAX_NAME_LENGTH = 200;
    public const int MAX_DESCRIPTION_LENGTH = 1000;

    // Event linkage
    public Guid EventId { get; private set; }

    // Add-on details
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Maximum quantity available for purchase. Null means unlimited.
    /// </summary>
    public int? QuantityLimit { get; private set; }

    /// <summary>
    /// Current quantity sold/reserved.
    /// IMPORTANT: Managed via atomic SQL, not EF Core tracking.
    /// Do NOT modify this property through domain methods.
    /// </summary>
    public int QuantitySold { get; private set; }

    /// <summary>
    /// Whether this add-on is active and available for purchase.
    /// Soft-delete: set to false to hide without deleting.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order for the add-on list.
    /// </summary>
    public int SortOrder { get; private set; }

    // EF Core constructor
    private AddOnDefinition()
    {
    }

    /// <summary>
    /// Creates a new add-on definition.
    /// </summary>
    public static Result<AddOnDefinition> Create(
        Guid eventId,
        string name,
        string? description,
        Money price,
        int? quantityLimit,
        int sortOrder = 0)
    {
        if (eventId == Guid.Empty)
            return Result<AddOnDefinition>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(name))
            return Result<AddOnDefinition>.Failure("Add-on name is required");

        if (name.Length > MAX_NAME_LENGTH)
            return Result<AddOnDefinition>.Failure($"Add-on name cannot exceed {MAX_NAME_LENGTH} characters");

        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result<AddOnDefinition>.Failure($"Add-on description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        if (price == null)
            return Result<AddOnDefinition>.Failure("Add-on price is required");

        if (price.Amount < 0)
            return Result<AddOnDefinition>.Failure("Add-on price cannot be negative");

        if (quantityLimit.HasValue && quantityLimit.Value <= 0)
            return Result<AddOnDefinition>.Failure("Quantity limit must be greater than zero");

        var definition = new AddOnDefinition
        {
            EventId = eventId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = price,
            QuantityLimit = quantityLimit,
            QuantitySold = 0,
            IsActive = true,
            SortOrder = sortOrder
        };

        return Result<AddOnDefinition>.Success(definition);
    }

    /// <summary>
    /// Updates the add-on details. Cannot change price if any purchases exist
    /// (price is snapshotted at purchase time, but changing it mid-event is confusing).
    /// </summary>
    public Result UpdateDetails(
        string name,
        string? description,
        Money price,
        int? quantityLimit,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Add-on name is required");

        if (name.Length > MAX_NAME_LENGTH)
            return Result.Failure($"Add-on name cannot exceed {MAX_NAME_LENGTH} characters");

        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result.Failure($"Add-on description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        if (price == null)
            return Result.Failure("Add-on price is required");

        if (price.Amount < 0)
            return Result.Failure("Add-on price cannot be negative");

        if (quantityLimit.HasValue && quantityLimit.Value <= 0)
            return Result.Failure("Quantity limit must be greater than zero");

        // Validate new quantity limit is not below already sold
        if (quantityLimit.HasValue && quantityLimit.Value < QuantitySold)
            return Result.Failure($"Quantity limit ({quantityLimit.Value}) cannot be less than quantity already sold ({QuantitySold})");

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        QuantityLimit = quantityLimit;
        SortOrder = sortOrder;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the add-on (soft delete — hides from purchase but retains data).
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Add-on is already inactive");

        IsActive = false;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Reactivates a previously deactivated add-on.
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Add-on is already active");

        IsActive = true;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Checks if there is available stock for the requested quantity.
    /// </summary>
    public bool HasAvailableStock(int requestedQty = 1)
    {
        if (!QuantityLimit.HasValue) return true;
        return QuantitySold + requestedQty <= QuantityLimit.Value;
    }

    /// <summary>
    /// Remaining stock count. Null means unlimited.
    /// </summary>
    public int? RemainingStock => QuantityLimit.HasValue
        ? QuantityLimit.Value - QuantitySold
        : null;
}

using System.ComponentModel.DataAnnotations.Schema;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Entity representing a pass/ticket type for an event
/// Examples: Adult Pass, Child Pass, Food Ticket, VIP Pass
/// </summary>
// W3C (2026-06-06): EventPass migrated to BB.Domain.Entity<Guid> + IAuditable per ADR-007.
public class EventPass : LankaConnect.BuildingBlocks.Domain.Entity<Guid>, LankaConnect.BuildingBlocks.Domain.IAuditable
{
    // IAuditable members — interceptor-populated.
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public IReadOnlyList<LankaConnect.BuildingBlocks.Domain.IDomainEvent> GetDomainEvents() => DomainEvents;

    // Wave 5.1.a-α.3 (2026-06-27): PassName/PassDescription VOs decomposed to scalar
    // strings + [NotMapped] facades — same Option A pattern applied to Money for
    // EventPass.Price. PassName/PassDescription are cross-assembly ComplexProperty
    // types after the move; EF Core 8 fails to bind their private constructors at
    // model discovery. Scalar pattern removes them from EF's model graph entirely.
    public string NameValue { get; private set; }
    public string DescriptionValue { get; private set; }

    [NotMapped]
    public PassName Name => PassName.Create(NameValue).Value;

    [NotMapped]
    public PassDescription Description => PassDescription.Create(DescriptionValue).Value;

    // Wave 5.1.a-α (2026-06-27): Money decomposed into 2 scalar columns + [NotMapped] facade.
    public decimal PriceAmount { get; private set; }
    public Currency PriceCurrency { get; private set; }

    [NotMapped]
    public Money Price => Money.Create(PriceAmount, PriceCurrency).Value;

    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity => TotalQuantity - ReservedQuantity;

    // EF Core constructor
    private EventPass()
    {
        NameValue = null!;
        DescriptionValue = null!;
    }

    private EventPass(PassName name, PassDescription description, Money price, int totalQuantity)
    {
        // W3C (2026-06-06): explicit Id init — see Notification W3A migration notes.
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        NameValue = name.Value;
        DescriptionValue = description.Value;
        PriceAmount = price.Amount;
        PriceCurrency = price.Currency;
        TotalQuantity = totalQuantity;
        ReservedQuantity = 0;
    }

    public static Result<EventPass> Create(
        PassName name,
        PassDescription description,
        Money price,
        int quantity)
    {
        if (name == null)
            return Result<EventPass>.Failure("Pass name is required");

        if (description == null)
            return Result<EventPass>.Failure("Pass description is required");

        if (price == null)
            return Result<EventPass>.Failure("Pass price is required");

        if (quantity <= 0)
            return Result<EventPass>.Failure("Quantity must be greater than 0");

        var eventPass = new EventPass(name, description, price, quantity);
        return Result<EventPass>.Success(eventPass);
    }

    /// <summary>
    /// Reserves a quantity of passes (decreases available quantity)
    /// Called when user purchases passes
    /// </summary>
    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (AvailableQuantity < quantity)
            return Result.Failure("Insufficient passes available");

        ReservedQuantity += quantity;

        return Result.Success();
    }

    /// <summary>
    /// Releases reserved passes (increases available quantity)
    /// Called when user cancels purchase
    /// </summary>
    public Result Release(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (ReservedQuantity < quantity)
            return Result.Failure("Cannot release more than reserved");

        ReservedQuantity -= quantity;

        return Result.Success();
    }

    /// <summary>
    /// Updates the pass details
    /// </summary>
    public Result Update(PassName name, PassDescription description, Money price)
    {
        if (name == null)
            return Result.Failure("Pass name is required");

        if (description == null)
            return Result.Failure("Pass description is required");

        if (price == null)
            return Result.Failure("Pass price is required");

        NameValue = name.Value;
        DescriptionValue = description.Value;
        PriceAmount = price.Amount;
        PriceCurrency = price.Currency;

        return Result.Success();
    }

    /// <summary>
    /// Increases the total quantity available
    /// </summary>
    public Result IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            return Result.Failure("Amount must be greater than 0");

        TotalQuantity += amount;

        return Result.Success();
    }
}

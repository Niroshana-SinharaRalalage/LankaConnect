using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.PurchaseAddOnCart;

/// <summary>
/// Cart item representing one add-on definition with a quantity.
/// </summary>
public record AddOnCartItem(
    Guid AddOnDefinitionId,
    int Quantity);

/// <summary>
/// Multi-add-on cart purchase command — creates a single Stripe Checkout session
/// with N line items (one per distinct add-on definition).
/// Free items complete immediately; only paid items appear on the Stripe checkout.
/// Returns: checkout URL if any paid items, or success URL if all items are free.
/// </summary>
public record PurchaseAddOnCartCommand(
    Guid EventId,
    List<AddOnCartItem> Items,
    string BuyerName,
    string BuyerEmail,
    string? BuyerPhone,
    string SuccessUrl,
    string CancelUrl,
    Guid? UserId = null
) : ICommand<string>;  // Returns checkout URL or success URL

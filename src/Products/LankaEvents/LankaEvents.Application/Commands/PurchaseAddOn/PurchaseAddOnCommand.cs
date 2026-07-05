using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PurchaseAddOn;

/// <summary>
/// Add-on purchase command — creates a Stripe Checkout session for purchasing event add-ons.
/// Returns the Stripe Checkout URL for the buyer to complete payment.
/// Supports both standalone and registration-bundled purchases.
/// </summary>
public record PurchaseAddOnCommand(
    Guid EventId,
    Guid AddOnDefinitionId,
    string BuyerName,
    string BuyerEmail,
    string? BuyerPhone,
    int Quantity,
    string SuccessUrl,
    string CancelUrl,
    // Null for anonymous purchases
    Guid? UserId = null,
    // Null for standalone purchases, set for registration-bundled
    Guid? RegistrationId = null
) : ICommand<string>;  // Returns checkout URL

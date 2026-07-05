using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateSignUpItem;

/// <summary>
/// Command to update sign-up item details (description, notes, and a type-specific size field).
/// Phase 6A.14: Edit Sign-Up Item feature
/// Phase 6A.131: Supports both quantity-based and slot-based items via dual discriminated fields.
/// The server loads the item and uses <c>SignUpItem.ItemType</c> as the authority — the client
/// cannot spoof the type. Exactly one of <see cref="TargetQuantity"/> or <see cref="AvailableSlots"/>
/// must be supplied; the handler validates the field matches the loaded item's type.
/// </summary>
public record UpdateSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    string ItemDescription,
    int? TargetQuantity,
    int? AvailableSlots,
    int? SuggestedPerSlot,
    string? Notes
) : ICommand;

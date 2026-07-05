using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddSignUpItem;

/// <summary>
/// Phase 6A.121: Command to add a signup item to a signup list.
/// Supports dual-field model: quantity-based (TargetQuantity) or slot-based (AvailableSlots).
/// </summary>
public record AddSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    string ItemDescription,
    SignUpItemType ItemType,
    SignUpItemCategory ItemCategory,
    int? TargetQuantity = null,
    int? AvailableSlots = null,
    int? SuggestedPerSlot = null,
    string? Notes = null
) : ICommand<Guid>; // Returns the newly created SignUpItem ID

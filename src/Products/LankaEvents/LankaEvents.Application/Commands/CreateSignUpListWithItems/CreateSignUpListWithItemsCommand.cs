using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSignUpListWithItems;

/// <summary>
/// Command to create a sign-up list with items in a single operation
/// Matches requirement: POST /api/events/{eventId}/signups with items array
/// Phase 6A.27: Added HasOpenItems for user-submitted items
/// Phase 7D.1: Added Kind with default Items for back-compat. When Kind=Volunteers
/// the handler routes to SignUpList.CreateVolunteerList (slot-only items).
/// </summary>
public record CreateSignUpListWithItemsCommand(
    Guid EventId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    List<SignUpItemDto> Items,
    bool HasOpenItems = false, // Phase 6A.27
    SignUpKind Kind = SignUpKind.Items // Phase 7D.1
) : ICommand<Guid>; // Returns the created sign-up list ID

/// <summary>
/// DTO for sign-up item within the command
/// Phase 6A.131: Added ItemType, TargetQuantity, AvailableSlots, SuggestedPerSlot for dual-field support
/// </summary>
public record SignUpItemDto(
    string ItemDescription,
    SignUpItemType ItemType,
    SignUpItemCategory ItemCategory,
    int? TargetQuantity = null,
    int? AvailableSlots = null,
    int? SuggestedPerSlot = null,
    string? Notes = null);

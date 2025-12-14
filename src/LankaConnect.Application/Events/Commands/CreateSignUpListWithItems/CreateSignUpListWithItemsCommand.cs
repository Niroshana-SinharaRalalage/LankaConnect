using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.CreateSignUpListWithItems;

/// <summary>
/// Command to create a sign-up list with items in a single operation
/// Matches requirement: POST /api/events/{eventId}/signups with items array
/// Phase 6A.27: Added HasOpenItems for user-submitted items
/// </summary>
public record CreateSignUpListWithItemsCommand(
    Guid EventId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    List<SignUpItemDto> Items,
    bool HasOpenItems = false // Phase 6A.27
) : ICommand<Guid>; // Returns the created sign-up list ID

/// <summary>
/// DTO for sign-up item within the command
/// </summary>
public record SignUpItemDto(
    string ItemDescription,
    int Quantity,
    SignUpItemCategory ItemCategory,
    string? Notes);

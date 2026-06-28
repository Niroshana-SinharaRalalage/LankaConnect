using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CommitToSignUpItem;

/// <summary>
/// Command to commit to a sign-up item.
/// Phase 2: Added optional contact information fields.
/// Phase 6A.125: Added PhysicalQuantity and SlotsClaimed for dual-field support.
/// Quantity is kept for backward compatibility but routes to PhysicalQuantity for quantity-based items.
/// </summary>
public record CommitToSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    Guid UserId,
    int Quantity,                           // Legacy - used as PhysicalQuantity for quantity-based items
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    int? PhysicalQuantity = null,           // Phase 6A.125: For quantity-based items (overrides Quantity if set)
    int? SlotsClaimed = null                // Phase 6A.125: For slot-based items
) : ICommand;

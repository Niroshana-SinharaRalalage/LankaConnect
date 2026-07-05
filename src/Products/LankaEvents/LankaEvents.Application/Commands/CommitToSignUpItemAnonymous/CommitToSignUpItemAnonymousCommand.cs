using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CommitToSignUpItemAnonymous;

/// <summary>
/// Command to commit to a sign-up item for anonymous users (not logged in).
/// Phase 6A.23: Supports anonymous sign-up workflow.
/// Phase 6A.125: Added PhysicalQuantity and SlotsClaimed for dual-field support.
/// The user must be registered for the event (verified by email) but doesn't need to be logged in.
/// </summary>
public record CommitToSignUpItemAnonymousCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    string ContactEmail,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null,
    int? PhysicalQuantity = null,           // Phase 6A.125: For quantity-based items
    int? SlotsClaimed = null                // Phase 6A.125: For slot-based items
) : ICommand<Guid>;

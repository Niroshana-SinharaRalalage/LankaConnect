using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateCollection;

/// <summary>
/// Standalone collection (event fund) contribution command — used from the event details page.
/// Returns the Stripe Checkout URL for the contributor to complete payment.
/// </summary>
public record CreateCollectionCommand(
    Guid EventId,
    string ContributorName,
    string ContributorEmail,
    string? ContributorPhone,
    string? ContributorNotes,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    // Null for anonymous contributions
    Guid? UserId = null
) : ICommand<string>;  // Returns checkout URL

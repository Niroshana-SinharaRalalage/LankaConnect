using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreatePackageSponsor;

/// <summary>
/// Phase 6A.157 — public/anonymous command creating a Sponsor row tied to a
/// chosen <c>SponsorshipPackage</c>. Atomically reserves stock, snapshots
/// the package fields onto the Sponsor row, then either:
///   - Creates a Stripe Checkout session for paid packages (returns the
///     Stripe URL); OR
///   - Instantly completes the Sponsor with a sentinel intent for free
///     ($0) packages (returns the SuccessUrl directly).
///
/// On any post-stock-reservation failure, stock is restored via
/// <c>SponsorshipPackageRepository.TryRestoreStockAsync</c> (same recovery
/// pattern as <c>PurchaseAddOnCommandHandler</c>).
///
/// Returns <see cref="CreatePackageSponsorResult"/> — checkout URL + sponsor
/// ID so the FE can attach a logo image to the Pending sponsor BEFORE the
/// Stripe redirect (mirrors 6A.145's widened CreateMoneySponsor response).
/// </summary>
public record CreatePackageSponsorCommand(
    Guid EventId,
    Guid PackageId,
    string BuyerName,
    string BuyerEmail,
    string? BuyerPhone,
    string? BuyerOrganization,
    string? BuyerNotes,
    string SuccessUrl,
    string CancelUrl,
    // Null for anonymous purchases
    Guid? UserId = null
) : ICommand<CreatePackageSponsorResult>;

/// <summary>
/// Phase 6A.157 — composite return so the FE can:
///   1. Attach the buyer's logo to the Pending sponsor via the existing
///      <c>POST /sponsors/{id}/image</c> endpoint BEFORE the Stripe redirect;
///   2. Redirect to <c>CheckoutUrl</c> (Stripe Checkout for paid packages,
///      or the SuccessUrl directly for free packages).
/// </summary>
public class CreatePackageSponsorResult
{
    public required string CheckoutUrl { get; init; }
    public required Guid SponsorId { get; init; }
}

using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Money;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsor;

/// <summary>
/// Money-based sponsorship command — creates a Stripe Checkout session for monetary sponsorship.
/// Returns the Stripe Checkout URL + the created Sponsor ID so the caller can attach an
/// image to the Pending sponsor BEFORE redirecting the user to Stripe (Phase 6A.145 Commit 6
/// per-sponsor-image flow).
/// </summary>
public record CreateMoneySponsorCommand(
    Guid EventId,
    string SponsorName,
    string SponsorEmail,
    string? SponsorPhone,
    string? SponsorOrganization,
    string? SponsorNotes,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    // Null for anonymous sponsors
    Guid? UserId = null
) : ICommand<CreateMoneySponsorResult>;

/// <summary>
/// Phase 6A.145 Commit 6 — return shape for the money-sponsor create flow. Was a bare
/// string (URL) pre-6A.145; widened to a record so the caller can also receive the
/// Sponsor ID for the optional image-upload follow-up call before Stripe redirect.
/// </summary>
public record CreateMoneySponsorResult(string CheckoutUrl, Guid SponsorId);

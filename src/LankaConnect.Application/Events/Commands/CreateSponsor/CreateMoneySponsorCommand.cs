using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CreateSponsor;

/// <summary>
/// Money-based sponsorship command — creates a Stripe Checkout session for monetary sponsorship.
/// Returns the Stripe Checkout URL for the sponsor to complete payment.
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
) : ICommand<string>;  // Returns checkout URL

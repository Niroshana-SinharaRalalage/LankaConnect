using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CreateDonation;

/// <summary>
/// Standalone donation command — used from the event details page at any time.
/// Returns the Stripe Checkout URL for the donor to complete payment.
/// </summary>
public record CreateDonationCommand(
    Guid EventId,
    string DonorName,
    string DonorEmail,
    string? DonorPhone,
    string? DonorNotes,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    // Null for anonymous donations
    Guid? UserId = null
) : ICommand<string>;  // Returns checkout URL

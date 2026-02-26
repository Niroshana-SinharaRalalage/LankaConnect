using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for Donation operations.
/// Part of the standalone Donation system for events.
/// </summary>
public interface IDonationRepository : IRepository<Donation>
{
    /// <summary>
    /// Gets a donation by its Stripe checkout session ID.
    /// Used during webhook processing to identify the donation.
    /// </summary>
    Task<Donation?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a donation by its ID with minimal tracking.
    /// Used in webhook processing for fast lookups.
    /// </summary>
    Task<Donation?> GetByDonationIdAsync(
        Guid donationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a donation linked to a specific registration (bundled donation).
    /// </summary>
    Task<Donation?> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all donations for an event (all statuses).
    /// Used by organizer for management view.
    /// </summary>
    Task<IReadOnlyList<Donation>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed donations for an event.
    /// Used for summary and export.
    /// </summary>
    Task<IReadOnlyList<Donation>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total donation amount for an event (completed donations only).
    /// Returns 0 if no completed donations exist.
    /// </summary>
    Task<decimal> GetTotalDonationsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired pending donations for cleanup.
    /// These are donations where CheckoutExpiresAt is in the past.
    /// </summary>
    Task<IReadOnlyList<Donation>> GetExpiredPendingDonationsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all donations for an event with a specific status.
    /// </summary>
    Task<IReadOnlyList<Donation>> GetByEventIdAndStatusAsync(
        Guid eventId,
        DonationStatus status,
        CancellationToken cancellationToken = default);
}

using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Repository interface for RegistrationAddition operations.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public interface IRegistrationAdditionRepository : IRepository<RegistrationAddition>
{
    /// <summary>
    /// Gets a pending addition for a specific registration.
    /// Only one pending addition per registration is allowed at a time.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pending addition or null if none exists.</returns>
    Task<RegistrationAddition?> GetPendingByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an addition by its Stripe checkout session ID.
    /// Used during webhook processing to identify the addition.
    /// </summary>
    /// <param name="sessionId">Stripe checkout session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The addition or null if not found.</returns>
    Task<RegistrationAddition?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an addition by its Stripe payment intent ID.
    /// Used during webhook processing.
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The addition or null if not found.</returns>
    Task<RegistrationAddition?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all additions for an event with a specific status.
    /// Useful for reporting and cleanup operations.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of additions matching the criteria.</returns>
    Task<IReadOnlyList<RegistrationAddition>> GetByEventIdAndStatusAsync(
        Guid eventId,
        RegistrationAdditionStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all additions for a registration (all statuses).
    /// Useful for viewing history of addition attempts.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all additions for the registration.</returns>
    Task<IReadOnlyList<RegistrationAddition>> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired pending additions for cleanup.
    /// These are additions where CheckoutExpiresAt is in the past.
    /// </summary>
    /// <param name="cutoffTime">Time before which additions are considered expired.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expired pending additions.</returns>
    Task<IReadOnlyList<RegistrationAddition>> GetExpiredPendingAdditionsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pending addition exists for a registration.
    /// Used to prevent concurrent additions.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a pending addition exists.</returns>
    Task<bool> HasPendingAdditionAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);
}

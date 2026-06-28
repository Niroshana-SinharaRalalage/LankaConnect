using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Repository interface for RegistrationPayment operations.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public interface IRegistrationPaymentRepository : IRepository<RegistrationPayment>
{
    /// <summary>
    /// Gets all payments for a registration.
    /// Includes both initial and addition payments.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of payments ordered by creation time.</returns>
    Task<IReadOnlyList<RegistrationPayment>> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment by its Stripe payment intent ID.
    /// Used to prevent duplicate payment records and for reconciliation.
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment or null if not found.</returns>
    Task<RegistrationPayment?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the initial payment for a registration.
    /// There should be exactly one initial payment per registration.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initial payment or null if not found.</returns>
    Task<RegistrationPayment?> GetInitialPaymentAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all addition payments for a registration.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of addition payments.</returns>
    Task<IReadOnlyList<RegistrationPayment>> GetAdditionPaymentsAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total amount paid for a registration across all payments.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total amount paid in the registration's currency.</returns>
    Task<decimal> GetTotalPaidAmountAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment with the given Stripe payment intent ID already exists.
    /// Used for idempotency in webhook processing.
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a payment with this ID exists.</returns>
    Task<bool> PaymentExistsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
}

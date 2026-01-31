using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

public interface IRegistrationRepository : IRepository<Registration>
{
    Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default);
    Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.24: Gets an anonymous registration by event ID and contact email
    /// Used to fetch registration details for anonymous users' confirmation emails
    /// </summary>
    Task<Registration?> GetAnonymousByEventAndEmailAsync(Guid eventId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.X: Gets a registration by Stripe PaymentIntentId.
    /// Used by charge.refunded webhook handler as fallback when refund metadata is missing.
    /// </summary>
    Task<Registration?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);
}
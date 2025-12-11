using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Repository interface for NewsletterSubscriber aggregate root
/// Follows Clean Architecture principles with domain-specific query methods
/// </summary>
public interface INewsletterSubscriberRepository : IRepository<NewsletterSubscriber>
{
    /// <summary>
    /// Gets a subscriber by email address
    /// </summary>
    Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a subscriber by confirmation token
    /// </summary>
    Task<NewsletterSubscriber?> GetByConfirmationTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a subscriber by unsubscribe token
    /// </summary>
    Task<NewsletterSubscriber?> GetByUnsubscribeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all confirmed and active subscribers for a specific metro area
    /// </summary>
    Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersByMetroAreaAsync(
        Guid metroAreaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all confirmed and active subscribers who receive all locations
    /// </summary>
    Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersForAllLocationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already subscribed (active subscription exists)
    /// </summary>
    Task<bool> IsEmailSubscribedAsync(string email, Guid? metroAreaId = null, CancellationToken cancellationToken = default);
}

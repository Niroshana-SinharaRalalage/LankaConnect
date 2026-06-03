namespace LankaConnect.Modules.Notifications.Contracts;

/// <summary>
/// Cross-module dispatch API for creating an in-app notification. Other
/// modules call this from their own application layer when a state change
/// in their bounded context should surface to the notification feed of a
/// specific user.
/// </summary>
/// <remarks>
/// <para>
/// All parameters are CLR primitives — no Notifications.Domain types leak
/// across the module boundary. The internal Notifications module materializes
/// the domain entity, persists it, and (in W3.3+) raises the corresponding
/// <see cref="NotificationCreatedIntegrationEventV1"/> for downstream
/// subscribers (Web push, mobile push, email digest, etc.).
/// </para>
/// <para>
/// The implementation in <c>Notifications.Infrastructure</c> dispatches via
/// MediatR <c>IPublisher</c> when running inside <c>Host.AllInOne</c>; in
/// Phase B it serializes onto a Service Bus topic — same contract, different
/// composition-root registration.
/// </para>
/// </remarks>
public interface INotificationDispatcher
{
    /// <summary>
    /// Creates a notification for <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">Recipient user identifier (must be non-empty).</param>
    /// <param name="title">Title text (1-200 characters).</param>
    /// <param name="message">Body text (1-1000 characters).</param>
    /// <param name="kind">Notification kind discriminator.</param>
    /// <param name="relatedEntityId">Optional reference to a related entity for deep-linking.</param>
    /// <param name="relatedEntityType">Optional discriminator for the related entity type (free-form string).</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    Task NotifyAsync(
        Guid userId,
        string title,
        string message,
        NotificationKind kind,
        string? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);
}

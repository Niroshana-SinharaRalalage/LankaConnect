using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.Modules.Notifications.Contracts;

/// <summary>
/// Integration event raised by the Notifications module after a new in-app
/// notification has been persisted. Other modules subscribe to drive their
/// own side effects (Web push, mobile push, email digest aggregation, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Versioning: this is the V1 schema. Breaking changes (renames, removed
/// fields, semantic shifts) require declaring <c>NotificationCreatedIntegrationEventV2</c>
/// alongside this type and supporting both for a transition window — see the
/// <see cref="IIntegrationEventV1"/> convention.
/// </para>
/// <para>
/// All fields are CLR primitives + the Contracts-local <see cref="NotificationKind"/>
/// enum. No <c>Notifications.Domain</c> types are referenced — Contracts is
/// the cross-module wire-format ABI and must not leak internal shapes.
/// </para>
/// </remarks>
public sealed record NotificationCreatedIntegrationEventV1 : IntegrationEventBase, IIntegrationEventV1
{
    /// <summary>Persisted notification identifier.</summary>
    public required Guid NotificationId { get; init; }

    /// <summary>Recipient user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Notification title (1-200 characters).</summary>
    public required string Title { get; init; }

    /// <summary>Notification body (1-1000 characters).</summary>
    public required string Message { get; init; }

    /// <summary>Discriminator describing the kind of notification.</summary>
    public required NotificationKind Kind { get; init; }

    /// <summary>Optional reference to a related entity for deep-linking.</summary>
    public string? RelatedEntityId { get; init; }

    /// <summary>Optional discriminator for the related entity type.</summary>
    public string? RelatedEntityType { get; init; }
}

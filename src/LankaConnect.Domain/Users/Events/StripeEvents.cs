using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users.Events;

/// <summary>
/// Domain event raised when a user's subscription is activated
/// </summary>
public record UserSubscriptionActivatedEvent(
    Guid UserId,
    string Email,
    SubscriptionStatus Status,
    DateTime? TrialEndDate) : DomainEvent;

/// <summary>
/// Domain event raised when a user's subscription status changes
/// </summary>
public record UserSubscriptionStatusChangedEvent(
    Guid UserId,
    string Email,
    SubscriptionStatus OldStatus,
    SubscriptionStatus NewStatus) : DomainEvent;

/// <summary>
/// Domain event raised when a user cancels their subscription
/// </summary>
public record UserSubscriptionCanceledEvent(
    Guid UserId,
    string Email,
    DateTime? EndDate) : DomainEvent;

using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Wave 8.5.f (Phase A.5) — per-module SaveChangesInterceptor that dispatches
/// <see cref="IDomainEvent"/>s raised on tracked <see cref="Entity{TId}"/> aggregates
/// after a successful <c>SaveChangesAsync</c>. Wired on module DbContexts that don't
/// route through <c>AppDbContext.CommitAsync</c> (LankaEventsDbContext, IdentityDbContext,
/// CommunicationsDbContext, MediaDbContext, FormsDbContext, NotificationsDbContext).
/// </summary>
/// <remarks>
/// <para>
/// Consult #25 Q2 (2026-07-13) mandated this interceptor as the prerequisite for direct-
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> handler migration
/// (Wave 8.5.g) so raised domain events don't get dropped when handlers bypass
/// <c>AppDbContext.CommitAsync</c>. Prior to this interceptor:
/// - <c>CreateEventCommandHandler</c> (sprint 17th deploy) called
///   <c>_lankaEventsDbContext.SaveChangesAsync</c> directly and any
///   <c>EventCreatedIntegrationEvent</c> on the Event aggregate was silently dropped.
/// - <c>RegisterUserHandler</c> (sprint 23rd deploy) called
///   <c>_identityDbContext.SaveChangesAsync</c> directly and any
///   <c>MemberVerificationRequestedEvent</c> was similarly dropped, breaking the
///   downstream email-verification cascade.
/// - Same shape for <c>PhotoAlbumsController</c> + RSVP flows (Wave 8.5.l diagnostic
///   confirmed same family: publish returns 200 but state transition doesn't dispatch,
///   so subsequent state-guard check sees stale state).
/// </para>
/// <para>
/// <b>Semantics mirror <c>AppDbContext.CommitAsync</c>:</b>
/// - Collect domain events from <c>ChangeTracker.Entries&lt;Entity&lt;Guid&gt;&gt;()</c>
///   BEFORE SaveChanges (during <c>SavingChangesAsync</c>).
/// - After successful save (<c>SavedChangesAsync</c>), publish each via
///   <see cref="IPublisher"/> wrapped in <see cref="DomainEventNotification{T}"/>.
/// - Clear events on entities after collect (before save) to prevent double-dispatch
///   if nested <c>SaveChanges</c> fires from within a handler.
/// - Handler exceptions are logged and swallowed per event so one failure doesn't
///   block others (matches Phase 6A.52 semantics).
/// </para>
/// <para>
/// <b>Scope constraint (Wave 8.5.f):</b> collects only aggregates deriving from
/// <c>Entity&lt;Guid&gt;</c> (BuildingBlocks base type). LegacyBaseEntity aggregates
/// still dispatch via <c>AppDbContext.CommitAsync</c> — this interceptor is additive.
/// </para>
/// </remarks>
public sealed class DomainEventSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventSaveChangesInterceptor> _logger;

    /// <summary>
    /// Domain events collected during <see cref="SavingChangesAsync"/>, dispatched
    /// after <see cref="SavedChangesAsync"/>. Instance-scoped: interceptor is
    /// registered per-DbContext-instance (scoped lifetime), so this field's
    /// lifetime matches the DbContext.
    /// </summary>
    private readonly List<IDomainEvent> _pendingEvents = new();

    public DomainEventSaveChangesInterceptor(
        IPublisher publisher,
        ILogger<DomainEventSaveChangesInterceptor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        context.ChangeTracker.DetectChanges();

        var domainEventEntries = context.ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        if (domainEventEntries.Count == 0)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var entry in domainEventEntries)
        {
            _pendingEvents.AddRange(entry.Entity.DomainEvents);
            entry.Entity.ClearDomainEvents();
        }

        _logger.LogInformation(
            "Wave 8.5.f interceptor collected {Count} domain events from {ContextType} for post-save dispatch: [{EventTypes}]",
            _pendingEvents.Count,
            context.GetType().Name,
            string.Join(", ", _pendingEvents.Select(e => e.GetType().Name)));

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingEvents.Count == 0)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();

        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            if (notification is null)
            {
                _logger.LogError(
                    "Wave 8.5.f interceptor failed to construct DomainEventNotification<{EventType}> instance",
                    eventType.Name);
                continue;
            }

            try
            {
                await _publisher.Publish((INotification)notification, cancellationToken);
                _logger.LogDebug(
                    "Wave 8.5.f interceptor dispatched domain event: {EventType}",
                    eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Wave 8.5.f interceptor handler failure while dispatching {EventType}; swallowing to preserve subsequent dispatches",
                    eventType.Name);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendingEvents.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pendingEvents.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}

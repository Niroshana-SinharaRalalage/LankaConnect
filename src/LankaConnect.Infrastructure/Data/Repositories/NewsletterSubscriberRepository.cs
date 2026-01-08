using Microsoft.EntityFrameworkCore;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Infrastructure.Common;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for NewsletterSubscriber aggregate root
/// Follows TDD principles and integrates with base Repository pattern
/// Phase 6A Event Notifications: Supports both full state names and abbreviations
/// Phase 6A.64: Overrides AddAsync to handle junction table inserts for metro area IDs
/// </summary>
public class NewsletterSubscriberRepository : Repository<NewsletterSubscriber>, INewsletterSubscriberRepository
{
    public NewsletterSubscriberRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Phase 6A.64: Override AddAsync to manually insert metro area IDs into junction table
    /// </summary>
    public override async Task AddAsync(NewsletterSubscriber entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            _logger.Information("[Phase 6A.64] Adding newsletter subscriber {EntityId} with {Count} metro areas",
                entity.Id, entity.MetroAreaIds.Count);

            // Add the subscriber entity
            await base.AddAsync(entity, cancellationToken);

            // Phase 6A.64: Manually insert metro area IDs into junction table
            // We do this because we're not using navigation properties (only storing IDs)
            if (entity.MetroAreaIds.Any())
            {
                _logger.Debug("[Phase 6A.64] Inserting {Count} metro area IDs into junction table for subscriber {SubscriberId}",
                    entity.MetroAreaIds.Count, entity.Id);

                foreach (var metroAreaId in entity.MetroAreaIds)
                {
                    var junctionEntry = new Dictionary<string, object>
                    {
                        ["subscriber_id"] = entity.Id,
                        ["metro_area_id"] = metroAreaId,
                        ["created_at"] = entity.CreatedAt
                    };

                    await _context.Set<Dictionary<string, object>>("newsletter_subscriber_metro_areas")
                        .AddAsync(junctionEntry, cancellationToken);
                }

                _logger.Information("[Phase 6A.64] Added {Count} junction table entries for subscriber {SubscriberId}",
                    entity.MetroAreaIds.Count, entity.Id);
            }
            else
            {
                _logger.Debug("[Phase 6A.64] No metro area IDs to insert (ReceiveAllLocations: true)");
            }
        }
    }

    /// <summary>
    /// Phase 6A.64: Override Remove to manually delete metro area junction table entries
    /// CASCADE DELETE should handle this automatically, but being explicit for clarity
    /// </summary>
    public override void Remove(NewsletterSubscriber entity)
    {
        using (LogContext.PushProperty("Operation", "Remove"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            _logger.Information("[Phase 6A.64] Removing newsletter subscriber {EntityId}", entity.Id);

            // Phase 6A.64: Junction table entries will be CASCADE deleted by FK constraint
            // No manual deletion needed, but logging for visibility
            _logger.Debug("[Phase 6A.64] Junction table entries will be CASCADE deleted by FK constraint");

            base.Remove(entity);
        }
    }

    public async Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEmail"))
        using (LogContext.PushProperty("Email", email))
        {
            _logger.Debug("Getting newsletter subscriber by email {Email}", email);

            var result = await _dbSet
                .FirstOrDefaultAsync(ns => ns.Email.Value == email, cancellationToken);

            _logger.Debug("Newsletter subscriber with email {Email} {Result}",
                email, result != null ? "found" : "not found");
            return result;
        }
    }

    public async Task<NewsletterSubscriber?> GetByConfirmationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByConfirmationToken"))
        {
            _logger.Debug("Getting newsletter subscriber by confirmation token");

            var result = await _dbSet
                .FirstOrDefaultAsync(ns => ns.ConfirmationToken == token, cancellationToken);

            _logger.Debug("Newsletter subscriber with confirmation token {Result}",
                result != null ? "found" : "not found");
            return result;
        }
    }

    public async Task<NewsletterSubscriber?> GetByUnsubscribeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUnsubscribeToken"))
        {
            _logger.Debug("Getting newsletter subscriber by unsubscribe token");

            var result = await _dbSet
                .FirstOrDefaultAsync(ns => ns.UnsubscribeToken == token, cancellationToken);

            _logger.Debug("Newsletter subscriber with unsubscribe token {Result}",
                result != null ? "found" : "not found");
            return result;
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersByMetroAreaAsync(
        Guid metroAreaId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersByMetroArea"))
        using (LogContext.PushProperty("MetroAreaId", metroAreaId))
        {
            _logger.Debug("[Phase 6A.64] Getting confirmed subscribers for metro area {MetroAreaId}", metroAreaId);

            // Phase 6A.64: Join with junction table to find subscribers for this metro area
            var result = await (
                from ns in _dbSet
                join nsma in _context.Set<Dictionary<string, object>>("newsletter_subscriber_metro_areas")
                    on ns.Id equals EF.Property<Guid>(nsma, "subscriber_id")
                where EF.Property<Guid>(nsma, "metro_area_id") == metroAreaId
                    && ns.IsActive
                    && ns.IsConfirmed
                select ns
            )
            .AsNoTracking()
            .ToListAsync(cancellationToken);

            _logger.Debug("[Phase 6A.64] Retrieved {Count} confirmed subscribers for metro area {MetroAreaId}",
                result.Count, metroAreaId);
            return result;
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersForAllLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersForAllLocations"))
        {
            _logger.Debug("Getting confirmed subscribers for all locations");

            var result = await _dbSet
                .Where(ns => ns.ReceiveAllLocations)
                .Where(ns => ns.IsActive)
                .Where(ns => ns.IsConfirmed)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.Debug("Retrieved {Count} confirmed subscribers for all locations", result.Count);
            return result;
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersByStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersByState"))
        using (LogContext.PushProperty("State", state))
        {
            _logger.Debug("[Phase 6A.64] Getting confirmed subscribers for ALL metro areas in state {State}", state);

            // Phase 6A.64: Get ALL metro areas in the state (not just state-level areas)
            // This matches the UI behavior where selecting "Ohio" checkbox selects all 5 Ohio metro areas
            var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

            _logger.Debug("[Phase 6A.64] Normalized state {State} to abbreviation {Abbreviation}",
                state, stateAbbreviation ?? "null");

            List<Guid> allStateMetroAreaIds;

            if (!string.IsNullOrEmpty(stateAbbreviation))
            {
                // Match using normalized abbreviation - GET ALL metro areas, not just state-level
                allStateMetroAreaIds = await _context.MetroAreas
                    .Where(m => m.State.ToLower() == stateAbbreviation.ToLower())
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Fallback: try exact match for non-US states
                allStateMetroAreaIds = await _context.MetroAreas
                    .Where(m => m.State.ToLower() == state.ToLower())
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);
            }

            if (!allStateMetroAreaIds.Any())
            {
                _logger.Debug("[Phase 6A.64] No metro areas found for state {State}", state);
                return new List<NewsletterSubscriber>();
            }

            _logger.Debug("[Phase 6A.64] Found {Count} metro areas in state {State}: [{MetroAreaIds}]",
                allStateMetroAreaIds.Count, state, string.Join(", ", allStateMetroAreaIds));

            // Phase 6A.64: Join with junction table to find subscribers with ANY of these metro areas
            var result = await (
                from ns in _dbSet
                join nsma in _context.Set<Dictionary<string, object>>("newsletter_subscriber_metro_areas")
                    on ns.Id equals EF.Property<Guid>(nsma, "subscriber_id")
                where allStateMetroAreaIds.Contains(EF.Property<Guid>(nsma, "metro_area_id"))
                    && ns.IsActive
                    && ns.IsConfirmed
                select ns
            )
            .Distinct()  // Remove duplicates (subscriber may have multiple metro areas in same state)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

            _logger.Debug("[Phase 6A.64] Retrieved {Count} confirmed subscribers for state {State}",
                result.Count, state);
            return result;
        }
    }

    public async Task<bool> IsEmailSubscribedAsync(string email, Guid? metroAreaId = null, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "IsEmailSubscribed"))
        using (LogContext.PushProperty("Email", email))
        using (LogContext.PushProperty("MetroAreaId", metroAreaId))
        {
            _logger.Debug("[Phase 6A.64] Checking if email {Email} is subscribed for metro area {MetroAreaId}",
                email, metroAreaId);

            bool result;

            if (metroAreaId.HasValue)
            {
                // Phase 6A.64: Check if subscriber has this specific metro area via junction table
                result = await (
                    from ns in _dbSet
                    join nsma in _context.Set<Dictionary<string, object>>("newsletter_subscriber_metro_areas")
                        on ns.Id equals EF.Property<Guid>(nsma, "subscriber_id")
                    where ns.Email.Value == email
                        && ns.IsActive
                        && EF.Property<Guid>(nsma, "metro_area_id") == metroAreaId.Value
                    select ns
                ).AnyAsync(cancellationToken);
            }
            else
            {
                // No specific metro area - just check if email is subscribed to anything
                result = await _dbSet
                    .Where(ns => ns.Email.Value == email)
                    .Where(ns => ns.IsActive)
                    .AnyAsync(cancellationToken);
            }

            _logger.Debug("[Phase 6A.64] Email {Email} subscription status for metro area {MetroAreaId}: {IsSubscribed}",
                email, metroAreaId, result);
            return result;
        }
    }
}

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
/// </summary>
public class NewsletterSubscriberRepository : Repository<NewsletterSubscriber>, INewsletterSubscriberRepository
{
    public NewsletterSubscriberRepository(AppDbContext context) : base(context)
    {
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
            _logger.Debug("Getting confirmed subscribers for metro area {MetroAreaId}", metroAreaId);

            var result = await _dbSet
                .Where(ns => ns.MetroAreaId == metroAreaId)
                .Where(ns => ns.IsActive)
                .Where(ns => ns.IsConfirmed)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.Debug("Retrieved {Count} confirmed subscribers for metro area {MetroAreaId}",
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
            _logger.Debug("Getting confirmed subscribers for state-level areas in state {State}", state);

            // Phase 6A Fix: Normalize state to abbreviation for matching
            // Database stores 2-letter abbreviations (e.g., "CA"), but events may have full names (e.g., "California")
            var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

            _logger.Debug("Normalized state {State} to abbreviation {Abbreviation}", state, stateAbbreviation ?? "null");

            List<Guid> stateMetroAreaIds;

            if (!string.IsNullOrEmpty(stateAbbreviation))
            {
                // Match using normalized abbreviation
                stateMetroAreaIds = await _context.MetroAreas
                    .Where(m => m.State.ToLower() == stateAbbreviation.ToLower() && m.IsStateLevelArea)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Fallback: try exact match for non-US states
                stateMetroAreaIds = await _context.MetroAreas
                    .Where(m => m.State.ToLower() == state.ToLower() && m.IsStateLevelArea)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);
            }

            if (!stateMetroAreaIds.Any())
            {
                _logger.Debug("No state-level metro areas found for state {State}", state);
                return new List<NewsletterSubscriber>();
            }

            // Get subscribers for those state-level metro areas
            var result = await _dbSet
                .Where(ns => ns.MetroAreaId.HasValue && stateMetroAreaIds.Contains(ns.MetroAreaId.Value))
                .Where(ns => ns.IsActive)
                .Where(ns => ns.IsConfirmed)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.Debug("Retrieved {Count} confirmed subscribers for state-level areas in {State}",
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
            _logger.Debug("Checking if email {Email} is subscribed for metro area {MetroAreaId}",
                email, metroAreaId);

            var query = _dbSet
                .Where(ns => ns.Email.Value == email)
                .Where(ns => ns.IsActive);

            if (metroAreaId.HasValue)
            {
                query = query.Where(ns => ns.MetroAreaId == metroAreaId);
            }

            var result = await query.AnyAsync(cancellationToken);

            _logger.Debug("Email {Email} subscription status for metro area {MetroAreaId}: {IsSubscribed}",
                email, metroAreaId, result);
            return result;
        }
    }
}

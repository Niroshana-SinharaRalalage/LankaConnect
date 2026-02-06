using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Infrastructure.Common;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for NewsletterSubscriber aggregate root
/// Follows TDD principles and integrates with base Repository pattern
/// Phase 6A Event Notifications: Supports both full state names and abbreviations
/// Phase 6A.64: Overrides AddAsync to handle junction table inserts for metro area IDs
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class NewsletterSubscriberRepository : Repository<NewsletterSubscriber>, INewsletterSubscriberRepository
{
    private readonly ILogger<NewsletterSubscriberRepository> _repoLogger;

    public NewsletterSubscriberRepository(
        AppDbContext context,
        ILogger<NewsletterSubscriberRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    private readonly Dictionary<Guid, List<Guid>> _pendingJunctionInserts = new();

    /// <summary>
    /// Phase 6A.64: Override AddAsync to stage metro area IDs for junction table insert
    /// Junction table inserts happen AFTER subscriber is saved to database (see InsertPendingJunctionEntries)
    /// </summary>
    public override async Task AddAsync(NewsletterSubscriber entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("EntityId", entity.Id))
        using (LogContext.PushProperty("MetroAreaCount", entity.MetroAreaIds.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "AddAsync START: EntityId={EntityId}, Email={Email}, MetroAreaCount={MetroAreaCount}",
                entity.Id, entity.Email.Value, entity.MetroAreaIds.Count);

            try
            {
                // Add the subscriber entity to change tracker
                await base.AddAsync(entity, cancellationToken);

                // Phase 6A.64: Stage metro area IDs for junction table insert AFTER SaveChanges
                // We cannot insert now because subscriber doesn't exist in database yet (FK constraint)
                if (entity.MetroAreaIds.Any())
                {
                    _pendingJunctionInserts[entity.Id] = entity.MetroAreaIds.ToList();
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "AddAsync COMPLETE: EntityId={EntityId}, Email={Email}, MetroAreaCount={MetroAreaCount}, StagedForJunction={StagedForJunction}, Duration={ElapsedMs}ms",
                    entity.Id,
                    entity.Email.Value,
                    entity.MetroAreaIds.Count,
                    entity.MetroAreaIds.Any(),
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "AddAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.64: Insert pending junction table entries after entities are saved
    /// Called by UnitOfWork after SaveChangesAsync completes successfully
    /// </summary>
    public async Task InsertPendingJunctionEntriesAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "InsertPendingJunctionEntries"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("PendingCount", _pendingJunctionInserts.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("InsertPendingJunctionEntriesAsync START: PendingCount={PendingCount}", _pendingJunctionInserts.Count);

            if (!_pendingJunctionInserts.Any())
            {
                stopwatch.Stop();
                _repoLogger.LogInformation("InsertPendingJunctionEntriesAsync COMPLETE: NoPendingInserts, Duration={ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return;
            }

            try
            {
                foreach (var (subscriberId, metroAreaIds) in _pendingJunctionInserts)
                {
                    var createdAt = DateTime.UtcNow;
                    var values = string.Join(", ", metroAreaIds.Select(id =>
                        $"('{subscriberId}', '{id}', '{createdAt:yyyy-MM-dd HH:mm:ss.ffffff}+00'::timestamptz)"));

                    var sql = $@"
                        INSERT INTO communications.newsletter_subscriber_metro_areas (subscriber_id, metro_area_id, created_at)
                        VALUES {values}";

                    await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

                    _repoLogger.LogDebug(
                        "Inserted junction entries: SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}",
                        subscriberId, metroAreaIds.Count);
                }

                var totalInserted = _pendingJunctionInserts.Sum(x => x.Value.Count);
                _pendingJunctionInserts.Clear();

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "InsertPendingJunctionEntriesAsync COMPLETE: TotalInserted={TotalInserted}, Duration={ElapsedMs}ms",
                    totalInserted,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "InsertPendingJunctionEntriesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
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
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("Remove START: EntityId={EntityId}, Email={Email}", entity.Id, entity.Email.Value);

            try
            {
                base.Remove(entity);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "Remove COMPLETE: EntityId={EntityId}, Email={Email}, Duration={ElapsedMs}ms (Junction entries CASCADE deleted)",
                    entity.Id,
                    entity.Email.Value,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "Remove FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEmail"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("Email", email))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEmailAsync START: Email={Email}", email);

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(ns => ns.Email.Value == email, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEmailAsync COMPLETE: Email={Email}, Found={Found}, Duration={ElapsedMs}ms",
                    email,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEmailAsync FAILED: Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<NewsletterSubscriber?> GetByConfirmationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByConfirmationToken"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByConfirmationTokenAsync START");

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(ns => ns.ConfirmationToken == token, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByConfirmationTokenAsync COMPLETE: Found={Found}, Duration={ElapsedMs}ms",
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByConfirmationTokenAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<NewsletterSubscriber?> GetByUnsubscribeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUnsubscribeToken"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUnsubscribeTokenAsync START");

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(ns => ns.UnsubscribeToken == token, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUnsubscribeTokenAsync COMPLETE: Found={Found}, Duration={ElapsedMs}ms",
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUnsubscribeTokenAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersByMetroAreaAsync(
        Guid metroAreaId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersByMetroArea"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("MetroAreaId", metroAreaId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetConfirmedSubscribersByMetroAreaAsync START: MetroAreaId={MetroAreaId}", metroAreaId);

            try
            {
                // Phase 6A.64 FIX: Use raw SQL to query junction table (avoiding EF Core shared-type entity error)
                var subscriberIds = await _context.Database
                    .SqlQuery<Guid>($@"
                        SELECT subscriber_id
                        FROM communications.newsletter_subscriber_metro_areas
                        WHERE metro_area_id = {metroAreaId}")
                    .ToListAsync(cancellationToken);

                if (!subscriberIds.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetConfirmedSubscribersByMetroAreaAsync COMPLETE: MetroAreaId={MetroAreaId}, Count=0, Duration={ElapsedMs}ms",
                        metroAreaId,
                        stopwatch.ElapsedMilliseconds);
                    return new List<NewsletterSubscriber>();
                }

                // Now fetch the actual subscriber entities
                var result = await _dbSet
                    .Where(ns => subscriberIds.Contains(ns.Id))
                    .Where(ns => ns.IsActive)
                    .Where(ns => ns.IsConfirmed)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetConfirmedSubscribersByMetroAreaAsync COMPLETE: MetroAreaId={MetroAreaId}, Count={Count}, Duration={ElapsedMs}ms",
                    metroAreaId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetConfirmedSubscribersByMetroAreaAsync FAILED: MetroAreaId={MetroAreaId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    metroAreaId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersForAllLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersForAllLocations"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetConfirmedSubscribersForAllLocationsAsync START");

            try
            {
                var result = await _dbSet
                    .Where(ns => ns.ReceiveAllLocations)
                    .Where(ns => ns.IsActive)
                    .Where(ns => ns.IsConfirmed)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetConfirmedSubscribersForAllLocationsAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetConfirmedSubscribersForAllLocationsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetConfirmedSubscribersByStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetConfirmedSubscribersByState"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("State", state))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetConfirmedSubscribersByStateAsync START: State={State}", state);

            try
            {
                // Phase 6A.64: Get ALL metro areas in the state (not just state-level areas)
                // This matches the UI behavior where selecting "Ohio" checkbox selects all 5 Ohio metro areas
                var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

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
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetConfirmedSubscribersByStateAsync COMPLETE: State={State}, MetroAreaCount=0, Count=0, Duration={ElapsedMs}ms",
                        state,
                        stopwatch.ElapsedMilliseconds);
                    return new List<NewsletterSubscriber>();
                }

                // Phase 6A.64 FIX: Use raw SQL to query junction table (avoiding EF Core shared-type entity error)
                var subscriberIds = await _context.Database
                    .SqlQuery<Guid>($@"
                        SELECT DISTINCT subscriber_id
                        FROM communications.newsletter_subscriber_metro_areas
                        WHERE metro_area_id = ANY({allStateMetroAreaIds})")
                    .ToListAsync(cancellationToken);

                if (!subscriberIds.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetConfirmedSubscribersByStateAsync COMPLETE: State={State}, MetroAreaCount={MetroAreaCount}, Count=0, Duration={ElapsedMs}ms",
                        state,
                        allStateMetroAreaIds.Count,
                        stopwatch.ElapsedMilliseconds);
                    return new List<NewsletterSubscriber>();
                }

                // Now fetch the actual subscriber entities
                var result = await _dbSet
                    .Where(ns => subscriberIds.Contains(ns.Id))
                    .Where(ns => ns.IsActive)
                    .Where(ns => ns.IsConfirmed)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetConfirmedSubscribersByStateAsync COMPLETE: State={State}, StateAbbr={StateAbbr}, MetroAreaCount={MetroAreaCount}, Count={Count}, Duration={ElapsedMs}ms",
                    state,
                    stateAbbreviation ?? "N/A",
                    allStateMetroAreaIds.Count,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetConfirmedSubscribersByStateAsync FAILED: State={State}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    state,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<bool> IsEmailSubscribedAsync(string email, Guid? metroAreaId = null, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "IsEmailSubscribed"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("Email", email))
        using (LogContext.PushProperty("MetroAreaId", metroAreaId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("IsEmailSubscribedAsync START: Email={Email}, MetroAreaId={MetroAreaId}", email, metroAreaId);

            try
            {
                bool result;

                if (metroAreaId.HasValue)
                {
                    // Phase 6A.64 FIX: Use raw SQL to query junction table (avoiding EF Core shared-type entity error)
                    var subscriberId = await _dbSet
                        .Where(ns => ns.Email.Value == email && ns.IsActive)
                        .Select(ns => ns.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (subscriberId == Guid.Empty)
                    {
                        result = false;
                    }
                    else
                    {
                        var count = await _context.Database
                            .SqlQuery<int>($@"
                                SELECT COUNT(*)::int
                                FROM communications.newsletter_subscriber_metro_areas
                                WHERE subscriber_id = {subscriberId} AND metro_area_id = {metroAreaId.Value}")
                            .FirstOrDefaultAsync(cancellationToken);

                        result = count > 0;
                    }
                }
                else
                {
                    // No specific metro area - just check if email is subscribed to anything
                    result = await _dbSet
                        .Where(ns => ns.Email.Value == email)
                        .Where(ns => ns.IsActive)
                        .AnyAsync(cancellationToken);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "IsEmailSubscribedAsync COMPLETE: Email={Email}, MetroAreaId={MetroAreaId}, IsSubscribed={IsSubscribed}, Duration={ElapsedMs}ms",
                    email,
                    metroAreaId,
                    result,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "IsEmailSubscribedAsync FAILED: Email={Email}, MetroAreaId={MetroAreaId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    email,
                    metroAreaId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}

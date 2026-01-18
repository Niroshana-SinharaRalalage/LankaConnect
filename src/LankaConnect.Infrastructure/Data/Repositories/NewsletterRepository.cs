using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Newsletter aggregate
/// Phase 6A.74 Part 3C: Newsletter infrastructure following EventRepository shadow navigation pattern
/// </summary>
public class NewsletterRepository : Repository<Newsletter>, INewsletterRepository
{
    private readonly ILogger<NewsletterRepository> _repoLogger;

    public NewsletterRepository(
        AppDbContext context,
        ILogger<NewsletterRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Phase 6A.74: Override AddAsync to sync shadow navigation for email groups and metro areas
    /// When creating a new newsletter with email groups and metro areas, the domain's _emailGroupIds and _metroAreaIds lists
    /// contain the GUIDs, but the shadow navigation _emailGroupEntities and _metroAreaEntities need to be populated
    /// with actual entities for EF Core to create the many-to-many junction table rows.
    /// Pattern mirrors EventRepository.AddAsync for email groups - no entity state changes, just set CurrentValue.
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public override async Task AddAsync(Newsletter entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "AddAsync"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", entity.Id))
        using (LogContext.PushProperty("EmailGroupCount", entity.EmailGroupIds.Count))
        using (LogContext.PushProperty("MetroAreaCount", entity.MetroAreaIds.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("AddAsync START: NewsletterId={NewsletterId}, EmailGroups={EmailGroupCount}, MetroAreas={MetroAreaCount}",
                entity.Id, entity.EmailGroupIds.Count, entity.MetroAreaIds.Count);

            try
            {
                // Call base implementation to add entity to DbSet (state = Added)
                await base.AddAsync(entity, cancellationToken);

                // Sync email groups from domain list to shadow navigation for persistence
                if (entity.EmailGroupIds.Any())
                {
                    _repoLogger.LogDebug("Loading {Count} email group entities for shadow navigation", entity.EmailGroupIds.Count);

                    // Load the EmailGroup entities from the database based on the domain's ID list
                    var emailGroupEntities = await _context.Set<EmailGroup>()
                        .Where(eg => entity.EmailGroupIds.Contains(eg.Id))
                        .ToListAsync(cancellationToken);

                    if (emailGroupEntities.Count != entity.EmailGroupIds.Count)
                    {
                        _repoLogger.LogWarning("Email group count mismatch - Expected: {Expected}, Found: {Found}",
                            entity.EmailGroupIds.Count, emailGroupEntities.Count);
                    }

                    // Access shadow navigation using EF Core's Entry API
                    var emailGroupsCollection = _context.Entry(entity).Collection("_emailGroupEntities");

                    // Set the loaded entities into the shadow navigation
                    // EF Core will detect this and create rows in newsletter_email_groups junction table
                    // Entity remains in Added state - NO state changes needed
                    emailGroupsCollection.CurrentValue = emailGroupEntities;

                    _repoLogger.LogDebug("Synced {Count} email groups to shadow navigation", emailGroupEntities.Count);
                }

                // Phase 6A.74 Enhancement 1: Sync metro areas from domain list to shadow navigation for persistence
                if (entity.MetroAreaIds.Any())
                {
                    _repoLogger.LogDebug("Loading {Count} metro area entities for shadow navigation", entity.MetroAreaIds.Count);

                    // Load the MetroArea entities from the database based on the domain's ID list
                    var metroAreaEntities = await _context.Set<Domain.Events.MetroArea>()
                        .Where(m => entity.MetroAreaIds.Contains(m.Id))
                        .ToListAsync(cancellationToken);

                    if (metroAreaEntities.Count != entity.MetroAreaIds.Count)
                    {
                        _repoLogger.LogWarning("Metro area count mismatch - Expected: {Expected}, Found: {Found}",
                            entity.MetroAreaIds.Count, metroAreaEntities.Count);
                    }

                    // Access shadow navigation using EF Core's Entry API
                    var metroAreasCollection = _context.Entry(entity).Collection("_metroAreaEntities");

                    // Set the loaded entities into the shadow navigation
                    // EF Core will detect this and create rows in newsletter_metro_areas junction table
                    metroAreasCollection.CurrentValue = metroAreaEntities;

                    _repoLogger.LogDebug("Synced {Count} metro areas to shadow navigation", metroAreaEntities.Count);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "AddAsync COMPLETE: NewsletterId={NewsletterId}, EmailGroups={EmailGroupCount}, MetroAreas={MetroAreaCount}, Duration={ElapsedMs}ms",
                    entity.Id,
                    entity.EmailGroupIds.Count,
                    entity.MetroAreaIds.Count,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "AddAsync FAILED: NewsletterId={NewsletterId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.74: Override GetByIdAsync to include email groups and metro areas shadow navigation
    /// After loading, sync shadow navigation entities back to domain's email group and metro area ID lists
    /// Supports trackChanges parameter for both command and query handlers
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<Newsletter?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdAsync"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", id))
        using (LogContext.PushProperty("TrackChanges", trackChanges))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByIdAsync START: NewsletterId={NewsletterId}, TrackChanges={TrackChanges}",
                id, trackChanges);

            try
            {
                // Build query with eager loading for shadow navigation
                IQueryable<Newsletter> query = _dbSet
                    .Include("_emailGroupEntities")
                    .Include("_metroAreaEntities");

                // Apply tracking behavior based on parameter
                // Command handlers need tracked entities (trackChanges: true) for EF Core change detection
                // Query handlers need untracked entities (trackChanges: false) for better performance
                if (!trackChanges)
                {
                    query = query.AsNoTracking();
                    _repoLogger.LogDebug("Loading entity WITHOUT change tracking (read-only)");
                }
                else
                {
                    _repoLogger.LogDebug("Loading entity WITH change tracking (for modifications)");
                }

                var newsletter = await query.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

                if (newsletter == null)
                {
                    stopwatch.Stop();

                    _repoLogger.LogWarning(
                        "GetByIdAsync COMPLETE (Not Found): NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        id,
                        stopwatch.ElapsedMilliseconds);

                    return null;
                }

                // Sync email group IDs from shadow navigation to domain
                var emailGroupsCollection = _context.Entry(newsletter).Collection("_emailGroupEntities");
                var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<EmailGroup>;

                if (emailGroupEntities != null)
                {
                    var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
                    newsletter.SyncEmailGroupIdsFromEntities(emailGroupIds);

                    _repoLogger.LogDebug("Synced {EmailGroupCount} email group IDs to domain entity",
                        emailGroupIds.Count);
                }

                // Phase 6A.74 Enhancement 1: Sync metro area IDs from shadow navigation to domain
                var metroAreasCollection = _context.Entry(newsletter).Collection("_metroAreaEntities");
                var metroAreaEntities = metroAreasCollection.CurrentValue as IEnumerable<Domain.Events.MetroArea>;

                if (metroAreaEntities != null)
                {
                    var metroAreaIds = metroAreaEntities.Select(m => m.Id).ToList();
                    newsletter.SyncMetroAreaIdsFromEntities(metroAreaIds);

                    _repoLogger.LogDebug("Synced {MetroAreaCount} metro area IDs to domain entity",
                        metroAreaIds.Count);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdAsync COMPLETE: NewsletterId={NewsletterId}, Status={Status}, TrackChanges={TrackChanges}, Duration={ElapsedMs}ms",
                    newsletter.Id,
                    newsletter.Status,
                    trackChanges,
                    stopwatch.ElapsedMilliseconds);

                return newsletter;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdAsync FAILED: NewsletterId={NewsletterId}, TrackChanges={TrackChanges}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    trackChanges,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.74: Override base GetByIdAsync to forward to trackChanges version
    /// This ensures ALL calls use the 3-parameter overload with explicit change tracking control.
    /// </summary>
    public override async Task<Newsletter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Forward to the 3-parameter version with tracking ENABLED by default
        // This makes tracked entities the default behavior for command handlers
        return await GetByIdAsync(id, trackChanges: true, cancellationToken);
    }

    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<Newsletter>> GetByCreatorAsync(Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCreator"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("CreatedByUserId", createdByUserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByCreatorAsync START: CreatedByUserId={CreatedByUserId}", createdByUserId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.CreatedByUserId == createdByUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCreatorAsync COMPLETE: CreatedByUserId={CreatedByUserId}, Count={Count}, Duration={ElapsedMs}ms",
                    createdByUserId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByCreatorAsync FAILED: CreatedByUserId={CreatedByUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    createdByUserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<Newsletter>> GetByStatusAsync(NewsletterStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.Status == status)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByStatusAsync FAILED: Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<Newsletter>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEvent"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.EventId == eventId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<Newsletter>> GetExpiredNewslettersAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredNewsletters"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        {
            var stopwatch = Stopwatch.StartNew();
            var now = DateTime.UtcNow;

            _repoLogger.LogDebug("GetExpiredNewslettersAsync START: CurrentTime={CurrentTime}", now);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.Status == NewsletterStatus.Active)
                    .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt <= now)
                    .OrderBy(n => n.ExpiresAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetExpiredNewslettersAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredNewslettersAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Newsletter>> GetPublishedNewslettersAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        _repoLogger.LogDebug("[Phase 6A.74] Getting published newsletters (Active or Sent status, limit: {Limit})", limit);

        // Phase 6A.74 Part 10 Issue #4: Include both Active and Sent newsletters for public display
        var result = await _dbSet
            .AsNoTracking()
            .Where(n => n.Status == NewsletterStatus.Active || n.Status == NewsletterStatus.Sent)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _repoLogger.LogInformation("[Phase 6A.74] Found {Count} published newsletters (Active or Sent)", result.Count);

        return result;
    }

    public async Task<IReadOnlyList<Newsletter>> GetPublishedWithFiltersAsync(
        DateTime? publishedFrom = null,
        DateTime? publishedTo = null,
        string? state = null,
        List<Guid>? metroAreaIds = null,
        string? searchTerm = null,
        Guid? userId = null,
        decimal? latitude = null,
        decimal? longitude = null,
        CancellationToken cancellationToken = default)
    {
        _repoLogger.LogDebug(
            "[Phase 6A.74 Parts 10/11] Getting published newsletters with filters - PublishedFrom: {From}, PublishedTo: {To}, State: {State}, MetroCount: {MetroCount}, SearchTerm: {Search}",
            publishedFrom,
            publishedTo,
            state,
            metroAreaIds?.Count ?? 0,
            searchTerm);

        // Phase 6A.74 Part 10 Issue #4: Include both Active and Sent newsletters for public display
        var query = _dbSet
            .AsNoTracking()
            .Where(n => n.Status == NewsletterStatus.Active || n.Status == NewsletterStatus.Sent);

        // Filter by published date range
        if (publishedFrom.HasValue)
        {
            query = query.Where(n => n.PublishedAt.HasValue && n.PublishedAt >= publishedFrom.Value);
        }

        if (publishedTo.HasValue)
        {
            query = query.Where(n => n.PublishedAt.HasValue && n.PublishedAt <= publishedTo.Value);
        }

        // Filter by metro areas
        // Issue #5 Fix: Query junction table directly since MetroAreaIds is ignored in EF Core mapping
        // Note: Newsletters use MetroAreaIds and TargetAllLocations, not state
        if (metroAreaIds != null && metroAreaIds.Count > 0)
        {
            query = query.Where(n =>
                n.TargetAllLocations ||                                           // Targets all locations
                _context.Set<Dictionary<string, object>>("newsletter_metro_areas")
                    .Any(j => (Guid)j["newsletter_id"] == n.Id && metroAreaIds.Contains((Guid)j["metro_area_id"])));
        }

        // Filter by search term (title or description)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(n =>
                n.Title.Value.Contains(searchTerm) ||
                n.Description.Value.Contains(searchTerm));
        }

        // Order by published date (most recent first)
        query = query.OrderByDescending(n => n.PublishedAt);

        var result = await query.ToListAsync(cancellationToken);

        _repoLogger.LogInformation(
            "[Phase 6A.74 Parts 10/11] Found {Count} published newsletters with filters",
            result.Count);

        // Phase 6A.74 Part 10 Issue #1: Log details of returned newsletters for debugging
        if (result.Count > 0)
        {
            foreach (var newsletter in result.Take(5)) // Log first 5 for debugging
            {
                _repoLogger.LogDebug(
                    "[Phase 6A.74] Newsletter returned - Id: {Id}, Title: {Title}, Status: {Status}, PublishedAt: {PublishedAt}, SentAt: {SentAt}",
                    newsletter.Id,
                    newsletter.Title.Value,
                    newsletter.Status,
                    newsletter.PublishedAt,
                    newsletter.SentAt);
            }
        }
        else
        {
            _repoLogger.LogWarning(
                "[Phase 6A.74 Parts 10/11] No newsletters found with Status = Active OR Sent. " +
                "Check database for newsletters.status values.");
        }

        return result;
    }
}

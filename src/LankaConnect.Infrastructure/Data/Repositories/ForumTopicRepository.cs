using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Community.Enums;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ForumTopic entity
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class ForumTopicRepository : Repository<ForumTopic>, IForumTopicRepository
{
    private readonly ILogger<ForumTopicRepository> _repoLogger;

    public ForumTopicRepository(
        AppDbContext context,
        ILogger<ForumTopicRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByForumAsync(Guid forumId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByForum"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("ForumId", forumId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByForumAsync START: ForumId={ForumId}", forumId);

            try
            {
                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.ForumId == forumId)
                    .OrderByDescending(t => t.IsPinned)
                    .ThenByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByForumAsync COMPLETE: ForumId={ForumId}, Count={Count}, Duration={ElapsedMs}ms",
                    forumId,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByForumAsync FAILED: ForumId={ForumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    forumId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.AuthorId == authorId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByCategoryAsync(ForumCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Category == category && t.Status == TopicStatus.Active)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByStatusAsync(TopicStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetPinnedTopicsAsync(Guid forumId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.ForumId == forumId && t.IsPinned && t.Status == TopicStatus.Active)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ForumTopic?> GetWithRepliesAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Replies)
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<ForumTopic>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == TopicStatus.Active &&
                       (t.Title.Value.ToLower().Contains(normalizedSearchTerm) ||
                        t.Content.Value.ToLower().Contains(normalizedSearchTerm)))
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
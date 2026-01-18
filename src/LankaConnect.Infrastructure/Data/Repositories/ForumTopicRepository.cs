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
        using (LogContext.PushProperty("Operation", "GetByAuthor"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("AuthorId", authorId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByAuthorAsync START: AuthorId={AuthorId}", authorId);

            try
            {
                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.AuthorId == authorId)
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByAuthorAsync COMPLETE: AuthorId={AuthorId}, Count={Count}, Duration={ElapsedMs}ms",
                    authorId,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByAuthorAsync FAILED: AuthorId={AuthorId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    authorId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByCategoryAsync(ForumCategory category, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCategory"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("Category", category))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByCategoryAsync START: Category={Category}", category);

            try
            {
                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.Category == category && t.Status == TopicStatus.Active)
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCategoryAsync COMPLETE: Category={Category}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByCategoryAsync FAILED: Category={Category}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByStatusAsync(TopicStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.Status == status)
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
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

    public async Task<IReadOnlyList<ForumTopic>> GetPinnedTopicsAsync(Guid forumId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPinnedTopics"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("ForumId", forumId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetPinnedTopicsAsync START: ForumId={ForumId}", forumId);

            try
            {
                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.ForumId == forumId && t.IsPinned && t.Status == TopicStatus.Active)
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPinnedTopicsAsync COMPLETE: ForumId={ForumId}, Count={Count}, Duration={ElapsedMs}ms",
                    forumId,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPinnedTopicsAsync FAILED: ForumId={ForumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    forumId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<ForumTopic?> GetWithRepliesAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetWithReplies"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("TopicId", topicId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetWithRepliesAsync START: TopicId={TopicId}", topicId);

            try
            {
                var topic = await _dbSet
                    .Include(t => t.Replies)
                    .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetWithRepliesAsync COMPLETE: TopicId={TopicId}, Found={Found}, ReplyCount={ReplyCount}, Duration={ElapsedMs}ms",
                    topicId,
                    topic != null,
                    topic?.Replies?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return topic;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetWithRepliesAsync FAILED: TopicId={TopicId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    topicId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<ForumTopic>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Search"))
        using (LogContext.PushProperty("EntityType", "ForumTopic"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchAsync COMPLETE: SearchTerm empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return new List<ForumTopic>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var topics = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.Status == TopicStatus.Active &&
                               (t.Title.Value.ToLower().Contains(normalizedSearchTerm) ||
                                t.Content.Value.ToLower().Contains(normalizedSearchTerm)))
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    topics.Count,
                    stopwatch.ElapsedMilliseconds);

                return topics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}

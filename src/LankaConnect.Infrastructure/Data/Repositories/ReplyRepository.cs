using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Community;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Reply entity
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class ReplyRepository : Repository<Reply>, IReplyRepository
{
    private readonly ILogger<ReplyRepository> _repoLogger;

    public ReplyRepository(
        AppDbContext context,
        ILogger<ReplyRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<Reply>> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTopic"))
        using (LogContext.PushProperty("EntityType", "Reply"))
        using (LogContext.PushProperty("TopicId", topicId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByTopicAsync START: TopicId={TopicId}", topicId);

            try
            {
                var replies = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.TopicId == topicId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByTopicAsync COMPLETE: TopicId={TopicId}, Count={Count}, Duration={ElapsedMs}ms",
                    topicId,
                    replies.Count,
                    stopwatch.ElapsedMilliseconds);

                return replies;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByTopicAsync FAILED: TopicId={TopicId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    topicId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Reply>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByAuthor"))
        using (LogContext.PushProperty("EntityType", "Reply"))
        using (LogContext.PushProperty("AuthorId", authorId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByAuthorAsync START: AuthorId={AuthorId}", authorId);

            try
            {
                var replies = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.AuthorId == authorId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByAuthorAsync COMPLETE: AuthorId={AuthorId}, Count={Count}, Duration={ElapsedMs}ms",
                    authorId,
                    replies.Count,
                    stopwatch.ElapsedMilliseconds);

                return replies;
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

    public async Task<IReadOnlyList<Reply>> GetByParentReplyAsync(Guid parentReplyId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByParentReply"))
        using (LogContext.PushProperty("EntityType", "Reply"))
        using (LogContext.PushProperty("ParentReplyId", parentReplyId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByParentReplyAsync START: ParentReplyId={ParentReplyId}", parentReplyId);

            try
            {
                var replies = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.ParentReplyId == parentReplyId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByParentReplyAsync COMPLETE: ParentReplyId={ParentReplyId}, Count={Count}, Duration={ElapsedMs}ms",
                    parentReplyId,
                    replies.Count,
                    stopwatch.ElapsedMilliseconds);

                return replies;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByParentReplyAsync FAILED: ParentReplyId={ParentReplyId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    parentReplyId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Reply>> GetSolutionsForTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetSolutionsForTopic"))
        using (LogContext.PushProperty("EntityType", "Reply"))
        using (LogContext.PushProperty("TopicId", topicId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetSolutionsForTopicAsync START: TopicId={TopicId}", topicId);

            try
            {
                var solutions = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.TopicId == topicId && r.IsMarkedAsSolution)
                    .OrderByDescending(r => r.HelpfulVotes)
                    .ThenBy(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetSolutionsForTopicAsync COMPLETE: TopicId={TopicId}, SolutionCount={Count}, Duration={ElapsedMs}ms",
                    topicId,
                    solutions.Count,
                    stopwatch.ElapsedMilliseconds);

                return solutions;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetSolutionsForTopicAsync FAILED: TopicId={TopicId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    topicId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Reply>> GetTopHelpfulRepliesAsync(Guid topicId, int count, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTopHelpfulReplies"))
        using (LogContext.PushProperty("EntityType", "Reply"))
        using (LogContext.PushProperty("TopicId", topicId))
        using (LogContext.PushProperty("Count", count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetTopHelpfulRepliesAsync START: TopicId={TopicId}, Count={Count}", topicId, count);

            try
            {
                var replies = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.TopicId == topicId && r.HelpfulVotes > 0)
                    .OrderByDescending(r => r.HelpfulVotes)
                    .ThenBy(r => r.CreatedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTopHelpfulRepliesAsync COMPLETE: TopicId={TopicId}, RequestedCount={RequestedCount}, ActualCount={ActualCount}, Duration={ElapsedMs}ms",
                    topicId,
                    count,
                    replies.Count,
                    stopwatch.ElapsedMilliseconds);

                return replies;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTopHelpfulRepliesAsync FAILED: TopicId={TopicId}, RequestedCount={Count}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    topicId,
                    count,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}

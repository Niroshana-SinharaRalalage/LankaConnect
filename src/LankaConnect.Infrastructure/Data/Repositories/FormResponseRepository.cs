using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class FormResponseRepository : Repository<FormResponse>, IFormResponseRepository
{
    private readonly ILogger<FormResponseRepository> _repoLogger;

    public FormResponseRepository(
        AppDbContext context,
        ILogger<FormResponseRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<FormResponse?> GetByIdWithAnswersAsync(Guid responseId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdWithAnswers"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("ResponseId", responseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByIdWithAnswersAsync START: ResponseId={ResponseId}",
                responseId);

            try
            {
                var result = await _dbSet
                    .Include(r => r.Answers)
                    .FirstOrDefaultAsync(r => r.Id == responseId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdWithAnswersAsync COMPLETE: ResponseId={ResponseId}, Found={Found}, AnswerCount={AnswerCount}, Duration={ElapsedMs}ms",
                    responseId,
                    result != null,
                    result?.Answers.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdWithAnswersAsync FAILED: ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    responseId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<FormResponse?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByAccessTokenHash"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByAccessTokenHashAsync START");

            try
            {
                var result = await _dbSet
                    .Include(r => r.Answers)
                    .FirstOrDefaultAsync(r => r.AccessTokenHash == tokenHash, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByAccessTokenHashAsync COMPLETE: Found={Found}, ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                    result != null,
                    result?.Id,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByAccessTokenHashAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<FormResponse?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByFormAndUser"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", formId))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByFormAndUserAsync START: FormId={FormId}, UserId={UserId}",
                formId, userId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Answers)
                    .Where(r => r.EventFormId == formId && r.RespondentUserId == userId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByFormAndUserAsync COMPLETE: FormId={FormId}, UserId={UserId}, Found={Found}, Duration={ElapsedMs}ms",
                    formId, userId,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByFormAndUserAsync FAILED: FormId={FormId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    formId, userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<FormResponse?> GetByFormAndEmailAsync(Guid formId, string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByFormAndEmail"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", formId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByFormAndEmailAsync START: FormId={FormId}, Email={Email}",
                formId, email);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Answers)
                    .Where(r => r.EventFormId == formId && r.RespondentEmail == email)
                    .OrderByDescending(r => r.SubmittedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByFormAndEmailAsync COMPLETE: FormId={FormId}, Email={Email}, Found={Found}, Duration={ElapsedMs}ms",
                    formId, email,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByFormAndEmailAsync FAILED: FormId={FormId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    formId, email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<int> GetCountByFormIdAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCountByFormId"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", formId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetCountByFormIdAsync START: FormId={FormId}",
                formId);

            try
            {
                var result = await _dbSet
                    .CountAsync(r => r.EventFormId == formId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCountByFormIdAsync COMPLETE: FormId={FormId}, Count={Count}, Duration={ElapsedMs}ms",
                    formId, result,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetCountByFormIdAsync FAILED: FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    formId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<(IReadOnlyList<FormResponse> Responses, int TotalCount)> GetPaginatedAsync(
        Guid formId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPaginated"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", formId))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetPaginatedAsync START: FormId={FormId}, Page={Page}, PageSize={PageSize}",
                formId, page, pageSize);

            try
            {
                var query = _dbSet
                    .AsNoTracking()
                    .Include(r => r.Answers)
                    .Where(r => r.EventFormId == formId);

                var totalCount = await query.CountAsync(cancellationToken);

                var responses = await query
                    .OrderByDescending(r => r.SubmittedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPaginatedAsync COMPLETE: FormId={FormId}, Page={Page}, PageSize={PageSize}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    formId, page, pageSize,
                    responses.Count, totalCount,
                    stopwatch.ElapsedMilliseconds);

                return (responses, totalCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPaginatedAsync FAILED: FormId={FormId}, Page={Page}, PageSize={PageSize}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    formId, page, pageSize,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}

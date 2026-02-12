using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventFormRepository : Repository<EventForm>, IEventFormRepository
{
    private readonly ILogger<EventFormRepository> _repoLogger;

    public EventFormRepository(
        AppDbContext context,
        ILogger<EventFormRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<EventForm?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdWithQuestions"))
        using (LogContext.PushProperty("EntityType", "EventForm"))
        using (LogContext.PushProperty("FormId", formId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByIdWithQuestionsAsync START: FormId={FormId}",
                formId);

            try
            {
                var result = await _dbSet
                    .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                    .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdWithQuestionsAsync COMPLETE: FormId={FormId}, Found={Found}, QuestionCount={QuestionCount}, Duration={ElapsedMs}ms",
                    formId,
                    result != null,
                    result?.Questions.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdWithQuestionsAsync FAILED: FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    formId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<EventForm>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "EventForm"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                    .Where(f => f.EventId == eventId)
                    .OrderBy(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, TotalQuestions={TotalQuestions}, Duration={ElapsedMs}ms",
                    eventId,
                    result.Count,
                    result.Sum(f => f.Questions.Count),
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}

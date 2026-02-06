using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetWaitingList;

/// <summary>
/// Handler for GetWaitingListQuery
/// Returns list of users on waiting list with their positions
/// </summary>
public class GetWaitingListQueryHandler : IQueryHandler<GetWaitingListQuery, IReadOnlyList<WaitingListEntryDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetWaitingListQueryHandler> _logger;

    public GetWaitingListQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetWaitingListQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WaitingListEntryDto>>> Handle(GetWaitingListQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetWaitingList"))
        using (LogContext.PushProperty("EntityType", "WaitingList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetWaitingList START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetWaitingList FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<WaitingListEntryDto>>.Failure("Event ID is required");
                }

                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetWaitingList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<WaitingListEntryDto>>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "GetWaitingList: Event loaded - EventId={EventId}, Title={Title}, WaitingListCount={WaitingListCount}",
                    @event.Id, @event.Title.Value, @event.WaitingList.Count);

                // Map waiting list entries to DTOs
                var waitingListDtos = @event.WaitingList
                    .OrderBy(w => w.Position)
                    .Select(w => new WaitingListEntryDto
                    {
                        UserId = w.UserId,
                        Position = w.Position,
                        JoinedAt = w.JoinedAt
                    })
                    .ToList()
                    .AsReadOnly();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetWaitingList COMPLETE: EventId={EventId}, EntryCount={EntryCount}, Duration={ElapsedMs}ms",
                    request.EventId, waitingListDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<WaitingListEntryDto>>.Success(waitingListDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetWaitingList FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

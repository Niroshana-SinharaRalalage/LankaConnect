using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;

public class GetPendingEventsForApprovalQueryHandler : IQueryHandler<GetPendingEventsForApprovalQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingEventsForApprovalQueryHandler> _logger;

    public GetPendingEventsForApprovalQueryHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<GetPendingEventsForApprovalQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetPendingEventsForApprovalQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPendingEventsForApproval"))
        using (LogContext.PushProperty("EntityType", "Event"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetPendingEventsForApproval START");

            try
            {
                // Get all events with UnderReview status
                var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
                var pendingEvents = allEvents
                    .Where(e => e.Status == EventStatus.UnderReview)
                    .OrderBy(e => e.CreatedAt) // Oldest first for admin review
                    .ToList();

                _logger.LogInformation(
                    "GetPendingEventsForApproval: Events filtered - TotalEvents={TotalEvents}, PendingCount={PendingCount}",
                    allEvents.Count(), pendingEvents.Count);

                var eventDtos = _mapper.Map<IReadOnlyList<EventDto>>(pendingEvents);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetPendingEventsForApproval COMPLETE: PendingCount={PendingCount}, Duration={ElapsedMs}ms",
                    eventDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(eventDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetPendingEventsForApproval FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

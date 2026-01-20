using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common; // Phase 6A.32: EmailGroupSummaryDto
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Communications; // Phase 6A.32: Email groups
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventById;

/// <summary>
/// Handler for retrieving a single event by its ID
/// Includes email group data for event management
/// </summary>
public class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailGroupRepository _emailGroupRepository; // Phase 6A.32: Email groups
    private readonly IMapper _mapper;
    private readonly ILogger<GetEventByIdQueryHandler> _logger;

    public GetEventByIdQueryHandler(
        IEventRepository eventRepository,
        IEmailGroupRepository emailGroupRepository, // Phase 6A.32: Email groups
        IMapper mapper,
        ILogger<GetEventByIdQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _emailGroupRepository = emailGroupRepository; // Phase 6A.32: Email groups
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<EventDto?>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventById"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventById START: EventId={EventId}",
                request.Id);

            try
            {
                // Validate request
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventById FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<EventDto?>.Failure("Event ID is required");
                }

                var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetEventById COMPLETE: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<EventDto?>.Success(null);
                }

                _logger.LogInformation(
                    "GetEventById: Event loaded - EventId={EventId}, Title={Title}, Status={Status}, Category={Category}",
                    @event.Id, @event.Title.Value, @event.Status, @event.Category);

                // Map base event properties using AutoMapper
                var result = _mapper.Map<EventDto>(@event);

                // Phase 6A.32: Batch query for email groups (Fix #3: No N+1)
                var emailGroupSummaries = new List<EmailGroupSummaryDto>();
                if (@event.EmailGroupIds.Any())
                {
                    _logger.LogInformation(
                        "GetEventById: Loading email groups - EventId={EventId}, EmailGroupCount={EmailGroupCount}",
                        @event.Id, @event.EmailGroupIds.Count);

                    var emailGroups = await _emailGroupRepository.GetByIdsAsync(@event.EmailGroupIds, cancellationToken);

                    foreach (var groupId in @event.EmailGroupIds)
                    {
                        var group = emailGroups.FirstOrDefault(g => g.Id == groupId);

                        if (group != null)
                        {
                            emailGroupSummaries.Add(new EmailGroupSummaryDto
                            {
                                Id = group.Id,
                                Name = group.Name,
                                IsActive = group.IsActive
                            });
                        }
                        else
                        {
                            _logger.LogWarning(
                                "GetEventById: Email group not found (may have been deleted) - EventId={EventId}, EmailGroupId={EmailGroupId}",
                                @event.Id, groupId);
                        }
                    }

                    _logger.LogInformation(
                        "GetEventById: Email groups loaded - EventId={EventId}, LoadedGroups={LoadedGroups}, RequestedGroups={RequestedGroups}",
                        @event.Id, emailGroupSummaries.Count, @event.EmailGroupIds.Count);
                }

                // Create new DTO with email group data (record with-expression)
                result = result with
                {
                    EmailGroupIds = @event.EmailGroupIds.ToList(),
                    EmailGroups = emailGroupSummaries
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventById COMPLETE: EventId={EventId}, Title={Title}, EmailGroups={EmailGroupCount}, Duration={ElapsedMs}ms",
                    @event.Id, @event.Title.Value, emailGroupSummaries.Count, stopwatch.ElapsedMilliseconds);

                return Result<EventDto?>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventById FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

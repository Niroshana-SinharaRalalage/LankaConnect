using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common; // Phase 6A.32: legacy EmailGroupSummaryDto consumed by EventDto
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Modules.Communications.Contracts; // Wave 5.4.d.1: IEmailGroupQueries swap
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
    private readonly IRegistrationRepository _registrationRepository; // Phase 6A.137: Registration status
    private readonly IEmailGroupQueries _emailGroupQueries; // Wave 5.4.d.1: was IEmailGroupRepository
    private readonly ICurrentUserService _currentUserService; // Phase 6A.133: Multi-organizer
    private readonly IMapper _mapper;
    private readonly ILogger<GetEventByIdQueryHandler> _logger;

    public GetEventByIdQueryHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository, // Phase 6A.137: Registration status
        IEmailGroupQueries emailGroupQueries, // Wave 5.4.d.1
        ICurrentUserService currentUserService, // Phase 6A.133: Multi-organizer
        IMapper mapper,
        ILogger<GetEventByIdQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _emailGroupQueries = emailGroupQueries;
        _currentUserService = currentUserService;
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

                // Perf RCA 2026-04-25: read-only handler — pass trackChanges:false explicitly so
                // the EF change tracker doesn't materialize the full aggregate (Images, Videos,
                // Registrations, SignUpLists.Items.Commitments, etc.). The parameterless overload
                // forwards to trackChanges:true, which adds wasted CPU + memory on every read.
                var @event = await _eventRepository.GetByIdAsync(request.Id, trackChanges: false, cancellationToken);

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
                var emailGroupSummaries = new List<LankaConnect.Application.Communications.Common.EmailGroupSummaryDto>();
                if (@event.EmailGroupIds.Any())
                {
                    _logger.LogInformation(
                        "GetEventById: Loading email groups - EventId={EventId}, EmailGroupCount={EmailGroupCount}",
                        @event.Id, @event.EmailGroupIds.Count);

                    // Wave 5.4.d.1 (2026-06-22): cross-module fetch via IEmailGroupQueries.
                    // Returns Modules.Communications.Contracts.EmailGroupSummaryDto; we project
                    // 3 fields into the legacy Application.Communications.Common.EmailGroupSummaryDto
                    // that EventDto consumes (full qualifier needed because both types share the name).
                    var emailGroups = await _emailGroupQueries.GetByIdsAsync(@event.EmailGroupIds, cancellationToken);

                    foreach (var groupId in @event.EmailGroupIds)
                    {
                        var group = emailGroups.FirstOrDefault(g => g.Id == groupId);

                        if (group != null)
                        {
                            emailGroupSummaries.Add(new LankaConnect.Application.Communications.Common.EmailGroupSummaryDto
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

                // Create new DTO with email group data and organizer status (record with-expression)
                // Phase 6A.133: Compute IsCurrentUserOrganizer server-side
                bool? isCurrentUserOrganizer = _currentUserService.IsAuthenticated
                    ? @event.IsOrganizer(_currentUserService.UserId)
                    : null;

                // Phase 6A.137: Populate UserRegistrationStatus for authenticated users
                RegistrationStatus? userRegistrationStatus = null;
                if (_currentUserService.IsAuthenticated && _currentUserService.UserId != Guid.Empty)
                {
                    try
                    {
                        var registration = await _registrationRepository.GetByEventAndUserAsync(
                            @event.Id, _currentUserService.UserId, cancellationToken);

                        if (registration != null)
                        {
                            userRegistrationStatus = registration.Status;

                            _logger.LogInformation(
                                "GetEventById: UserRegistrationStatus populated - EventId={EventId}, UserId={UserId}, Status={Status}",
                                @event.Id, _currentUserService.UserId, registration.Status);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail — registration status is supplementary data
                        _logger.LogWarning(ex,
                            "GetEventById: Failed to populate UserRegistrationStatus - EventId={EventId}, UserId={UserId}, Error={ErrorMessage}",
                            @event.Id, _currentUserService.UserId, ex.Message);
                    }
                }

                result = result with
                {
                    EmailGroupIds = @event.EmailGroupIds.ToList(),
                    EmailGroups = emailGroupSummaries,
                    IsCurrentUserOrganizer = isCurrentUserOrganizer,
                    UserRegistrationStatus = userRegistrationStatus
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

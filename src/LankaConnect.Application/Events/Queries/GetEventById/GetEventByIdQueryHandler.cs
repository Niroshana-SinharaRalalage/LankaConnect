using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Communications; // Phase 6A.32: Email groups

namespace LankaConnect.Application.Events.Queries.GetEventById;

public class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailGroupRepository _emailGroupRepository; // Phase 6A.32: Email groups
    private readonly IMapper _mapper;

    public GetEventByIdQueryHandler(
        IEventRepository eventRepository,
        IEmailGroupRepository emailGroupRepository, // Phase 6A.32: Email groups
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _emailGroupRepository = emailGroupRepository; // Phase 6A.32: Email groups
        _mapper = mapper;
    }

    public async Task<Result<EventDto?>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

        if (@event == null)
            return Result<EventDto?>.Success(null);

        // Map base event properties using AutoMapper
        var result = _mapper.Map<EventDto>(@event);

        // Phase 6A.32: Batch query for email groups (Fix #3: No N+1)
        var emailGroupSummaries = new List<EmailGroupSummaryDto>();
        if (@event.EmailGroupIds.Any())
        {
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
                // Note: If group is null, it was deleted from the database
                // We skip it rather than adding a null entry
            }
        }

        // Create new DTO with email group data (record with-expression)
        result = result with
        {
            EmailGroupIds = @event.EmailGroupIds.ToList(),
            EmailGroups = emailGroupSummaries
        };

        return Result<EventDto?>.Success(result);
    }
}

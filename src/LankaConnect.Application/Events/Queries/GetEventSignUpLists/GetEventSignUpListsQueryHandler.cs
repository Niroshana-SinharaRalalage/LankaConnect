using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetEventSignUpLists;

public class GetEventSignUpListsQueryHandler : IQueryHandler<GetEventSignUpListsQuery, List<SignUpListDto>>
{
    private readonly IEventRepository _eventRepository;

    public GetEventSignUpListsQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Result<List<SignUpListDto>>> Handle(GetEventSignUpListsQuery request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<List<SignUpListDto>>.Failure($"Event with ID {request.EventId} not found");

        var signUpListDtos = @event.SignUpLists.Select(signUpList => new SignUpListDto
        {
            Id = signUpList.Id,
            Category = signUpList.Category,
            Description = signUpList.Description,
            SignUpType = signUpList.SignUpType,
            PredefinedItems = signUpList.PredefinedItems.ToList(),
            Commitments = signUpList.Commitments.Select(c => new SignUpCommitmentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                ItemDescription = c.ItemDescription,
                Quantity = c.Quantity,
                CommittedAt = c.CommittedAt
            }).ToList(),
            CommitmentCount = signUpList.GetCommitmentCount()
        }).ToList();

        return Result<List<SignUpListDto>>.Success(signUpListDtos);
    }
}

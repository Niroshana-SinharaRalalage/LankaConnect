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

            // Legacy fields (for Open/Predefined sign-ups)
            PredefinedItems = signUpList.PredefinedItems.ToList(),
            Commitments = signUpList.Commitments.Select(c => new SignUpCommitmentDto
            {
                Id = c.Id,
                SignUpItemId = c.SignUpItemId,
                UserId = c.UserId,
                ItemDescription = c.ItemDescription,
                Quantity = c.Quantity,
                CommittedAt = c.CommittedAt,
                Notes = c.Notes
            }).ToList(),
            CommitmentCount = signUpList.GetCommitmentCount(),

            // New category-based fields
            HasMandatoryItems = signUpList.HasMandatoryItems,
            HasPreferredItems = signUpList.HasPreferredItems,
            HasSuggestedItems = signUpList.HasSuggestedItems,
            Items = signUpList.Items.Select(item => new SignUpItemDto
            {
                Id = item.Id,
                ItemDescription = item.ItemDescription,
                Quantity = item.Quantity,
                RemainingQuantity = item.RemainingQuantity,
                ItemCategory = item.ItemCategory,
                Notes = item.Notes,
                Commitments = item.Commitments.Select(c => new SignUpCommitmentDto
                {
                    Id = c.Id,
                    SignUpItemId = c.SignUpItemId,
                    UserId = c.UserId,
                    ItemDescription = c.ItemDescription,
                    Quantity = c.Quantity,
                    CommittedAt = c.CommittedAt,
                    Notes = c.Notes
                }).ToList()
            }).ToList()
        }).ToList();

        return Result<List<SignUpListDto>>.Success(signUpListDtos);
    }
}

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.AddSignUpItem;

public class AddSignUpItemCommandHandler : ICommandHandler<AddSignUpItemCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event with sign-up lists
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<Guid>.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Add item to the sign-up list
        var itemResult = signUpList.AddItem(
            request.ItemDescription,
            request.Quantity,
            request.ItemCategory,
            request.Notes);

        if (itemResult.IsFailure)
            return Result<Guid>.Failure(itemResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(itemResult.Value.Id);
    }
}

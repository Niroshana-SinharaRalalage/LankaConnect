using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.RemoveSignUpItem;

public class RemoveSignUpItemCommandHandler : ICommandHandler<RemoveSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event with sign-up lists
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Remove item from the sign-up list
        var removeResult = signUpList.RemoveItem(request.SignUpItemId);
        if (removeResult.IsFailure)
            return Result.Failure(removeResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

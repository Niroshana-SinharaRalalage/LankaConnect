using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.CommitToSignUp;

public class CommitToSignUpCommandHandler : ICommandHandler<CommitToSignUpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CommitToSignUpCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CommitToSignUpCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list
        var signUpList = @event.GetSignUpList(request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Add commitment
        var commitmentResult = signUpList.AddCommitment(
            request.UserId,
            request.ItemDescription,
            request.Quantity);

        if (commitmentResult.IsFailure)
            return Result.Failure(commitmentResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

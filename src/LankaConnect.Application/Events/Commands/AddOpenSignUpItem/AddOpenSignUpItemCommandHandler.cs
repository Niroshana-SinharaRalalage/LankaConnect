using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for adding a user-submitted Open item to a sign-up list
/// </summary>
public class AddOpenSignUpItemCommandHandler : ICommandHandler<AddOpenSignUpItemCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddOpenSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<Guid>.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list from the event
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Add the Open item (domain method handles validation and auto-commitment)
        var itemResult = signUpList.AddOpenItem(
            request.UserId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        if (itemResult.IsFailure)
            return Result<Guid>.Failure(itemResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        // Return the created item ID
        return Result<Guid>.Success(itemResult.Value.Id);
    }
}

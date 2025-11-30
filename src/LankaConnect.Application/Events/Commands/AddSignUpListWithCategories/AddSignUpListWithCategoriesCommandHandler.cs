using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Application.Events.Commands.AddSignUpListWithCategories;

public class AddSignUpListWithCategoriesCommandHandler : ICommandHandler<AddSignUpListWithCategoriesCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSignUpListWithCategoriesCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddSignUpListWithCategoriesCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Create category-based sign-up list
        var signUpListResult = SignUpList.CreateWithCategories(
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems);

        if (signUpListResult.IsFailure)
            return Result.Failure(signUpListResult.Error);

        // Add to event
        var addResult = @event.AddSignUpList(signUpListResult.Value);
        if (addResult.IsFailure)
            return Result.Failure(addResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

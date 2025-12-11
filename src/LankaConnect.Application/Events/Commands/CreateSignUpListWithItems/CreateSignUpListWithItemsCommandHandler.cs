using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Application.Events.Commands.CreateSignUpListWithItems;

/// <summary>
/// Handler for creating a sign-up list with items in a single transactional operation
/// </summary>
public class CreateSignUpListWithItemsCommandHandler : ICommandHandler<CreateSignUpListWithItemsCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSignUpListWithItemsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSignUpListWithItemsCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<Guid>.Failure($"Event with ID {request.EventId} not found");

        // Convert items to tuple format expected by domain method
        var items = request.Items.Select(item => (
            description: item.ItemDescription,
            quantity: item.Quantity,
            category: item.ItemCategory,
            notes: item.Notes
        ));

        // Create sign-up list with items in single operation
        var signUpListResult = SignUpList.CreateWithCategoriesAndItems(
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems,
            items);

        if (signUpListResult.IsFailure)
            return Result<Guid>.Failure(signUpListResult.Error);

        // Add to event
        var addResult = @event.AddSignUpList(signUpListResult.Value);
        if (addResult.IsFailure)
            return Result<Guid>.Failure(addResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        // Return the created sign-up list ID
        return Result<Guid>.Success(signUpListResult.Value.Id);
    }
}

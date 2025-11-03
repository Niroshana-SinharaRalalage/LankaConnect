using FluentValidation;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.ReorderEventImages;

/// <summary>
/// Command to reorder event images by specifying new display orders
/// </summary>
public record ReorderEventImagesCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Dictionary<Guid, int> NewOrders { get; init; } = new();
}

public class ReorderEventImagesCommandValidator : AbstractValidator<ReorderEventImagesCommand>
{
    public ReorderEventImagesCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.NewOrders)
            .NotNull()
            .NotEmpty()
            .WithMessage("New display orders are required");

        RuleFor(x => x.NewOrders)
            .Must(orders => orders.Values.All(o => o > 0))
            .When(x => x.NewOrders != null)
            .WithMessage("All display orders must be greater than 0");
    }
}

public class ReorderEventImagesCommandHandler : IRequestHandler<ReorderEventImagesCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderEventImagesCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReorderEventImagesCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Reorder images using domain method (enforces business rules)
        var reorderResult = @event.ReorderImages(request.NewOrders);
        if (!reorderResult.IsSuccess)
            return reorderResult;

        // 3. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

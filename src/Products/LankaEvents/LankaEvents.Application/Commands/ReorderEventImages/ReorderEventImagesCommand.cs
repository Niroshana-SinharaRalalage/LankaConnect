using FluentValidation;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using MediatR;
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
namespace LankaConnect.Products.LankaEvents.Application.Commands.ReorderEventImages;

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
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges

    public ReorderEventImagesCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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

        // 3. Save changes (Wave 8.5.g: direct SaveChanges on LankaEventsDbContext)
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

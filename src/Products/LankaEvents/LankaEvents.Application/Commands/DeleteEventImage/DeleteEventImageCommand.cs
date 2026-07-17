using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using MediatR;
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteEventImage;

/// <summary>
/// Command to delete an image from an event's gallery
/// Removes image from Event aggregate and deletes from Azure Blob Storage via domain event handler
/// </summary>
public record DeleteEventImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid ImageId { get; init; }
}

public class DeleteEventImageCommandHandler : IRequestHandler<DeleteEventImageCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges

    public DeleteEventImageCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteEventImageCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Remove image from Event aggregate (raises ImageRemovedFromEventDomainEvent)
        var removeResult = @event.RemoveImage(request.ImageId);
        if (!removeResult.IsSuccess)
            return removeResult;

        // 3. Save changes (Wave 8.5.g: direct SaveChanges on LankaEventsDbContext)
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

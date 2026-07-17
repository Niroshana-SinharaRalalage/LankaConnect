using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddToWaitingList;

/// <summary>
/// Handler for AddToWaitingListCommand
/// Adds user to event waiting list when event is at capacity
/// </summary>
public class AddToWaitingListCommandHandler : ICommandHandler<AddToWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<AddToWaitingListCommandHandler> _logger;

    public AddToWaitingListCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<AddToWaitingListCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(AddToWaitingListCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddToWaitingList"))
        using (LogContext.PushProperty("EntityType", "WaitingListEntry"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddToWaitingList START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddToWaitingList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "AddToWaitingList: Event loaded - EventId={EventId}, Title={Title}, Capacity={Capacity}, WaitingListCount={WaitingListCount}",
                    @event.Id, @event.Title.Value, @event.Capacity, @event.WaitingList.Count);

                // Use domain method to add user to waiting list
                var addResult = @event.AddToWaitingList(request.UserId);
                if (addResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddToWaitingList FAILED: Domain validation failed - EventId={EventId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, addResult.Error, stopwatch.ElapsedMilliseconds);

                    return addResult;
                }

                _logger.LogInformation(
                    "AddToWaitingList: Domain method succeeded - EventId={EventId}, UserId={UserId}, NewWaitingListCount={WaitingListCount}",
                    @event.Id, request.UserId, @event.WaitingList.Count);

                // Save changes (Wave 8.5.g: direct SaveChanges on LankaEventsDbContext)
                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddToWaitingList COMPLETE: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddToWaitingList FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}

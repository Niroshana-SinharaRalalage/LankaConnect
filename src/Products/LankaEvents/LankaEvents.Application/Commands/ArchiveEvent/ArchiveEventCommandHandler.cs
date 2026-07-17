using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ArchiveEvent;

public class ArchiveEventCommandHandler : ICommandHandler<ArchiveEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<ArchiveEventCommandHandler> _logger;

    public ArchiveEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<ArchiveEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ArchiveEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ArchiveEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("ArchiveEvent START: EventId={EventId}", request.EventId);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ArchiveEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "ArchiveEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Use domain method to archive
                var archiveResult = @event.Archive();
                if (archiveResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ArchiveEvent FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, archiveResult.Error, stopwatch.ElapsedMilliseconds);

                    return archiveResult;
                }

                _logger.LogInformation(
                    "ArchiveEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes (Wave 8.5.g: direct SaveChanges on LankaEventsDbContext)
                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ArchiveEvent COMPLETE: EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ArchiveEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}

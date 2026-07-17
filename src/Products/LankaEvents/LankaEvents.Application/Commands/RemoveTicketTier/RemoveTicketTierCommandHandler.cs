using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RemoveTicketTier;

public class RemoveTicketTierCommandHandler : ICommandHandler<RemoveTicketTierCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<RemoveTicketTierCommandHandler> _logger;

    public RemoveTicketTierCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<RemoveTicketTierCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveTicketTierCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveTicketTier"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("TierId", request.TierId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveTicketTier START: EventId={EventId}, TierId={TierId}",
                request.EventId, request.TierId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("RemoveTicketTier FAILED: Event not found - EventId={EventId}", request.EventId);
                    return Result.Failure("Event not found");
                }

                var result = @event.RemoveTicketTier(request.TierId);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "RemoveTicketTier FAILED: Domain validation - EventId={EventId}, TierId={TierId}, Error={Error}",
                        request.EventId, request.TierId, result.Error);
                    return result;
                }

                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g direct-SaveChanges

                stopwatch.Stop();
                _logger.LogInformation(
                    "RemoveTicketTier COMPLETE: EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "RemoveTicketTier FAILED: Exception - EventId={EventId}, TierId={TierId}, Duration={ElapsedMs}ms",
                    request.EventId, request.TierId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

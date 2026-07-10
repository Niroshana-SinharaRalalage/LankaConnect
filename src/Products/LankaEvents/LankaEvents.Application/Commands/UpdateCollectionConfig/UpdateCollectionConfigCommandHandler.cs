using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateCollectionConfig;

/// <summary>
/// Handles updating the collection configuration on an event.
/// Creates a CollectionConfiguration value object and sets it on the event aggregate.
/// </summary>
public class UpdateCollectionConfigCommandHandler : ICommandHandler<UpdateCollectionConfigCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCollectionConfigCommandHandler> _logger;

    public UpdateCollectionConfigCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCollectionConfigCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateCollectionConfigCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateCollectionConfig"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateCollectionConfig START: EventId={EventId}, IsEnabled={IsEnabled}",
                request.EventId, request.IsEnabled);

            try
            {
                // 1. Load event, validate exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result.Failure("Event not found");

                if (request.IsEnabled)
                {
                    // 2. Create CollectionConfiguration and set on event
                    var configResult = CollectionConfiguration.Create(
                        request.IsEnabled,
                        request.GoalAmount,
                        request.ShowProgress,
                        request.SuggestedAmounts,
                        request.AllowCustomAmount,
                        request.MinAmount,
                        request.MaxAmount,
                        request.CollectionMessage,
                        request.ShowContributorCount);

                    if (configResult.IsFailure)
                        return Result.Failure(configResult.Error);

                    var setResult = @event.SetCollectionConfiguration(configResult.Value);
                    if (setResult.IsFailure)
                        return Result.Failure(setResult.Error);
                }
                else
                {
                    // 3. Disable collections
                    var disableResult = @event.DisableCollections();
                    if (disableResult.IsFailure)
                        return Result.Failure(disableResult.Error);
                }

                // 4. Save + commit
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateCollectionConfig COMPLETE: EventId={EventId}, IsEnabled={IsEnabled}, Duration={ElapsedMs}ms",
                    request.EventId, request.IsEnabled, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateCollectionConfig FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Failed to update collection configuration: {ex.Message}");
            }
        }
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateAddOnConfig;

/// <summary>
/// Handles updating the add-on configuration on an event.
/// Creates an AddOnConfiguration value object and sets it on the event aggregate.
/// </summary>
public class UpdateAddOnConfigCommandHandler : ICommandHandler<UpdateAddOnConfigCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAddOnConfigCommandHandler> _logger;

    public UpdateAddOnConfigCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAddOnConfigCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAddOnConfigCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateAddOnConfig"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateAddOnConfig START: EventId={EventId}, IsEnabled={IsEnabled}, DuringRegistration={DuringRegistration}, Standalone={Standalone}",
                request.EventId, request.IsEnabled, request.AvailableDuringRegistration, request.AvailableStandalone);

            try
            {
                // 1. Load event, validate exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result.Failure("Event not found");

                if (request.IsEnabled)
                {
                    // 2. Create AddOnConfiguration and set on event
                    var configResult = AddOnConfiguration.Create(
                        request.IsEnabled,
                        request.AvailableDuringRegistration,
                        request.AvailableStandalone,
                        request.AddOnMessage);

                    if (configResult.IsFailure)
                        return Result.Failure(configResult.Error);

                    var setResult = @event.SetAddOnConfiguration(configResult.Value);
                    if (setResult.IsFailure)
                        return Result.Failure(setResult.Error);
                }
                else
                {
                    // 3. Disable add-ons
                    var disableResult = @event.DisableAddOns();
                    if (disableResult.IsFailure)
                        return Result.Failure(disableResult.Error);
                }

                // 4. Save + commit
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateAddOnConfig COMPLETE: EventId={EventId}, IsEnabled={IsEnabled}, Duration={ElapsedMs}ms",
                    request.EventId, request.IsEnabled, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateAddOnConfig FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Failed to update add-on configuration: {ex.Message}");
            }
        }
    }
}

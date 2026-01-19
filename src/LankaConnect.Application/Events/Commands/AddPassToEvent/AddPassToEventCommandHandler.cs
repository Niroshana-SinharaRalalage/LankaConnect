using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AddPassToEvent;

/// <summary>
/// Handler for adding event passes
/// Supports multi-tier ticket pricing for paid events
/// </summary>
public class AddPassToEventCommandHandler : ICommandHandler<AddPassToEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddPassToEventCommandHandler> _logger;

    public AddPassToEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddPassToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AddPassToEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddPassToEvent"))
        using (LogContext.PushProperty("EntityType", "EventPass"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddPassToEvent START: EventId={EventId}, PassName={PassName}, PriceAmount={PriceAmount}, PriceCurrency={PriceCurrency}, TotalQuantity={TotalQuantity}",
                request.EventId, request.PassName, request.PriceAmount, request.PriceCurrency, request.TotalQuantity);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "AddPassToEvent: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Create PassName value object
                var passNameResult = PassName.Create(request.PassName);
                if (passNameResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: PassName validation failed - EventId={EventId}, PassName={PassName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PassName, passNameResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(passNameResult.Error);
                }

                _logger.LogInformation(
                    "AddPassToEvent: PassName created - PassName={PassName}",
                    passNameResult.Value.Value);

                // Create PassDescription value object
                var passDescriptionResult = PassDescription.Create(request.PassDescription);
                if (passDescriptionResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: PassDescription validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, passDescriptionResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(passDescriptionResult.Error);
                }

                _logger.LogInformation(
                    "AddPassToEvent: PassDescription created - DescriptionLength={DescriptionLength}",
                    passDescriptionResult.Value.Value?.Length ?? 0);

                // Create Money value object for price
                var priceResult = Money.Create(request.PriceAmount, request.PriceCurrency);
                if (priceResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: Price validation failed - EventId={EventId}, PriceAmount={PriceAmount}, PriceCurrency={PriceCurrency}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PriceAmount, request.PriceCurrency, priceResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(priceResult.Error);
                }

                _logger.LogInformation(
                    "AddPassToEvent: Price created - Amount={Amount}, Currency={Currency}",
                    priceResult.Value.Amount, priceResult.Value.Currency);

                // Create EventPass entity
                var eventPassResult = EventPass.Create(
                    passNameResult.Value,
                    passDescriptionResult.Value,
                    priceResult.Value,
                    request.TotalQuantity);

                if (eventPassResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: EventPass creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, eventPassResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(eventPassResult.Error);
                }

                _logger.LogInformation(
                    "AddPassToEvent: EventPass entity created - PassId={PassId}",
                    eventPassResult.Value.Id);

                // Add pass to event
                var addResult = @event.AddPass(eventPassResult.Value);
                if (addResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddPassToEvent FAILED: Domain AddPass failed - EventId={EventId}, PassId={PassId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, eventPassResult.Value.Id, addResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(addResult.Error);
                }

                _logger.LogInformation(
                    "AddPassToEvent: Pass added to event - EventId={EventId}, PassId={PassId}, TotalPasses={TotalPasses}",
                    @event.Id, eventPassResult.Value.Id, @event.Passes.Count);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddPassToEvent COMPLETE: EventId={EventId}, PassId={PassId}, PassName={PassName}, Price={Price}, Duration={ElapsedMs}ms",
                    request.EventId, eventPassResult.Value.Id, passNameResult.Value.Value, priceResult.Value.Amount, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddPassToEvent FAILED: Exception occurred - EventId={EventId}, PassName={PassName}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PassName, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}

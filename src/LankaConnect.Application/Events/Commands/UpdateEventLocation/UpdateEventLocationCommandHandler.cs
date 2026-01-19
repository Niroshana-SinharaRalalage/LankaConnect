using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateEventLocation;

public class UpdateEventLocationCommandHandler : ICommandHandler<UpdateEventLocationCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEventLocationCommandHandler> _logger;

    public UpdateEventLocationCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEventLocationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateEventLocationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEventLocation"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEventLocation START: EventId={EventId}, City={City}, State={State}",
                request.EventId, request.LocationCity, request.LocationState);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventLocation FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "UpdateEventLocation: Event loaded - EventId={EventId}, Title={Title}, CurrentLocation={CurrentLocation}",
                    @event.Id, @event.Title.Value, @event.Location != null ? $"{@event.Location.Address.City}, {@event.Location.Address.State}" : "None");

                // Validate location data provided
                if (string.IsNullOrWhiteSpace(request.LocationAddress) ||
                    string.IsNullOrWhiteSpace(request.LocationCity))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventLocation FAILED: Validation failed - Address and City are required, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Location address and city are required");
                }

                // Create Address value object
                var addressResult = Address.Create(
                    request.LocationAddress,
                    request.LocationCity,
                    request.LocationState ?? string.Empty,
                    request.LocationZipCode ?? string.Empty,
                    request.LocationCountry ?? "Sri Lanka"
                );

                if (addressResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventLocation FAILED: Address creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, addressResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(addressResult.Error);
                }

                // Create GeoCoordinate if provided
                GeoCoordinate? coordinates = null;
                if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue)
                {
                    var coordinatesResult = GeoCoordinate.Create(
                        request.LocationLatitude.Value,
                        request.LocationLongitude.Value
                    );

                    if (coordinatesResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateEventLocation FAILED: GeoCoordinate creation failed - EventId={EventId}, Lat={Lat}, Lng={Lng}, Error={Error}, Duration={ElapsedMs}ms",
                            request.EventId, request.LocationLatitude, request.LocationLongitude, coordinatesResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result.Failure(coordinatesResult.Error);
                    }

                    coordinates = coordinatesResult.Value;
                }

                // Create EventLocation
                var locationResult = EventLocation.Create(addressResult.Value, coordinates);
                if (locationResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventLocation FAILED: EventLocation creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, locationResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(locationResult.Error);
                }

                _logger.LogInformation(
                    "UpdateEventLocation: Location value objects created - Address={Address}, HasCoordinates={HasCoordinates}",
                    $"{request.LocationCity}, {request.LocationState}", coordinates != null);

                // Use domain method to set location
                var setLocationResult = @event.SetLocation(locationResult.Value);
                if (setLocationResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventLocation FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, setLocationResult.Error, stopwatch.ElapsedMilliseconds);

                    return setLocationResult;
                }

                _logger.LogInformation(
                    "UpdateEventLocation: Domain method succeeded - EventId={EventId}, NewLocation={NewLocation}",
                    @event.Id, $"{@event.Location!.Address.City}, {@event.Location.Address.State}");

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateEventLocation COMPLETE: EventId={EventId}, Location={Location}, Duration={ElapsedMs}ms",
                    request.EventId, $"{request.LocationCity}, {request.LocationState}", stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateEventLocation FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}

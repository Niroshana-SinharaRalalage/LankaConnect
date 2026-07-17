using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventLocation;

public class UpdateEventLocationCommandHandler : ICommandHandler<UpdateEventLocationCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<UpdateEventLocationCommandHandler> _logger;

    public UpdateEventLocationCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<UpdateEventLocationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
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

                // Save changes (Wave 8.5.g: direct SaveChanges on LankaEventsDbContext)
                await _dbContext.SaveChangesAsync(cancellationToken);

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

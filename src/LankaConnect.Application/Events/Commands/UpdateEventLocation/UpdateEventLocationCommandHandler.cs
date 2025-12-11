using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Events.Commands.UpdateEventLocation;

public class UpdateEventLocationCommandHandler : ICommandHandler<UpdateEventLocationCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventLocationCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateEventLocationCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Validate location data provided
        if (string.IsNullOrWhiteSpace(request.LocationAddress) ||
            string.IsNullOrWhiteSpace(request.LocationCity))
        {
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
            return Result.Failure(addressResult.Error);

        // Create GeoCoordinate if provided
        GeoCoordinate? coordinates = null;
        if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue)
        {
            var coordinatesResult = GeoCoordinate.Create(
                request.LocationLatitude.Value,
                request.LocationLongitude.Value
            );

            if (coordinatesResult.IsFailure)
                return Result.Failure(coordinatesResult.Error);

            coordinates = coordinatesResult.Value;
        }

        // Create EventLocation
        var locationResult = EventLocation.Create(addressResult.Value, coordinates);
        if (locationResult.IsFailure)
            return Result.Failure(locationResult.Error);

        // Use domain method to set location
        var setLocationResult = @event.SetLocation(locationResult.Value);
        if (setLocationResult.IsFailure)
            return setLocationResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

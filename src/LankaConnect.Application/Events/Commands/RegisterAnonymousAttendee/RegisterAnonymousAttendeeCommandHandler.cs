using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

public class RegisterAnonymousAttendeeCommandHandler : ICommandHandler<RegisterAnonymousAttendeeCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAnonymousAttendeeCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegisterAnonymousAttendeeCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Create AttendeeInfo value object
        var attendeeInfoResult = AttendeeInfo.Create(
            request.Name,
            request.Age,
            request.Address,
            request.Email,
            request.PhoneNumber
        );

        if (attendeeInfoResult.IsFailure)
            return Result.Failure(attendeeInfoResult.Error);

        // Use domain method to register anonymous attendee
        var registerResult = @event.RegisterAnonymous(attendeeInfoResult.Value, request.Quantity);
        if (registerResult.IsFailure)
            return registerResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;

/// <summary>
/// Phase 6A.14: Handler for updating registration details
/// Orchestrates the update through the Event aggregate root
/// </summary>
public class UpdateRegistrationDetailsCommandHandler : ICommandHandler<UpdateRegistrationDetailsCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateRegistrationDetailsCommandHandler> _logger;

    public UpdateRegistrationDetailsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateRegistrationDetailsCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateRegistrationDetailsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateRegistrationDetails"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateRegistrationDetails START: EventId={EventId}, UserId={UserId}, AttendeeCount={AttendeeCount}",
                request.EventId, request.UserId, request.Attendees.Count);

            try
            {
                // Get the event with registrations
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRegistrationDetails FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "UpdateRegistrationDetails: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Convert DTOs to domain value objects
                // Phase 6A.43: Updated to use AgeCategory instead of Age
                var attendeeResults = request.Attendees
                    .Select(a => AttendeeDetails.Create(a.Name, a.AgeCategory, a.Gender))
                    .ToList();

                // Check if any attendee creation failed
                var failedAttendee = attendeeResults.FirstOrDefault(r => r.IsFailure);
                if (failedAttendee != null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRegistrationDetails FAILED: Attendee value object creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, failedAttendee.Errors, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(failedAttendee.Errors);
                }

                var attendees = attendeeResults.Select(r => r.Value).ToList();

                // Create contact value object
                var contactResult = RegistrationContact.Create(
                    request.Email,
                    request.PhoneNumber,
                    request.Address);

                if (contactResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRegistrationDetails FAILED: Contact value object creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, contactResult.Errors, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(contactResult.Errors);
                }

                _logger.LogInformation(
                    "UpdateRegistrationDetails: Value objects created - AttendeeCount={AttendeeCount}, ContactEmail={Email}",
                    attendees.Count, request.Email);

                // Update registration through the aggregate root
                var updateResult = @event.UpdateRegistrationDetails(
                    request.UserId,
                    attendees,
                    contactResult.Value);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateRegistrationDetails FAILED: Domain validation failed - EventId={EventId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateRegistrationDetails: Domain method succeeded - EventId={EventId}, UserId={UserId}",
                    @event.Id, request.UserId);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateRegistrationDetails COMPLETE: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateRegistrationDetails FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}

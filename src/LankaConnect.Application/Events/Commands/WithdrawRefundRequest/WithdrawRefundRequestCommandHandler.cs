using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.WithdrawRefundRequest;

/// <summary>
/// Phase 6A.91: Handler for withdrawing a pending refund request.
/// Transitions registration from RefundRequested back to Confirmed.
/// </summary>
public class WithdrawRefundRequestCommandHandler : ICommandHandler<WithdrawRefundRequestCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WithdrawRefundRequestCommandHandler> _logger;

    public WithdrawRefundRequestCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        ILogger<WithdrawRefundRequestCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(WithdrawRefundRequestCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "WithdrawRefundRequest"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.91] WithdrawRefundRequest START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Verify event exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                // Phase 6A.91: Check if event has started - cannot withdraw after event starts
                if (@event.StartDate <= DateTime.UtcNow)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Cannot withdraw after event has started - EventId={EventId}, StartDate={StartDate}, Duration={ElapsedMs}ms",
                        request.EventId, @event.StartDate, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Cannot withdraw refund request after the event has started");
                }

                // Find the registration
                var registration = await _registrationRepository.GetByEventAndUserAsync(
                    request.EventId, request.UserId, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Registration not found - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Registration not found");
                }

                // Verify registration is in RefundRequested status
                if (registration.Status != RegistrationStatus.RefundRequested)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Invalid status - RegId={RegId}, Status={Status}, ExpectedStatus=RefundRequested, Duration={ElapsedMs}ms",
                        registration.Id, registration.Status, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Cannot withdraw refund request. Registration status is {registration.Status}, expected RefundRequested.");
                }

                // Get tracked entity for update
                var trackedRegistration = await _registrationRepository.GetByIdAsync(registration.Id, cancellationToken);
                if (trackedRegistration == null)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Could not load tracked registration - RegId={RegId}, Duration={ElapsedMs}ms",
                        registration.Id, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Failed to process refund withdrawal");
                }

                // Withdraw the refund request (transitions to Confirmed)
                var withdrawResult = trackedRegistration.WithdrawRefundRequest();
                if (withdrawResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "[Phase 6A.91] WithdrawRefundRequest FAILED: Domain operation failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                        trackedRegistration.Id, withdrawResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(withdrawResult.Error);
                }

                // Update the registration
                _registrationRepository.Update(trackedRegistration);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[Phase 6A.91] WithdrawRefundRequest COMPLETE: Registration restored to Confirmed - RegId={RegId}, EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    trackedRegistration.Id, request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[Phase 6A.91] WithdrawRefundRequest FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.Commands.ForceCancelStuckRefund;

/// <summary>
/// Handler for <see cref="ForceCancelStuckRefundCommand"/>. Loads the registration with
/// change-tracking enabled, defers the status guard to the domain (<c>Registration.ForceCancelStuckRefund</c>),
/// commits, and emits a structured audit log line so the action is traceable.
///
/// Authorization is performed at the controller layer (organiser-only) — the application layer
/// is intentionally not aware of the caller's identity here, mirroring the existing
/// <c>CancelEventCommandHandler</c> pattern.
/// </summary>
public class ForceCancelStuckRefundCommandHandler : ICommandHandler<ForceCancelStuckRefundCommand>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ForceCancelStuckRefundCommandHandler> _logger;

    public ForceCancelStuckRefundCommandHandler(
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ForceCancelStuckRefundCommandHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ForceCancelStuckRefundCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ForceCancelStuckRefund"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ForceCancelStuckRefund START: EventId={EventId}, RegistrationId={RegistrationId}",
                request.EventId, request.RegistrationId);

            try
            {
                // Load the registration directly with tracking. We don't need the full Event aggregate
                // because the status-transition invariant lives on Registration; the EventId on the
                // command is mostly for logging + a sanity check below.
                var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
                if (registration == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ForceCancelStuckRefund FAILED: Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Registration not found");
                }

                // Sanity: the registration must belong to the event the caller specified. Prevents
                // any cross-event mistakes from a malformed client call.
                if (registration.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ForceCancelStuckRefund FAILED: Registration belongs to a different event - RegistrationId={RegistrationId}, ActualEventId={ActualEventId}, RequestedEventId={RequestedEventId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, registration.EventId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Registration does not belong to the specified event");
                }

                // Audit log of the pre-transition state for traceability.
                _logger.LogInformation(
                    "ForceCancelStuckRefund: Pre-transition state - RegistrationId={RegistrationId}, EventId={EventId}, " +
                    "Status={Status}, ContactEmail={ContactEmail}, RefundRequestedAt={RefundRequestedAt}",
                    registration.Id, registration.EventId,
                    registration.Status,
                    registration.Contact?.Email ?? "(null)",
                    registration.RefundRequestedAt?.ToString("o") ?? "(null)");

                // Domain-level guard: only RefundRequested → Cancelled is permitted.
                var transitionResult = registration.ForceCancelStuckRefund();
                if (transitionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ForceCancelStuckRefund FAILED: Domain guard rejected transition - RegistrationId={RegistrationId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        registration.Id, string.Join("; ", transitionResult.Errors), stopwatch.ElapsedMilliseconds);
                    return Result.Failure(transitionResult.Errors);
                }

                _registrationRepository.Update(registration);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "ForceCancelStuckRefund COMPLETE: RefundRequested → Cancelled - RegistrationId={RegistrationId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    registration.Id, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ForceCancelStuckRefund FAILED: Exception occurred - EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}

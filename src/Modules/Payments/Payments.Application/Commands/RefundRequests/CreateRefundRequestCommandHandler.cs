using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee creates a refund request (Pending). All domain invariants
/// (single-active-request, scan guard, line-item uniqueness) live in
/// <see cref="Registration.CreateRefundRequest"/>; this handler is orchestration only.
/// </summary>
public class CreateRefundRequestCommandHandler
    : ICommandHandler<CreateRefundRequestCommand, CreateRefundRequestResult>
{
    private readonly IRegistrationRepository _registrationRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateRefundRequestCommandHandler> _logger;

    public CreateRefundRequestCommandHandler(
        IRegistrationRepository registrationRepo,
        ITicketRepository ticketRepo,
        IUnitOfWork uow,
        ILogger<CreateRefundRequestCommandHandler> logger)
    {
        _registrationRepo = registrationRepo;
        _ticketRepo = ticketRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CreateRefundRequestResult>> Handle(
        CreateRefundRequestCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateRefundRequest"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148] CreateRefundRequest START: EventId={EventId} CallerUserId={UserId} Lines={LineCount}",
                request.EventId, request.CallerUserId, request.LineItems?.Count ?? 0);

            try
            {
                // Load registration (tracked — we'll mutate)
                var registration = await _registrationRepo.GetByEventAndUserAsync(
                    request.EventId, request.CallerUserId, cancellationToken);
                if (registration is null)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] CreateRefundRequest NotFound: no registration for EventId={EventId} UserId={UserId} Duration={ElapsedMs}ms",
                        request.EventId, request.CallerUserId, sw.ElapsedMilliseconds);
                    return Result<CreateRefundRequestResult>.NotFound("Registration not found for this event");
                }

                // Re-load tracked for mutation
                var tracked = await _registrationRepo.GetByIdAsync(registration.Id, cancellationToken);
                if (tracked is null)
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148] CreateRefundRequest FAILED: tracked load returned null - RegId={RegId}",
                        registration.Id);
                    return Result<CreateRefundRequestResult>.Failure("Failed to load registration for update");
                }

                // Scan-guard check (architect's design — application layer computes the boolean
                // and passes it to the domain method).
                var anyTicketsScanned = await _ticketRepo.AnyValidatedTicketForRegistrationAsync(
                    tracked.Id, cancellationToken);

                var lineItems = (request.LineItems ?? Array.Empty<RefundLineItemInputDto>())
                    .Select(li => new RefundRequestLineItemInput(
                        li.Type,
                        li.ReferenceId,
                        new Money(li.RequestedAmount, li.Currency)))
                    .ToList();

                var createResult = tracked.CreateRefundRequest(
                    requestedByUserId: request.CallerUserId,
                    isOrganizerInitiated: false,
                    requesterReason: request.RequesterReason,
                    organizerNotes: null,
                    overrideScanGuard: false,
                    anyTicketsScanned: anyTicketsScanned,
                    lineItems: lineItems);

                if (createResult.IsFailure)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] CreateRefundRequest Validation FAILED: RegId={RegId} Error={Error} Duration={ElapsedMs}ms",
                        tracked.Id, createResult.Error, sw.ElapsedMilliseconds);
                    return Result<CreateRefundRequestResult>.Failure(createResult.Error);
                }

                _registrationRepo.Update(tracked);
                await _uow.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148] CreateRefundRequest COMPLETE: RegId={RegId} RefundRequestId={RrId} Status={Status} Duration={ElapsedMs}ms",
                    tracked.Id, createResult.Value.Id, createResult.Value.Status, sw.ElapsedMilliseconds);

                return Result<CreateRefundRequestResult>.Success(
                    new CreateRefundRequestResult(createResult.Value.Id));
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148] CreateRefundRequest EXCEPTION: EventId={EventId} UserId={UserId} Duration={ElapsedMs}ms",
                    request.EventId, request.CallerUserId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

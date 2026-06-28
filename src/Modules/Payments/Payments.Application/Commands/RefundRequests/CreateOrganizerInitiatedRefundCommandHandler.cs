using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer creates a refund on behalf of an attendee. Skips Pending —
/// request is created directly in Approved. Stripe dispatch is triggered via
/// <see cref="IRefundExecutionService"/> after the creation transaction commits.
///
/// Auth: caller must pass <c>Event.IsOrganizer</c>. Scan-guard can be overridden with
/// <c>OverrideScanGuard=true</c> + non-empty <c>OrganizerNotes</c> (architect F7).
/// </summary>
public class CreateOrganizerInitiatedRefundCommandHandler
    : ICommandHandler<CreateOrganizerInitiatedRefundCommand, CreateRefundRequestResult>
{
    private readonly IEventRepository _eventRepo;
    private readonly IRegistrationRepository _registrationRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRefundExecutionService _executionService;
    private readonly ILogger<CreateOrganizerInitiatedRefundCommandHandler> _logger;

    public CreateOrganizerInitiatedRefundCommandHandler(
        IEventRepository eventRepo,
        IRegistrationRepository registrationRepo,
        ITicketRepository ticketRepo,
        IUnitOfWork uow,
        IRefundExecutionService executionService,
        ILogger<CreateOrganizerInitiatedRefundCommandHandler> logger)
    {
        _eventRepo = eventRepo;
        _registrationRepo = registrationRepo;
        _ticketRepo = ticketRepo;
        _uow = uow;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<Result<CreateRefundRequestResult>> Handle(
        CreateOrganizerInitiatedRefundCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateOrganizerInitiatedRefund"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148] CreateOrganizerInitiatedRefund START: EventId={EventId} RegId={RegId} CallerUserId={UserId} Override={Override}",
                request.EventId, request.RegistrationId, request.CallerUserId, request.OverrideScanGuard);

            try
            {
                var @event = await _eventRepo.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null) return Result<CreateRefundRequestResult>.NotFound("Event not found");
                if (!@event.IsOrganizer(request.CallerUserId))
                    return Result<CreateRefundRequestResult>.Forbidden(
                        "Only organizers of this event can initiate refunds on behalf of attendees");

                var tracked = await _registrationRepo.GetByIdAsync(request.RegistrationId, cancellationToken);
                if (tracked is null)
                    return Result<CreateRefundRequestResult>.NotFound("Registration not found");

                if (tracked.EventId != request.EventId)
                    return Result<CreateRefundRequestResult>.NotFound(
                        "Registration does not belong to this event");

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
                    isOrganizerInitiated: true,
                    requesterReason: null,
                    organizerNotes: request.OrganizerNotes,
                    overrideScanGuard: request.OverrideScanGuard,
                    anyTicketsScanned: anyTicketsScanned,
                    lineItems: lineItems);

                if (createResult.IsFailure)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] CreateOrganizerInitiatedRefund Validation FAILED: RegId={RegId} Error={Error}",
                        tracked.Id, createResult.Error);
                    return Result<CreateRefundRequestResult>.Failure(createResult.Error);
                }

                _registrationRepo.Update(tracked);
                await _uow.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148] CreateOrganizerInitiatedRefund COMMITTED: RegId={RegId} RrId={RrId} Duration={ElapsedMs}ms; dispatching Stripe",
                    tracked.Id, createResult.Value.Id, sw.ElapsedMilliseconds);

                // Architect F10: dispatch AFTER commit. Failures recover via reconciler.
                try
                {
                    var dispatchResult = await _executionService.DispatchAsync(
                        createResult.Value.Id, cancellationToken);
                    if (dispatchResult.IsFailure)
                        _logger.LogWarning(
                            "[6A.148] Post-commit Stripe dispatch failed for RrId={RrId} Error={Error}; reconciler will retry",
                            createResult.Value.Id, dispatchResult.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[6A.148] Post-commit Stripe dispatch EXCEPTION for RrId={RrId}",
                        createResult.Value.Id);
                }

                return Result<CreateRefundRequestResult>.Success(
                    new CreateRefundRequestResult(createResult.Value.Id));
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148] CreateOrganizerInitiatedRefund EXCEPTION: EventId={EventId} Duration={ElapsedMs}ms",
                    request.EventId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

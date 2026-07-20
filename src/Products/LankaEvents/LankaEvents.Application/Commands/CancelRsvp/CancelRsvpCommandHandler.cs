using System.Diagnostics;
using LankaConnect.SharedKernel.Money;
using System.Linq;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;
using LankaConnect.Products.LankaEvents.Contracts.Shims; // Wave 6.5.g Day 5: refund interfaces promoted
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
namespace LankaConnect.Products.LankaEvents.Application.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand, CancelRsvpResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IRegistrationRefundService _refundService;
    private readonly IFormCommands _formCommands;
    private readonly IAddOnRefundService _addOnRefundService;
    // Phase 6A.137F: Collection and sponsor refund support
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<CancelRsvpCommandHandler> _logger;
    // Phase 6A.148: Refund approval workflow integration
    private readonly LankaConnect.Products.LankaEvents.Domain.Repositories.ITicketRepository _ticketRepository;
    private readonly LankaConnect.Products.LankaEvents.Domain.Repositories.IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IRegistrationPaymentRepository _registrationPaymentRepository;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IRegistrationRefundService refundService,
        IFormCommands formCommands,
        IAddOnRefundService addOnRefundService,
        // Phase 6A.137F: Inject collection/sponsor refund dependencies
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        IStripePaymentService stripePaymentService,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<CancelRsvpCommandHandler> logger,
        // Phase 6A.148: Refund approval workflow integration
        LankaConnect.Products.LankaEvents.Domain.Repositories.ITicketRepository ticketRepository,
        LankaConnect.Products.LankaEvents.Domain.Repositories.IAddOnPurchaseRepository addOnPurchaseRepository,
        IRegistrationPaymentRepository registrationPaymentRepository,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _refundService = refundService;
        _formCommands = formCommands;
        _addOnRefundService = addOnRefundService;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
        _stripePaymentService = stripePaymentService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
        _ticketRepository = ticketRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _registrationPaymentRepository = registrationPaymentRepository;
        _configuration = configuration;
    }

    public async Task<Result<CancelRsvpResult>> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CancelRsvp"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CancelRsvp START: EventId={EventId}, UserId={UserId}, DeleteCommitments={DeleteCommitments}, DeleteFormResponses={DeleteFormResponses}, RefundAddOnPurchases={RefundAddOnPurchases}",
                request.EventId, request.UserId, request.DeleteSignUpCommitments, request.DeleteFormResponses, request.RefundAddOnPurchases);

            try
            {
                // Verify event exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelRsvp FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "CancelRsvp: Event loaded - EventId={EventId}, Title={Title}, Status={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Phase 6A.91: Check if event has started - cannot cancel after event starts
                if (@event.StartDate <= DateTime.UtcNow)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] CancelRsvp FAILED: Cannot cancel after event has started - EventId={EventId}, StartDate={StartDate}, Now={Now}, Duration={ElapsedMs}ms",
                        request.EventId, @event.StartDate, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Cannot cancel registration after the event has started");
                }

                // Find active registration using GetByEventAndUserAsync (read-only query)
                var registrationReadOnly = await _registrationRepository.GetByEventAndUserAsync(request.EventId, request.UserId, cancellationToken);

                _logger.LogInformation(
                    "CancelRsvp: Registration query result - Found={Found}, Status={Status}",
                    registrationReadOnly != null, registrationReadOnly?.Status.ToString() ?? "N/A");

                if (registrationReadOnly == null)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CancelRsvp COMPLETE: No registration found (idempotent) - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    // Phase 6A.45: Since we hard delete, no registration found means operation already succeeded (idempotent)
                    return Result<CancelRsvpResult>.Success(new CancelRsvpResult(
                        RegistrationCancelled: true,
                        CommitmentsDeleted: null,
                        FormResponsesDeleted: null,
                        FormResponsesDeletedCount: null,
                        AddOnRefundsProcessed: null,
                        AddOnRefundedCount: null,
                        AddOnFailedCount: null,
                        AddOnRefundTotal: null,
                        Warnings: null));
                }

                // Phase 6A.45 FIX: Hard delete registration instead of soft delete (marking as cancelled)
                // This prevents duplicate/cancelled registrations from cluttering the database
                // Get the registration WITH tracking so EF Core can delete it
                var registration = await _registrationRepository.GetByIdAsync(registrationReadOnly.Id, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "CancelRsvp FAILED: Could not retrieve registration with tracking - RegId={RegId}, Duration={ElapsedMs}ms",
                        registrationReadOnly.Id, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Failed to cancel registration");
                }

                // =====================================================================
                // Phase 6A.148 â€” paid-cancel + refund-request compound flow (NEW path)
                //
                // When BOTH conditions hold:
                //   (a) the approval workflow feature flag is ON
                //   (b) this is a paid confirmed registration
                //       (registration.Status == Confirmed && PaymentStatus.Completed)
                //
                // We follow the architect-approved decoupled lifecycle:
                //   1. Validate scan guard (block if any ticket already validated).
                //   2. Build line items from the four bucket selections (Ticket / AddOn
                //      / Collection / Sponsor) based on what the attendee actually paid.
                //   3. Atomically: create a Pending RefundRequest + transition the
                //      registration to Cancelled (releases the seat NOW) + raise
                //      RegistrationCancelledEvent (existing email path).
                //   4. NO Stripe call. NO RegistrationRefundService. NO AddOnRefundService.
                //      The organizer reviews the Pending RefundRequest later and
                //      RefundExecutionService dispatches Stripe at that time.
                //
                // For all other cases (flag OFF, free events, Preliminary, etc.), the
                // legacy bucket-by-bucket flow below runs unchanged.
                // =====================================================================
                var approvalFlagOn = _configuration.GetValue<bool>("Refund:ApprovalWorkflow:Enabled");
                var isPaidConfirmed = registration.Status == RegistrationStatus.Confirmed &&
                                      registration.PaymentStatus == PaymentStatus.Completed;
                if (approvalFlagOn && isPaidConfirmed)
                {
                    return await HandlePaidCancelViaApprovalWorkflowAsync(
                        request, @event, registration, stopwatch, cancellationToken);
                }

                using (LogContext.PushProperty("RegistrationId", registration.Id))
                {
                    // Track warnings for partial failures
                    var warnings = new List<string>();

                    // Phase 6A.28: Handle sign-up commitments based on user choice
                    bool? commitmentsDeleted = null;
                    if (request.DeleteSignUpCommitments)
                    {
                        _logger.LogInformation(
                            "CancelRsvp: Deleting commitments via domain model - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);

                        var cancelResult = @event.CancelAllUserCommitments(request.UserId);

                        if (cancelResult.IsFailure)
                        {
                            commitmentsDeleted = false;
                            warnings.Add($"Failed to delete sign-up commitments: {cancelResult.Error}");
                            _logger.LogWarning(
                                "CancelRsvp: Failed to delete commitments - EventId={EventId}, UserId={UserId}, Error={Error}",
                                request.EventId, request.UserId, cancelResult.Error);
                        }
                        else
                        {
                            commitmentsDeleted = true;
                            _logger.LogInformation(
                                "CancelRsvp: Commitments cancelled successfully - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);
                        }

                        // CRITICAL FIX ADR-007: Explicitly mark event as modified for EF Core change tracking
                        _eventRepository.Update(@event);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "CancelRsvp: User chose to keep sign-up commitments - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);
                    }

                    // Cancellation enhancement: Handle form response deletion (non-blocking).
                    // Wave 5.3d.2: routed through IFormCommands (Forms.Contracts) instead of the
                    // legacy IFormResponseRepository. FormCommands.DeleteResponsesByEventAndUserAsync
                    // raises FormResponseDeletedEvent per response before delete (mirrors the
                    // pre-5.3d in-line loop) so the cancellation email + WhatsApp pipeline still
                    // fires. Atomicity: the call self-saves on FormsDbContext (W4.3 / ADR-010)
                    // â€” a partial failure surfaces as the existing warning collection and the
                    // outer try/catch records it without blocking the cancel.
                    bool? formResponsesDeleted = null;
                    int? formResponsesDeletedCount = null;
                    if (request.DeleteFormResponses)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Deleting form responses - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);

                            var deletedCount = await _formCommands.DeleteResponsesByEventAndUserAsync(
                                request.EventId, request.UserId, cancellationToken);

                            formResponsesDeleted = true;
                            formResponsesDeletedCount = deletedCount;

                            _logger.LogInformation(
                                "CancelRsvp: Deleted {Count} form responses - EventId={EventId}, UserId={UserId}",
                                deletedCount, request.EventId, request.UserId);
                        }
                        catch (Exception ex)
                        {
                            formResponsesDeleted = false;
                            warnings.Add($"Failed to delete form responses: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to delete form responses (non-blocking) - EventId={EventId}, UserId={UserId}, Error={ErrorMessage}",
                                request.EventId, request.UserId, ex.Message);
                        }
                    }

                    // Cancellation enhancement: Handle add-on purchase refunds BEFORE registration refund
                    // so the add-on total can be included in the refund email.
                    // Phase 6A.137F-Fix3: Pass registrationId to scope refunds to current registration only,
                    // excluding orphaned purchases from previous cancelled registrations.
                    decimal addOnRefundTotal = 0m;
                    int addOnRefundedCount = 0;
                    int addOnFailedCount = 0;
                    bool? addOnRefundsProcessed = null;

                    if (request.RefundAddOnPurchases)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Refunding add-on purchases - EventId={EventId}, UserId={UserId}, RegistrationId={RegistrationId}",
                                request.EventId, request.UserId, registration.Id);

                            var addOnMetadata = new Dictionary<string, string>
                            {
                                ["event_id"] = request.EventId.ToString(),
                                ["user_id"] = request.UserId.ToString(),
                                ["refund_type"] = "user_cancellation_add_on_refund"
                            };

                            var addOnRefundResult = await _addOnRefundService.RefundUserPurchasesAsync(
                                request.UserId,
                                request.EventId,
                                "requested_by_customer",
                                addOnMetadata,
                                registration.Id,
                                isPreApproved: false, // Phase 6A.148: legacy path only runs when flag OFF
                                cancellationToken);

                            if (addOnRefundResult.IsFailure)
                            {
                                addOnRefundsProcessed = false;
                                warnings.Add($"Add-on refund failed: {addOnRefundResult.Error}");
                                _logger.LogWarning(
                                    "CancelRsvp: Add-on refund failed (non-blocking) - EventId={EventId}, UserId={UserId}, Error={Error}",
                                    request.EventId, request.UserId, addOnRefundResult.Error);
                            }
                            else
                            {
                                addOnRefundTotal = addOnRefundResult.Value.TotalAmountRefunded;
                                addOnRefundedCount = addOnRefundResult.Value.PurchasesRefunded;
                                addOnFailedCount = addOnRefundResult.Value.PurchasesFailed;
                                addOnRefundsProcessed = addOnFailedCount == 0;

                                if (addOnFailedCount > 0)
                                {
                                    warnings.Add($"{addOnFailedCount} add-on purchase(s) failed to refund. {addOnRefundedCount} succeeded (${addOnRefundTotal:F2} refunded).");
                                }

                                _logger.LogInformation(
                                    "CancelRsvp: Add-on refunds processed - EventId={EventId}, UserId={UserId}, Refunded={Refunded}, Failed={Failed}, TotalAmount=${TotalAmount}",
                                    request.EventId, request.UserId,
                                    addOnRefundedCount, addOnFailedCount, addOnRefundTotal);
                            }
                        }
                        catch (Exception ex)
                        {
                            addOnRefundsProcessed = false;
                            warnings.Add($"Add-on refund error: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to refund add-on purchases (non-blocking) - EventId={EventId}, UserId={UserId}, Error={ErrorMessage}",
                                request.EventId, request.UserId, ex.Message);
                        }
                    }

                    // Phase 6A.137F: Handle collection refund
                    bool? collectionRefundProcessed = null;
                    decimal? collectionRefundAmount = null;

                    if (request.RefundCollections)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Refunding collection contribution - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);

                            var collections = await _collectionRepository.GetByUserIdAndEventIdAsync(
                                request.UserId, request.EventId, cancellationToken);

                            var refundableCollection = collections?.FirstOrDefault(c =>
                                c.Status == CollectionStatus.Completed &&
                                !string.IsNullOrEmpty(c.StripePaymentIntentId));

                            if (refundableCollection != null)
                            {
                                var refundAmountInCents = (long)(refundableCollection.Amount.Amount * 100);
                                var refundRequest = new CreateRefundRequest
                                {
                                    PaymentIntentId = refundableCollection.StripePaymentIntentId!,
                                    RegistrationId = refundableCollection.Id,
                                    AmountInCents = refundAmountInCents,
                                    Reason = "requested_by_customer",
                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["collection_id"] = refundableCollection.Id.ToString(),
                                        ["refund_type"] = "user_cancellation_collection_refund"
                                    }
                                };

                                var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest);
                                if (stripeResult.IsSuccess)
                                {
                                    refundableCollection.MarkAsRefunded();
                                    _collectionRepository.Update(refundableCollection);
                                    collectionRefundProcessed = true;
                                    collectionRefundAmount = refundableCollection.Amount.Amount;

                                    _logger.LogInformation(
                                        "CancelRsvp: Collection refund succeeded - CollectionId={CollectionId}, Amount=${Amount}",
                                        refundableCollection.Id, collectionRefundAmount);
                                }
                                else
                                {
                                    collectionRefundProcessed = false;
                                    warnings.Add($"Collection refund failed: {stripeResult.Error}");
                                    _logger.LogWarning(
                                        "CancelRsvp: Collection refund failed - CollectionId={CollectionId}, Error={Error}",
                                        refundableCollection.Id, stripeResult.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            collectionRefundProcessed = false;
                            warnings.Add($"Collection refund error: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to refund collection (non-blocking) - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);
                        }
                    }

                    // Phase 6A.137F: Handle sponsor refund
                    bool? sponsorRefundProcessed = null;
                    decimal? sponsorRefundAmount = null;

                    if (request.RefundSponsors)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Refunding sponsorship - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);

                            var sponsors = await _sponsorRepository.GetByUserIdAndEventIdAsync(
                                request.UserId, request.EventId, cancellationToken);

                            // Only refund money sponsors that have been completed
                            var refundableSponsor = sponsors?.FirstOrDefault(s =>
                                s.Type == SponsorType.Money &&
                                s.Status == SponsorStatus.Completed &&
                                !string.IsNullOrEmpty(s.StripePaymentIntentId));

                            if (refundableSponsor != null && refundableSponsor.Amount != null)
                            {
                                var refundAmountInCents = (long)(refundableSponsor.Amount.Amount * 100);
                                var refundRequest = new CreateRefundRequest
                                {
                                    PaymentIntentId = refundableSponsor.StripePaymentIntentId!,
                                    RegistrationId = refundableSponsor.Id,
                                    AmountInCents = refundAmountInCents,
                                    Reason = "requested_by_customer",
                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["sponsor_id"] = refundableSponsor.Id.ToString(),
                                        ["refund_type"] = "user_cancellation_sponsor_refund"
                                    }
                                };

                                var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest);
                                if (stripeResult.IsSuccess)
                                {
                                    refundableSponsor.MarkAsRefunded();
                                    _sponsorRepository.Update(refundableSponsor);
                                    sponsorRefundProcessed = true;
                                    sponsorRefundAmount = refundableSponsor.Amount.Amount;

                                    _logger.LogInformation(
                                        "CancelRsvp: Sponsor refund succeeded - SponsorId={SponsorId}, Amount=${Amount}",
                                        refundableSponsor.Id, sponsorRefundAmount);
                                }
                                else
                                {
                                    sponsorRefundProcessed = false;
                                    warnings.Add($"Sponsor refund failed: {stripeResult.Error}");
                                    _logger.LogWarning(
                                        "CancelRsvp: Sponsor refund failed - SponsorId={SponsorId}, Error={Error}",
                                        refundableSponsor.Id, stripeResult.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sponsorRefundProcessed = false;
                            warnings.Add($"Sponsor refund error: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to refund sponsor (non-blocking) - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);
                        }
                    }

                    // Phase 6A.81 Part 3: Different handling for Preliminary vs Confirmed registrations
                    if (registration.Status == RegistrationStatus.Preliminary)
                    {
                        // Preliminary registrations: Mark as Abandoned (preserve audit trail)
                        _logger.LogInformation(
                            "[Phase 6A.81-Part3] Marking Preliminary registration as Abandoned - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        var abandonResult = registration.MarkAbandoned();
                        if (abandonResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.81-Part3] Failed to abandon Preliminary registration - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, abandonResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result<CancelRsvpResult>.Failure(abandonResult.Error);
                        }

                        _registrationRepository.Update(registration);

                        _logger.LogInformation(
                            "[Phase 6A.81-Part3] Preliminary registration marked as Abandoned successfully - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        // No cancellation email for Preliminary (they never got confirmation email)
                    }
                    else if (registration.Status == RegistrationStatus.Confirmed &&
                             registration.PaymentStatus == PaymentStatus.Completed)
                    {
                        // Phase 6A.92: Paid confirmed registration - use shared refund service
                        _logger.LogInformation(
                            "[Phase 6A.92] Initiating refund via shared service - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        var metadata = new Dictionary<string, string>
                        {
                            ["event_id"] = request.EventId.ToString(),
                            ["user_id"] = request.UserId.ToString(),
                            ["event_title"] = @event.Title.Value,
                            ["refund_type"] = "user_initiated_cancellation"
                        };

                        // Phase 6A.137F-Fix5: Combine ALL successful bundled item refund amounts
                        // so the refund email shows the correct total (ticket + add-ons + collection + sponsor).
                        // Only include amounts from SUCCESSFUL refunds to avoid inflated totals.
                        var totalAdditionalRefund = addOnRefundTotal
                            + (collectionRefundProcessed == true ? collectionRefundAmount ?? 0m : 0m)
                            + (sponsorRefundProcessed == true ? sponsorRefundAmount ?? 0m : 0m);

                        _logger.LogInformation(
                            "CancelRsvp: Combined additional refund total - AddOns=${AddOnTotal}, Collection=${CollectionAmount}, Sponsor=${SponsorAmount}, TotalAdditional=${TotalAdditional}",
                            addOnRefundTotal,
                            collectionRefundProcessed == true ? collectionRefundAmount ?? 0m : 0m,
                            sponsorRefundProcessed == true ? sponsorRefundAmount ?? 0m : 0m,
                            totalAdditionalRefund);

                        // Use shared refund service - handles Stripe call and RequestRefund() transition.
                        // Phase 6A.148: legacy path; only reached when feature flag is OFF (otherwise
                        // HandlePaidCancelViaApprovalWorkflowAsync took the early branch above).
                        var refundResult = await _refundService.ProcessRefundAsync(
                            registration,
                            "requested_by_customer",
                            metadata,
                            totalAdditionalRefund,
                            isPreApproved: false,
                            cancellationToken);

                        if (refundResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.92] Refund failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, refundResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result<CancelRsvpResult>.Failure($"Refund failed: {refundResult.Error}");
                        }

                        _registrationRepository.Update(registration);

                        _logger.LogInformation(
                            "[Phase 6A.92] Refund request successful - RegId={RegId}, StripeRefundId={RefundId}, Amount=${Amount}. Webhook will complete the refund.",
                            registration.Id, refundResult.Value.StripeRefundId, refundResult.Value.AmountRefunded);

                        // Phase 6A.93: Also raise RegistrationCancelledEvent for paid cancellations
                        @event.RaiseRegistrationCancelledEvent(request.UserId);
                        _eventRepository.Update(@event);

                        _logger.LogInformation(
                            "[Phase 6A.93] Raised RegistrationCancelledEvent for cancellation email - EventId={EventId}, UserId={UserId}. User will receive two emails: cancellation + refund.",
                            request.EventId, request.UserId);

                        // Issue #56.1 Diagnostic: Log Event entity's domain event count to verify event was raised
                        _logger.LogInformation(
                            "[Issue #56.1 DIAGNOSTIC] Event entity domain events after RaiseRegistrationCancelledEvent - EventId={EventId}, DomainEventCount={Count}, EventTypes=[{Types}]",
                            @event.Id, @event.DomainEvents.Count,
                            string.Join(", ", @event.DomainEvents.Select(e => e.GetType().Name)));
                    }
                    else
                    {
                        // Free confirmed/other registrations: Hard delete (existing behavior from Phase 6A.45)
                        _logger.LogInformation(
                            "CancelRsvp: Hard deleting registration - RegId={RegId}, EventId={EventId}, UserId={UserId}, Status={Status}, PaymentStatus={PaymentStatus}",
                            registration.Id, request.EventId, request.UserId, registration.Status, registration.PaymentStatus);

                        _registrationRepository.Remove(registration);

                        // Phase 6A.62 Fix: Raise domain event for email notification
                        @event.RaiseRegistrationCancelledEvent(request.UserId);
                        _eventRepository.Update(@event);

                        _logger.LogInformation(
                            "CancelRsvp: Raised RegistrationCancelledEvent for email notification - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);

                        // Issue #56.1 Diagnostic: Log Event entity's domain event count to verify event was raised
                        _logger.LogInformation(
                            "[Issue #56.1 DIAGNOSTIC] Event entity domain events after RaiseRegistrationCancelledEvent (free) - EventId={EventId}, DomainEventCount={Count}, EventTypes=[{Types}]",
                            @event.Id, @event.DomainEvents.Count,
                            string.Join(", ", @event.DomainEvents.Select(e => e.GetType().Name)));
                    }

                    // Save changes
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CancelRsvp COMPLETE: EventId={EventId}, UserId={UserId}, RegId={RegId}, DeletedCommitments={DeletedCommitments}, DeletedFormResponses={DeletedFormResponses}, RefundedAddOns={RefundedAddOns}, Warnings={WarningCount}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, registration.Id, request.DeleteSignUpCommitments, request.DeleteFormResponses, request.RefundAddOnPurchases, warnings.Count, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Success(new CancelRsvpResult(
                        RegistrationCancelled: true,
                        CommitmentsDeleted: commitmentsDeleted,
                        FormResponsesDeleted: formResponsesDeleted,
                        FormResponsesDeletedCount: formResponsesDeletedCount,
                        AddOnRefundsProcessed: addOnRefundsProcessed,
                        AddOnRefundedCount: addOnRefundsProcessed.HasValue ? addOnRefundedCount : null,
                        AddOnFailedCount: addOnRefundsProcessed.HasValue ? addOnFailedCount : null,
                        AddOnRefundTotal: addOnRefundsProcessed.HasValue ? addOnRefundTotal : null,
                        // Phase 6A.137F: Include collection/sponsor refund results
                        CollectionRefundProcessed: collectionRefundProcessed,
                        CollectionRefundAmount: collectionRefundAmount,
                        SponsorRefundProcessed: sponsorRefundProcessed,
                        SponsorRefundAmount: sponsorRefundAmount,
                        Warnings: warnings.Count > 0 ? warnings : null));
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CancelRsvp FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }

    // ====================================================================================
    // Phase 6A.148 â€” paid-cancel + refund-request compound flow.
    //
    // Cancels the registration immediately (releases the seat) AND atomically creates a
    // Pending RefundRequest with the four bucket selections as line items. NO Stripe call.
    // The organizer reviews via the new approval endpoints; RefundExecutionService
    // dispatches Stripe at that time.
    //
    // Only invoked when: flag is ON AND registration is Confirmed + PaymentCompleted.
    // ====================================================================================
    private async Task<Result<CancelRsvpResult>> HandlePaidCancelViaApprovalWorkflowAsync(
        CancelRsvpCommand request,
        Event @event,
        Registration registration,
        System.Diagnostics.Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("RegistrationId", registration.Id))
        using (LogContext.PushProperty("ApprovalWorkflow", "Phase6A148"))
        {
            _logger.LogInformation(
                "[6A.148] HandlePaidCancelViaApprovalWorkflow START: RegId={RegId} EventId={EventId} UserId={UserId} " +
                "RefundTicket={RT} RefundAddOns={RA} RefundCollections={RC} RefundSponsors={RS}",
                registration.Id, request.EventId, request.UserId,
                request.RefundTicket, request.RefundAddOnPurchases, request.RefundCollections, request.RefundSponsors);

            try
            {
                // Scan guard â€” block attendee-initiated refund if any ticket is already
                // scanned/validated (architect rule #2). Organizer-initiated override is
                // handled by a different endpoint, not this handler.
                var anyTicketsScanned = await _ticketRepository
                    .AnyValidatedTicketForRegistrationAsync(registration.Id, cancellationToken);
                if (anyTicketsScanned)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[6A.148] HandlePaidCancelViaApprovalWorkflow REJECTED: ticket scanned â€” RegId={RegId}",
                        registration.Id);
                    return Result<CancelRsvpResult>.Failure(
                        "Cannot cancel and refund: one or more tickets have been scanned and used. " +
                        "Contact the event organizer if you believe this is in error.");
                }

                // Build line items from bucket selections.
                var lineItems = new List<LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput>();

                if (request.RefundTicket)
                {
                    var initialPayment = await _registrationPaymentRepository
                        .GetInitialPaymentAsync(registration.Id, cancellationToken);
                    if (initialPayment is not null && initialPayment.Amount is not null && initialPayment.Amount.Amount > 0)
                    {
                        lineItems.Add(new LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput(
                            LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Ticket,
                            initialPayment.Id,
                            initialPayment.Amount));
                    }
                    else if (!string.IsNullOrWhiteSpace(registration.StripePaymentIntentId)
                             && registration.TotalPrice is not null
                             && registration.TotalPrice.Amount > 0)
                    {
                        // Phase 6A.148.c (D5 fix): legacy-registration fallback. Some pre-
                        // Add-Only-Attendees registrations have NO row in registration_payments
                        // â€” their Stripe charge lives on Registration.StripePaymentIntentId
                        // directly. Without this fallback, GetInitialPaymentAsync returns null
                        // and the Ticket bucket silently drops from the refund request even
                        // though the attendee checked it. Mirrors the dispatch-time fallback
                        // already present in RefundExecutionService.ResolvePaymentIntentAsync.
                        // ReferenceId = registration.Id is the convention the downstream
                        // resolver understands.
                        _logger.LogInformation(
                            "[6A.148.c] Ticket line via legacy fallback (no RegistrationPayment row): " +
                            "RegId={RegId} StripePaymentIntentId={Pii} Amount=${Amount}",
                            registration.Id, registration.StripePaymentIntentId, registration.TotalPrice.Amount);
                        lineItems.Add(new LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput(
                            LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Ticket,
                            registration.Id,
                            registration.TotalPrice));
                    }
                }

                if (request.RefundAddOnPurchases)
                {
                    var purchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(
                        request.UserId, request.EventId, cancellationToken);
                    foreach (var p in purchases.Where(p =>
                        p.Status == LankaConnect.Products.LankaEvents.Domain.Enums.AddOnPurchaseStatus.Completed &&
                        !string.IsNullOrWhiteSpace(p.StripePaymentIntentId) &&
                        p.TotalAmount is not null &&
                        p.TotalAmount.Amount > 0 &&
                        (p.RegistrationId == null || p.RegistrationId == registration.Id)))
                    {
                        lineItems.Add(new LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput(
                            LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.AddOn,
                            p.Id,
                            p.TotalAmount));
                    }
                }

                if (request.RefundCollections)
                {
                    var collections = await _collectionRepository.GetByUserIdAndEventIdAsync(
                        request.UserId, request.EventId, cancellationToken);
                    foreach (var c in collections.Where(c =>
                        c.Status == LankaConnect.Products.LankaEvents.Domain.Enums.CollectionStatus.Completed &&
                        !string.IsNullOrWhiteSpace(c.StripePaymentIntentId) &&
                        c.Amount is not null &&
                        c.Amount.Amount > 0))
                    {
                        lineItems.Add(new LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput(
                            LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Collection,
                            c.Id,
                            c.Amount));
                    }
                }

                if (request.RefundSponsors)
                {
                    var sponsors = await _sponsorRepository.GetByUserIdAndEventIdAsync(
                        request.UserId, request.EventId, cancellationToken);
                    foreach (var s in sponsors.Where(s =>
                        s.Status == LankaConnect.Products.LankaEvents.Domain.Enums.SponsorStatus.Completed &&
                        !string.IsNullOrWhiteSpace(s.StripePaymentIntentId) &&
                        s.Amount is not null &&
                        s.Amount.Amount > 0))
                    {
                        lineItems.Add(new LankaConnect.Products.LankaEvents.Domain.ValueObjects.RefundRequestLineItemInput(
                            LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Sponsor,
                            s.Id,
                            s.Amount!));
                    }
                }

                _logger.LogInformation(
                    "[6A.148] Built {Count} refund line items: {Breakdown}",
                    lineItems.Count,
                    string.Join(", ", lineItems.Select(li => $"{li.Type}=${li.RequestedAmount.Amount}")));

                // Atomically:
                //   1. Cancel the registration (Status -> Cancelled, seat released)
                //   2. If buckets were selected: create the Pending RefundRequest
                //   3. Raise RegistrationCancelledEvent so the existing email pipeline fires
                //   4. Save
                //
                // The architect-decoupled domain (Phase 6A.148 post-rework) allows
                // CreateRefundRequest to operate on a Cancelled registration.
                registration.Cancel();

                Guid? refundRequestId = null;
                if (lineItems.Count > 0)
                {
                    var rrResult = registration.CreateRefundRequest(
                        requestedByUserId: request.UserId,
                        isOrganizerInitiated: false,
                        requesterReason: request.RequesterReason,
                        organizerNotes: null,
                        overrideScanGuard: false,
                        anyTicketsScanned: false, // we already gated above
                        lineItems: lineItems);
                    if (rrResult.IsFailure)
                    {
                        // CreateRefundRequest failed AFTER Cancel() succeeded â€” bad. Roll back
                        // by returning failure and letting the DbContext drop pending changes.
                        // (UnitOfWork hasn't committed yet, so no DB state has changed.)
                        stopwatch.Stop();
                        _logger.LogError(
                            "[6A.148] HandlePaidCancelViaApprovalWorkflow CreateRefundRequest FAILED post-cancel: RegId={RegId} Error={Error}",
                            registration.Id, rrResult.Error);
                        return Result<CancelRsvpResult>.Failure(
                            $"Cancel-and-refund failed at refund creation: {rrResult.Error}");
                    }
                    refundRequestId = rrResult.Value.Id;
                }

                @event.RaiseRegistrationCancelledEvent(request.UserId);
                _registrationRepository.Update(registration);
                _eventRepository.Update(@event);
                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[6A.148] HandlePaidCancelViaApprovalWorkflow COMPLETE: RegId={RegId} RefundRequestId={RrId} LineCount={Count} Duration={ElapsedMs}ms",
                    registration.Id, refundRequestId, lineItems.Count, stopwatch.ElapsedMilliseconds);

                return Result<CancelRsvpResult>.Success(new CancelRsvpResult(
                    RegistrationCancelled: true,
                    CommitmentsDeleted: null,
                    FormResponsesDeleted: null,
                    FormResponsesDeletedCount: null,
                    AddOnRefundsProcessed: null,
                    AddOnRefundedCount: null,
                    AddOnFailedCount: null,
                    AddOnRefundTotal: null,
                    Warnings: null,
                    RefundRequestId: refundRequestId));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[6A.148] HandlePaidCancelViaApprovalWorkflow EXCEPTION: RegId={RegId} Duration={ElapsedMs}ms",
                    registration.Id, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

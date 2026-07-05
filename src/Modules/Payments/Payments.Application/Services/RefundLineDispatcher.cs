using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Products.LankaEvents.Application.Services; // W4.4.c.4: interfaces stay in legacy (4 cross-module consumers; circular ref otherwise)
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.Services;

/// <inheritdoc cref="IRefundLineDispatcher"/>
public class RefundLineDispatcher : IRefundLineDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefundLineDispatcher> _logger;

    public RefundLineDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<RefundLineDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result> DispatchAsync(
        Guid lineItemId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RefundLineDispatch"))
        using (LogContext.PushProperty("LineItemId", lineItemId))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.W5.D2 DISP] START: LineItemId={LineItemId} RegistrationId={RegistrationId}",
                lineItemId, registrationId);

            // ★ The defining structural property: fresh scope per call. Mutations and
            //   SaveChangesAsync happen inside this scope, isolated from any outer DbContext
            //   (especially the one that loaded the parent Registration aggregate). The
            //   xmin clash that caused the W5.D7 stuck refund cannot occur here — we never
            //   touch the Registration row.
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var refundRepo = sp.GetRequiredService<IRefundRequestRepository>();
            var paymentRepo = sp.GetRequiredService<IRegistrationPaymentRepository>();
            var addOnRepo = sp.GetRequiredService<IAddOnPurchaseRepository>();
            var collectionRepo = sp.GetRequiredService<ICollectionRepository>();
            var sponsorRepo = sp.GetRequiredService<ISponsorRepository>();
            var registrationRepo = sp.GetRequiredService<IRegistrationRepository>();
            var stripe = sp.GetRequiredService<IStripePaymentService>();
            var uow = sp.GetRequiredService<IUnitOfWork>();

            try
            {
                var line = await refundRepo.GetLineItemByIdAsync(lineItemId, cancellationToken);
                if (line is null)
                {
                    _logger.LogWarning(
                        "[6A.148.W5.D2 DISP] Line {LineItemId} not found — possibly deleted between outer DispatchAsync and per-line scope",
                        lineItemId);
                    return Result.NotFound("Line item not found");
                }

                if (line.Status != RefundLineItemStatus.Approved)
                {
                    // Idempotent — repeat calls for the same line are a no-op. Common via
                    // reconciler re-dispatch after a successful first attempt.
                    _logger.LogInformation(
                        "[6A.148.W5.D2 DISP] Line {LineItemId} not in Approved (Status={Status}); skipping",
                        lineItemId, line.Status);
                    return Result.Success();
                }

                if (line.ApprovedAmount is null || line.ApprovedAmount.Amount <= 0)
                {
                    _logger.LogWarning(
                        "[6A.148.W5.D2 DISP] Line {LineItemId} has no positive ApprovedAmount; cannot dispatch",
                        lineItemId);
                    return Result.Failure("Line has no positive ApprovedAmount");
                }

                var paymentIntentLookup = await ResolvePaymentIntentAsync(
                    line, paymentRepo, addOnRepo, collectionRepo, sponsorRepo,
                    registrationRepo, cancellationToken);
                if (paymentIntentLookup.IsFailure)
                {
                    _logger.LogError(
                        "[6A.148.W5.D2 DISP] PaymentIntent lookup failed for Line {LineItemId} Type={Type} Ref={Ref}: {Error}",
                        line.Id, line.Type, line.ReferenceId, paymentIntentLookup.Error);
                    line.MarkProcessing("pending_resolution", null);
                    line.MarkFailed($"Unable to resolve Stripe charge: {paymentIntentLookup.Error}");
                    await uow.CommitAsync(cancellationToken);
                    return Result.Failure(paymentIntentLookup.Error);
                }

                var paymentIntentId = paymentIntentLookup.Value;

                // W5.D1: stable per-line idempotency key. Stripe guarantees ≤1 successful
                // refund per key for 24h — re-dispatch returns the prior refund object,
                // never charges twice.
                var idempotencyKey = $"refund_line_{line.Id:N}";

                var refundTypeForRouting = line.Type switch
                {
                    RefundLineItemType.AddOn => "add_on_purchase",
                    RefundLineItemType.Sponsor => "sponsor",
                    RefundLineItemType.Collection => "collection",
                    RefundLineItemType.Ticket => "registration",
                    _ => "registration"
                };

                var amountInCents = (long)Math.Round(
                    line.ApprovedAmount.Amount * 100m, MidpointRounding.AwayFromZero);

                var stripeReq = new CreateRefundRequest
                {
                    PaymentIntentId = paymentIntentId,
                    RegistrationId = registrationId,
                    AmountInCents = amountInCents,
                    Reason = "requested_by_customer",
                    IdempotencyKey = idempotencyKey,
                    Metadata = new Dictionary<string, string>
                    {
                        ["refund_request_id"] = line.RefundRequestId.ToString(),
                        ["line_item_id"] = line.Id.ToString(),
                        ["line_type"] = line.Type.ToString(),
                        ["reference_id"] = line.ReferenceId.ToString(),
                        ["refund_type"] = refundTypeForRouting
                    }
                };

                try
                {
                    var stripeResult = await stripe.CreateRefundAsync(stripeReq, cancellationToken);
                    if (stripeResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "[6A.148.W5.D2 DISP] Stripe CreateRefund failed for Line {LineItemId}: {Error}",
                            line.Id, stripeResult.Error);
                        line.MarkProcessing($"failed_dispatch_{Guid.NewGuid():N}", null);
                        line.MarkFailed(stripeResult.Error);
                        await uow.CommitAsync(cancellationToken);
                        return Result.Failure(stripeResult.Error);
                    }

                    var processingResult = line.MarkProcessing(
                        stripeResult.Value.RefundId, paymentIntentId);
                    if (processingResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "[6A.148.W5.D2 DISP] MarkProcessing failed for Line {LineItemId}: {Error}",
                            line.Id, processingResult.Error);
                        // Don't commit a half-mutation; line stays Approved for next reconcile.
                        return Result.Failure(processingResult.Error);
                    }

                    // Stripe often returns "succeeded" inline. Flip to Refunded immediately
                    // so the W5.D3 request-level commit can roll the RR to Completed
                    // without waiting for the webhook (the webhook is still the authoritative
                    // source for eventual consistency — idempotency guard makes late webhook
                    // delivery a no-op).
                    if (string.Equals(stripeResult.Value.Status, "succeeded",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        line.MarkRefunded(DateTime.UtcNow);
                    }

                    // ★ The save that survives concurrent Registration mutations: only the
                    //   refund_request_line_items row is dirty in this scope. No xmin token
                    //   on this table (RefundRequestLineItemConfiguration.cs verified).
                    await uow.CommitAsync(cancellationToken);

                    sw.Stop();
                    _logger.LogInformation(
                        "[6A.148.W5.D2 DISP] Line {LineItemId} Type={Type} dispatched: " +
                        "StripeRefundId={Sri} StripeStatus={Status} AmountInCents={Amt} " +
                        "IdempotencyKey={Key} Duration={ElapsedMs}ms",
                        line.Id, line.Type, stripeResult.Value.RefundId,
                        stripeResult.Value.Status, amountInCents, idempotencyKey,
                        sw.ElapsedMilliseconds);

                    return Result.Success();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[6A.148.W5.D2 DISP] EXCEPTION during dispatch of Line {LineItemId} " +
                        "(PaymentIntentId={Pii}); line stays Approved for reconciler",
                        line.Id, paymentIntentId);
                    // Don't try to save a failure marker — outer scope may already be
                    // poisoned. The W5.D6 reconciler will pick this up next cycle and
                    // re-dispatch idempotently via the W5.D1 key.
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.W5.D2 DISP] OUTER EXCEPTION for Line {LineItemId} after {ElapsedMs}ms",
                    lineItemId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// Resolves the Stripe PaymentIntent ID for a line based on its Type + ReferenceId.
    /// Mirrors <c>RefundExecutionService.ResolvePaymentIntentAsync</c> — kept private to
    /// the dispatcher so the resolution always uses scoped repository instances.
    /// </summary>
    private static async Task<Result<string>> ResolvePaymentIntentAsync(
        RefundRequestLineItem line,
        IRegistrationPaymentRepository paymentRepo,
        IAddOnPurchaseRepository addOnRepo,
        ICollectionRepository collectionRepo,
        ISponsorRepository sponsorRepo,
        IRegistrationRepository registrationRepo,
        CancellationToken cancellationToken)
    {
        switch (line.Type)
        {
            case RefundLineItemType.Ticket:
            {
                var payment = await paymentRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (payment is null)
                {
                    var initial = await paymentRepo.GetInitialPaymentAsync(
                        line.ReferenceId, cancellationToken);
                    if (initial is not null && !string.IsNullOrWhiteSpace(initial.StripePaymentIntentId))
                        return Result<string>.Success(initial.StripePaymentIntentId);

                    var registration = await registrationRepo.GetByIdAsync(
                        line.ReferenceId, cancellationToken);
                    if (registration is not null
                        && !string.IsNullOrWhiteSpace(registration.StripePaymentIntentId))
                        return Result<string>.Success(registration.StripePaymentIntentId);

                    return Result<string>.Failure(
                        "Ticket line: no RegistrationPayment found and no Registration.StripePaymentIntentId fallback available");
                }
                if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
                    return Result<string>.Failure("RegistrationPayment has no StripePaymentIntentId");
                return Result<string>.Success(payment.StripePaymentIntentId);
            }

            case RefundLineItemType.AddOn:
            {
                var purchase = await addOnRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (purchase is null || string.IsNullOrWhiteSpace(purchase.StripePaymentIntentId))
                    return Result<string>.Failure("AddOnPurchase or its PaymentIntentId not found");
                return Result<string>.Success(purchase.StripePaymentIntentId);
            }

            case RefundLineItemType.Collection:
            {
                var collection = await collectionRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (collection is null || string.IsNullOrWhiteSpace(collection.StripePaymentIntentId))
                    return Result<string>.Failure("Collection or its PaymentIntentId not found");
                return Result<string>.Success(collection.StripePaymentIntentId);
            }

            case RefundLineItemType.Sponsor:
            {
                var sponsor = await sponsorRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (sponsor is null || string.IsNullOrWhiteSpace(sponsor.StripePaymentIntentId))
                    return Result<string>.Failure("Sponsor or its PaymentIntentId not found");
                return Result<string>.Success(sponsor.StripePaymentIntentId);
            }

            default:
                return Result<string>.Failure($"Unsupported line item type: {line.Type}");
        }
    }
}

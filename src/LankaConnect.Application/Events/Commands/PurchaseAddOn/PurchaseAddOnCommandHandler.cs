using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PurchaseAddOn;

/// <summary>
/// Handles add-on purchase creation with atomic stock reservation.
/// Flow: validate event + add-ons enabled -> load definition -> validate stock ->
/// ATOMIC reserve stock -> snapshot price -> create AddOnPurchase entity ->
/// create Stripe Checkout -> set session -> calculate revenue -> save -> return checkout URL
///
/// CRITICAL: If any step after stock reservation fails, stock is restored via TryRestoreStockAsync.
/// </summary>
public class PurchaseAddOnCommandHandler : ICommandHandler<PurchaseAddOnCommand, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<PurchaseAddOnCommandHandler> _logger;

    public PurchaseAddOnCommandHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<PurchaseAddOnCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(PurchaseAddOnCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PurchaseAddOn"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("AddOnDefinitionId", request.AddOnDefinitionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PurchaseAddOn START: EventId={EventId}, DefinitionId={DefinitionId}, BuyerEmail={BuyerEmail}, Quantity={Quantity}, IsBundled={IsBundled}, IsAnonymous={IsAnonymous}",
                request.EventId, request.AddOnDefinitionId, request.BuyerEmail, request.Quantity, request.RegistrationId != null, request.UserId == null);

            bool stockReserved = false;

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<string>.Failure("Event not found");

                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<string>.Failure("Add-on purchases are only available for published events");

                // 2. Validate add-ons are enabled
                if (!@event.AreAddOnsEnabled())
                    return Result<string>.Failure("Add-ons are not enabled for this event");

                // 3. Load AddOnDefinition, validate it exists and is active
                var definition = await _addOnDefinitionRepository.GetByIdAsync(request.AddOnDefinitionId, cancellationToken);
                if (definition == null)
                    return Result<string>.Failure("Add-on not found");

                if (!definition.IsActive)
                    return Result<string>.Failure("This add-on is no longer available");

                if (definition.EventId != request.EventId)
                    return Result<string>.Failure("Add-on does not belong to this event");

                // 4. Validate quantity
                if (request.Quantity <= 0)
                    return Result<string>.Failure("Quantity must be greater than zero");

                // Soft check for stock availability (informational only, not authoritative)
                if (!definition.HasAvailableStock(request.Quantity))
                    return Result<string>.Failure("Insufficient stock for the requested quantity");

                // 5. ATOMIC stock reservation via raw SQL
                stockReserved = await _addOnDefinitionRepository.TryReserveStockAsync(
                    request.AddOnDefinitionId, request.Quantity, cancellationToken);

                if (!stockReserved)
                    return Result<string>.Failure("Insufficient stock — another purchase was completed first");

                // 6. PRICE SNAPSHOT: Capture unit price at this moment (M3 modification)
                var unitPrice = definition.Price;
                var totalAmount = unitPrice.Amount * request.Quantity;

                // 7. Create AddOnPurchase entity
                var unitPriceMoney = unitPrice;
                Result<AddOnPurchase> purchaseResult;

                if (request.RegistrationId.HasValue)
                {
                    purchaseResult = AddOnPurchase.CreateBundledWithRegistration(
                        request.EventId,
                        request.AddOnDefinitionId,
                        request.RegistrationId.Value,
                        request.UserId,
                        request.BuyerName,
                        request.BuyerEmail,
                        request.BuyerPhone,
                        request.Quantity,
                        unitPriceMoney);
                }
                else
                {
                    purchaseResult = AddOnPurchase.Create(
                        request.EventId,
                        request.AddOnDefinitionId,
                        request.UserId,
                        request.BuyerName,
                        request.BuyerEmail,
                        request.BuyerPhone,
                        request.Quantity,
                        unitPriceMoney);
                }

                if (purchaseResult.IsFailure)
                {
                    // Restore stock since entity creation failed
                    await RestoreStockSafely(request.AddOnDefinitionId, request.Quantity, cancellationToken);
                    stockReserved = false;
                    return Result<string>.Failure(purchaseResult.Error);
                }

                var purchase = purchaseResult.Value;

                // 8a. FREE ADD-ON BYPASS: Skip Stripe for $0 add-ons, immediately complete
                if (totalAmount == 0)
                {
                    _logger.LogInformation(
                        "PurchaseAddOn FREE: DefinitionId={DefinitionId}, Quantity={Quantity} — Skipping Stripe checkout for free add-on",
                        request.AddOnDefinitionId, request.Quantity);

                    var completeResult = purchase.CompletePayment("free_addon_no_payment_required");
                    if (completeResult.IsFailure)
                    {
                        await RestoreStockSafely(request.AddOnDefinitionId, request.Quantity, cancellationToken);
                        stockReserved = false;
                        return Result<string>.Failure(completeResult.Error);
                    }

                    // Set zero revenue breakdown for free add-ons
                    // Each Money must be a distinct instance — EF Core owned entities cannot share references
                    purchase.SetRevenueBreakdown(
                        Money.Zero(unitPrice.Currency),
                        Money.Zero(unitPrice.Currency),
                        Money.Zero(unitPrice.Currency));

                    await _addOnPurchaseRepository.AddAsync(purchase, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "PurchaseAddOn FREE COMPLETE: PurchaseId={PurchaseId}, EventId={EventId}, DefinitionId={DefinitionId}, Quantity={Quantity}, Duration={ElapsedMs}ms",
                        purchase.Id, request.EventId, request.AddOnDefinitionId, request.Quantity, stopwatch.ElapsedMilliseconds);

                    // Return success URL directly (no checkout redirect needed)
                    return Result<string>.Success(request.SuccessUrl);
                }

                // 8b. Create Stripe Checkout session (paid add-ons only)
                var checkoutRequest = new CreateAddOnPurchaseCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    AddOnPurchaseId = purchase.Id,
                    AddOnDefinitionId = request.AddOnDefinitionId,
                    EventTitle = @event.Title.Value,
                    AddOnName = definition.Name,
                    Quantity = request.Quantity,
                    UnitPrice = unitPrice.Amount,
                    TotalAmount = totalAmount,
                    Currency = unitPrice.Currency.ToString(),
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "addon_purchase" },
                        { "event_id", request.EventId.ToString() },
                        { "addon_purchase_id", purchase.Id.ToString() },
                        { "addon_definition_id", request.AddOnDefinitionId.ToString() },
                        { "buyer_user_id", request.UserId?.ToString() ?? "anonymous" },
                        { "quantity", request.Quantity.ToString() }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreateAddOnPurchaseCheckoutSessionAsync(checkoutRequest, cancellationToken);
                if (checkoutResult.IsFailure)
                {
                    await RestoreStockSafely(request.AddOnDefinitionId, request.Quantity, cancellationToken);
                    stockReserved = false;
                    return Result<string>.Failure($"Failed to create payment session: {checkoutResult.Error}");
                }

                // 9. Set Stripe session on purchase
                var setSessionResult = purchase.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    checkoutResult.Value.ExpiresAt);
                if (setSessionResult.IsFailure)
                {
                    await RestoreStockSafely(request.AddOnDefinitionId, request.Quantity, cancellationToken);
                    stockReserved = false;
                    return Result<string>.Failure(setSessionResult.Error);
                }

                // 10. Calculate and store revenue breakdown
                try
                {
                    var totalMoney = Money.Create(totalAmount, unitPrice.Currency);
                    if (totalMoney.IsSuccess)
                    {
                        var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                            totalMoney.Value,
                            @event.Location,
                            cancellationToken);

                        if (breakdownResult.IsSuccess)
                        {
                            purchase.SetRevenueBreakdown(
                                breakdownResult.Value.StripeFeeAmount,
                                breakdownResult.Value.PlatformCommission,
                                breakdownResult.Value.OrganizerPayout);

                            _logger.LogInformation(
                                "Revenue breakdown calculated for add-on purchase {PurchaseId}: StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                                purchase.Id,
                                breakdownResult.Value.StripeFeeAmount.Amount,
                                breakdownResult.Value.PlatformCommission.Amount,
                                breakdownResult.Value.OrganizerPayout.Amount);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Revenue breakdown calculation failed for add-on purchase {PurchaseId}: {Error}",
                                purchase.Id, breakdownResult.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception calculating revenue breakdown for add-on purchase {PurchaseId}. Purchase will continue without breakdown.",
                        purchase.Id);
                }

                // 11. Save purchase
                await _addOnPurchaseRepository.AddAsync(purchase, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PurchaseAddOn COMPLETE: PurchaseId={PurchaseId}, EventId={EventId}, DefinitionId={DefinitionId}, Quantity={Quantity}, TotalAmount={TotalAmount}, Duration={ElapsedMs}ms",
                    purchase.Id, request.EventId, request.AddOnDefinitionId, request.Quantity, totalAmount, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(checkoutResult.Value.CheckoutUrl);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Restore stock if it was reserved but the operation failed
                if (stockReserved)
                {
                    await RestoreStockSafely(request.AddOnDefinitionId, request.Quantity, cancellationToken);
                }

                _logger.LogError(ex,
                    "PurchaseAddOn FAILED: EventId={EventId}, DefinitionId={DefinitionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.AddOnDefinitionId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<string>.Failure($"Add-on purchase failed: {ex.Message}");
            }
        }
    }

    private async Task RestoreStockSafely(Guid definitionId, int quantity, CancellationToken cancellationToken)
    {
        try
        {
            var restored = await _addOnDefinitionRepository.TryRestoreStockAsync(definitionId, quantity, cancellationToken);
            if (restored)
            {
                _logger.LogInformation(
                    "Stock restored for add-on definition {DefinitionId}: Quantity={Quantity}",
                    definitionId, quantity);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to restore stock for add-on definition {DefinitionId}: Quantity={Quantity}. Manual intervention may be required.",
                    definitionId, quantity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception restoring stock for add-on definition {DefinitionId}: Quantity={Quantity}. Manual intervention required.",
                definitionId, quantity);
        }
    }
}

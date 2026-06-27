using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PurchaseAddOnCart;

/// <summary>
/// Handles multi-add-on cart purchase with atomic stock reservation per item.
/// Flow: validate event -> validate all definitions -> reserve stock for each ->
/// create N AddOnPurchase entities -> free items complete immediately ->
/// paid items go into 1 Stripe Checkout session with N line items ->
/// set session on paid purchases -> calculate revenue -> save -> return URL.
///
/// CRITICAL: If any step fails after stock reservation, ALL reserved stock is restored.
/// </summary>
public class PurchaseAddOnCartCommandHandler : ICommandHandler<PurchaseAddOnCartCommand, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<PurchaseAddOnCartCommandHandler> _logger;

    public PurchaseAddOnCartCommandHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<PurchaseAddOnCartCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(PurchaseAddOnCartCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PurchaseAddOnCart"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PurchaseAddOnCart START: EventId={EventId}, ItemCount={ItemCount}, BuyerEmail={BuyerEmail}, IsAnonymous={IsAnonymous}",
                request.EventId, request.Items.Count, request.BuyerEmail, request.UserId == null);

            // Track which definitions had stock reserved (for rollback on failure)
            var reservedStock = new List<(Guid DefinitionId, int Quantity)>();

            try
            {
                // 1. Basic validation
                if (request.Items == null || request.Items.Count == 0)
                    return Result<string>.Failure("Cart is empty — add at least one item");

                if (request.Items.Count > 20)
                    return Result<string>.Failure("Cart cannot contain more than 20 items");

                // Check for duplicate definitions
                var definitionIds = request.Items.Select(i => i.AddOnDefinitionId).ToList();
                if (definitionIds.Distinct().Count() != definitionIds.Count)
                    return Result<string>.Failure("Cart contains duplicate add-on items — combine quantities instead");

                // 2. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<string>.Failure("Event not found");

                if (@event.Status != Domain.Events.Enums.EventStatus.Published)
                    return Result<string>.Failure("Add-on purchases are only available for published events");

                if (!@event.AreAddOnsEnabled())
                    return Result<string>.Failure("Add-ons are not enabled for this event");

                // 3. Load and validate ALL definitions
                var definitions = new Dictionary<Guid, AddOnDefinition>();
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        return Result<string>.Failure($"Quantity must be greater than zero for each item");

                    var definition = await _addOnDefinitionRepository.GetByIdAsync(item.AddOnDefinitionId, cancellationToken);
                    if (definition == null)
                        return Result<string>.Failure($"Add-on not found: {item.AddOnDefinitionId}");

                    if (!definition.IsActive)
                        return Result<string>.Failure($"Add-on '{definition.Name}' is no longer available");

                    if (definition.EventId != request.EventId)
                        return Result<string>.Failure($"Add-on '{definition.Name}' does not belong to this event");

                    if (!definition.HasAvailableStock(item.Quantity))
                        return Result<string>.Failure($"Insufficient stock for '{definition.Name}'");

                    definitions[item.AddOnDefinitionId] = definition;
                }

                // 4. ATOMIC stock reservation for EACH item
                foreach (var item in request.Items)
                {
                    var reserved = await _addOnDefinitionRepository.TryReserveStockAsync(
                        item.AddOnDefinitionId, item.Quantity, cancellationToken);

                    if (!reserved)
                    {
                        // Rollback all previously reserved stock
                        await RollbackAllReservedStock(reservedStock, cancellationToken);
                        var defName = definitions[item.AddOnDefinitionId].Name;
                        return Result<string>.Failure($"Insufficient stock for '{defName}' — another purchase was completed first");
                    }

                    reservedStock.Add((item.AddOnDefinitionId, item.Quantity));
                }

                _logger.LogInformation(
                    "PurchaseAddOnCart STOCK RESERVED: EventId={EventId}, ItemCount={ItemCount}",
                    request.EventId, reservedStock.Count);

                // 5. Create AddOnPurchase entities for each item
                var freePurchases = new List<AddOnPurchase>();
                var paidPurchases = new List<AddOnPurchase>();
                var paidLineItems = new List<AddOnCartCheckoutLineItem>();

                foreach (var item in request.Items)
                {
                    var definition = definitions[item.AddOnDefinitionId];
                    var unitPrice = definition.Price;
                    var totalAmount = unitPrice.Amount * item.Quantity;

                    var purchaseResult = AddOnPurchase.Create(
                        request.EventId,
                        item.AddOnDefinitionId,
                        request.UserId,
                        request.BuyerName,
                        request.BuyerEmail,
                        request.BuyerPhone,
                        item.Quantity,
                        unitPrice);

                    if (purchaseResult.IsFailure)
                    {
                        await RollbackAllReservedStock(reservedStock, cancellationToken);
                        return Result<string>.Failure(purchaseResult.Error);
                    }

                    var purchase = purchaseResult.Value;

                    if (totalAmount == 0)
                    {
                        // FREE: complete immediately
                        var completeResult = purchase.CompletePayment("free_addon_no_payment_required");
                        if (completeResult.IsFailure)
                        {
                            await RollbackAllReservedStock(reservedStock, cancellationToken);
                            return Result<string>.Failure(completeResult.Error);
                        }

                        // Each Money.Zero must be distinct — EF Core owned entities cannot share references
                        purchase.SetRevenueBreakdown(
                            Money.Zero(unitPrice.Currency),
                            Money.Zero(unitPrice.Currency),
                            Money.Zero(unitPrice.Currency));

                        freePurchases.Add(purchase);

                        _logger.LogInformation(
                            "PurchaseAddOnCart FREE ITEM: DefinitionId={DefinitionId}, Name={Name}, Quantity={Quantity}",
                            item.AddOnDefinitionId, definition.Name, item.Quantity);
                    }
                    else
                    {
                        // PAID: will go to Stripe checkout
                        paidPurchases.Add(purchase);
                        paidLineItems.Add(new AddOnCartCheckoutLineItem
                        {
                            AddOnPurchaseId = purchase.Id,
                            AddOnDefinitionId = item.AddOnDefinitionId,
                            Name = definition.Name,
                            Description = definition.Description,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice.Amount,
                            Currency = unitPrice.Currency.ToString()
                        });
                    }
                }

                // 6. Handle all-free cart scenario
                if (paidPurchases.Count == 0)
                {
                    _logger.LogInformation(
                        "PurchaseAddOnCart ALL FREE: EventId={EventId}, FreeItems={FreeCount}",
                        request.EventId, freePurchases.Count);

                    foreach (var fp in freePurchases)
                        await _addOnPurchaseRepository.AddAsync(fp, cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "PurchaseAddOnCart ALL FREE COMPLETE: EventId={EventId}, FreeItems={FreeCount}, Duration={ElapsedMs}ms",
                        request.EventId, freePurchases.Count, stopwatch.ElapsedMilliseconds);

                    return Result<string>.Success(request.SuccessUrl);
                }

                // 7. Create single Stripe Checkout session with N line items
                var cartCheckoutRequest = new CreateAddOnCartCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Items = paidLineItems,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "add_on_purchase" },
                        { "event_id", request.EventId.ToString() },
                        { "buyer_user_id", request.UserId?.ToString() ?? "anonymous" },
                        { "cart_item_count", paidPurchases.Count.ToString() },
                        { "purchase_ids", string.Join(",", paidPurchases.Select(p => p.Id)) }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreateAddOnCartCheckoutSessionAsync(
                    cartCheckoutRequest, cancellationToken);

                if (checkoutResult.IsFailure)
                {
                    await RollbackAllReservedStock(reservedStock, cancellationToken);
                    return Result<string>.Failure($"Failed to create payment session: {checkoutResult.Error}");
                }

                // 8. Set Stripe session on ALL paid purchases (they share the same session)
                foreach (var purchase in paidPurchases)
                {
                    var setSessionResult = purchase.SetStripeCheckoutSession(
                        checkoutResult.Value.SessionId,
                        checkoutResult.Value.ExpiresAt);

                    if (setSessionResult.IsFailure)
                    {
                        await RollbackAllReservedStock(reservedStock, cancellationToken);
                        return Result<string>.Failure(setSessionResult.Error);
                    }
                }

                // 9. Calculate revenue breakdown for each paid purchase
                foreach (var purchase in paidPurchases)
                {
                    try
                    {
                        var totalMoney = purchase.TotalAmount;
                        var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                            totalMoney, @event.Location, cancellationToken);

                        if (breakdownResult.IsSuccess)
                        {
                            purchase.SetRevenueBreakdown(
                                breakdownResult.Value.StripeFeeAmount,
                                breakdownResult.Value.PlatformCommission,
                                breakdownResult.Value.OrganizerPayout);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Revenue breakdown failed for cart purchase {PurchaseId}: {Error}",
                                purchase.Id, breakdownResult.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Exception calculating revenue for cart purchase {PurchaseId}",
                            purchase.Id);
                    }
                }

                // 10. Save ALL purchases (free + paid)
                foreach (var fp in freePurchases)
                    await _addOnPurchaseRepository.AddAsync(fp, cancellationToken);

                foreach (var pp in paidPurchases)
                    await _addOnPurchaseRepository.AddAsync(pp, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PurchaseAddOnCart COMPLETE: EventId={EventId}, FreeItems={FreeCount}, PaidItems={PaidCount}, SessionId={SessionId}, Duration={ElapsedMs}ms",
                    request.EventId, freePurchases.Count, paidPurchases.Count,
                    checkoutResult.Value.SessionId, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(checkoutResult.Value.CheckoutUrl);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Rollback all reserved stock on any exception
                if (reservedStock.Count > 0)
                    await RollbackAllReservedStock(reservedStock, cancellationToken);

                _logger.LogError(ex,
                    "PurchaseAddOnCart FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<string>.Failure($"Cart purchase failed: {ex.Message}");
            }
        }
    }

    private async Task RollbackAllReservedStock(
        List<(Guid DefinitionId, int Quantity)> reservedStock,
        CancellationToken cancellationToken)
    {
        foreach (var (definitionId, quantity) in reservedStock)
        {
            try
            {
                var restored = await _addOnDefinitionRepository.TryRestoreStockAsync(
                    definitionId, quantity, cancellationToken);

                if (restored)
                {
                    _logger.LogInformation(
                        "Cart stock restored: DefinitionId={DefinitionId}, Quantity={Quantity}",
                        definitionId, quantity);
                }
                else
                {
                    _logger.LogWarning(
                        "Cart stock restore FAILED: DefinitionId={DefinitionId}, Quantity={Quantity}. Manual intervention may be needed.",
                        definitionId, quantity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception restoring cart stock: DefinitionId={DefinitionId}, Quantity={Quantity}. Manual intervention required.",
                    definitionId, quantity);
            }
        }
    }
}

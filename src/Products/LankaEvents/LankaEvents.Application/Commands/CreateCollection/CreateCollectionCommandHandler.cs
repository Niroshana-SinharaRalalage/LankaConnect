using System.Diagnostics;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateCollection;

/// <summary>
/// Handles standalone collection contribution creation from the event details page.
/// Flow: validate event + collections enabled -> validate amount -> create Collection entity ->
/// create Stripe Checkout -> set session -> calculate revenue -> save -> return checkout URL
/// </summary>
public class CreateCollectionCommandHandler : ICommandHandler<CreateCollectionCommand, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<CreateCollectionCommandHandler> _logger;

    public CreateCollectionCommandHandler(
        IEventRepository eventRepository,
        ICollectionRepository collectionRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<CreateCollectionCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _collectionRepository = collectionRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateCollection"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateCollection START: EventId={EventId}, ContributorEmail={ContributorEmail}, Amount={Amount}, Currency={Currency}, IsAnonymous={IsAnonymous}",
                request.EventId, request.ContributorEmail, request.Amount, request.Currency, request.UserId == null);

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<string>.Failure("Event not found");

                // 2. Validate event is Published
                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<string>.Failure("Collections are only available for published events");

                // 3. Validate collections are enabled
                if (!@event.AreCollectionsEnabled())
                    return Result<string>.Failure("Collections are not enabled for this event");

                // 4. Validate collection amount (C3 Guard: always check > 0)
                if (request.Amount <= 0)
                    return Result<string>.Failure("Contribution amount must be greater than zero");

                var validateAmountResult = @event.ValidateCollectionAmount(request.Amount);
                if (validateAmountResult.IsFailure)
                    return Result<string>.Failure(validateAmountResult.Error);

                // 5. Parse currency
                if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
                    return Result<string>.Failure($"Invalid currency: {request.Currency}");

                // 6. Create Money and Collection entity
                var amountResult = Money.Create(request.Amount, currency);
                if (amountResult.IsFailure)
                    return Result<string>.Failure(amountResult.Error);

                var collectionResult = Collection.Create(
                    request.EventId,
                    request.UserId,
                    request.ContributorName,
                    request.ContributorEmail,
                    request.ContributorPhone,
                    request.ContributorNotes,
                    amountResult.Value);

                if (collectionResult.IsFailure)
                    return Result<string>.Failure(collectionResult.Error);

                var collection = collectionResult.Value;

                // 7. Create Stripe Checkout session
                var checkoutRequest = new CreateCollectionCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    CollectionId = collection.Id,
                    EventTitle = @event.Title.Value,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "collection" },
                        { "event_id", request.EventId.ToString() },
                        { "collection_id", collection.Id.ToString() },
                        { "contributor_user_id", request.UserId?.ToString() ?? "anonymous" }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreateCollectionCheckoutSessionAsync(checkoutRequest, cancellationToken);
                if (checkoutResult.IsFailure)
                    return Result<string>.Failure($"Failed to create payment session: {checkoutResult.Error}");

                // 8. Set Stripe session on collection
                var setSessionResult = collection.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    checkoutResult.Value.ExpiresAt);
                if (setSessionResult.IsFailure)
                    return Result<string>.Failure(setSessionResult.Error);

                // 9. Calculate and store revenue breakdown
                try
                {
                    var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                        amountResult.Value,
                        @event.Location,
                        cancellationToken);

                    if (breakdownResult.IsSuccess)
                    {
                        collection.SetRevenueBreakdown(
                            breakdownResult.Value.StripeFeeAmount,
                            breakdownResult.Value.PlatformCommission,
                            breakdownResult.Value.OrganizerPayout);

                        _logger.LogInformation(
                            "Revenue breakdown calculated for collection {CollectionId}: StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                            collection.Id,
                            breakdownResult.Value.StripeFeeAmount.Amount,
                            breakdownResult.Value.PlatformCommission.Amount,
                            breakdownResult.Value.OrganizerPayout.Amount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Revenue breakdown calculation failed for collection {CollectionId}: {Error}",
                            collection.Id, breakdownResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception calculating revenue breakdown for collection {CollectionId}. Collection will continue without breakdown.",
                        collection.Id);
                }

                // 10. Save collection
                await _collectionRepository.AddAsync(collection, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateCollection COMPLETE: CollectionId={CollectionId}, EventId={EventId}, Amount={Amount}, Duration={ElapsedMs}ms",
                    collection.Id, request.EventId, request.Amount, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(checkoutResult.Value.CheckoutUrl);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateCollection FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<string>.Failure($"Collection creation failed: {ex.Message}");
            }
        }
    }
}

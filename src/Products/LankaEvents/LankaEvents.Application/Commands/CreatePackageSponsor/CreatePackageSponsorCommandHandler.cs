using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreatePackageSponsor;

/// <summary>
/// Phase 6A.157 — handles packaged sponsorship purchase creation with atomic
/// stock reservation. Flow mirrors <c>PurchaseAddOnCommandHandler</c>
/// byte-for-byte where possible:
///
/// 1. Validate event exists + Published
/// 2. Reject ExternalPaid events (Stripe + off-platform reconciliation hell)
/// 3. Validate sponsors enabled AND packages enabled
/// 4. Load SponsorshipPackage, validate exists + IsActive + belongs to event
/// 5. ATOMIC stock reservation via raw SQL <c>TryReserveStockAsync</c>
/// 6. Snapshot package fields → call <c>Sponsor.CreatePackageSponsor</c>
/// 7a. FREE PACKAGE BYPASS: Skip Stripe for $0 packages, immediately complete
///     with sentinel "free_package_no_payment_required" intent
/// 7b. Create Stripe Checkout session via
///     <c>CreatePackageSponsorCheckoutSessionAsync</c> (metadata
///     <c>payment_type: "package_sponsor"</c>)
/// 8. Set Stripe session on Sponsor
/// 9. Calculate + store revenue breakdown
/// 10. Save + commit
///
/// If any step after stock reservation fails, stock is restored via
/// <c>TryRestoreStockAsync</c> (idempotent — same recovery pattern as
/// AddOn handler).
/// </summary>
public class CreatePackageSponsorCommandHandler : ICommandHandler<CreatePackageSponsorCommand, CreatePackageSponsorResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<CreatePackageSponsorCommandHandler> _logger;

    public CreatePackageSponsorCommandHandler(
        IEventRepository eventRepository,
        ISponsorshipPackageRepository packageRepository,
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<CreatePackageSponsorCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _packageRepository = packageRepository;
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<CreatePackageSponsorResult>> Handle(
        CreatePackageSponsorCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreatePackageSponsor"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PackageId", request.PackageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreatePackageSponsor START: EventId={EventId}, PackageId={PackageId}, BuyerEmail={BuyerEmail}, IsAnonymous={IsAnonymous}",
                request.EventId, request.PackageId, request.BuyerEmail, request.UserId == null);

            bool stockReserved = false;

            try
            {
                // 1. Validate event exists + Published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<CreatePackageSponsorResult>.Failure("Event not found");

                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorship packages are only available for published events");

                // 2. Reject ExternalPaid events (architect decision #15)
                if (@event.PaymentMode == LankaConnect.Products.LankaEvents.Domain.Enums.EventPaymentMode.ExternalPaid)
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorship packages are not supported on externally-paid events");

                // 3. Validate sponsors enabled AND packages enabled
                if (!@event.AreSponsorsEnabled())
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorships are not enabled for this event");

                if (@event.SponsorConfig?.EnablePackages != true)
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorship packages are not enabled for this event");

                // 4. Load package, validate exists + active + belongs to event
                var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
                if (package == null)
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorship package not found");

                if (!package.IsActive)
                    return Result<CreatePackageSponsorResult>.Failure("This sponsorship package is no longer available");

                if (package.EventId != request.EventId)
                    return Result<CreatePackageSponsorResult>.Failure("Sponsorship package does not belong to this event");

                // 5. ATOMIC stock reservation via raw SQL
                stockReserved = await _packageRepository.TryReserveStockAsync(
                    request.PackageId, 1, cancellationToken);

                if (!stockReserved)
                    return Result<CreatePackageSponsorResult>.Failure(
                        "This sponsorship package is no longer available — another buyer just claimed the last slot");

                // 6. Snapshot + create Sponsor entity
                var sponsorResult = Sponsor.CreatePackageSponsor(
                    request.EventId,
                    request.UserId,
                    request.BuyerName,
                    request.BuyerEmail,
                    request.BuyerPhone,
                    request.BuyerOrganization,
                    request.BuyerNotes,
                    request.PackageId,
                    package.Name,
                    package.Tier,
                    package.Price,
                    package.IncludedTicketCount);

                if (sponsorResult.IsFailure)
                {
                    await RestoreStockSafely(request.PackageId, cancellationToken);
                    stockReserved = false;
                    return Result<CreatePackageSponsorResult>.Failure(sponsorResult.Error);
                }

                var sponsor = sponsorResult.Value;

                // 7a. FREE PACKAGE BYPASS: Skip Stripe for $0 packages
                if (package.Price.Amount == 0)
                {
                    _logger.LogInformation(
                        "CreatePackageSponsor FREE: PackageId={PackageId} — Skipping Stripe checkout for free package",
                        request.PackageId);

                    var completeResult = sponsor.CompletePackagePayment("free_package_no_payment_required");
                    if (completeResult.IsFailure)
                    {
                        await RestoreStockSafely(request.PackageId, cancellationToken);
                        stockReserved = false;
                        return Result<CreatePackageSponsorResult>.Failure(completeResult.Error);
                    }

                    // Free packages have zero revenue breakdown.
                    // EF Core owned-type rule — each Money must be a distinct instance.
                    sponsor.SetRevenueBreakdown(
                        Money.Zero(package.Price.Currency),
                        Money.Zero(package.Price.Currency),
                        Money.Zero(package.Price.Currency));

                    await _sponsorRepository.AddAsync(sponsor, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CreatePackageSponsor FREE COMPLETE: SponsorId={SponsorId}, EventId={EventId}, PackageId={PackageId}, Duration={ElapsedMs}ms",
                        sponsor.Id, request.EventId, request.PackageId, stopwatch.ElapsedMilliseconds);

                    return Result<CreatePackageSponsorResult>.Success(new CreatePackageSponsorResult
                    {
                        CheckoutUrl = request.SuccessUrl,
                        SponsorId = sponsor.Id
                    });
                }

                // 7b. Create Stripe Checkout session (paid packages only)
                var checkoutRequest = new CreatePackageSponsorCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    SponsorId = sponsor.Id,
                    SponsorshipPackageId = request.PackageId,
                    EventTitle = @event.Title.Value,
                    PackageName = package.Name,
                    PackageTier = package.Tier,
                    SponsorOrganization = request.BuyerOrganization,
                    Amount = package.Price.Amount,
                    Currency = package.Price.Currency.ToString(),
                    IncludedTicketCount = package.IncludedTicketCount,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        // Literal MUST be byte-identical with PaymentsController
                        // dispatcher case (architect bonus catch #2). Don't repeat
                        // the "addon_purchase" vs "add_on_purchase" footgun.
                        { "payment_type", "package_sponsor" },
                        { "event_id", request.EventId.ToString() },
                        { "sponsor_id", sponsor.Id.ToString() },
                        { "sponsorship_package_id", request.PackageId.ToString() },
                        { "buyer_user_id", request.UserId?.ToString() ?? "anonymous" }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreatePackageSponsorCheckoutSessionAsync(
                    checkoutRequest, cancellationToken);
                if (checkoutResult.IsFailure)
                {
                    await RestoreStockSafely(request.PackageId, cancellationToken);
                    stockReserved = false;
                    return Result<CreatePackageSponsorResult>.Failure(
                        $"Failed to create payment session: {checkoutResult.Error}");
                }

                // 8. Set Stripe session on sponsor
                var setSessionResult = sponsor.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    checkoutResult.Value.ExpiresAt);
                if (setSessionResult.IsFailure)
                {
                    await RestoreStockSafely(request.PackageId, cancellationToken);
                    stockReserved = false;
                    return Result<CreatePackageSponsorResult>.Failure(setSessionResult.Error);
                }

                // 9. Calculate + store revenue breakdown
                try
                {
                    var totalMoney = Money.Create(package.Price.Amount, package.Price.Currency);
                    if (totalMoney.IsSuccess)
                    {
                        var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                            totalMoney.Value,
                            @event.Location,
                            cancellationToken);

                        if (breakdownResult.IsSuccess)
                        {
                            sponsor.SetRevenueBreakdown(
                                breakdownResult.Value.StripeFeeAmount,
                                breakdownResult.Value.PlatformCommission,
                                breakdownResult.Value.OrganizerPayout);

                            _logger.LogInformation(
                                "Revenue breakdown calculated for package sponsor {SponsorId}: StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                                sponsor.Id,
                                breakdownResult.Value.StripeFeeAmount.Amount,
                                breakdownResult.Value.PlatformCommission.Amount,
                                breakdownResult.Value.OrganizerPayout.Amount);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Revenue breakdown calculation failed for package sponsor {SponsorId}: {Error}",
                                sponsor.Id, breakdownResult.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception calculating revenue breakdown for package sponsor {SponsorId}. Sponsor will continue without breakdown.",
                        sponsor.Id);
                }

                // 10. Save sponsor + commit
                await _sponsorRepository.AddAsync(sponsor, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreatePackageSponsor COMPLETE: SponsorId={SponsorId}, EventId={EventId}, PackageId={PackageId}, Amount={Amount}, Duration={ElapsedMs}ms",
                    sponsor.Id, request.EventId, request.PackageId, package.Price.Amount, stopwatch.ElapsedMilliseconds);

                return Result<CreatePackageSponsorResult>.Success(new CreatePackageSponsorResult
                {
                    CheckoutUrl = checkoutResult.Value.CheckoutUrl,
                    SponsorId = sponsor.Id
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (stockReserved)
                {
                    await RestoreStockSafely(request.PackageId, cancellationToken);
                }

                _logger.LogError(ex,
                    "CreatePackageSponsor FAILED: EventId={EventId}, PackageId={PackageId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PackageId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<CreatePackageSponsorResult>.Failure($"Package sponsorship purchase failed: {ex.Message}");
            }
        }
    }

    private async Task RestoreStockSafely(Guid packageId, CancellationToken cancellationToken)
    {
        try
        {
            var restored = await _packageRepository.TryRestoreStockAsync(packageId, 1, cancellationToken);
            if (restored)
            {
                _logger.LogInformation(
                    "Stock restored for sponsorship package {PackageId}",
                    packageId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to restore stock for sponsorship package {PackageId}. Manual intervention may be required.",
                    packageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception restoring stock for sponsorship package {PackageId}. Manual intervention required.",
                packageId);
        }
    }
}

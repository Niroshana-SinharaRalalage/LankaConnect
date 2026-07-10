using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsor;

/// <summary>
/// Handles money-based sponsorship creation from the event details page.
/// Flow: validate event + sponsors enabled + money sponsors accepted -> validate amount ->
/// create Sponsor entity -> create Stripe Checkout -> set session -> calculate revenue -> save -> return checkout URL
/// </summary>
public class CreateMoneySponsorCommandHandler : ICommandHandler<CreateMoneySponsorCommand, CreateMoneySponsorResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<CreateMoneySponsorCommandHandler> _logger;

    public CreateMoneySponsorCommandHandler(
        IEventRepository eventRepository,
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<CreateMoneySponsorCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<CreateMoneySponsorResult>> Handle(CreateMoneySponsorCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateMoneySponsor"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateMoneySponsor START: EventId={EventId}, SponsorEmail={SponsorEmail}, Amount={Amount}, Currency={Currency}, Organization={Organization}, IsAnonymous={IsAnonymous}",
                request.EventId, request.SponsorEmail, request.Amount, request.Currency, request.SponsorOrganization, request.UserId == null);

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<CreateMoneySponsorResult>.Failure("Event not found");

                // 2. Validate event is Published
                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<CreateMoneySponsorResult>.Failure("Sponsorships are only available for published events");

                // 3. Validate sponsors are enabled AND money sponsors are accepted
                if (!@event.AreSponsorsEnabled())
                    return Result<CreateMoneySponsorResult>.Failure("Sponsorships are not enabled for this event");

                if (!@event.SponsorConfig!.AcceptMoneySponsors)
                    return Result<CreateMoneySponsorResult>.Failure("Money sponsorships are not accepted for this event");

                // 4. Validate sponsor amount (C3 Guard: always check > 0)
                if (request.Amount <= 0)
                    return Result<CreateMoneySponsorResult>.Failure("Sponsorship amount must be greater than zero");

                var validateAmountResult = @event.SponsorConfig.ValidateMoneyAmount(request.Amount);
                if (validateAmountResult.IsFailure)
                    return Result<CreateMoneySponsorResult>.Failure(validateAmountResult.Error);

                // 5. Parse currency
                if (!MoneyBuilder.TryParseCurrency(request.Currency, out var currency))
                    return Result<CreateMoneySponsorResult>.Failure($"Invalid currency: {request.Currency}");

                // 6. Create Money and Sponsor entity
                var amountResult = MoneyBuilder.Create(request.Amount, currency);
                if (amountResult.IsFailure)
                    return Result<CreateMoneySponsorResult>.Failure(amountResult.Error);

                var sponsorResult = Sponsor.CreateMoneySponsor(
                    request.EventId,
                    request.UserId,
                    request.SponsorName,
                    request.SponsorEmail,
                    request.SponsorPhone,
                    request.SponsorOrganization,
                    request.SponsorNotes,
                    amountResult.Value);

                if (sponsorResult.IsFailure)
                    return Result<CreateMoneySponsorResult>.Failure(sponsorResult.Error);

                var sponsor = sponsorResult.Value;

                // 7. Create Stripe Checkout session
                var checkoutRequest = new CreateSponsorCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    SponsorId = sponsor.Id,
                    EventTitle = @event.Title.Value,
                    SponsorOrganization = request.SponsorOrganization,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "sponsor" },
                        { "event_id", request.EventId.ToString() },
                        { "sponsor_id", sponsor.Id.ToString() },
                        { "sponsor_user_id", request.UserId?.ToString() ?? "anonymous" }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreateSponsorCheckoutSessionAsync(checkoutRequest, cancellationToken);
                if (checkoutResult.IsFailure)
                    return Result<CreateMoneySponsorResult>.Failure($"Failed to create payment session: {checkoutResult.Error}");

                // 8. Set Stripe session on sponsor
                var setSessionResult = sponsor.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    checkoutResult.Value.ExpiresAt);
                if (setSessionResult.IsFailure)
                    return Result<CreateMoneySponsorResult>.Failure(setSessionResult.Error);

                // 9. Calculate and store revenue breakdown
                try
                {
                    var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                        amountResult.Value,
                        @event.Location,
                        cancellationToken);

                    if (breakdownResult.IsSuccess)
                    {
                        sponsor.SetRevenueBreakdown(
                            breakdownResult.Value.StripeFeeAmount,
                            breakdownResult.Value.PlatformCommission,
                            breakdownResult.Value.OrganizerPayout);

                        _logger.LogInformation(
                            "Revenue breakdown calculated for sponsor {SponsorId}: StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                            sponsor.Id,
                            breakdownResult.Value.StripeFeeAmount.Amount,
                            breakdownResult.Value.PlatformCommission.Amount,
                            breakdownResult.Value.OrganizerPayout.Amount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Revenue breakdown calculation failed for sponsor {SponsorId}: {Error}",
                            sponsor.Id, breakdownResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception calculating revenue breakdown for sponsor {SponsorId}. Sponsor will continue without breakdown.",
                        sponsor.Id);
                }

                // 10. Save sponsor
                await _sponsorRepository.AddAsync(sponsor, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateMoneySponsor COMPLETE: SponsorId={SponsorId}, EventId={EventId}, Amount={Amount}, Duration={ElapsedMs}ms",
                    sponsor.Id, request.EventId, request.Amount, stopwatch.ElapsedMilliseconds);

                return Result<CreateMoneySponsorResult>.Success(
                    new CreateMoneySponsorResult(checkoutResult.Value.CheckoutUrl, sponsor.Id));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateMoneySponsor FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<CreateMoneySponsorResult>.Failure($"Money sponsorship creation failed: {ex.Message}");
            }
        }
    }
}

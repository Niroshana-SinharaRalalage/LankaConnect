using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateDonation;

/// <summary>
/// Handles standalone donation creation from the event details page.
/// Flow: validate event + donations enabled → validate amount → create Donation entity →
/// create Stripe Checkout → set session → calculate revenue → save → return checkout URL
/// </summary>
public class CreateDonationCommandHandler : ICommandHandler<CreateDonationCommand, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<CreateDonationCommandHandler> _logger;

    public CreateDonationCommandHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<CreateDonationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(CreateDonationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateDonation"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateDonation START: EventId={EventId}, DonorEmail={DonorEmail}, Amount={Amount}, Currency={Currency}, IsAnonymous={IsAnonymous}",
                request.EventId, request.DonorEmail, request.Amount, request.Currency, request.UserId == null);

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<string>.Failure("Event not found");

                // 2. Validate event is Published (cannot donate to Draft/Cancelled events)
                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<string>.Failure("Donations are only available for published events");

                // 3. Validate donations are enabled
                if (!@event.AreDonationsEnabled())
                    return Result<string>.Failure("Donations are not enabled for this event");

                // 4. Validate donation amount (C3 Guard: always check > 0)
                if (request.Amount <= 0)
                    return Result<string>.Failure("Donation amount must be greater than zero");

                var validateAmountResult = @event.ValidateDonationAmount(request.Amount);
                if (validateAmountResult.IsFailure)
                    return Result<string>.Failure(validateAmountResult.Error);

                // 5. Parse currency
                if (!MoneyBuilder.TryParseCurrency(request.Currency, out var currency))
                    return Result<string>.Failure($"Invalid currency: {request.Currency}");

                // 6. Create Money and Donation entity
                var amountResult = MoneyBuilder.Create(request.Amount, currency);
                if (amountResult.IsFailure)
                    return Result<string>.Failure(amountResult.Error);

                var donationResult = Donation.Create(
                    request.EventId,
                    request.UserId,
                    request.DonorName,
                    request.DonorEmail,
                    request.DonorPhone,
                    request.DonorNotes,
                    amountResult.Value);

                if (donationResult.IsFailure)
                    return Result<string>.Failure(donationResult.Error);

                var donation = donationResult.Value;

                // 7. Create Stripe Checkout session
                var checkoutRequest = new CreateDonationCheckoutSessionRequest
                {
                    EventId = request.EventId,
                    DonationId = donation.Id,
                    EventTitle = @event.Title.Value,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "donation" },
                        { "event_id", request.EventId.ToString() },
                        { "donation_id", donation.Id.ToString() },
                        { "donor_user_id", request.UserId?.ToString() ?? "anonymous" }
                    }
                };

                var checkoutResult = await _stripePaymentService.CreateDonationCheckoutSessionAsync(checkoutRequest, cancellationToken);
                if (checkoutResult.IsFailure)
                    return Result<string>.Failure($"Failed to create payment session: {checkoutResult.Error}");

                // 8. Set Stripe session on donation
                var setSessionResult = donation.SetStripeCheckoutSession(
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
                        donation.SetRevenueBreakdown(
                            breakdownResult.Value.StripeFeeAmount,
                            breakdownResult.Value.PlatformCommission,
                            breakdownResult.Value.OrganizerPayout);

                        _logger.LogInformation(
                            "Revenue breakdown calculated for donation {DonationId}: StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                            donation.Id,
                            breakdownResult.Value.StripeFeeAmount.Amount,
                            breakdownResult.Value.PlatformCommission.Amount,
                            breakdownResult.Value.OrganizerPayout.Amount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Revenue breakdown calculation failed for donation {DonationId}: {Error}",
                            donation.Id, breakdownResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception calculating revenue breakdown for donation {DonationId}. Donation will continue without breakdown.",
                        donation.Id);
                }

                // 10. Save donation
                await _donationRepository.AddAsync(donation, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateDonation COMPLETE: DonationId={DonationId}, EventId={EventId}, Amount={Amount}, Duration={ElapsedMs}ms",
                    donation.Id, request.EventId, request.Amount, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(checkoutResult.Value.CheckoutUrl);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateDonation FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<string>.Failure($"Donation creation failed: {ex.Message}");
            }
        }
    }
}

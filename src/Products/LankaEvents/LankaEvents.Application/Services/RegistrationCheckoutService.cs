using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Phase 7E.3b implementation of <see cref="IRegistrationCheckoutService"/>. Builds a
/// single-line-item Stripe Checkout session for the registration's <c>TotalPrice</c>,
/// stores the resulting session ID on the registration, and returns the redirect URL.
///
/// Observability: structured logging via <see cref="LogContext"/> mirrors the existing
/// patterns in <c>RsvpToEventCommandHandler</c> + <c>CreateDonationCommandHandler</c>.
/// Every failure path emits a warning with the registration ID for traceability.
/// </summary>
public class RegistrationCheckoutService : IRegistrationCheckoutService
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<RegistrationCheckoutService> _logger;

    public RegistrationCheckoutService(
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<RegistrationCheckoutService> logger)
    {
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string>> CreateSessionForRegistrationAsync(
        Event @event,
        Registration registration,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "RegistrationCheckoutService.CreateSession"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", @event.Id))
        using (LogContext.PushProperty("RegistrationId", registration.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Argument validation — defensive even though callers already check.
                if (registration.TotalPrice == null || registration.TotalPrice.Amount <= 0)
                    return Result<string>.Failure(
                        "Cannot create Stripe checkout for a registration without a positive TotalPrice. " +
                        $"RegistrationId={registration.Id}");

                if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
                    return Result<string>.Failure("Success and Cancel URLs are required for paid events");

                _logger.LogInformation(
                    "CreateSession START: EventId={EventId}, RegistrationId={RegistrationId}, " +
                    "Amount={Amount} {Currency}, RegistrationMode={Mode}",
                    @event.Id, registration.Id,
                    registration.TotalPrice.Amount, registration.TotalPrice.Currency,
                    registration.RegistrationMode);

                // Compute revenue breakdown (tax / Stripe fee / commission / payout) and store it
                // on the registration. Mirrors HandleMultiAttendeeRsvp's revenue-breakdown step so
                // dashboards + exports + organiser-payout calculations work identically for B-mode.
                try
                {
                    var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                        registration.TotalPrice, @event.Location, ct);

                    if (breakdownResult.IsSuccess)
                    {
                        registration.SetRevenueBreakdown(breakdownResult.Value);
                        _logger.LogInformation(
                            "Revenue breakdown set: RegId={RegId}, Tax={Tax}, StripeFee={Fee}, " +
                            "Commission={Commission}, Payout={Payout}",
                            registration.Id,
                            breakdownResult.Value.SalesTaxAmount.Amount,
                            breakdownResult.Value.StripeFeeAmount.Amount,
                            breakdownResult.Value.PlatformCommission.Amount,
                            breakdownResult.Value.OrganizerPayout.Amount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Revenue breakdown calculation failed (continuing without): " +
                            "RegId={RegId}, Error={Error}",
                            registration.Id, breakdownResult.Error);
                    }
                }
                catch (Exception breakdownEx)
                {
                    // Non-blocking — registration can proceed without breakdown; revenue dashboards
                    // will fall back to gross amount.
                    _logger.LogError(breakdownEx,
                        "Revenue breakdown threw — continuing without. RegId={RegId}",
                        registration.Id);
                }

                // Build the single-line-item Stripe Checkout request.
                var checkoutRequest = new CreateEventCheckoutSessionRequest
                {
                    EventId = @event.Id,
                    RegistrationId = registration.Id,
                    EventTitle = @event.Title.Value,
                    Amount = registration.TotalPrice.Amount,
                    Currency = registration.TotalPrice.Currency.ToString(),
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "event_id", @event.Id.ToString() },
                        { "registration_id", registration.Id.ToString() },
                        { "registration_mode", registration.RegistrationMode.ToString() },
                    },
                };

                if (registration.UserId.HasValue && registration.UserId.Value != Guid.Empty)
                    checkoutRequest.Metadata["user_id"] = registration.UserId.Value.ToString();

                var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(
                    checkoutRequest, ct);

                if (checkoutResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Stripe session creation FAILED: RegId={RegId}, Error={Error}, Duration={Ms}ms",
                        registration.Id, checkoutResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<string>.Failure(
                        $"Failed to create payment session: {checkoutResult.Error}");
                }

                // Store session ID on the registration so the webhook handler can correlate
                // when payment completes.
                var setSessionResult = registration.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId, checkoutResult.Value.ExpiresAt);
                if (setSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "SetStripeCheckoutSession FAILED: RegId={RegId}, Error={Error}",
                        registration.Id, setSessionResult.Error);
                    return Result<string>.Failure(setSessionResult.Error);
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "CreateSession COMPLETE: RegId={RegId}, SessionId={SessionId}, Duration={Ms}ms",
                    registration.Id, checkoutResult.Value.SessionId, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(checkoutResult.Value.CheckoutUrl);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreateSession FAILED with unhandled exception: RegId={RegId}, Duration={Ms}ms",
                    registration.Id, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

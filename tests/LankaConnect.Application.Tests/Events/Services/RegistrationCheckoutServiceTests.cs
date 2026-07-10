using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 7E.3b — unit tests for <see cref="RegistrationCheckoutService"/>. The architect required
/// a single test surface for the money path so the auth + anonymous handlers don't fork. These
/// tests assert:
/// <list type="bullet">
/// <item>The Stripe checkout request carries the EXACT registration TotalPrice amount (cents-exact).</item>
/// <item>The session ID Stripe returns is stored on the registration via SetStripeCheckoutSession.</item>
/// <item>Failures in revenue-breakdown are non-blocking (we still create the session).</item>
/// <item>Failures in Stripe session creation propagate as Result.Failure.</item>
/// <item>Argument-validation rejects invalid inputs (no TotalPrice, missing URLs).</item>
/// </list>
/// </summary>
public class RegistrationCheckoutServiceTests
{
    private readonly Mock<IStripePaymentService> _stripeService = new();
    private readonly Mock<IRevenueCalculatorService> _revenueService = new();
    private readonly Mock<ILogger<RegistrationCheckoutService>> _logger = new();

    public RegistrationCheckoutServiceTests()
    {
        // Default: revenue calc succeeds with a benign breakdown so it doesn't get in the way.
        _revenueService
            .Setup(s => s.CalculateBreakdownAsync(It.IsAny<Money>(), It.IsAny<EventLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBreakdown(50m));
    }

    private RegistrationCheckoutService BuildSut() => new(
        _stripeService.Object, _revenueService.Object, _logger.Object);

    private static Result<RevenueBreakdown> BuildBreakdown(decimal grossAmount)
    {
        var gross = Money.Create(grossAmount, Currency.USD).Value;
        return RevenueBreakdown.Create(grossAmount: gross, salesTaxRate: 0m);
    }

    private static Event CreatePaidPublishedEvent(decimal price)
    {
        var title = EventTitle.Create("Checkout svc test").Value;
        var description = EventDescription.Create("7E.3b").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), 100).Value;
        ev.SetPricing(Money.Create(price, Currency.USD).Value).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        return ev;
    }

    private static Registration CreatePaidPreliminaryHeadCountRegistration(Event ev, decimal totalPriceAmount, RegistrationMode mode = RegistrationMode.HeadCountByAge)
    {
        ev.SetRegistrationMode(mode).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByAge(2, 0).Value;
        var contact = RegistrationContact.Create("test@example.com", "555-0100", null).Value;

        // Build the registration via the public domain method so its state is realistic.
        var price = Money.Create(totalPriceAmount, Currency.USD).Value;
        var reg = Registration.CreateWithHeadCount(
            ev.Id, Guid.NewGuid(), mode, "Lead", head, contact,
            price, isPaidEvent: true).Value;
        return reg;
    }

    [Fact]
    public async Task CreateSession_PassesExactRegistrationTotalPrice_ToStripeService()
    {
        // Architect-required: the Stripe request must carry the EXACT TotalPrice amount.
        var ev = CreatePaidPublishedEvent(15m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 37m);

        CreateEventCheckoutSessionRequest? captured = null;
        _stripeService
            .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateEventCheckoutSessionRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(Result<EventCheckoutResult>.Success(new EventCheckoutResult
            {
                SessionId = "cs_test_123",
                CheckoutUrl = "https://checkout.stripe.com/c/cs_test_123",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
            }));

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, "https://staging/success", "https://staging/cancel");

        result.IsSuccess.Should().BeTrue($"errors: {result.Error}");
        result.Value.Should().Be("https://checkout.stripe.com/c/cs_test_123");
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(37m, "Stripe must receive the exact TotalPrice (no rounding)");
        captured.Currency.Should().Be("USD");
        captured.RegistrationId.Should().Be(reg.Id);
        captured.EventId.Should().Be(ev.Id);
        captured.Metadata.Should().ContainKey("registration_mode")
            .WhoseValue.Should().Be("HeadCountByAge");
    }

    [Fact]
    public async Task CreateSession_StoresSessionIdOnRegistration_OnSuccess()
    {
        var ev = CreatePaidPublishedEvent(10m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 30m);

        var expiresAt = DateTime.UtcNow.AddHours(24);
        _stripeService
            .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventCheckoutResult>.Success(new EventCheckoutResult
            {
                SessionId = "cs_stored",
                CheckoutUrl = "https://checkout.stripe.com/c/cs_stored",
                ExpiresAt = expiresAt,
            }));

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, "https://staging/success", "https://staging/cancel");

        result.IsSuccess.Should().BeTrue();
        reg.StripeCheckoutSessionId.Should().Be("cs_stored",
            "the service must persist the session ID so the webhook can correlate");
    }

    [Fact]
    public async Task CreateSession_PropagatesFailure_WhenStripeServiceFails()
    {
        var ev = CreatePaidPublishedEvent(10m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 30m);

        _stripeService
            .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventCheckoutResult>.Failure("Stripe rate limit exceeded"));

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, "https://staging/success", "https://staging/cancel");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stripe rate limit exceeded");
    }

    [Fact]
    public async Task CreateSession_StillSucceeds_WhenRevenueBreakdownFails()
    {
        // Revenue breakdown is non-blocking — if the calculator throws, registration must
        // still be able to redirect to Stripe.
        var ev = CreatePaidPublishedEvent(10m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 30m);

        _revenueService
            .Setup(s => s.CalculateBreakdownAsync(It.IsAny<Money>(), It.IsAny<EventLocation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("tax service down"));

        _stripeService
            .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventCheckoutResult>.Success(new EventCheckoutResult
            {
                SessionId = "cs_x", CheckoutUrl = "https://staging/cs_x", ExpiresAt = DateTime.UtcNow.AddHours(1),
            }));

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, "https://staging/success", "https://staging/cancel");

        result.IsSuccess.Should().BeTrue("registration must NOT fail because tax calc failed");
    }

    [Fact]
    public async Task CreateSession_Rejects_RegistrationWithoutTotalPrice()
    {
        var ev = CreatePaidPublishedEvent(10m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 0m);
        // Force TotalPrice = 0 (free) — service must reject explicit-zero too.

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, "https://staging/success", "https://staging/cancel");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("TotalPrice");
    }

    [Fact]
    public async Task CreateSession_Rejects_MissingUrls()
    {
        var ev = CreatePaidPublishedEvent(10m);
        var reg = CreatePaidPreliminaryHeadCountRegistration(ev, totalPriceAmount: 30m);

        var result = await BuildSut().CreateSessionForRegistrationAsync(
            ev, reg, successUrl: "", cancelUrl: "https://staging/cancel");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("URLs are required");
    }
}

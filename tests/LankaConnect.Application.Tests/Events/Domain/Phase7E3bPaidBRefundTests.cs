using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.3b — architect-required (review iteration 1, edit #4) refund regression test
/// for paid Mode-B registrations. The existing refund handler is mode-agnostic — it reads
/// <see cref="Registration.TotalPrice"/> only. This test guards against a future change
/// silently breaking that invariant.
///
/// Architect direction: ONE test, exercises the existing handler against a paid B-mode
/// registration. Don't ship new refund functionality.
/// </summary>
public class Phase7E3bPaidBRefundTests
{
    [Fact]
    public void RefundHandler_PaidBRegistration_RefundsTotalPrice_Successfully()
    {
        // Arrange — build a paid Mode-B registration in Confirmed + Completed-payment state.
        var registration = CreatePaidBModeRegistration_Completed();

        // Pre-conditions: registration must look like a finished payment from a HeadCountByAge event.
        registration.RegistrationMode.Should().Be(RegistrationMode.HeadCountByAge);
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
        registration.PaymentStatus.Should().Be(PaymentStatus.Completed);
        registration.TotalPrice!.Amount.Should().Be(37m);

        // Act — the same domain method the existing refund pipeline uses (no mode-specific code).
        var result = registration.RequestRefund();

        // Assert
        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        registration.Status.Should().Be(RegistrationStatus.RefundRequested,
            "paid B-mode refund flow must transition through the same RefundRequested state as Mode A");
        registration.RefundRequestedAt.Should().NotBeNull();
        registration.RefundRequestedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        registration.TotalPrice!.Amount.Should().Be(37m,
            "RequestRefund must NOT mutate TotalPrice — Stripe refund amount is computed from this");
    }

    private static Registration CreatePaidBModeRegistration_Completed()
    {
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value; // 2×$15 + 1×$7 = $37
        var contact = RegistrationContact.Create("test@example.com", "555-0100", null).Value;
        var price = Money.Create(37m, Currency.USD).Value;

        var registration = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAge,
            "Lead",
            head, contact,
            price,
            isPaidEvent: true).Value;

        // Walk it through the paid lifecycle: Preliminary → Stripe checkout → Confirmed via webhook.
        registration.SetStripeCheckoutSession("cs_test_b_refund").IsSuccess.Should().BeTrue();
        registration.CompletePayment("pi_test_b_refund").IsSuccess.Should().BeTrue();
        registration.ClearDomainEvents();
        return registration;
    }
}

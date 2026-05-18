using FluentAssertions;
using Xunit;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Payments.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.148.D9 (Wave 3): pins the SponsorWebhookHandler dedupe guard.
///
/// When a refund came through the new approval workflow, the consolidated D8
/// "decision" email already covered the attendee — the legacy per-Sponsor
/// "Sponsorship Refund Confirmation" email is redundant and creates the operator
/// UAT defect E3 (attendee gets a separate "$125 Sponsor refund" email that
/// looks like a final authoritative total competing with the consolidated $255
/// decision email).
///
/// Detection: IRefundRequestRepository.ExistsWorkflowLineItemForSponsorAsync.
/// Fail-OPEN: if the lookup throws, default to sending the legacy email so a
/// transient DB issue never silences a legitimate notification.
///
/// Load-bearing behavioural assertion: did the fire-and-forget email path even
/// START? Detected via IServiceScopeFactory.CreateScope() invocation — the very
/// first thing the email task does. If the guard returned early, CreateScope is
/// never called; if the guard let the path through, CreateScope IS called.
/// </summary>
public class SponsorWebhookHandlerD9Tests
{
    private readonly Mock<ISponsorRepository> _sponsorRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepo = new();

    private SponsorWebhookHandler BuildHandler() =>
        new SponsorWebhookHandler(
            _sponsorRepo.Object,
            _unitOfWork.Object,
            _scopeFactory.Object,
            _refundRequestRepo.Object,
            Mock.Of<ILogger<SponsorWebhookHandler>>());

    [Fact]
    public async Task WorkflowOwnedRefund_SuppressesStandaloneEmail()
    {
        var sponsor = SponsorCompleted();
        var refundId = "re_workflow_match";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());

        _scopeFactory.Verify(s => s.CreateScope(), Times.Never,
            "guard must short-circuit before the fire-and-forget email block creates a DI scope");
    }

    [Fact]
    public async Task NonWorkflowRefund_SendsStandaloneEmail_RegressionGuard()
    {
        var sponsor = SponsorCompleted();
        var refundId = "re_legacy_only";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());
        // Fire-and-forget Task.Run for the email block — yield the scheduler so it actually executes.
        await Task.Delay(100);

        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "legacy path must still send the standalone email when no workflow line-item exists");
    }

    [Fact]
    public async Task WorkflowLookupThrows_DefaultsToSendingStandaloneEmail_FailOpenGuardrail()
    {
        var sponsor = SponsorCompleted();
        var refundId = "re_lookup_throws";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated transient DB error"));

        var act = async () => await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());

        await act.Should().NotThrowAsync("guard must catch lookup exception and fall through to the legacy email path");
        // Fire-and-forget Task.Run for the email block — yield the scheduler so it actually executes.
        await Task.Delay(100);

        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "fail-OPEN: when the lookup throws, the legacy email must still be sent so we don't silence the notification");
    }

    [Fact]
    public async Task TwoSponsorsRefundedInOneWorkflow_BothSuppressed()
    {
        // Both sponsors share a workflow refund — verify each invocation independently
        // short-circuits to ensure we don't have any cross-state leak between calls.
        var sponsor1 = SponsorCompleted("pi_aaa");
        var sponsor2 = SponsorCompleted("pi_bbb");
        _sponsorRepo.SetupSequence(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor1)
            .ReturnsAsync(sponsor2);
        _refundRequestRepo.Setup(r => r.ExistsWorkflowLineItemForSponsorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = BuildHandler();
        await handler.HandleChargeRefundedAsync("pi_aaa", "re_workflow_1", Guid.NewGuid());
        await handler.HandleChargeRefundedAsync("pi_bbb", "re_workflow_2", Guid.NewGuid());

        _scopeFactory.Verify(s => s.CreateScope(), Times.Never,
            "neither sponsor invocation should leak through to the email path");
    }

    [Fact]
    public async Task DifferentStripeRefundId_NoFalsePositiveOnCrossRefundCollision()
    {
        // Sponsor was previously refunded via the workflow under refundId "re_old", but
        // the webhook now fires for a different refundId "re_new". The repo predicate
        // matches (SponsorId, StripeRefundId) so it returns false → legacy path runs.
        var sponsor = SponsorCompleted();
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, "re_new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, "re_new", Guid.NewGuid());
        // Fire-and-forget Task.Run for the email block — yield the scheduler so it actually executes.
        await Task.Delay(100);

        _refundRequestRepo.Verify(r => r.ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, "re_new", It.IsAny<CancellationToken>()),
            Times.Once);
        _scopeFactory.Verify(s => s.CreateScope(), Times.Once,
            "predicate must scope to (sponsorId + stripeRefundId) so unrelated refundIds don't get suppressed");
    }

    [Fact]
    public async Task SponsorNotFound_ReturnsEarly_BeforeReachingGuard()
    {
        // Defensive regression: if the sponsor lookup misses, the legacy code returned
        // early (before the email block) — D9 must preserve this behaviour. Repository
        // dedupe lookup is NOT invoked because we never reached the guard.
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sponsor?)null);

        await BuildHandler().HandleChargeRefundedAsync("pi_orphan", "re_orphan", Guid.NewGuid());

        _refundRequestRepo.Verify(
            r => r.ExistsWorkflowLineItemForSponsorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "guard must not fire when no sponsor was loaded");
        _scopeFactory.Verify(s => s.CreateScope(), Times.Never,
            "email path must not fire when no sponsor was loaded");
    }

    // -------------------------------------------------------------------------
    // Fixture
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a money sponsor that has completed payment (StripePaymentIntentId set,
    /// Status = Completed) so MarkAsRefunded inside the handler succeeds.
    /// </summary>
    private static Sponsor SponsorCompleted(string paymentIntentId = "pi_test_default")
    {
        var sponsor = Sponsor.CreateMoneySponsor(
            eventId: Guid.NewGuid(),
            sponsorUserId: Guid.NewGuid(),
            sponsorName: "Test Sponsor",
            sponsorEmail: "sponsor@example.com",
            sponsorPhone: "+1-555-1234",
            sponsorOrganization: "Test Corp",
            sponsorNotes: "",
            amount: Money.Create(125m, Currency.USD).Value).Value;
        sponsor.CompletePayment(paymentIntentId);
        return sponsor;
    }
}

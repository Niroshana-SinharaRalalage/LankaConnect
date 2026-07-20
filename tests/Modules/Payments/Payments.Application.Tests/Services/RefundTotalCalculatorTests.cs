using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using LankaConnect.Modules.Payments.Application.Services; // W4.4.c.4: service impls moved here (interfaces stay in legacy)
using FluentAssertions;
// Wave 8.5.e (2026-07-19): IRefundRequestRepository + IRefundTotalCalculator promoted
// from LankaEvents.Application/Domain to LankaEvents.Contracts in Wave 8.5.d.
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.SharedKernel.Money;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Modules.Payments.Application.Tests.Services;

/// <summary>
/// Phase 6A.148.W5.6.A â€” pins the contract of <see cref="IRefundTotalCalculator"/>.
///
/// Operator UAT context (2026-05-22): refund request 329f2505 approved $262 across
/// 4 lines (Ticket $80 + AddOn $7 + Sponsor $100 + Sponsor $75). The
/// <c>RefundCompletedEvent</c> however carries only the legacy
/// (Registration.TotalPrice + AddOnRefundAmount) = ($80 + $0) = $80, because Sponsor
/// + Collection refund totals never land on Registration columns under the 6A.148
/// workflow model. Result: completion email said "$80" when the truth was "$262".
///
/// The calculator is the handler-side aggregation that closes that gap. These four
/// tests pin the behaviour the operator depends on:
///
/// 1. Workflow refund: returns the SUM of all Refunded line items' ApprovedAmount.
/// 2. Legacy refund (no workflow line): returns the caller's legacy total verbatim
///    (preserves byte-identical pre-6A.148 email behaviour).
/// 3. Repo throws: returns the legacy total (fail-OPEN â€” a transient DB blip never
///    silences the completion email).
/// 4. Partial Refunded (one line still Processing or Failed): only Refunded lines
///    contribute â€” the email never over-promises money that hasn't moved.
/// </summary>
public class RefundTotalCalculatorTests
{
    private readonly Mock<IRefundRequestRepository> _repo = new();

    private RefundTotalCalculator Build()
        => new(_repo.Object, Mock.Of<ILogger<RefundTotalCalculator>>());

    [Fact]
    public async Task Workflow_AllFourLinesRefunded_ReturnsAggregatedTotal()
    {
        // The exact operator-UAT shape: 4 Refunded lines totalling $262, legacy formula
        // would have said $80.
        const string ticketSri = "re_3Ta75VLvfbr023L11Ki1Huqw";
        var rrId = Guid.NewGuid();
        var ticketLine = BuildRefundedLine(rrId, RefundLineItemType.Ticket, 80m, ticketSri);
        var rr = BuildRefundRequest(rrId, new[]
        {
            ticketLine,
            BuildRefundedLine(rrId, RefundLineItemType.AddOn,   7m,   "re_addon_7"),
            BuildRefundedLine(rrId, RefundLineItemType.Sponsor, 100m, "re_sponsor_100"),
            BuildRefundedLine(rrId, RefundLineItemType.Sponsor, 75m,  "re_sponsor_75"),
        });

        _repo.Setup(r => r.GetWorkflowLineByStripeRefundIdAsync(ticketSri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketLine);
        _repo.Setup(r => r.GetByIdAsync(rrId, It.IsAny<CancellationToken>())).ReturnsAsync(rr);

        var actual = await Build().ComputeAttendeeFacingTotalAsync(
            ticketSri, legacyFallbackTotal: 80m);

        actual.Should().Be(262m, "operator UAT: 4 Refunded lines totalling $262, not the $80 legacy formula");
    }

    [Fact]
    public async Task Legacy_NoWorkflowLine_ReturnsLegacyFallbackVerbatim()
    {
        // Pre-6A.148 direct-Stripe CancelRsvp path: no workflow line exists for the
        // refund id. The caller's (RefundAmount + AddOnRefundAmount) formula was the
        // RIGHT answer for that path before workflow existed, and remains right.
        const string sri = "re_legacy_001";
        _repo.Setup(r => r.GetWorkflowLineByStripeRefundIdAsync(sri, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundRequestLineItem?)null);

        var actual = await Build().ComputeAttendeeFacingTotalAsync(sri, legacyFallbackTotal: 123.45m);

        actual.Should().Be(123.45m, "legacy refunds use the caller's existing formula unchanged");
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never, "no need to load the RR when no workflow line exists");
    }

    [Fact]
    public async Task RepoThrows_FailsOpen_ReturnsLegacyFallback()
    {
        // A transient DB blip on the lookup must NEVER silence the completion email.
        // The legacy total is a valid number (correct for legacy paths; partial-correct
        // for workflow paths). Better to under-display than to never display.
        const string sri = "re_db_blip_001";
        _repo.Setup(r => r.GetWorkflowLineByStripeRefundIdAsync(sri, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated transient DB outage"));

        var actual = await Build().ComputeAttendeeFacingTotalAsync(sri, legacyFallbackTotal: 50m);

        actual.Should().Be(50m, "fail-OPEN â€” return legacy total when repo lookup throws");
    }

    [Fact]
    public async Task Workflow_PartialRefunded_SumsOnlyRefundedLines()
    {
        // Architect timing guarantee says all lines should be terminal by the time
        // RefundCompletedEvent fires from the ticket webhook. This test ensures that
        // IF a sibling line is in Processing or Failed (defensive â€” shouldn't happen
        // in practice but the calculator must not over-promise), it is EXCLUDED from
        // the total.
        const string ticketSri = "re_partial_ticket";
        var rrId = Guid.NewGuid();
        var ticketLine = BuildRefundedLine(rrId, RefundLineItemType.Ticket, 80m, ticketSri);
        var failedSponsor = BuildLine(rrId, RefundLineItemType.Sponsor, 100m,
            stripeRefundId: "re_sponsor_failed",
            status: RefundLineItemStatus.Failed,
            approvedAmount: 100m);
        var processingAddOn = BuildLine(rrId, RefundLineItemType.AddOn, 50m,
            stripeRefundId: "re_addon_inflight",
            status: RefundLineItemStatus.Processing,
            approvedAmount: 50m);

        var rr = BuildRefundRequest(rrId, new[] { ticketLine, failedSponsor, processingAddOn });

        _repo.Setup(r => r.GetWorkflowLineByStripeRefundIdAsync(ticketSri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketLine);
        _repo.Setup(r => r.GetByIdAsync(rrId, It.IsAny<CancellationToken>())).ReturnsAsync(rr);

        var actual = await Build().ComputeAttendeeFacingTotalAsync(
            ticketSri, legacyFallbackTotal: 80m);

        actual.Should().Be(80m,
            "only Refunded lines contribute â€” the $100 Failed sponsor and $50 Processing addon are excluded");
    }

    // ========================================================================
    // Helpers â€” build RefundRequestLineItem + RefundRequest via reflection
    // ========================================================================

    private static RefundRequestLineItem BuildRefundedLine(
        Guid rrId, RefundLineItemType type, decimal amount, string stripeRefundId)
        => BuildLine(rrId, type, amount, stripeRefundId,
            status: RefundLineItemStatus.Refunded, approvedAmount: amount);

    private static RefundRequestLineItem BuildLine(
        Guid rrId,
        RefundLineItemType type,
        decimal requestedAmount,
        string stripeRefundId,
        RefundLineItemStatus status,
        decimal? approvedAmount)
    {
        var li = (RefundRequestLineItem)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(RefundRequestLineItem));
        SetProp(li, nameof(li.Id), Guid.NewGuid());
        SetProp(li, nameof(li.RefundRequestId), rrId);
        SetProp(li, nameof(li.Type), type);
        SetProp(li, nameof(li.ReferenceId), Guid.NewGuid());
        SetProp(li, nameof(li.Status), status);
        SetProp(li, nameof(li.StripeRefundId), stripeRefundId);
        SetProp(li, nameof(li.RequestedAmount),
            new Money(requestedAmount, Currency.USD));
        if (approvedAmount.HasValue)
            SetProp(li, nameof(li.ApprovedAmount),
                new Money(approvedAmount.Value, Currency.USD));
        return li;
    }

    private static RefundRequest BuildRefundRequest(Guid id, IEnumerable<RefundRequestLineItem> lines)
    {
        var rr = (RefundRequest)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(RefundRequest));
        SetProp(rr, nameof(rr.Id), id);
        SetProp(rr, nameof(rr.Status), RefundRequestStatus.Completed);
        // Backing field _lineItems is what the public LineItems IReadOnlyList<T> wraps.
        var field = typeof(RefundRequest).GetField("_lineItems",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(rr, lines.ToList());
        return rr;
    }

    private static void SetProp(object target, string name, object? value)
    {
        var prop = target.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(target, value);
    }
}

using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using FluentAssertions;
using Xunit;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Payments.Services;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.148.D9 (Wave 3) — pins the SponsorWebhookHandler dedupe guard.
///
/// Phase 6A.148.W5.6.B Phase 3 — refines the suppression predicate from "is workflow-owned"
/// to "is workflow-owned AND sponsor email == attendee email". The earlier behaviour
/// suppressed the standalone email for ALL workflow refunds, but the consolidated D8
/// decision email goes only to the attendee — a third-party money sponsor with a
/// different email would have received NO notification of their refund (operator UAT bug 1).
///
/// Phase 6A.148.W5.6.B.OBS3 — the suppression branch now ALSO writes a row to
/// communications.email_dispatch_log (via IRefundDispatchAuditService) so operators
/// can distinguish "deliberately suppressed" from "send failed silently." The test
/// behavioural assertions now key off which SERVICE was resolved from the scope
/// (audit service for suppress, email service for send) rather than off
/// IServiceScopeFactory.CreateScope() invocation counts (which can be ambiguous
/// when both branches use the scope factory).
/// </summary>
public class SponsorWebhookHandlerD9Tests
{
    private const string SponsorEmail = "sponsor@example.com";

    private readonly Mock<ISponsorRepository> _sponsorRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepo = new();
    private readonly Mock<IRefundDispatchAuditService> _auditService = new();
    private readonly Mock<ITypedEmailService> _emailService = new();
    private readonly Mock<IEventRepository> _eventRepo = new();

    private SponsorWebhookHandler BuildHandler()
    {
        // Real ServiceProvider so CreateScope() returns a real IServiceScope whose
        // ServiceProvider resolves our mock services (the way production runs them).
        var services = new ServiceCollection();
        services.AddSingleton(_auditService.Object);
        services.AddSingleton(_emailService.Object);
        services.AddSingleton(_eventRepo.Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new SponsorWebhookHandler(
            _sponsorRepo.Object,
            _unitOfWork.Object,
            scopeFactory,
            _refundRequestRepo.Object,
            Mock.Of<ILogger<SponsorWebhookHandler>>());
    }

    [Fact]
    public async Task WorkflowOwnedRefund_SameEmail_SuppressesStandaloneEmail_AndWritesAuditRow()
    {
        var sponsor = SponsorCompleted(sponsorEmail: SponsorEmail);
        var refundId = "re_workflow_match";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SponsorEmail);

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());

        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(),
            sponsor.SponsorEmail,
            sponsor.SponsorName,
            It.Is<string>(reason => reason.Contains("workflow-owned")),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            "Sponsor",
            sponsor.Id,
            It.IsAny<CancellationToken>()),
            Times.Once,
            "same-email workflow-owned refund: suppression branch must write audit row");
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "suppression branch must NOT send the standalone email");
    }

    [Fact]
    public async Task WorkflowOwnedRefund_DifferentSponsorEmail_StillSendsStandaloneEmail()
    {
        // Phase 6A.148.W5.6.B Phase 3 — third-party money sponsor with email DIFFERENT
        // from attendee's. The consolidated D8 decision email goes to attendee only;
        // suppressing the standalone here would silence the third party's only refund
        // notification. MUST fall through to send.
        var sponsor = SponsorCompleted(sponsorEmail: "third-party-sponsor@example.com");
        var refundId = "re_workflow_third_party";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("attendee@example.com");
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());
        await Task.Delay(200); // fire-and-forget

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "third-party sponsor (different email) is the SOLE recipient of any refund notification — standalone email MUST fire");
        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Never,
            "non-suppressed path must not write a suppression audit row");
    }

    [Fact]
    public async Task WorkflowOwnedRefund_SameEmail_DifferentCasing_StillSuppresses()
    {
        // Case-insensitive comparison — operator UAT defect mode where capitalisation
        // varies between sponsor signup form and attendee registration form.
        var sponsor = SponsorCompleted(sponsorEmail: "User@Example.COM");
        var refundId = "re_case_diff";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("user@example.com");

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());

        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Once,
            "email comparison must be case-insensitive — same logical recipient regardless of capitalisation");
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NonWorkflowRefund_SendsStandaloneEmail_RegressionGuard()
    {
        var sponsor = SponsorCompleted();
        var refundId = "re_legacy_only";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());
        await Task.Delay(200);

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "legacy path must still send the standalone email when no workflow line-item exists");
    }

    [Fact]
    public async Task WorkflowLookupThrows_DefaultsToSendingStandaloneEmail_FailOpenGuardrail()
    {
        var sponsor = SponsorCompleted();
        var refundId = "re_lookup_throws";
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _refundRequestRepo.Setup(r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(sponsor.Id, refundId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated transient DB error"));
        _eventRepo.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr", 1));

        var act = async () => await BuildHandler().HandleChargeRefundedAsync(
            sponsor.StripePaymentIntentId!, refundId, Guid.NewGuid());

        await act.Should().NotThrowAsync("guard must catch lookup exception and fall through to the legacy email path");
        await Task.Delay(200);

        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "fail-OPEN: when the lookup throws, the legacy email must still be sent so we don't silence the notification");
    }

    [Fact]
    public async Task SponsorNotFound_ReturnsEarly_BeforeReachingGuard()
    {
        _sponsorRepo.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sponsor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sponsor?)null);

        await BuildHandler().HandleChargeRefundedAsync("pi_orphan", "re_orphan", Guid.NewGuid());

        _refundRequestRepo.Verify(
            r => r.GetWorkflowOwnedAttendeeEmailForSponsorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "guard must not fire when no sponsor was loaded");
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "email path must not fire when no sponsor was loaded");
        _auditService.Verify(a => a.WriteSuppressionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // Fixture
    // -------------------------------------------------------------------------

    private static Sponsor SponsorCompleted(
        string paymentIntentId = "pi_test_default",
        string sponsorEmail = SponsorEmail)
    {
        var sponsor = Sponsor.CreateMoneySponsor(
            eventId: Guid.NewGuid(),
            sponsorUserId: Guid.NewGuid(),
            sponsorName: "Test Sponsor",
            sponsorEmail: sponsorEmail,
            sponsorPhone: "+1-555-1234",
            sponsorOrganization: "Test Corp",
            sponsorNotes: "",
            amount: Money.Create(125m, Currency.USD).Value).Value;
        sponsor.CompletePayment(paymentIntentId);
        return sponsor;
    }
}

using FluentAssertions;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Payments.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Payments;

/// <summary>
/// Phase 6A.157 — webhook handler coverage. 8 cases per architect lock:
/// completed happy path + completed idempotent skip + completed misrouted
/// (not a package sponsor) + completed sponsor-not-found + expired happy
/// path with stock restore + expired idempotent skip + expired
/// sponsor-not-found + expired stock-restore-fails.
/// </summary>
public class PackageSponsorWebhookHandlerTests
{
    private readonly Mock<ISponsorRepository> _sponsorRepo = new();
    private readonly Mock<ISponsorshipPackageRepository> _packageRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly PackageSponsorWebhookHandler _sut;
    private static readonly Guid PackageId = Guid.NewGuid();

    public PackageSponsorWebhookHandlerTests()
    {
        _sut = new PackageSponsorWebhookHandler(
            _sponsorRepo.Object, _packageRepo.Object, _uow.Object,
            Mock.Of<ILogger<PackageSponsorWebhookHandler>>());
    }

    private static Sponsor MakePackageSponsor(SponsorStatus status = SponsorStatus.Pending)
    {
        var sponsor = Sponsor.CreatePackageSponsor(
            Guid.NewGuid(), null, "John Doe", "john@example.com", null, null, null,
            PackageId, "Gold", "Gold", Money.Create(500m, Currency.USD).Value,
            includedTicketCount: 3).Value;
        if (status == SponsorStatus.Completed)
        {
            sponsor.SetStripeCheckoutSession("cs_x", DateTime.UtcNow.AddHours(1));
            sponsor.CompletePackagePayment("pi_x");
        }
        return sponsor;
    }

    private static Sponsor MakeGenericMoneySponsor()
    {
        return Sponsor.CreateMoneySponsor(
            Guid.NewGuid(), null, "John Doe", "john@example.com", null, null, null,
            Money.Create(500m, Currency.USD).Value).Value;
    }

    [Fact]
    public async Task Completed_SponsorNotFound_SwallowsErrorNoSave()
    {
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sponsor?)null);

        await _sut.HandleCheckoutCompletedAsync("cs_missing", "pi_x",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Completed_SponsorAlreadyCompleted_IdempotentSkip()
    {
        var sponsor = MakePackageSponsor(SponsorStatus.Completed);
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        await _sut.HandleCheckoutCompletedAsync("cs_dup", "pi_x",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Completed_MisroutedGenericSponsor_DoesNotComplete()
    {
        // Defensive: if a generic money sponsor gets routed here, the handler
        // must NOT call any package method (would silently raise wrong event).
        var generic = MakeGenericMoneySponsor();
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_misrouted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(generic);

        await _sut.HandleCheckoutCompletedAsync("cs_misrouted", "pi_x",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        generic.Status.Should().Be(SponsorStatus.Pending);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Completed_HappyPath_CallsCompletePackagePaymentAndCommits()
    {
        var sponsor = MakePackageSponsor();
        sponsor.SetStripeCheckoutSession("cs_ok", DateTime.UtcNow.AddHours(1));
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        await _sut.HandleCheckoutCompletedAsync("cs_ok", "pi_test_xyz",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        sponsor.Status.Should().Be(SponsorStatus.Completed);
        sponsor.StripePaymentIntentId.Should().Be("pi_test_xyz");
        _sponsorRepo.Verify(r => r.Update(sponsor), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Expired_SponsorNotFound_SwallowsNoStockRestore()
    {
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sponsor?)null);

        await _sut.HandleCheckoutExpiredAsync("cs_missing",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        _packageRepo.Verify(r => r.TryRestoreStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Expired_SponsorAlreadyCompleted_SkipsAndDoesNotRestoreStock()
    {
        var sponsor = MakePackageSponsor(SponsorStatus.Completed);
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_done", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        await _sut.HandleCheckoutExpiredAsync("cs_done",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        _packageRepo.Verify(r => r.TryRestoreStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Expired_HappyPath_MarksAbandonedAndRestoresStock()
    {
        var sponsor = MakePackageSponsor();
        sponsor.SetStripeCheckoutSession("cs_exp", DateTime.UtcNow.AddHours(1));
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_exp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _packageRepo.Setup(r => r.TryRestoreStockAsync(PackageId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleCheckoutExpiredAsync("cs_exp",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        sponsor.Status.Should().Be(SponsorStatus.Abandoned);
        _packageRepo.Verify(r => r.TryRestoreStockAsync(PackageId, 1, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Expired_StockRestoreFails_StillMarksAbandonedAndCommits()
    {
        // Loss-of-slot recovery isn't critical-path enough to block the abandonment
        // commit — a manual reconcile can re-add the slot. Architect locked this
        // contract for AddOn; mirrored here.
        var sponsor = MakePackageSponsor();
        sponsor.SetStripeCheckoutSession("cs_exp", DateTime.UtcNow.AddHours(1));
        _sponsorRepo.Setup(r => r.GetByCheckoutSessionIdAsync("cs_exp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _packageRepo.Setup(r => r.TryRestoreStockAsync(PackageId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.HandleCheckoutExpiredAsync("cs_exp",
            new Dictionary<string, string>(), Guid.NewGuid(), CancellationToken.None);

        sponsor.Status.Should().Be(SponsorStatus.Abandoned);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

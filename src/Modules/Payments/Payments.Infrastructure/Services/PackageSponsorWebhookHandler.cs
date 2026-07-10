using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Phase 6A.157 — handles Stripe webhook events for packaged sponsorship
/// payments. Sibling to <see cref="SponsorWebhookHandler"/> — the split is
/// load-bearing: completed-package webhooks call
/// <c>Sponsor.CompletePackagePayment</c> (raises
/// <c>PackageSponsorCompletedEvent</c> → drives the forked email template)
/// instead of <c>Sponsor.CompletePayment</c> (which would raise the generic
/// event + send the wrong email per the mutual-guard contract from
/// commit [1/6]).
///
/// Refund handling is NOT implemented here — refund webhooks match on
/// <c>StripePaymentIntentId</c> not <c>payment_type</c> metadata, so
/// <see cref="SponsorWebhookHandler.HandleChargeRefundedAsync"/> already
/// covers package sponsors (calls <c>Sponsor.MarkAsRefunded()</c> which is
/// package-agnostic).
///
/// Errors swallowed (return 200 to Stripe) to avoid retry storms. Sponsor
/// stays Pending; expiry cleanup job handles it.
/// </summary>
public class PackageSponsorWebhookHandler : IPackageSponsorWebhookHandler
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PackageSponsorWebhookHandler> _logger;

    public PackageSponsorWebhookHandler(
        ISponsorRepository sponsorRepository,
        ISponsorshipPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        ILogger<PackageSponsorWebhookHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[PackageSponsor] [Webhook-PackageSponsor-1] Processing package sponsor payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogError(
                    "[PackageSponsor] [Webhook-PackageSponsor-ERROR] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[PackageSponsor] [Webhook-PackageSponsor-2] Sponsor loaded - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}, PackageId: {PackageId}",
                correlationId, sponsor.Id, sponsor.Status, sponsor.SponsorshipPackageId);

            // Idempotency check (also catches Stripe duplicate webhook delivery)
            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[PackageSponsor] [Webhook-PackageSponsor-WARN] Sponsor not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, CurrentStatus: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            // Defensive: this dispatcher should only route here for package sponsors,
            // but a misrouted webhook would silently misbehave if we relied on metadata.
            if (!sponsor.SponsorshipPackageId.HasValue)
            {
                _logger.LogError(
                    "[PackageSponsor] [Webhook-PackageSponsor-ERROR] Sponsor is not a package sponsor (misrouted webhook?) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                    correlationId, sponsor.Id);
                return;
            }

            var completeResult = sponsor.CompletePackagePayment(paymentIntentId);
            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[PackageSponsor] [Webhook-PackageSponsor-ERROR] CompletePackagePayment failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[PackageSponsor] [Webhook-PackageSponsor-3] Payment completed on package sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);

            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[PackageSponsor] [Webhook-PackageSponsor-SUCCESS] Package sponsor payment completed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);
        }
        catch (Exception ex)
        {
            // Swallow: failures should NOT return HTTP 500 to Stripe.
            _logger.LogError(ex,
                "[PackageSponsor] [Webhook-PackageSponsor-ERROR] Error handling package sponsor checkout (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[PackageSponsor] [Webhook-Expired-PackageSponsor-1] Processing package sponsor expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogWarning(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-WARN] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-WARN] Sponsor not in Pending status - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            if (!sponsor.SponsorshipPackageId.HasValue)
            {
                _logger.LogError(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-ERROR] Sponsor is not a package sponsor (misrouted webhook?) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                    correlationId, sponsor.Id);
                return;
            }

            var packageId = sponsor.SponsorshipPackageId.Value;
            var abandonResult = sponsor.MarkAsAbandoned();
            if (abandonResult.IsFailure)
            {
                _logger.LogWarning(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-WARN] MarkAsAbandoned failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, abandonResult.Error);
                return;
            }

            // Restore reserved stock — same recovery pattern as AddOnPurchaseWebhookHandler
            var stockRestored = await _packageRepository.TryRestoreStockAsync(packageId, 1, ct);
            if (stockRestored)
            {
                _logger.LogInformation(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-2] Stock restored - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PackageId: {PackageId}",
                    correlationId, sponsor.Id, packageId);
            }
            else
            {
                _logger.LogWarning(
                    "[PackageSponsor] [Webhook-Expired-PackageSponsor-WARN] Stock restore failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PackageId: {PackageId}",
                    correlationId, sponsor.Id, packageId);
            }

            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[PackageSponsor] [Webhook-Expired-PackageSponsor-SUCCESS] Package sponsor abandoned, stock restored - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                correlationId, sponsor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PackageSponsor] [Webhook-Expired-PackageSponsor-ERROR] Error handling package sponsor expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }
}

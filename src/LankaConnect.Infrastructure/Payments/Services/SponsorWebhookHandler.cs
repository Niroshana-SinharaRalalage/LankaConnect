using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for money sponsor payments.
/// Follows DonationWebhookHandler pattern.
/// Errors are swallowed to prevent HTTP 500 to Stripe (sponsor stays Pending; expiry cleanup handles it).
/// Phase 6A.137B2: Added refund email notification via fire-and-forget.
/// </summary>
public class SponsorWebhookHandler : ISponsorWebhookHandler
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SponsorWebhookHandler> _logger;

    public SponsorWebhookHandler(
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        ILogger<SponsorWebhookHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
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
                "[Sponsor] [Webhook-Sponsor-1] Processing sponsor payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Load sponsor by checkout session
            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogError(
                    "[Sponsor] [Webhook-Sponsor-ERROR] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-2] Sponsor loaded - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}, Type: {Type}",
                correlationId, sponsor.Id, sponsor.Status, sponsor.Type);

            // Verify the sponsor is still pending and is a money sponsor (idempotency + type check)
            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Sponsor-WARN] Sponsor not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, CurrentStatus: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            if (sponsor.Type != SponsorType.Money)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Sponsor-WARN] Sponsor is not a money sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Type: {Type}",
                    correlationId, sponsor.Id, sponsor.Type);
                return;
            }

            // Complete payment on the sponsor entity
            var completeResult = sponsor.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[Sponsor] [Webhook-Sponsor-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-3] Payment completed on sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);

            // Save changes and dispatch domain events
            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-SUCCESS] Sponsor payment completed successfully - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);
        }
        catch (Exception ex)
        {
            // Swallow sponsor errors: failures should NOT return HTTP 500
            // to Stripe (causes retry storms). Sponsor stays Pending; expiry cleanup handles it.
            _logger.LogError(ex,
                "[Sponsor] [Webhook-Sponsor-ERROR] Error handling sponsor checkout (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
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
                "[Sponsor] [Webhook-Expired-Sponsor-1] Processing sponsor expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor not in Pending status - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            if (sponsor.Type != SponsorType.Money)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor is not a money sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Type: {Type}",
                    correlationId, sponsor.Id, sponsor.Type);
                return;
            }

            sponsor.MarkAsAbandoned();
            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Sponsor] [Webhook-Expired-Sponsor-SUCCESS] Sponsor abandoned - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                correlationId, sponsor.Id);
        }
        catch (Exception ex)
        {
            // Swallow: sponsor expiry failure should NOT return HTTP 500 to Stripe.
            // Sponsor stays Pending and can be cleaned up by background job.
            _logger.LogError(ex,
                "[Sponsor] [Webhook-Expired-Sponsor-ERROR] Error handling sponsor expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task HandleChargeRefundedAsync(
        string paymentIntentId,
        string refundId,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-1] Processing sponsor refund - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);

            var sponsor = await _sponsorRepository.FindFirstAsync(
                s => s.StripePaymentIntentId == paymentIntentId, ct);

            if (sponsor == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Sponsor-Refund-WARN] Sponsor not found for PaymentIntentId - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, paymentIntentId);
                return;
            }

            var refundResult = sponsor.MarkAsRefunded();
            if (refundResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Sponsor-Refund-WARN] MarkAsRefunded failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, refundResult.Error);
                return;
            }

            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-SUCCESS] Sponsor marked as refunded - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, RefundId: {RefundId}",
                correlationId, sponsor.Id, refundId);

            // Phase 6A.137B2: Fire-and-forget refund notification email
            var capturedSponsorName = sponsor.SponsorName;
            var capturedSponsorEmail = sponsor.SponsorEmail;
            var capturedSponsorOrganization = sponsor.SponsorOrganization;
            var capturedEventId = sponsor.EventId;
            var capturedSponsorId = sponsor.Id;
            var capturedAmount = sponsor.Amount?.Amount ?? 0;
            var capturedCurrency = sponsor.Amount?.Currency.ToString() ?? "USD";
            var capturedRefundedAt = sponsor.RefundedAt ?? DateTime.UtcNow;
            var capturedPaymentIntentId = sponsor.StripePaymentIntentId ?? paymentIntentId;
            var capturedScopeFactory = _scopeFactory;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    var eventTitle = $"Event {capturedEventId:N}";
                    try
                    {
                        var @event = await eventRepository.GetByIdAsync(capturedEventId, trackChanges: false, CancellationToken.None);
                        if (@event != null)
                            eventTitle = @event.Title.Value;
                    }
                    catch (Exception titleEx)
                    {
                        _logger.LogWarning(titleEx,
                            "[Phase 6A.137B2] Sponsor refund email: Failed to load event title for EventId={EventId}",
                            capturedEventId);
                    }

                    var baseUrl = configuration["Application:FrontendBaseUrl"]
                        ?? configuration["FrontendBaseUrl"]
                        ?? "https://lankaconnect.com";
                    var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                    var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                    var emailParams = SponsorRefundEmailParams.Create(
                        sponsorName: capturedSponsorName,
                        sponsorEmail: capturedSponsorEmail,
                        sponsorOrganization: capturedSponsorOrganization,
                        eventTitle: eventTitle,
                        amount: capturedAmount,
                        currency: capturedCurrency,
                        refundedAt: capturedRefundedAt,
                        paymentIntentId: capturedPaymentIntentId,
                        eventDetailsUrl: eventDetailsUrl);

                    var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "[Phase 6A.137B2] Sponsor refund EMAIL SENT: Email={Email}, SponsorId={SponsorId}, EventTitle={EventTitle}",
                            capturedSponsorEmail, capturedSponsorId, eventTitle);
                    }
                    else
                    {
                        _logger.LogError(
                            "[Phase 6A.137B2] Sponsor refund EMAIL FAILED: Email={Email}, SponsorId={SponsorId}, Errors={Errors}",
                            capturedSponsorEmail, capturedSponsorId, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "[Phase 6A.137B2] Sponsor refund EMAIL EXCEPTION: Email={Email}, SponsorId={SponsorId}",
                        capturedSponsorEmail, capturedSponsorId);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-CRITICAL] Error handling sponsor refund (swallowed) - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, paymentIntentId);
        }
    }
}

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared;
using Microsoft.Extensions.Logging;
using MediatR;

namespace LankaConnect.Application.Billing;

/// <summary>
/// Handles Stripe webhook events for Cultural Intelligence billing
/// </summary>
public class StripeWebhookHandler : IStripeWebhookHandler
{
    private readonly IBillingRepository _billingRepository;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IMediator _mediator;
    private readonly ILogger<StripeWebhookHandler> _logger;

    public StripeWebhookHandler(
        IBillingRepository billingRepository,
        IUsageTrackingService usageTrackingService,
        IMediator mediator,
        ILogger<StripeWebhookHandler> logger)
    {
        _billingRepository = billingRepository ?? throw new ArgumentNullException(nameof(billingRepository));
        _usageTrackingService = usageTrackingService ?? throw new ArgumentNullException(nameof(usageTrackingService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> HandleWebhookEventAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Stripe webhook event {EventType} with ID {EventId}", 
                webhookEvent.Type, webhookEvent.Id);

            return webhookEvent.Type switch
            {
                "customer.subscription.created" => await HandleSubscriptionCreatedAsync(webhookEvent, cancellationToken),
                "customer.subscription.updated" => await HandleSubscriptionUpdatedAsync(webhookEvent, cancellationToken),
                "customer.subscription.deleted" => await HandleSubscriptionCancelledAsync(webhookEvent, cancellationToken),
                "invoice.payment_succeeded" => await HandlePaymentSucceededAsync(webhookEvent, cancellationToken),
                "invoice.payment_failed" => await HandlePaymentFailedAsync(webhookEvent, cancellationToken),
                "customer.subscription.trial_will_end" => await HandleTrialEndingAsync(webhookEvent, cancellationToken),
                "usage_record.created" => await HandleUsageRecordCreatedAsync(webhookEvent, cancellationToken),
                "payout.paid" => await HandlePayoutPaidAsync(webhookEvent, cancellationToken),
                "payout.failed" => await HandlePayoutFailedAsync(webhookEvent, cancellationToken),
                _ => await HandleUnknownEventAsync(webhookEvent, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook event {EventType} with ID {EventId}", 
                webhookEvent.Type, webhookEvent.Id);
            return Result.Failure($"Webhook processing failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleSubscriptionCreatedAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription created event");

        try
        {
            var subscriptionData = ExtractSubscriptionData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(subscriptionData.Metadata);

            if (userId == null)
            {
                _logger.LogWarning("Could not extract user ID from subscription metadata");
                return Result.Failure("User ID not found in subscription metadata");
            }

            var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(userId, cancellationToken);
            if (subscription != null)
            {
                subscription.SetStripeSubscriptionId(subscriptionData.Id);
                await _billingRepository.SaveSubscriptionAsync(subscription, cancellationToken);

                // Publish domain event
                await _mediator.Publish(new CulturalIntelligenceSubscriptionActivatedEvent
                {
                    UserId = userId,
                    SubscriptionId = subscription.Id,
                    TierName = subscription.Tier.Name.Value,
                    ActivatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription created event");
            return Result.Failure($"Subscription creation handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleSubscriptionUpdatedAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription updated event");

        try
        {
            var subscriptionData = ExtractSubscriptionData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(subscriptionData.Metadata);

            if (userId != null)
            {
                var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(userId, cancellationToken);
                if (subscription != null)
                {
                    // Update billing date if changed
                    if (subscriptionData.NextBillingDate.HasValue)
                    {
                        subscription.UpdateNextBillingDate(subscriptionData.NextBillingDate.Value);
                    }

                    // Update tier if changed
                    if (subscriptionData.NewTier != null)
                    {
                        subscription.UpdateTier(subscriptionData.NewTier);
                    }

                    await _billingRepository.SaveSubscriptionAsync(subscription, cancellationToken);

                    // Publish domain event
                    await _mediator.Publish(new CulturalIntelligenceSubscriptionUpdatedEvent
                    {
                        UserId = userId,
                        SubscriptionId = subscription.Id,
                        UpdatedAt = DateTime.UtcNow,
                        Changes = subscriptionData.Changes
                    }, cancellationToken);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription updated event");
            return Result.Failure($"Subscription update handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleSubscriptionCancelledAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling subscription cancelled event");

        try
        {
            var subscriptionData = ExtractSubscriptionData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(subscriptionData.Metadata);

            if (userId != null)
            {
                var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(userId, cancellationToken);
                if (subscription != null)
                {
                    subscription.Cancel();
                    await _billingRepository.SaveSubscriptionAsync(subscription, cancellationToken);

                    // Publish domain event
                    await _mediator.Publish(new CulturalIntelligenceSubscriptionCancelledEvent
                    {
                        UserId = userId,
                        SubscriptionId = subscription.Id,
                        CancelledAt = DateTime.UtcNow,
                        Reason = subscriptionData.CancellationReason
                    }, cancellationToken);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription cancelled event");
            return Result.Failure($"Subscription cancellation handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandlePaymentSucceededAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment succeeded event");

        try
        {
            var invoiceData = ExtractInvoiceData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(invoiceData.Metadata);

            if (userId != null)
            {
                // Update subscription status to active if it was suspended
                var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(userId, cancellationToken);
                if (subscription != null && !subscription.IsActive)
                {
                    // Reactivate subscription if payment succeeded after failure
                    subscription.UpdateNextBillingDate(DateTime.UtcNow.AddMonths(1));
                    await _billingRepository.SaveSubscriptionAsync(subscription, cancellationToken);
                }

                // Publish domain event
                await _mediator.Publish(new CulturalIntelligencePaymentSucceededEvent
                {
                    UserId = userId,
                    Amount = invoiceData.Amount,
                    Currency = invoiceData.Currency,
                    InvoiceId = invoiceData.Id,
                    PaidAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment succeeded event");
            return Result.Failure($"Payment succeeded handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandlePaymentFailedAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payment failed event");

        try
        {
            var invoiceData = ExtractInvoiceData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(invoiceData.Metadata);

            if (userId != null)
            {
                // Publish domain event for business logic handling
                await _mediator.Publish(new CulturalIntelligencePaymentFailedEvent
                {
                    UserId = userId,
                    Amount = invoiceData.Amount,
                    Currency = invoiceData.Currency,
                    InvoiceId = invoiceData.Id,
                    FailureReason = invoiceData.FailureReason,
                    AttemptCount = invoiceData.AttemptCount,
                    FailedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failed event");
            return Result.Failure($"Payment failure handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleTrialEndingAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling trial ending event");

        try
        {
            var subscriptionData = ExtractSubscriptionData(webhookEvent.Data);
            var userId = ExtractUserIdFromMetadata(subscriptionData.Metadata);

            if (userId != null)
            {
                // Publish domain event for business logic handling (e.g., send email notification)
                await _mediator.Publish(new CulturalIntelligenceTrialEndingEvent
                {
                    UserId = userId,
                    TrialEndDate = subscriptionData.TrialEndDate ?? DateTime.UtcNow.AddDays(7),
                    TierName = subscriptionData.TierName
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling trial ending event");
            return Result.Failure($"Trial ending handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleUsageRecordCreatedAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling usage record created event");

        try
        {
            var usageData = ExtractUsageRecordData(webhookEvent.Data);
            
            // Track usage internally for analytics
            await _usageTrackingService.TrackUsageAsync(new CulturalAPIUsage(
                new APIKey(usageData.ApiKey, APIKeyTier.Professional, UserId.Create(usageData.UserId).Value),
                LankaConnect.Domain.Billing.BillingEndpoint.BuddhistCalendar(usageData.Endpoint),
                new UsageCost(usageData.Amount, 1.0m, Currency.USD(), CostBreakdown.Create(usageData.Amount, 1.0m, 0.0m)),
                new CulturalComplexityScore(50, Array.Empty<ComplexityFactor>()),
                UsageMetadata.Create(usageData.Id, usageData.ClientId)
            ), cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling usage record created event");
            return Result.Failure($"Usage record handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandlePayoutPaidAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payout paid event");

        try
        {
            var payoutData = ExtractPayoutData(webhookEvent.Data);
            var partnershipId = ExtractPartnershipIdFromMetadata(payoutData.Metadata);

            if (partnershipId != null)
            {
                // Publish domain event
                await _mediator.Publish(new PartnershipPayoutCompletedEvent
                {
                    PartnershipId = partnershipId,
                    Amount = payoutData.Amount,
                    Currency = payoutData.Currency,
                    PayoutId = payoutData.Id,
                    CompletedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payout paid event");
            return Result.Failure($"Payout paid handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandlePayoutFailedAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling payout failed event");

        try
        {
            var payoutData = ExtractPayoutData(webhookEvent.Data);
            var partnershipId = ExtractPartnershipIdFromMetadata(payoutData.Metadata);

            if (partnershipId != null)
            {
                // Publish domain event
                await _mediator.Publish(new PartnershipPayoutFailedEvent
                {
                    PartnershipId = partnershipId,
                    Amount = payoutData.Amount,
                    Currency = payoutData.Currency,
                    PayoutId = payoutData.Id,
                    FailureReason = payoutData.FailureReason,
                    FailedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payout failed event");
            return Result.Failure($"Payout failed handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleUnknownEventAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received unknown webhook event type: {EventType}", webhookEvent.Type);
        
        // Log for monitoring but don't fail
        await _mediator.Publish(new UnknownStripeWebhookEvent
        {
            EventType = webhookEvent.Type,
            EventId = webhookEvent.Id,
            ReceivedAt = DateTime.UtcNow
        }, cancellationToken);

        return Result.Success();
    }

    // Private helper methods for data extraction
    private SubscriptionData ExtractSubscriptionData(object data)
    {
        // Implementation would extract relevant data from Stripe webhook payload
        // This is a simplified version for the example
        return new SubscriptionData
        {
            Id = "sub_1234567890",
            Metadata = new Dictionary<string, string>(),
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            Changes = new List<string>()
        };
    }

    private InvoiceData ExtractInvoiceData(object data)
    {
        // Implementation would extract relevant data from Stripe webhook payload
        return new InvoiceData
        {
            Id = "in_1234567890",
            Amount = 99.00m,
            Currency = "USD",
            Metadata = new Dictionary<string, string>(),
            AttemptCount = 1
        };
    }

    private UsageRecordData ExtractUsageRecordData(object data)
    {
        // Implementation would extract relevant data from Stripe webhook payload
        return new UsageRecordData
        {
            Id = "ur_1234567890",
            Amount = 0.25m,
            ApiKey = "lc_prof_test",
            UserId = Guid.NewGuid(),
            Endpoint = "/api/cultural-appropriateness/validate",
            ClientId = "client_123"
        };
    }

    private PayoutData ExtractPayoutData(object data)
    {
        // Implementation would extract relevant data from Stripe webhook payload
        return new PayoutData
        {
            Id = "po_1234567890",
            Amount = 750.00m,
            Currency = "USD",
            Metadata = new Dictionary<string, string>()
        };
    }

    private UserId? ExtractUserIdFromMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue("user_id", out var userIdString))
            return null;

        if (Guid.TryParse(userIdString, out var userIdGuid))
        {
            var userIdResult = UserId.Create(userIdGuid);
            return userIdResult.IsSuccess ? userIdResult.Value : null;
        }

        return null;
    }

    private PartnershipId? ExtractPartnershipIdFromMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue("partnership_id", out var partnershipIdString))
            return null;

        if (Guid.TryParse(partnershipIdString, out var partnershipIdGuid))
        {
            return new PartnershipId(partnershipIdGuid);
        }

        return null;
    }
}

// Supporting interfaces and data classes
public interface IStripeWebhookHandler
{
    Task<Result> HandleWebhookEventAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}

// Data extraction classes
public class SubscriptionData
{
    public string Id { get; init; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; init; }
    public DateTime? NextBillingDate { get; init; }
    public CulturalIntelligenceTier? NewTier { get; init; }
    public List<string> Changes { get; init; } = new();
    public DateTime? TrialEndDate { get; init; }
    public string TierName { get; init; } = string.Empty;
    public string? CancellationReason { get; init; }
}

public class InvoiceData
{
    public string Id { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public Dictionary<string, string>? Metadata { get; init; }
    public string? FailureReason { get; init; }
    public int AttemptCount { get; init; }
}

public class UsageRecordData
{
    public string Id { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
}

public class PayoutData
{
    public string Id { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public Dictionary<string, string>? Metadata { get; init; }
    public string? FailureReason { get; init; }
}

// Domain events for webhook handling
public class CulturalIntelligenceSubscriptionActivatedEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public CulturalIntelligenceSubscriptionId SubscriptionId { get; init; } = null!;
    public string TierName { get; init; } = string.Empty;
    public DateTime ActivatedAt { get; init; }
}

public class CulturalIntelligenceSubscriptionUpdatedEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public CulturalIntelligenceSubscriptionId SubscriptionId { get; init; } = null!;
    public DateTime UpdatedAt { get; init; }
    public List<string> Changes { get; init; } = new();
}

public class CulturalIntelligenceSubscriptionCancelledEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public CulturalIntelligenceSubscriptionId SubscriptionId { get; init; } = null!;
    public DateTime CancelledAt { get; init; }
    public string? Reason { get; init; }
}

public class CulturalIntelligencePaymentSucceededEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string InvoiceId { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
}

public class CulturalIntelligencePaymentFailedEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string InvoiceId { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public int AttemptCount { get; init; }
    public DateTime FailedAt { get; init; }
}

public class CulturalIntelligenceTrialEndingEvent : INotification
{
    public UserId UserId { get; init; } = null!;
    public DateTime TrialEndDate { get; init; }
    public string TierName { get; init; } = string.Empty;
}

public class PartnershipPayoutCompletedEvent : INotification
{
    public PartnershipId PartnershipId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PayoutId { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}

public class PartnershipPayoutFailedEvent : INotification
{
    public PartnershipId PartnershipId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PayoutId { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public DateTime FailedAt { get; init; }
}

public class UnknownStripeWebhookEvent : INotification
{
    public string EventType { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
}
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.ConfirmNewsletterSubscription;

/// <summary>
/// Handler for newsletter subscription confirmation command
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class ConfirmNewsletterSubscriptionCommandHandler : IRequestHandler<ConfirmNewsletterSubscriptionCommand, Result<ConfirmNewsletterSubscriptionResponse>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmNewsletterSubscriptionCommandHandler> _logger;

    public ConfirmNewsletterSubscriptionCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmNewsletterSubscriptionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ConfirmNewsletterSubscriptionResponse>> Handle(
        ConfirmNewsletterSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ConfirmNewsletterSubscription"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("ConfirmationToken", request.ConfirmationToken?.Substring(0, Math.Min(8, request.ConfirmationToken?.Length ?? 0))))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ConfirmNewsletterSubscription START: Token={TokenPreview}",
                request.ConfirmationToken?.Substring(0, Math.Min(8, request.ConfirmationToken?.Length ?? 0)));

            try
            {
                // Validation: Confirmation token is required
                if (string.IsNullOrWhiteSpace(request.ConfirmationToken))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ConfirmNewsletterSubscription FAILED: Confirmation token is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<ConfirmNewsletterSubscriptionResponse>.Failure("Confirmation token is required");
                }

                // Find subscriber by confirmation token
                var subscriber = await _repository.GetByConfirmationTokenAsync(
                    request.ConfirmationToken,
                    cancellationToken);

                if (subscriber == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ConfirmNewsletterSubscription FAILED: Invalid or expired confirmation token - Token={TokenPreview}, Duration={ElapsedMs}ms",
                        request.ConfirmationToken.Substring(0, Math.Min(8, request.ConfirmationToken.Length)),
                        stopwatch.ElapsedMilliseconds);
                    return Result<ConfirmNewsletterSubscriptionResponse>.Failure("Invalid or expired confirmation token");
                }

                using (LogContext.PushProperty("SubscriberId", subscriber.Id))
                using (LogContext.PushProperty("SubscriberEmail", subscriber.Email.Value))
                {
                    _logger.LogInformation(
                        "ConfirmNewsletterSubscription: Subscriber found - SubscriberId={SubscriberId}, Email={Email}",
                        subscriber.Id,
                        subscriber.Email.Value);

                    // Confirm subscription
                    var confirmResult = subscriber.Confirm(request.ConfirmationToken);
                    if (!confirmResult.IsSuccess)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "ConfirmNewsletterSubscription FAILED: Confirmation failed - SubscriberId={SubscriberId}, Error={Error}, Duration={ElapsedMs}ms",
                            subscriber.Id,
                            confirmResult.Error,
                            stopwatch.ElapsedMilliseconds);
                        return Result<ConfirmNewsletterSubscriptionResponse>.Failure(confirmResult.Error);
                    }

                    _logger.LogInformation(
                        "ConfirmNewsletterSubscription: Subscription confirmed successfully - SubscriberId={SubscriberId}, ConfirmedAt={ConfirmedAt}",
                        subscriber.Id,
                        subscriber.ConfirmedAt);

                    // Save changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new ConfirmNewsletterSubscriptionResponse(
                        subscriber.Id,
                        subscriber.Email.Value,
                        subscriber.MetroAreaIds.FirstOrDefault(),  // Phase 6A.64: Use first metro area for backward compatibility
                        subscriber.ReceiveAllLocations,
                        subscriber.ConfirmedAt!.Value);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "ConfirmNewsletterSubscription COMPLETE: SubscriberId={SubscriberId}, Email={Email}, MetroAreaCount={MetroAreaCount}, ReceiveAllLocations={ReceiveAllLocations}, Duration={ElapsedMs}ms",
                        subscriber.Id,
                        subscriber.Email.Value,
                        subscriber.MetroAreaIds.Count,
                        subscriber.ReceiveAllLocations,
                        stopwatch.ElapsedMilliseconds);

                    return Result<ConfirmNewsletterSubscriptionResponse>.Success(response);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ConfirmNewsletterSubscription FAILED: Unexpected error - Token={TokenPreview}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.ConfirmationToken?.Substring(0, Math.Min(8, request.ConfirmationToken?.Length ?? 0)),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result<ConfirmNewsletterSubscriptionResponse>.Failure("An error occurred while confirming your subscription");
            }
        }
    }
}

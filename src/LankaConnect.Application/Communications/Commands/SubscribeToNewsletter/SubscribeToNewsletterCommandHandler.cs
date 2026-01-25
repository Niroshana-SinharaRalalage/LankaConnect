using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Shared.ValueObjects;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;

/// <summary>
/// Handler for newsletter subscription command
/// Phase 6A.64: Newsletter subscription with email confirmation
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class SubscribeToNewsletterCommandHandler : IRequestHandler<SubscribeToNewsletterCommand, Result<SubscribeToNewsletterResponse>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscribeToNewsletterCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public SubscribeToNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SubscribeToNewsletterCommandHandler> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<SubscribeToNewsletterResponse>> Handle(
        SubscribeToNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SubscribeToNewsletter"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SubscribeToNewsletter START: Email={Email}, MetroAreaCount={MetroAreaCount}, ReceiveAll={ReceiveAll}",
                request.Email,
                request.MetroAreaIds?.Count ?? 0,
                request.ReceiveAllLocations);

            try
            {

                // Validate email
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubscribeToNewsletter FAILED: Email validation failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        emailResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SubscribeToNewsletterResponse>.Failure($"Invalid email: {emailResult.Error}");
                }

                var email = emailResult.Value;

                _logger.LogInformation(
                    "SubscribeToNewsletter: Email validation passed - Email={Email}",
                    request.Email);

                // Check for existing subscriber
                var existingSubscriber = await _repository.GetByEmailAsync(request.Email, cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Existing subscriber check - Email={Email}, Found={Found}",
                    request.Email,
                    existingSubscriber != null);

                NewsletterSubscriber subscriber;

                // Phase 5B: Handle multiple metro areas by using first metro area for backward compatibility
                // In future, this could be extended to create multiple subscriptions per metro area
                var metroAreaId = request.MetroAreaIds?.FirstOrDefault();

                if (existingSubscriber != null)
                {
                    // If already active, return error
                    if (existingSubscriber.IsActive)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Email already active - Email={Email}, SubscriberId={SubscriberId}, Duration={ElapsedMs}ms",
                            request.Email,
                            existingSubscriber.Id,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure("Email is already subscribed to the newsletter");
                    }

                    _logger.LogInformation(
                        "SubscribeToNewsletter: Reactivating inactive subscriber - Email={Email}, SubscriberId={SubscriberId}",
                        request.Email,
                        existingSubscriber.Id);

                    // For inactive subscribers, create a new subscription instead of reactivating
                    // This ensures a fresh confirmation token and follows the domain model
                    // Phase 6A.64: Convert single metro area ID to collection for new API
                    var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                    var reactivateResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds,
                        request.ReceiveAllLocations);

                    if (!reactivateResult.IsSuccess)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Reactivation create failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                            request.Email,
                            reactivateResult.Error,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure(reactivateResult.Error);
                    }

                    subscriber = reactivateResult.Value;

                    // Remove old subscription
                    _repository.Remove(existingSubscriber);
                    await _repository.AddAsync(subscriber, cancellationToken);

                    _logger.LogInformation(
                        "SubscribeToNewsletter: Reactivated newsletter subscription - Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}",
                        request.Email,
                        subscriber.Id,
                        subscriber.MetroAreaIds.Count);
                }
                else
                {
                    // Create new subscriber
                    _logger.LogInformation(
                        "SubscribeToNewsletter: Creating new subscriber - Email={Email}",
                        request.Email);

                    // Phase 6A.64: Convert single metro area ID to collection for new API
                    var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                    var createResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds,
                        request.ReceiveAllLocations);

                    if (!createResult.IsSuccess)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Create failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                            request.Email,
                            createResult.Error,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure(createResult.Error);
                    }

                    subscriber = createResult.Value;

                    _logger.LogInformation(
                        "SubscribeToNewsletter: Domain entity created - Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}",
                        request.Email,
                        subscriber.Id,
                        subscriber.MetroAreaIds.Count);

                    await _repository.AddAsync(subscriber, cancellationToken);
                }

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Database commit successful - Email={Email}, SubscriberId={SubscriberId}",
                    request.Email,
                    subscriber.Id);

                // Phase 6A.64: Insert junction table entries AFTER subscriber is saved
                // This avoids FK constraint violation (subscriber_id must exist in newsletter_subscribers table first)
                await _repository.InsertPendingJunctionEntriesAsync(cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Junction table entries inserted - Email={Email}, SubscriberId={SubscriberId}",
                    request.Email,
                    subscriber.Id);

                // Send confirmation email
                var metroAreaDescription = request.ReceiveAllLocations
                    ? "All Locations"
                    : (request.MetroAreaIds?.Count > 0 ? $"{request.MetroAreaIds.Count} Location(s)" : "All Locations");

                // Phase 6A.71: Build confirmation and unsubscribe URLs from configuration
                var apiBaseUrl = _configuration["ApplicationUrls:ApiBaseUrl"] ?? "https://lankaconnect.com";
                var confirmPath = _configuration["ApplicationUrls:NewsletterConfirmPath"] ?? "/newsletter/confirm";
                var unsubscribePath = _configuration["ApplicationUrls:NewsletterUnsubscribePath"] ?? "/newsletter/unsubscribe";

                // Phase 6A.83 Part 3: Fix parameter name to match template expectation
                var emailParameters = new Dictionary<string, object>
                {
                    { "Email", request.Email },
                    { "ConfirmationToken", subscriber.ConfirmationToken! },
                    { "ConfirmationLink", $"{apiBaseUrl}{confirmPath}?token={subscriber.ConfirmationToken}" },
                    { "UnsubscribeUrl", $"{apiBaseUrl}{unsubscribePath}?token={subscriber.UnsubscribeToken}" },  // Phase 6A.83: Changed from UnsubscribeLink to UnsubscribeUrl
                    { "MetroAreasText", metroAreaDescription },
                    { "CompanyName", "LankaConnect" },
                    { "Date", DateTime.UtcNow.ToString("MMMM dd, yyyy") }
                };

                var sendEmailResult = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.NewsletterSubscriptionConfirmation,
                    request.Email,
                    emailParameters,
                    cancellationToken);

                if (!sendEmailResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "SubscribeToNewsletter: Failed to send confirmation email - Email={Email}, Error={Error}. Subscription saved but email not sent.",
                        request.Email,
                        sendEmailResult.Error);
                    // Don't fail the subscription - email sending is non-critical for testing/staging
                    // In production, email service should be properly configured
                }
                else
                {
                    _logger.LogInformation(
                        "SubscribeToNewsletter: Newsletter confirmation email sent - Email={Email}",
                        request.Email);
                }

                var response = new SubscribeToNewsletterResponse(
                    subscriber.Id,
                    subscriber.Email.Value,
                    subscriber.MetroAreaIds.FirstOrDefault(),  // Phase 6A.64: Use first metro area for backward compatibility
                    request.MetroAreaIds,
                    subscriber.ReceiveAllLocations,
                    subscriber.IsActive,
                    subscriber.IsConfirmed,
                    subscriber.ConfirmationToken!,
                    subscriber.ConfirmationSentAt!.Value);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SubscribeToNewsletter COMPLETE: Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.Email,
                    subscriber.Id,
                    subscriber.MetroAreaIds.Count,
                    stopwatch.ElapsedMilliseconds);

                return Result<SubscribeToNewsletterResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SubscribeToNewsletter FAILED: Unexpected error - Email={Email}, MetroAreaCount={MetroAreaCount}, ReceiveAll={ReceiveAll}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Email,
                    request.MetroAreaIds?.Count ?? 0,
                    request.ReceiveAllLocations,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

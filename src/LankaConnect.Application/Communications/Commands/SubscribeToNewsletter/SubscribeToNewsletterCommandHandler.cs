using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;

/// <summary>
/// Handler for newsletter subscription command
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
        try
        {
            _logger.LogInformation("[Phase 6A.64] Newsletter subscription START - Email: {Email}, MetroAreaCount: {Count}, ReceiveAll: {ReceiveAll}",
                request.Email, request.MetroAreaIds?.Count ?? 0, request.ReceiveAllLocations);

            // Validate email
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("[Phase 6A.64] Email validation FAILED - Email: {Email}, Error: {Error}",
                    request.Email, emailResult.Error);
                return Result<SubscribeToNewsletterResponse>.Failure($"Invalid email: {emailResult.Error}");
            }

            var email = emailResult.Value;
            _logger.LogDebug("[Phase 6A.64] Email validation PASSED - Email: {Email}", request.Email);

            // Check for existing subscriber
            _logger.LogDebug("[Phase 6A.64] Checking for existing subscriber - Email: {Email}", request.Email);
            var existingSubscriber = await _repository.GetByEmailAsync(request.Email, cancellationToken);
            _logger.LogDebug("[Phase 6A.64] Existing subscriber check - Email: {Email}, Found: {Found}",
                request.Email, existingSubscriber != null);

            NewsletterSubscriber subscriber;

            // Phase 5B: Handle multiple metro areas by using first metro area for backward compatibility
            // In future, this could be extended to create multiple subscriptions per metro area
            var metroAreaId = request.MetroAreaIds?.FirstOrDefault();

            if (existingSubscriber != null)
            {
                // If already active, return error
                if (existingSubscriber.IsActive)
                {
                    _logger.LogWarning("[Phase 6A.64] Subscription REJECTED - Email already active: {Email}", request.Email);
                    return Result<SubscribeToNewsletterResponse>.Failure("Email is already subscribed to the newsletter");
                }

                _logger.LogInformation("[Phase 6A.64] Reactivating inactive subscriber - Email: {Email}", request.Email);

                // For inactive subscribers, create a new subscription instead of reactivating
                // This ensures a fresh confirmation token and follows the domain model
                // Phase 6A.64: Convert single metro area ID to collection for new API
                var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                _logger.LogDebug("[Phase 6A.64] Creating reactivation subscriber - Email: {Email}, MetroAreaIds: [{MetroAreaIds}], ReceiveAll: {ReceiveAll}",
                    request.Email, string.Join(", ", metroAreaIds), request.ReceiveAllLocations);

                var reactivateResult = NewsletterSubscriber.Create(
                    email,
                    metroAreaIds,
                    request.ReceiveAllLocations);

                if (!reactivateResult.IsSuccess)
                {
                    _logger.LogError("[Phase 6A.64] Reactivation Create FAILED - Email: {Email}, Error: {Error}",
                        request.Email, reactivateResult.Error);
                    return Result<SubscribeToNewsletterResponse>.Failure(reactivateResult.Error);
                }

                subscriber = reactivateResult.Value;

                // Remove old subscription
                _repository.Remove(existingSubscriber);
                await _repository.AddAsync(subscriber, cancellationToken);

                _logger.LogInformation("[Phase 6A.64] Reactivated newsletter subscription - Email: {Email}, MetroAreaCount: {Count}",
                    request.Email, subscriber.MetroAreaIds.Count);
            }
            else
            {
                // Create new subscriber
                _logger.LogInformation("[Phase 6A.64] Creating NEW subscriber - Email: {Email}", request.Email);

                // Phase 6A.64: Convert single metro area ID to collection for new API
                var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                _logger.LogDebug("[Phase 6A.64] Creating new subscriber - Email: {Email}, MetroAreaIds: [{MetroAreaIds}], ReceiveAll: {ReceiveAll}",
                    request.Email, string.Join(", ", metroAreaIds), request.ReceiveAllLocations);

                var createResult = NewsletterSubscriber.Create(
                    email,
                    metroAreaIds,
                    request.ReceiveAllLocations);

                if (!createResult.IsSuccess)
                {
                    _logger.LogError("[Phase 6A.64] Create FAILED - Email: {Email}, Error: {Error}",
                        request.Email, createResult.Error);
                    return Result<SubscribeToNewsletterResponse>.Failure(createResult.Error);
                }

                subscriber = createResult.Value;

                _logger.LogDebug("[Phase 6A.64] Domain entity created - Email: {Email}, SubscriberId: {SubscriberId}, MetroAreaCount: {Count}",
                    request.Email, subscriber.Id, subscriber.MetroAreaIds.Count);

                await _repository.AddAsync(subscriber, cancellationToken);
                _logger.LogInformation("[Phase 6A.64] Added subscriber to repository - Email: {Email}, SubscriberId: {SubscriberId}",
                    request.Email, subscriber.Id);
            }

            // Save changes
            _logger.LogDebug("[Phase 6A.64] Committing changes to database - Email: {Email}", request.Email);
            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("[Phase 6A.64] Database commit SUCCESSFUL - Email: {Email}, SubscriberId: {SubscriberId}",
                request.Email, subscriber.Id);

            // Phase 6A.64: Insert junction table entries AFTER subscriber is saved
            // This avoids FK constraint violation (subscriber_id must exist in newsletter_subscribers table first)
            _logger.LogDebug("[Phase 6A.64] Inserting junction table entries - Email: {Email}", request.Email);
            await _repository.InsertPendingJunctionEntriesAsync(cancellationToken);
            _logger.LogInformation("[Phase 6A.64] Junction table entries inserted - Email: {Email}", request.Email);

            // Send confirmation email
            var metroAreaDescription = request.ReceiveAllLocations
                ? "All Locations"
                : (request.MetroAreaIds?.Count > 0 ? $"{request.MetroAreaIds.Count} Location(s)" : "All Locations");

            // Phase 6A.71: Build confirmation and unsubscribe URLs from configuration
            var apiBaseUrl = _configuration["ApplicationUrls:ApiBaseUrl"] ?? "https://lankaconnect.com";
            var confirmPath = _configuration["ApplicationUrls:NewsletterConfirmPath"] ?? "/newsletter/confirm";
            var unsubscribePath = _configuration["ApplicationUrls:NewsletterUnsubscribePath"] ?? "/newsletter/unsubscribe";

            var emailParameters = new Dictionary<string, object>
            {
                { "Email", request.Email },
                { "ConfirmationToken", subscriber.ConfirmationToken! },
                { "ConfirmationLink", $"{apiBaseUrl}{confirmPath}?token={subscriber.ConfirmationToken}" },
                { "UnsubscribeLink", $"{apiBaseUrl}{unsubscribePath}?token={subscriber.UnsubscribeToken}" },
                { "MetroArea", metroAreaDescription },
                { "CompanyName", "LankaConnect" },
                { "Date", DateTime.UtcNow.ToString("MMMM dd, yyyy") }
            };

            var sendEmailResult = await _emailService.SendTemplatedEmailAsync(
                "newsletter-confirmation",
                request.Email,
                emailParameters,
                cancellationToken);

            if (!sendEmailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send confirmation email to {Email}: {Error}. Subscription saved but email not sent.",
                    request.Email, sendEmailResult.Error);
                // Don't fail the subscription - email sending is non-critical for testing/staging
                // In production, email service should be properly configured
            }
            else
            {
                _logger.LogInformation("Newsletter confirmation email sent to {Email}", request.Email);
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

            return Result<SubscribeToNewsletterResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.64] EXCEPTION during newsletter subscription - Email: {Email}, MetroAreaCount: {Count}, ReceiveAll: {ReceiveAll}, ExceptionType: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                request.Email,
                request.MetroAreaIds?.Count ?? 0,
                request.ReceiveAllLocations,
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace);

            // Log inner exception if exists
            if (ex.InnerException != null)
            {
                _logger.LogError("[Phase 6A.64] INNER EXCEPTION - Type: {InnerType}, Message: {InnerMessage}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message);
            }

            return Result<SubscribeToNewsletterResponse>.Failure("An error occurred while processing your subscription");
        }
    }
}

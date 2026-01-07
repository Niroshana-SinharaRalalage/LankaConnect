using MediatR;
using Microsoft.Extensions.Logging;
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

    public SubscribeToNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SubscribeToNewsletterCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<SubscribeToNewsletterResponse>> Handle(
        SubscribeToNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate email
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                return Result<SubscribeToNewsletterResponse>.Failure($"Invalid email: {emailResult.Error}");
            }

            var email = emailResult.Value;

            // Check for existing subscriber
            var existingSubscriber = await _repository.GetByEmailAsync(request.Email, cancellationToken);

            NewsletterSubscriber subscriber;

            // Phase 5B: Handle multiple metro areas by using first metro area for backward compatibility
            // In future, this could be extended to create multiple subscriptions per metro area
            var metroAreaId = request.MetroAreaIds?.FirstOrDefault();

            if (existingSubscriber != null)
            {
                // If already active, return error
                if (existingSubscriber.IsActive)
                {
                    return Result<SubscribeToNewsletterResponse>.Failure("Email is already subscribed to the newsletter");
                }

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
                    return Result<SubscribeToNewsletterResponse>.Failure(reactivateResult.Error);
                }

                subscriber = reactivateResult.Value;

                // Remove old subscription
                _repository.Remove(existingSubscriber);
                await _repository.AddAsync(subscriber, cancellationToken);

                _logger.LogInformation("Reactivated newsletter subscription for {Email}", request.Email);
            }
            else
            {
                // Create new subscriber
                // Phase 6A.64: Convert single metro area ID to collection for new API
                var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());
                var createResult = NewsletterSubscriber.Create(
                    email,
                    metroAreaIds,
                    request.ReceiveAllLocations);

                if (!createResult.IsSuccess)
                {
                    return Result<SubscribeToNewsletterResponse>.Failure(createResult.Error);
                }

                subscriber = createResult.Value;
                await _repository.AddAsync(subscriber, cancellationToken);
                _logger.LogInformation("Created new newsletter subscription for {Email}", request.Email);
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            // Send confirmation email
            var metroAreaDescription = request.ReceiveAllLocations
                ? "All Locations"
                : (request.MetroAreaIds?.Count > 0 ? $"{request.MetroAreaIds.Count} Location(s)" : "All Locations");

            var emailParameters = new Dictionary<string, object>
            {
                { "Email", request.Email },
                { "ConfirmationToken", subscriber.ConfirmationToken! },
                { "ConfirmationLink", $"https://lankaconnect.com/newsletter/confirm?token={subscriber.ConfirmationToken}" },
                { "UnsubscribeLink", $"https://lankaconnect.com/newsletter/unsubscribe?token={subscriber.UnsubscribeToken}" },
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
            _logger.LogError(ex, "Error subscribing {Email} to newsletter", request.Email);
            return Result<SubscribeToNewsletterResponse>.Failure("An error occurred while processing your subscription");
        }
    }
}

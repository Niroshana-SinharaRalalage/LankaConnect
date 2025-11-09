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

            if (existingSubscriber != null)
            {
                // If already active, return error
                if (existingSubscriber.IsActive)
                {
                    return Result<SubscribeToNewsletterResponse>.Failure("Email is already subscribed to the newsletter");
                }

                // For inactive subscribers, create a new subscription instead of reactivating
                // This ensures a fresh confirmation token and follows the domain model
                var reactivateResult = NewsletterSubscriber.Create(
                    email,
                    request.MetroAreaId,
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
                var createResult = NewsletterSubscriber.Create(
                    email,
                    request.MetroAreaId,
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
            var emailParameters = new Dictionary<string, object>
            {
                { "Email", request.Email },
                { "ConfirmationToken", subscriber.ConfirmationToken! },
                { "ConfirmationLink", $"https://lankaconnect.com/newsletter/confirm?token={subscriber.ConfirmationToken}" },
                { "MetroArea", request.MetroAreaId.HasValue ? "Specific Location" : "All Locations" },
                { "CompanyName", "LankaConnect" }
            };

            var sendEmailResult = await _emailService.SendTemplatedEmailAsync(
                "newsletter-confirmation",
                request.Email,
                emailParameters,
                cancellationToken);

            if (!sendEmailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send confirmation email to {Email}: {Error}",
                    request.Email, sendEmailResult.Error);
                return Result<SubscribeToNewsletterResponse>.Failure("Failed to send confirmation email. Please try again.");
            }

            _logger.LogInformation("Newsletter confirmation email sent to {Email}", request.Email);

            var response = new SubscribeToNewsletterResponse(
                subscriber.Id,
                subscriber.Email.Value,
                subscriber.MetroAreaId,
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

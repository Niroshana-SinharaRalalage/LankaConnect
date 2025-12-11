using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.ConfirmNewsletterSubscription;

/// <summary>
/// Handler for newsletter subscription confirmation command
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
        try
        {
            if (string.IsNullOrWhiteSpace(request.ConfirmationToken))
            {
                return Result<ConfirmNewsletterSubscriptionResponse>.Failure("Confirmation token is required");
            }

            // Find subscriber by confirmation token
            var subscriber = await _repository.GetByConfirmationTokenAsync(
                request.ConfirmationToken,
                cancellationToken);

            if (subscriber == null)
            {
                return Result<ConfirmNewsletterSubscriptionResponse>.Failure("Invalid or expired confirmation token");
            }

            // Confirm subscription
            var confirmResult = subscriber.Confirm(request.ConfirmationToken);
            if (!confirmResult.IsSuccess)
            {
                return Result<ConfirmNewsletterSubscriptionResponse>.Failure(confirmResult.Error);
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Newsletter subscription confirmed for {Email}", subscriber.Email.Value);

            var response = new ConfirmNewsletterSubscriptionResponse(
                subscriber.Id,
                subscriber.Email.Value,
                subscriber.MetroAreaId,
                subscriber.ReceiveAllLocations,
                subscriber.ConfirmedAt!.Value);

            return Result<ConfirmNewsletterSubscriptionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming newsletter subscription with token {Token}", request.ConfirmationToken);
            return Result<ConfirmNewsletterSubscriptionResponse>.Failure("An error occurred while confirming your subscription");
        }
    }
}

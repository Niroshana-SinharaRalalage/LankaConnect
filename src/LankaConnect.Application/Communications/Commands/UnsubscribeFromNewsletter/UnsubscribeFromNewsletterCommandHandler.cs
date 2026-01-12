using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.UnsubscribeFromNewsletter;

/// <summary>
/// Handler for newsletter unsubscribe command
/// Removes subscriber from database based on unsubscribe token
/// </summary>
public class UnsubscribeFromNewsletterCommandHandler : IRequestHandler<UnsubscribeFromNewsletterCommand, Result<bool>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnsubscribeFromNewsletterCommandHandler> _logger;

    public UnsubscribeFromNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UnsubscribeFromNewsletterCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        UnsubscribeFromNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Phase 6A.71] Newsletter unsubscribe START - Token: {Token}", request.UnsubscribeToken);

            if (string.IsNullOrWhiteSpace(request.UnsubscribeToken))
            {
                _logger.LogWarning("[Phase 6A.71] Unsubscribe FAILED - Empty token");
                return Result<bool>.Failure("Invalid unsubscribe token");
            }

            // Find subscriber by unsubscribe token
            var subscriber = await _repository.GetByUnsubscribeTokenAsync(request.UnsubscribeToken, cancellationToken);

            if (subscriber == null)
            {
                _logger.LogWarning("[Phase 6A.71] Unsubscribe FAILED - Subscriber not found for token");
                return Result<bool>.Failure("Invalid or expired unsubscribe link");
            }

            _logger.LogInformation("[Phase 6A.71] Found subscriber - Email: {Email}, SubscriberId: {SubscriberId}, IsActive: {IsActive}",
                subscriber.Email.Value, subscriber.Id, subscriber.IsActive);

            // Remove subscriber from database
            // Phase 6A.64: Junction table entries will be CASCADE deleted automatically
            _repository.Remove(subscriber);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Phase 6A.71] Unsubscribe SUCCESSFUL - Email: {Email}, SubscriberId: {SubscriberId}",
                subscriber.Email.Value, subscriber.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.71] EXCEPTION during newsletter unsubscribe - Token: {Token}, ExceptionType: {ExceptionType}, Message: {Message}",
                request.UnsubscribeToken,
                ex.GetType().Name,
                ex.Message);

            return Result<bool>.Failure("An error occurred while processing your unsubscribe request");
        }
    }
}

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.PublishNewsletter;

public class PublishNewsletterCommandHandler : ICommandHandler<PublishNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PublishNewsletterCommandHandler> _logger;

    public PublishNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<PublishNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(PublishNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Newsletter] Publishing newsletter: {NewsletterID}", request.Id);

            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
            {
                _logger.LogWarning("[Newsletter] Newsletter not found: {NewsletterID}", request.Id);
                return Result<bool>.Failure("Newsletter not found");
            }

            // Authorization check
            var isAdmin = _currentUserService.IsAdmin;
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !isAdmin)
            {
                _logger.LogWarning("[Newsletter] Unauthorized publish attempt by user {UserID}", _currentUserService.UserId);
                return Result<bool>.Failure("You do not have permission to publish this newsletter");
            }

            // Publish newsletter
            var publishResult = newsletter.Publish();
            if (publishResult.IsFailure)
            {
                _logger.LogWarning("[Newsletter] Publish failed: {Error}", publishResult.Error);
                return Result<bool>.Failure(publishResult.Error);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Newsletter] Newsletter published successfully: {NewsletterID}", request.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Newsletter] Error publishing newsletter: {NewsletterID}", request.Id);
            return Result<bool>.Failure("An error occurred while publishing the newsletter");
        }
    }
}

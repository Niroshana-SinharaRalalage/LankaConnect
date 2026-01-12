using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.PublishNewsletter;

/// <summary>
/// Phase 6A.74: Handler for publishing newsletters
/// Changes status from Draft to Active and sets PublishedAt/ExpiresAt
/// </summary>
public class PublishNewsletterCommandHandler : ICommandHandler<PublishNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PublishNewsletterCommandHandler> _logger;

    public PublishNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<PublishNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(PublishNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] PublishNewsletterCommandHandler STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.Id, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
                return Result<bool>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can publish
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<bool>.Failure("You do not have permission to publish this newsletter");

            // Publish newsletter (Draft → Active)
            var publishResult = newsletter.Publish();
            if (publishResult.IsFailure)
                return Result<bool>.Failure(publishResult.Error);

            // Commit changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74] Newsletter published successfully - ID: {NewsletterId}, PublishedAt: {PublishedAt}, ExpiresAt: {ExpiresAt}",
                request.Id, newsletter.PublishedAt, newsletter.ExpiresAt);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR publishing newsletter - Newsletter {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);
            throw;
        }
    }
}

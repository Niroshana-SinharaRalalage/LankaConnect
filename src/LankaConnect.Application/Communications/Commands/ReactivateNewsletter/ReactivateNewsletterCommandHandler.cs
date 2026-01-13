using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.ReactivateNewsletter;

/// <summary>
/// Phase 6A.74 Hotfix: Handler for reactivating inactive newsletters
/// Changes status from Inactive to Active and extends ExpiresAt by 7 days
/// </summary>
public class ReactivateNewsletterCommandHandler : ICommandHandler<ReactivateNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactivateNewsletterCommandHandler> _logger;

    public ReactivateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<ReactivateNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReactivateNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74 Hotfix] ReactivateNewsletterCommandHandler STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.Id, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
                return Result<bool>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can reactivate
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<bool>.Failure("You do not have permission to reactivate this newsletter");

            // Reactivate newsletter (Inactive → Active, extends by 7 days)
            var reactivateResult = newsletter.Reactivate();
            if (reactivateResult.IsFailure)
                return Result<bool>.Failure(reactivateResult.Error);

            // Commit changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74 Hotfix] Newsletter reactivated successfully - ID: {NewsletterId}, ExpiresAt: {ExpiresAt}",
                request.Id, newsletter.ExpiresAt);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74 Hotfix] ERROR reactivating newsletter - Newsletter {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);
            throw;
        }
    }
}

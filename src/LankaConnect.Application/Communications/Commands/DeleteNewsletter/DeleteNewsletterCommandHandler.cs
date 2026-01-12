using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.DeleteNewsletter;

public class DeleteNewsletterCommandHandler : ICommandHandler<DeleteNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteNewsletterCommandHandler> _logger;

    public DeleteNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Newsletter] Deleting newsletter: {NewsletterID}", request.Id);

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
                _logger.LogWarning("[Newsletter] Unauthorized delete attempt by user {UserID}", _currentUserService.UserId);
                return Result<bool>.Failure("You do not have permission to delete this newsletter");
            }

            if (!newsletter.CanDelete())
            {
                _logger.LogWarning("[Newsletter] Cannot delete newsletter - Status: {Status}", newsletter.Status);
                return Result<bool>.Failure("Only draft newsletters can be deleted");
            }

            _newsletterRepository.Delete(newsletter);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Newsletter] Newsletter deleted successfully: {NewsletterID}", request.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Newsletter] Error deleting newsletter: {NewsletterID}", request.Id);
            return Result<bool>.Failure("An error occurred while deleting the newsletter");
        }
    }
}

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.DeleteNewsletter;

/// <summary>
/// Phase 6A.74: Handler for deleting newsletters
/// Only Draft newsletters can be deleted
/// </summary>
public class DeleteNewsletterCommandHandler : ICommandHandler<DeleteNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteNewsletterCommandHandler> _logger;

    public DeleteNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] DeleteNewsletterCommandHandler STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.Id, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
                return Result<bool>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can delete
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<bool>.Failure("You do not have permission to delete this newsletter");

            // Validate newsletter can be deleted
            if (!newsletter.CanDelete())
                return Result<bool>.Failure("Only Draft newsletters can be deleted");

            // Remove newsletter
            _newsletterRepository.Remove(newsletter);

            // Commit changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74] Newsletter deleted successfully - ID: {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR deleting newsletter - Newsletter {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);
            throw;
        }
    }
}

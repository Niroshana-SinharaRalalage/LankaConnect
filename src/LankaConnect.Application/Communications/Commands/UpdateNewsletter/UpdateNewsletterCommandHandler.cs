using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.UpdateNewsletter;

public class UpdateNewsletterCommandHandler : ICommandHandler<UpdateNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateNewsletterCommandHandler> _logger;

    public UpdateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Newsletter] Updating newsletter: {NewsletterID}", request.Id);

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
                _logger.LogWarning("[Newsletter] Unauthorized update attempt by user {UserID}", _currentUserService.UserId);
                return Result<bool>.Failure("You do not have permission to update this newsletter");
            }

            // Create value objects
            var titleResult = NewsletterTitle.Create(request.Title);
            if (titleResult.IsFailure)
                return Result<bool>.Failure(titleResult.Error);

            var descriptionResult = NewsletterDescription.Create(request.Description);
            if (descriptionResult.IsFailure)
                return Result<bool>.Failure(descriptionResult.Error);

            // Update newsletter
            var updateResult = newsletter.Update(
                titleResult.Value,
                descriptionResult.Value,
                request.EmailGroupIds ?? new List<Guid>(),
                request.IncludeNewsletterSubscribers,
                request.EventId,
                request.MetroAreaIds,
                request.TargetAllLocations);

            if (updateResult.IsFailure)
            {
                _logger.LogWarning("[Newsletter] Update failed: {Error}", updateResult.Error);
                return Result<bool>.Failure(updateResult.Error);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Newsletter] Newsletter updated successfully: {NewsletterID}", request.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Newsletter] Error updating newsletter: {NewsletterID}", request.Id);
            return Result<bool>.Failure("An error occurred while updating the newsletter");
        }
    }
}

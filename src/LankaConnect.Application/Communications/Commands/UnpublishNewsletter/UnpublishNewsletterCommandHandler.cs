using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.UnpublishNewsletter;

/// <summary>
/// Handler for UnpublishNewsletterCommand
/// Phase 6A.74 Part 9A: Unpublish functionality
///
/// Business Rules:
/// 1. Only Active newsletters can be unpublished
/// 2. Cannot unpublish if already sent
/// 3. Only newsletter creator (or Admin) can unpublish
/// 4. Clears PublishedAt and ExpiresAt dates
/// </summary>
public class UnpublishNewsletterCommandHandler : IRequestHandler<UnpublishNewsletterCommand, Result>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UnpublishNewsletterCommandHandler> _logger;

    public UnpublishNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UnpublishNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(UnpublishNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74 Part 9A] Unpublishing newsletter {Id}, User {UserId}",
            request.Id, _currentUserService.UserId);

        // Get newsletter
        var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (newsletter == null)
        {
            _logger.LogWarning("[Phase 6A.74 Part 9A] Newsletter {Id} not found", request.Id);
            return Result.Failure("Newsletter not found");
        }

        // Authorization: Only creator (or Admin) can unpublish
        if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
        {
            _logger.LogWarning(
                "[Phase 6A.74 Part 9A] User {UserId} cannot unpublish newsletter {Id} (creator: {CreatorId})",
                _currentUserService.UserId, request.Id, newsletter.CreatedByUserId);
            return Result.Failure("Only the newsletter creator or Admin can unpublish this newsletter");
        }

        // Unpublish newsletter (domain validation)
        var result = newsletter.Unpublish();
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "[Phase 6A.74 Part 9A] Failed to unpublish newsletter {Id}: {Error}",
                request.Id, result.Error);
            return result;
        }

        // Persist changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "[Phase 6A.74 Part 9A] Newsletter {Id} unpublished successfully (Active → Draft)",
            request.Id);
        return Result.Success();
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.ReactivateNewsletter;

/// <summary>
/// Phase 6A.74 Hotfix: Handler for reactivating inactive newsletters
/// Changes status from Inactive to Active and extends ExpiresAt by 7 days
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "ReactivateNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReactivateNewsletter START: NewsletterId={NewsletterId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReactivateNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReactivateNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "ReactivateNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}, CurrentExpiresAt={ExpiresAt}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId,
                    newsletter.ExpiresAt);

                // Authorization: Only creator or admin can reactivate
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReactivateNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("You do not have permission to reactivate this newsletter");
                }

                // Reactivate newsletter (Inactive → Active, extends by 7 days)
                _logger.LogInformation(
                    "ReactivateNewsletter: Reactivating newsletter - NewsletterId={NewsletterId}, CurrentStatus={Status}",
                    request.Id,
                    newsletter.Status);

                var reactivateResult = newsletter.Reactivate();
                if (reactivateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReactivateNewsletter FAILED: Reactivate operation failed - NewsletterId={NewsletterId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Id,
                        reactivateResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure(reactivateResult.Error);
                }

                _logger.LogInformation(
                    "ReactivateNewsletter: Newsletter reactivation successful - NewsletterId={NewsletterId}, NewExpiresAt={ExpiresAt}",
                    request.Id,
                    newsletter.ExpiresAt);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "ReactivateNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, ExpiresAt={ExpiresAt}, Duration={ElapsedMs}ms",
                    request.Id,
                    _currentUserService.UserId,
                    newsletter.ExpiresAt,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ReactivateNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.PublishNewsletter;

/// <summary>
/// Phase 6A.74: Handler for publishing newsletters
/// Changes status from Draft to Active and sets PublishedAt/ExpiresAt
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "PublishNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PublishNewsletter START: NewsletterId={NewsletterId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "PublishNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId);

                // Authorization: Only creator or admin can publish
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("You do not have permission to publish this newsletter");
                }

                // Publish newsletter (Draft → Active)
                _logger.LogInformation(
                    "PublishNewsletter: Publishing newsletter - NewsletterId={NewsletterId}, CurrentStatus={Status}",
                    request.Id,
                    newsletter.Status);

                var publishResult = newsletter.Publish();
                if (publishResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishNewsletter FAILED: Publish operation failed - NewsletterId={NewsletterId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Id,
                        publishResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure(publishResult.Error);
                }

                _logger.LogInformation(
                    "PublishNewsletter: Newsletter publish successful - NewsletterId={NewsletterId}, PublishedAt={PublishedAt}, ExpiresAt={ExpiresAt}",
                    request.Id,
                    newsletter.PublishedAt,
                    newsletter.ExpiresAt);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "PublishNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, PublishedAt={PublishedAt}, ExpiresAt={ExpiresAt}, Duration={ElapsedMs}ms",
                    request.Id,
                    _currentUserService.UserId,
                    newsletter.PublishedAt,
                    newsletter.ExpiresAt,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "PublishNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

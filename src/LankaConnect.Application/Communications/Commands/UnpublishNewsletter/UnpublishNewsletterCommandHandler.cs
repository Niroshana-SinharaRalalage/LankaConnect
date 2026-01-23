using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.UnpublishNewsletter;

/// <summary>
/// Handler for UnpublishNewsletterCommand
/// Phase 6A.74 Part 9A: Unpublish functionality
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "UnpublishNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UnpublishNewsletter START: NewsletterId={NewsletterId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnpublishNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Newsletter ID is required");
                }

                // Get newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnpublishNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "UnpublishNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}, SentAt={SentAt}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId,
                    newsletter.SentAt);

                // Authorization: Only creator (or Admin) can unpublish
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnpublishNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the newsletter creator or Admin can unpublish this newsletter");
                }

                // Unpublish newsletter (domain validation for status and sent check)
                _logger.LogInformation(
                    "UnpublishNewsletter: Unpublishing newsletter - NewsletterId={NewsletterId}, CurrentStatus={Status}",
                    request.Id,
                    newsletter.Status);

                var result = newsletter.Unpublish();
                if (!result.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnpublishNewsletter FAILED: Unpublish operation failed - NewsletterId={NewsletterId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Id,
                        result.Error,
                        stopwatch.ElapsedMilliseconds);
                    return result;
                }

                _logger.LogInformation(
                    "UnpublishNewsletter: Newsletter unpublish successful - NewsletterId={NewsletterId}, NewStatus={Status}",
                    request.Id,
                    newsletter.Status);

                // Persist changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "UnpublishNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, Status={Status} (Active → Draft), Duration={ElapsedMs}ms",
                    request.Id,
                    _currentUserService.UserId,
                    newsletter.Status,
                    stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UnpublishNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

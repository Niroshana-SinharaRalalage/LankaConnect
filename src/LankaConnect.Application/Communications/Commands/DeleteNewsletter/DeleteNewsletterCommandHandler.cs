using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.DeleteNewsletter;

/// <summary>
/// Phase 6A.74: Handler for deleting newsletters
/// Only Draft newsletters can be deleted
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "DeleteNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteNewsletter START: NewsletterId={NewsletterId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "DeleteNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId);

                // Authorization: Only creator or admin can delete
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("You do not have permission to delete this newsletter");
                }

                // Validate newsletter can be deleted (only Draft newsletters)
                if (!newsletter.CanDelete())
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteNewsletter FAILED: Only Draft newsletters can be deleted - NewsletterId={NewsletterId}, CurrentStatus={Status}, Duration={ElapsedMs}ms",
                        request.Id,
                        newsletter.Status,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Only Draft newsletters can be deleted");
                }

                _logger.LogInformation(
                    "DeleteNewsletter: Removing newsletter - NewsletterId={NewsletterId}",
                    request.Id);

                // Remove newsletter
                _newsletterRepository.Remove(newsletter);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "DeleteNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

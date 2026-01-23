using System.Diagnostics;
using Hangfire;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.BackgroundJobs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SendNewsletter;

/// <summary>
/// Phase 6A.74: Handler for sending newsletters
/// Queues Hangfire background job for email delivery
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class SendNewsletterCommandHandler : ICommandHandler<SendNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SendNewsletterCommandHandler> _logger;

    public SendNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<SendNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SendNewsletterCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendNewsletter START: NewsletterId={NewsletterId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "SendNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}, SentAt={SentAt}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId,
                    newsletter.SentAt);

                // Authorization: Only creator or admin can send
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("You do not have permission to send this newsletter");
                }

                // Validate newsletter can be sent
                if (!newsletter.CanSendEmail())
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendNewsletter FAILED: Newsletter cannot be sent - NewsletterId={NewsletterId}, Status={Status}, AlreadySent={AlreadySent}, Duration={ElapsedMs}ms",
                        request.Id,
                        newsletter.Status,
                        newsletter.SentAt.HasValue,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter cannot be sent (must be Active and not already sent)");
                }

                _logger.LogInformation(
                    "SendNewsletter: Newsletter validation passed - NewsletterId={NewsletterId}, queueing background job",
                    request.Id);

                // Queue background job for email sending
                var jobId = _backgroundJobClient.Enqueue<NewsletterEmailJob>(
                    job => job.ExecuteAsync(request.Id));

                _logger.LogInformation(
                    "SendNewsletter: Queued NewsletterEmailJob - NewsletterId={NewsletterId}, JobId={JobId}",
                    request.Id,
                    jobId);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, JobId={JobId}, Duration={ElapsedMs}ms (emails will be sent asynchronously in background)",
                    request.Id,
                    _currentUserService.UserId,
                    jobId,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

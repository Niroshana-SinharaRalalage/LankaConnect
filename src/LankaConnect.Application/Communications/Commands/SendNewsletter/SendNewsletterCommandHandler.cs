using Hangfire;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.BackgroundJobs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.SendNewsletter;

/// <summary>
/// Phase 6A.74: Handler for sending newsletters
/// Queues Hangfire background job for email delivery
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
        _logger.LogInformation(
            "[Phase 6A.74] SendNewsletterCommandHandler STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.Id, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
                return Result<bool>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can send
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<bool>.Failure("You do not have permission to send this newsletter");

            // Validate newsletter can be sent
            if (!newsletter.CanSendEmail())
                return Result<bool>.Failure("Newsletter cannot be sent (must be Active and not already sent)");

            // Queue background job for email sending
            var jobId = _backgroundJobClient.Enqueue<NewsletterEmailJob>(
                job => job.ExecuteAsync(request.Id));

            _logger.LogInformation(
                "[Phase 6A.74] Queued NewsletterEmailJob for newsletter {NewsletterId} with job ID {JobId}. " +
                "Emails will be sent asynchronously in background.",
                request.Id, jobId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR queueing newsletter send job - Newsletter {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);
            throw;
        }
    }
}

using Hangfire;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.SendNewsletter;

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
        try
        {
            _logger.LogInformation("[Newsletter] Sending newsletter: {NewsletterID}", request.Id);

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
                _logger.LogWarning("[Newsletter] Unauthorized send attempt by user {UserID}", _currentUserService.UserId);
                return Result<bool>.Failure("You do not have permission to send this newsletter");
            }

            if (!newsletter.CanSendEmail())
            {
                _logger.LogWarning("[Newsletter] Cannot send email - Status: {Status}, SentAt: {SentAt}",
                    newsletter.Status, newsletter.SentAt);
                return Result<bool>.Failure("Newsletter must be Active and not already sent");
            }

            // Enqueue background job for async email sending
            // Note: Background job class will be created separately
            _backgroundJobClient.Enqueue(
                "SendNewsletterEmailJob",
                "ExecuteAsync",
                new object[] { request.Id, CancellationToken.None });

            _logger.LogInformation("[Newsletter] Newsletter email job enqueued: {NewsletterID}", request.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Newsletter] Error sending newsletter: {NewsletterID}", request.Id);
            return Result<bool>.Failure("An error occurred while sending the newsletter");
        }
    }
}

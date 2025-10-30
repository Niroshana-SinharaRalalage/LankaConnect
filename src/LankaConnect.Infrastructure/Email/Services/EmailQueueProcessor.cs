using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Background service that processes queued emails from the database
/// Implements retry logic with exponential backoff for failed emails
/// </summary>
public class EmailQueueProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueProcessor> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly TimeSpan _processingInterval;

    public EmailQueueProcessor(
        IServiceProvider serviceProvider,
        ILogger<EmailQueueProcessor> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _processingInterval = TimeSpan.FromSeconds(_emailSettings.ProcessingIntervalInSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Queue Processor is starting");

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing email queue");
            }

            // Wait for next processing interval
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Email Queue Processor is stopping");
    }

    private async Task ProcessQueuedEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailMessageRepository = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Get queued and failed emails that are ready for processing
            var queuedEmails = await emailMessageRepository.GetQueuedEmailsAsync(
                _emailSettings.BatchSize,
                cancellationToken);

            if (!queuedEmails.Any())
            {
                _logger.LogDebug("No queued emails to process");
                return;
            }

            _logger.LogInformation("Processing {Count} queued emails", queuedEmails.Count);

            foreach (var emailMessage in queuedEmails)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await ProcessSingleEmailAsync(emailMessage, emailService, emailMessageRepository, unitOfWork, cancellationToken);
            }

            _logger.LogInformation("Finished processing email queue batch");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessQueuedEmailsAsync");
        }
    }

    private async Task ProcessSingleEmailAsync(
        Domain.Communications.Entities.EmailMessage emailMessage,
        IEmailService emailService,
        IEmailMessageRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if email should be retried (exponential backoff logic)
            if (!ShouldRetryEmail(emailMessage))
            {
                _logger.LogWarning("Email {EmailId} exceeded retry attempts or is not ready for retry", emailMessage.Id);
                return;
            }

            _logger.LogDebug("Processing email {EmailId} to {Recipients}",
                emailMessage.Id,
                string.Join(", ", emailMessage.ToEmails));

            // Convert domain entity to DTO
            var emailDto = new EmailMessageDto
            {
                ToEmail = emailMessage.ToEmails.FirstOrDefault() ?? string.Empty,
                ToName = emailMessage.ToEmails.FirstOrDefault() ?? string.Empty,
                Subject = emailMessage.Subject.Value,
                HtmlBody = emailMessage.HtmlContent ?? string.Empty,
                PlainTextBody = emailMessage.TextContent,
                Priority = emailMessage.Priority
            };

            // Send email
            var result = await emailService.SendEmailAsync(emailDto, cancellationToken);

            if (result.IsSuccess)
            {
                emailMessage.MarkAsSent();
                _logger.LogInformation("Email {EmailId} sent successfully", emailMessage.Id);
            }
            else
            {
                emailMessage.MarkAsFailed(result.Error);
                _logger.LogWarning("Email {EmailId} failed to send: {Error}", emailMessage.Id, result.Error);

                // Schedule retry with exponential backoff
                ScheduleRetry(emailMessage);
            }

            repository.Update(emailMessage);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email {EmailId}", emailMessage.Id);

            try
            {
                emailMessage.MarkAsFailed($"Exception: {ex.Message}");
                ScheduleRetry(emailMessage);
                repository.Update(emailMessage);
                await unitOfWork.CommitAsync(cancellationToken);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to update email status for {EmailId}", emailMessage.Id);
            }
        }
    }

    private bool ShouldRetryEmail(Domain.Communications.Entities.EmailMessage emailMessage)
    {
        // Check if max retry attempts reached
        if (emailMessage.RetryCount >= _emailSettings.MaxRetryAttempts)
        {
            _logger.LogWarning("Email {EmailId} exceeded max retry attempts ({MaxRetries})",
                emailMessage.Id, _emailSettings.MaxRetryAttempts);
            return false;
        }

        // For queued emails, always process
        if (emailMessage.Status == EmailStatus.Queued)
        {
            return true;
        }

        // For failed emails, check if enough time has passed for retry
        if (emailMessage.Status == EmailStatus.Failed && emailMessage.NextRetryAt.HasValue)
        {
            if (DateTime.UtcNow >= emailMessage.NextRetryAt.Value)
            {
                return true;
            }

            _logger.LogDebug("Email {EmailId} not ready for retry. Next retry at {NextRetry}",
                emailMessage.Id, emailMessage.NextRetryAt.Value);
            return false;
        }

        return false;
    }

    private void ScheduleRetry(Domain.Communications.Entities.EmailMessage emailMessage)
    {
        var retryDelay = CalculateRetryDelay(emailMessage.RetryCount);
        var nextRetryTime = DateTime.UtcNow.AddMinutes(retryDelay);

        _logger.LogInformation("Scheduling retry for email {EmailId}. Retry #{Retry} scheduled for {NextRetry}",
            emailMessage.Id,
            emailMessage.RetryCount + 1,
            nextRetryTime);
    }

    private int CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: 2^retryCount * base delay
        // e.g., retry 0: 5 min, retry 1: 10 min, retry 2: 20 min, retry 3: 40 min
        var baseDelay = _emailSettings.RetryDelayInMinutes;
        var exponentialDelay = Math.Pow(2, retryCount) * baseDelay;

        // Cap at 2 hours to prevent excessive delays
        return (int)Math.Min(exponentialDelay, 120);
    }

}

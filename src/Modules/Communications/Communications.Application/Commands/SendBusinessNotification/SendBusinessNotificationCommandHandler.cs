using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts; // W4.7.b
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Commands.SendBusinessNotification;

/// <summary>
/// Handler for sending business notification emails
/// Phase 6A.100: Migrated to ITypedEmailService with BusinessNotificationEmailParams
/// </summary>
public class SendBusinessNotificationCommandHandler : IRequestHandler<SendBusinessNotificationCommand, Result<SendBusinessNotificationResponse>>
{
    private readonly IIdentityQueries _identityQueries;
    private readonly IBusinessRepository _businessRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly ILogger<SendBusinessNotificationCommandHandler> _logger;

    public SendBusinessNotificationCommandHandler(
        IIdentityQueries identityQueries,
        IBusinessRepository businessRepository,
        ITypedEmailService typedEmailService,
        ILogger<SendBusinessNotificationCommandHandler> logger)
    {
        _identityQueries = identityQueries;
        _businessRepository = businessRepository;
        _typedEmailService = typedEmailService;
        _logger = logger;
    }

    public async Task<Result<SendBusinessNotificationResponse>> Handle(SendBusinessNotificationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendBusinessNotification"))
        using (LogContext.PushProperty("EntityType", "BusinessNotification"))
        using (LogContext.PushProperty("UserId", request.UserId))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        using (LogContext.PushProperty("NotificationType", request.NotificationType))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.100] SendBusinessNotification START: UserId={UserId}, BusinessId={BusinessId}, NotificationType={NotificationType}, Subject={Subject}",
                request.UserId,
                request.BusinessId,
                request.NotificationType,
                request.Subject);

            try
            {
                // Get user via Identity.Contracts (W4.7.b)
                var user = await _identityQueries.GetContactInfoAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendBusinessNotification FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendBusinessNotificationResponse>.Failure("User not found");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendBusinessNotification FAILED: User is inactive - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendBusinessNotificationResponse>.Failure("Cannot send notification to inactive user");
                }

                // Get business
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendBusinessNotification FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendBusinessNotificationResponse>.Failure("Business not found");
                }

                _logger.LogInformation(
                    "SendBusinessNotification: Entities found - UserEmail={UserEmail}, BusinessName={BusinessName}",
                    user.Email,
                    business.Profile.Name);

                // Phase 6A.100: Create typed email parameters
                var emailParams = BusinessNotificationEmailParams.Create(
                    notificationType: request.NotificationType,
                    userId: user.Id,
                    userName: user.DisplayName,
                    userEmail: user.Email,
                    firstName: user.FirstName,
                    businessId: business.Id,
                    businessName: business.Profile.Name,
                    businessCategory: business.Category.ToString(),
                    businessStatus: business.Status.ToString(),
                    subject: request.Subject,
                    additionalData: request.Data);

                _logger.LogDebug(
                    "SendBusinessNotification: Created BusinessNotificationEmailParams - Template={Template}, Recipient={Recipient}",
                    emailParams.TemplateName,
                    emailParams.RecipientEmail);

                // Phase 6A.100: Send email via ITypedEmailService
                var sendResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (!sendResult.Success)
                {
                    _logger.LogWarning(
                        "SendBusinessNotification FAILED: Email send failed - Email={Email}, BusinessId={BusinessId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email,
                        request.BusinessId,
                        string.Join(", ", sendResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendBusinessNotificationResponse>.Failure("Failed to send notification email");
                }

                var response = new SendBusinessNotificationResponse(
                    request.BusinessId,
                    request.UserId,
                    user.Email,
                    request.NotificationType,
                    request.Subject,
                    DateTime.UtcNow);

                _logger.LogInformation(
                    "[Phase 6A.100] SendBusinessNotification COMPLETE: Email={Email}, BusinessId={BusinessId}, NotificationType={NotificationType}, CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                    user.Email,
                    request.BusinessId,
                    request.NotificationType,
                    sendResult.CorrelationId,
                    stopwatch.ElapsedMilliseconds);

                return Result<SendBusinessNotificationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendBusinessNotification FAILED: Unexpected error - BusinessId={BusinessId}, UserId={UserId}, NotificationType={NotificationType}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.BusinessId,
                    request.UserId,
                    request.NotificationType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.WhatsApp.Commands.SendTestWhatsApp;

/// <summary>
/// Phase 7A.2: Admin-only command to send a test WhatsApp message using a template.
/// Uses IWhatsAppService.SendTemplateMessageToPhoneAsync directly for testing purposes.
/// </summary>
public record SendTestWhatsAppCommand(
    Guid AdminUserId,
    string PhoneNumber,
    string TemplateName,
    Dictionary<string, string> Parameters) : ICommand<SendTestWhatsAppResponse>;

/// <summary>
/// Response returned after sending a test WhatsApp message.
/// </summary>
public class SendTestWhatsAppResponse
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for SendTestWhatsAppCommand.
/// Sends a template-based WhatsApp message via IWhatsAppService for admin testing.
/// </summary>
public class SendTestWhatsAppCommandHandler : IRequestHandler<SendTestWhatsAppCommand, Result<SendTestWhatsAppResponse>>
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IOptions<WhatsAppOptions> _settings;
    private readonly ILogger<SendTestWhatsAppCommandHandler> _logger;

    public SendTestWhatsAppCommandHandler(
        IWhatsAppService whatsAppService,
        IOptions<WhatsAppOptions> settings,
        ILogger<SendTestWhatsAppCommandHandler> logger)
    {
        _whatsAppService = whatsAppService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<SendTestWhatsAppResponse>> Handle(
        SendTestWhatsAppCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendTestWhatsApp"))
        using (LogContext.PushProperty("AdminUserId", request.AdminUserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendTestWhatsApp START: AdminUserId={AdminUserId}, PhoneNumber={PhoneNumber}, TemplateName={TemplateName}",
                request.AdminUserId, request.PhoneNumber, request.TemplateName);

            try
            {
                if (!_settings.Value.Enabled)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendTestWhatsApp FAILED: WhatsApp feature is globally disabled. AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                        request.AdminUserId, stopwatch.ElapsedMilliseconds);
                    return Result<SendTestWhatsAppResponse>.Failure("WhatsApp messaging is currently disabled.");
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    stopwatch.Stop();
                    return Result<SendTestWhatsAppResponse>.Failure("Phone number is required.");
                }

                if (string.IsNullOrWhiteSpace(request.TemplateName))
                {
                    stopwatch.Stop();
                    return Result<SendTestWhatsAppResponse>.Failure("Template name is required.");
                }

                var sendResult = await _whatsAppService.SendTemplateMessageToPhoneAsync(
                    request.PhoneNumber,
                    request.TemplateName,
                    request.Parameters,
                    eventId: null,
                    cancellationToken);

                if (sendResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendTestWhatsApp FAILED: Send failed - AdminUserId={AdminUserId}, TemplateName={TemplateName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AdminUserId, request.TemplateName, sendResult.Error, stopwatch.ElapsedMilliseconds);

                    var failedResponse = new SendTestWhatsAppResponse
                    {
                        Success = false,
                        MessageId = null,
                        ErrorMessage = sendResult.Error
                    };
                    return Result<SendTestWhatsAppResponse>.Success(failedResponse);
                }

                var result = sendResult.Value;

                if (result.WasSkipped)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "SendTestWhatsApp SKIPPED: AdminUserId={AdminUserId}, TemplateName={TemplateName}, Reason={SkipReason}, Duration={ElapsedMs}ms",
                        request.AdminUserId, request.TemplateName, result.SkipReason, stopwatch.ElapsedMilliseconds);

                    var skippedResponse = new SendTestWhatsAppResponse
                    {
                        Success = false,
                        MessageId = null,
                        ErrorMessage = result.SkipReason
                    };
                    return Result<SendTestWhatsAppResponse>.Success(skippedResponse);
                }

                var response = new SendTestWhatsAppResponse
                {
                    Success = true,
                    MessageId = result.AcsMessageId,
                    ErrorMessage = null
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendTestWhatsApp COMPLETE: AdminUserId={AdminUserId}, TemplateName={TemplateName}, MessageId={MessageId}, Duration={ElapsedMs}ms",
                    request.AdminUserId, request.TemplateName, result.AcsMessageId, stopwatch.ElapsedMilliseconds);

                return Result<SendTestWhatsAppResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendTestWhatsApp FAILED: Unexpected error - AdminUserId={AdminUserId}, TemplateName={TemplateName}, Duration={ElapsedMs}ms",
                    request.AdminUserId, request.TemplateName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

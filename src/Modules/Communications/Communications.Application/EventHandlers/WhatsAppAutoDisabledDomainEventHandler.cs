using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.Modules.Communications.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Contracts; // W4.7.b
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Communications.Application.EventHandlers;

/// <summary>
/// Phase 7D Fix 4: dispatches the "we turned WhatsApp off" email when the daily background
/// job calls <see cref="Domain.Communications.Entities.UserWhatsAppPreferences.AutoDisableUnverified"/>.
///
/// Architecture: raised as a domain event (not a direct email call inside the job) so the
/// same flow runs for any future administrative / compliance-driven auto-disable path —
/// the aggregate method stays the single seam that fires the notification.
///
/// Fire-and-forget: follows the MEMORY 6A.122 pattern — captures every local before Task.Run
/// so the DI scope that holds DbContext / repositories can be disposed without pulling the
/// rug out from under the background email send.
/// </summary>
public class WhatsAppAutoDisabledDomainEventHandler
    : INotificationHandler<DomainEventNotification<WhatsAppAutoDisabledDomainEvent>>
{
    // Phase 7D Fix 4: default when config is missing — stays in sync with the appsettings default.
    private const int DefaultGracePeriodDays = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdentityQueries _identityQueries;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly ILogger<WhatsAppAutoDisabledDomainEventHandler> _logger;

    public WhatsAppAutoDisabledDomainEventHandler(
        IServiceScopeFactory scopeFactory,
        IIdentityQueries identityQueries,
        IEmailUrlHelper emailUrlHelper,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<WhatsAppAutoDisabledDomainEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _identityQueries = identityQueries;
        _emailUrlHelper = emailUrlHelper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<WhatsAppAutoDisabledDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            var user = await _identityQueries.GetContactInfoAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "WhatsAppAutoDisabled email skipped: user not found. UserId={UserId}",
                    domainEvent.UserId);
                return;
            }

            var gracePeriodDays = int.TryParse(
                _configuration["WhatsAppSettings:UnverifiedGracePeriodDays"],
                out var configuredDays) && configuredDays > 0
                ? configuredDays
                : DefaultGracePeriodDays;

            var emailParams = new WhatsAppAutoDisabledEmailParams
            {
                UserName = user.FirstName,
                UserEmail = user.Email,
                MaskedPhone = MaskPhone(domainEvent.PhoneNumber),
                EnabledAt = domainEvent.OccurredAt.ToString("MMMM d, yyyy"),
                GracePeriodDays = gracePeriodDays,
                ReEnableUrl = $"{_emailUrlHelper.GetFrontendBaseUrl()}/profile/whatsapp"
            };

            var capturedParams = emailParams;
            var capturedEmail = user.Email;
            var capturedUserId = domainEvent.UserId;
            var capturedReason = domainEvent.Reason;
            var capturedScopeFactory = _scopeFactory;

            _logger.LogInformation(
                "WhatsAppAutoDisabled: dispatching email async. UserId={UserId}, Reason={Reason}",
                capturedUserId, capturedReason);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                    var emailResult = await emailService.SendEmailAsync(capturedParams, CancellationToken.None);
                    if (emailResult.Success)
                    {
                        _logger.LogInformation(
                            "WhatsAppAutoDisabled EMAIL SENT: Email={Email}, UserId={UserId}",
                            capturedEmail, capturedUserId);
                    }
                    else
                    {
                        _logger.LogError(
                            "WhatsAppAutoDisabled EMAIL FAILED: Email={Email}, Errors={Errors}",
                            capturedEmail, string.Join(", ", emailResult.Errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "WhatsAppAutoDisabled EMAIL EXCEPTION: Email={Email}, UserId={UserId}",
                        capturedEmail, capturedUserId);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Fail-silent: the background job has already committed the auto-disable.
            // Throwing here would rollback nothing (domain event fires post-save) and
            // would only surface as an unhandled MediatR pipeline exception.
            _logger.LogError(ex,
                "WhatsAppAutoDisabled handler failed. UserId={UserId}",
                domainEvent.UserId);
        }
    }

    /// <summary>
    /// "+12343513717" → "•••• 3717". Null/short input returns a safe placeholder rather
    /// than a raw number — defense-in-depth against accidental PII leak into emails.
    /// </summary>
    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "••••";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "••••";
        return $"•••• {digits[^4..]}";
    }
}

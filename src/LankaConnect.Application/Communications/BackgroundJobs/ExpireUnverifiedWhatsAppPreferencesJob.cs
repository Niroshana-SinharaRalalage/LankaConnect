using System.Diagnostics;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.BackgroundJobs;

/// <summary>
/// Phase 7D Fix 4: daily job that auto-disables WhatsApp for users who turned it on
/// but never completed phone verification within the configured grace window.
///
/// Why a job and not a request-time check: the user who never returns to verify never
/// triggers a request, so the only way to close the loop is a scheduled sweep.
///
/// Grace window: <c>WhatsAppSettings:UnverifiedGracePeriodDays</c> (default 30). The
/// partial index <c>IX_UserWhatsAppPreferences_EnabledAt_EnabledUnverified</c>
/// (WHERE whatsapp_enabled = true AND phone_verified = false) keeps the scan cheap.
///
/// Per-row guard: we re-check <c>WhatsAppEnabled</c> + <c>!PhoneVerified</c> on the
/// tracked entity before calling <see cref="Domain.Communications.Entities.UserWhatsAppPreferences.AutoDisableUnverified"/>.
/// This is belt-and-braces against a rare race where the user verifies between the
/// query and the loop iteration (the domain method would return Failure either way —
/// we just don't want to log it as a skip when the rows should never appear).
///
/// Domain event: the aggregate raises <c>WhatsAppAutoDisabledDomainEvent</c> post-save,
/// which MediatR bridges to <c>WhatsAppAutoDisabledDomainEventHandler</c> for the
/// notification email. The job itself never sees email concerns.
/// </summary>
public class ExpireUnverifiedWhatsAppPreferencesJob
{
    private const int DefaultGracePeriodDays = 30;

    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpireUnverifiedWhatsAppPreferencesJob> _logger;

    public ExpireUnverifiedWhatsAppPreferencesJob(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<ExpireUnverifiedWhatsAppPreferencesJob> logger)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using (LogContext.PushProperty("Operation", "ExpireUnverifiedWhatsAppPreferences"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid();

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                var graceDays = int.TryParse(
                    _configuration["WhatsAppSettings:UnverifiedGracePeriodDays"],
                    out var configuredDays) && configuredDays > 0
                    ? configuredDays
                    : DefaultGracePeriodDays;
                var cutoff = DateTime.UtcNow.AddDays(-graceDays);

                _logger.LogInformation(
                    "UnverifiedWhatsAppAutoDisable START: CorrelationId={CorrelationId}, GraceDays={GraceDays}, Cutoff={Cutoff:o}",
                    correlationId, graceDays, cutoff);

                try
                {
                    var stalePreferences = await _preferencesRepository.GetStaleUnverifiedAsync(cutoff);

                    if (stalePreferences.Count == 0)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "UnverifiedWhatsAppAutoDisabled: Count=0, Duration={ElapsedMs}ms, CorrelationId={CorrelationId}",
                            stopwatch.ElapsedMilliseconds, correlationId);
                        return;
                    }

                    var disabledCount = 0;
                    var skippedCount = 0;
                    var failedCount = 0;

                    foreach (var prefs in stalePreferences)
                    {
                        try
                        {
                            // Defensive re-check: the query already filters on these, but a
                            // concurrent verify between query + loop iteration could flip them.
                            if (!prefs.WhatsAppEnabled || prefs.PhoneVerified)
                            {
                                skippedCount++;
                                continue;
                            }

                            var result = prefs.AutoDisableUnverified(WhatsAppAutoDisableReason.UnverifiedGraceExpired);
                            if (!result.IsSuccess)
                            {
                                skippedCount++;
                                _logger.LogDebug(
                                    "UnverifiedWhatsAppAutoDisable SKIP: UserId={UserId}, Reason={Reason}",
                                    prefs.UserId, result.Error);
                                continue;
                            }

                            disabledCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex,
                                "UnverifiedWhatsAppAutoDisable row FAILED: UserId={UserId}, Error={ErrorMessage}",
                                prefs.UserId, ex.Message);
                        }
                    }

                    if (disabledCount > 0)
                    {
                        // CommitAsync flushes the UPDATE rows AND dispatches the
                        // WhatsAppAutoDisabledDomainEvent → MediatR → email handler for each.
                        await _unitOfWork.CommitAsync();
                    }

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "UnverifiedWhatsAppAutoDisabled: Count={Count}, Skipped={Skipped}, Failed={Failed}, Duration={ElapsedMs}ms, CorrelationId={CorrelationId}",
                        disabledCount, skippedCount, failedCount, stopwatch.ElapsedMilliseconds, correlationId);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex,
                        "UnverifiedWhatsAppAutoDisable FAILED: CorrelationId={CorrelationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                        correlationId, stopwatch.ElapsedMilliseconds, ex.Message);
                    throw; // re-throw for Hangfire retry
                }
            }
        }
    }
}

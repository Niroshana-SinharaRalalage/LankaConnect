using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.81: Background job that runs hourly to mark expired Preliminary registrations as Abandoned
///
/// Purpose: Clean up registrations where users started checkout but never completed payment
/// - Finds Preliminary registrations older than 25 hours (Stripe expires at 24h)
/// - Marks them as Abandoned (frees up email for retry)
/// - Prevents capacity from being consumed by unpaid registrations
///
/// Schedule: Hourly (Stripe checkout sessions expire at 24 hours)
/// Retention: Abandoned registrations soft-deleted after 30 days (separate job)
/// </summary>
public class CleanupAbandonedRegistrationsJob
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleanupAbandonedRegistrationsJob> _logger;

    // Stripe checkout sessions expire at 24 hours
    // We wait 25 hours to ensure we don't race with late-arriving webhooks
    private const int ExpirationHours = 25;

    public CleanupAbandonedRegistrationsJob(
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        ILogger<CleanupAbandonedRegistrationsJob> logger)
    {
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using (LogContext.PushProperty("Operation", "CleanupAbandonedRegistrations"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("Phase", "6A.81"))
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid();

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation(
                    "[Phase 6A.81] [Cleanup-Job-START] Beginning cleanup of abandoned registrations - CorrelationId={CorrelationId}, ExpirationHours={Hours}",
                    correlationId, ExpirationHours);

                try
                {
                    var now = DateTime.UtcNow;
                    var cutoffTime = now.AddHours(-ExpirationHours);

                    _logger.LogDebug(
                        "[Phase 6A.81] [Cleanup-Job-1] Query parameters - Now={Now}, Cutoff={Cutoff}, TargetStatus=Preliminary",
                        now.ToString("o"), cutoffTime.ToString("o"));

                    // Get all Preliminary registrations older than cutoff time
                    var preliminaryRegistrations = await _registrationRepository.GetByStatusAsync(
                        RegistrationStatus.Preliminary,
                        cancellationToken: default);

                    var expiredRegistrations = preliminaryRegistrations
                        .Where(r => r.CreatedAt < cutoffTime)
                        .ToList();

                    if (!expiredRegistrations.Any())
                    {
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "[Phase 6A.81] [Cleanup-Job-COMPLETE] No abandoned registrations found - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                            correlationId, stopwatch.ElapsedMilliseconds);
                        return;
                    }

                    _logger.LogInformation(
                        "[Phase 6A.81] [Cleanup-Job-2] Found {Count} expired Preliminary registrations - CorrelationId={CorrelationId}, OldestCreatedAt={OldestDate}, CutoffTime={Cutoff}",
                        expiredRegistrations.Count,
                        correlationId,
                        expiredRegistrations.Min(r => r.CreatedAt).ToString("o"),
                        cutoffTime.ToString("o"));

                    var abandonedCount = 0;
                    var failedCount = 0;
                    var errors = new List<string>();

                    // Mark each expired registration as Abandoned
                    foreach (var registration in expiredRegistrations)
                    {
                        try
                        {
                            var age = now - registration.CreatedAt;

                            _logger.LogDebug(
                                "[Phase 6A.81] [Cleanup-Job-Processing] Processing registration {RegistrationId} - Age={AgeHours}h, CreatedAt={CreatedAt}, ExpiresAt={ExpiresAt}",
                                registration.Id,
                                age.TotalHours,
                                registration.CreatedAt.ToString("o"),
                                registration.CheckoutSessionExpiresAt?.ToString("o") ?? "null");

                            // Use domain method to mark as abandoned
                            var result = registration.MarkAbandoned();

                            if (result.IsSuccess)
                            {
                                _registrationRepository.Update(registration);
                                abandonedCount++;

                                _logger.LogInformation(
                                    "[Phase 6A.81] [Cleanup-Job-Abandoned] Marked registration as Abandoned - RegistrationId={RegistrationId}, EventId={EventId}, Age={AgeHours}h, Email={Email}",
                                    registration.Id,
                                    registration.EventId,
                                    Math.Round(age.TotalHours, 2),
                                    registration.Contact?.Email ?? "N/A");
                            }
                            else
                            {
                                failedCount++;
                                var error = $"RegistrationId={registration.Id}: {result.Error}";
                                errors.Add(error);

                                _logger.LogWarning(
                                    "[Phase 6A.81] [Cleanup-Job-Failed] Failed to mark registration as Abandoned - RegistrationId={RegistrationId}, Error={Error}, CurrentStatus={Status}",
                                    registration.Id, result.Error, registration.Status);
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            var error = $"RegistrationId={registration.Id}: {ex.Message}";
                            errors.Add(error);

                            _logger.LogError(ex,
                                "[Phase 6A.81] [Cleanup-Job-Exception] Exception while processing registration - RegistrationId={RegistrationId}, EventId={EventId}",
                                registration.Id, registration.EventId);
                        }
                    }

                    // Commit all changes in a single transaction
                    if (abandonedCount > 0)
                    {
                        await _unitOfWork.CommitAsync();

                        _logger.LogInformation(
                            "[Phase 6A.81] [Cleanup-Job-Committed] Database transaction committed - AbandonedCount={AbandonedCount}, FailedCount={FailedCount}",
                            abandonedCount, failedCount);
                    }

                    stopwatch.Stop();

                    // Log summary with all relevant metrics
                    _logger.LogInformation(
                        "[Phase 6A.81] [Cleanup-Job-COMPLETE] Cleanup finished successfully - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms, TotalFound={TotalCount}, Abandoned={AbandonedCount}, Failed={FailedCount}, Errors={ErrorCount}",
                        correlationId,
                        stopwatch.ElapsedMilliseconds,
                        expiredRegistrations.Count,
                        abandonedCount,
                        failedCount,
                        errors.Count);

                    // Log errors for debugging if any failed
                    if (errors.Any())
                    {
                        _logger.LogWarning(
                            "[Phase 6A.81] [Cleanup-Job-Errors] Some registrations failed to mark as Abandoned: {Errors}",
                            string.Join("; ", errors));
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    _logger.LogError(ex,
                        "[Phase 6A.81] [Cleanup-Job-FAILED] Job execution failed - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                        correlationId, stopwatch.ElapsedMilliseconds, ex.Message);

                    throw; // Re-throw for Hangfire retry mechanism
                }
            }
        }
    }
}

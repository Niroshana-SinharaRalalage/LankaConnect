using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 6A.99: Background service to cleanup expired email failure records.
///
/// Purpose:
/// - GDPR compliance: Remove personal data (encrypted emails) after retention period
/// - Database hygiene: Prevent email_failure_details table from growing indefinitely
/// - Runs daily at 2 AM UTC to minimize impact on production traffic
///
/// Retention Policy:
/// - Records have ExpiresAt field set to 30 days from creation
/// - This service deletes records where ExpiresAt < NOW()
/// </summary>
public class EmailFailureCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailFailureCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public EmailFailureCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailFailureCleanupService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Phase 6A.99] EmailFailureCleanupService started - cleanup runs daily at 2 AM UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate delay until next 2 AM UTC
                var now = DateTime.UtcNow;
                var next2AM = now.Date.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
                var delay = next2AM - now;

                _logger.LogInformation(
                    "[Phase 6A.99] Next email failure cleanup scheduled for {NextRun} (in {Hours:F1} hours)",
                    next2AM, delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupExpiredRecordsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[Phase 6A.99] EmailFailureCleanupService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 6A.99] Error in EmailFailureCleanupService - will retry in 30 minutes");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("[Phase 6A.99] EmailFailureCleanupService stopped");
    }

    /// <summary>
    /// Deletes email failure records that have expired (ExpiresAt < NOW)
    /// </summary>
    private async Task CleanupExpiredRecordsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.99] Starting email failure cleanup...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var now = DateTime.UtcNow;

            // Count expired records first
            var expiredCount = await ((DbContext)dbContext).Set<EmailFailureDetail>()
                .Where(f => f.ExpiresAt < now)
                .CountAsync(cancellationToken);

            if (expiredCount == 0)
            {
                _logger.LogInformation("[Phase 6A.99] No expired email failure records to clean up");
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.99] Found {Count} expired email failure records to delete",
                expiredCount);

            // Use ExecuteDeleteAsync for efficient bulk deletion (EF Core 7+)
            var deletedCount = await ((DbContext)dbContext).Set<EmailFailureDetail>()
                .Where(f => f.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.99] Successfully deleted {DeletedCount} expired email failure records (GDPR compliance)",
                deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.99] Failed to cleanup expired email failure records");
        }
    }
}

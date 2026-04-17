using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Repositories;

namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 2: Background service to release expired seat holds every 60 seconds.
///
/// Seat holds expire after 10 minutes. This service:
/// 1. Queries for all active holds past their ExpiresAt time
/// 2. Marks them as Expired
/// 3. Persists changes
///
/// Runs every 60 seconds to keep seat availability accurate.
/// </summary>
public class SeatHoldCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

    public SeatHoldCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SeatHoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Phase 2] SeatHoldCleanupService started — runs every {Interval}s", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupExpiredHoldsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 2] SeatHoldCleanupService encountered an error");
            }
        }

        _logger.LogInformation("[Phase 2] SeatHoldCleanupService stopped");
    }

    private async Task CleanupExpiredHoldsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var seatHoldRepository = scope.ServiceProvider.GetRequiredService<ISeatHoldRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Domain.Common.IUnitOfWork>();

        try
        {
            var expiredHolds = await seatHoldRepository.GetExpiredActiveHoldsAsync(cancellationToken);
            var expiredList = expiredHolds.ToList();

            if (expiredList.Count == 0)
                return;

            foreach (var hold in expiredList)
            {
                hold.Expire();
            }

            await unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 2] Released {Count} expired seat holds",
                expiredList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 2] Failed to cleanup expired seat holds");
        }
    }
}

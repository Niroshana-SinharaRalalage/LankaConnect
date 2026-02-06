using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Application.Common.Interfaces;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ServiceRepository : Repository<Service>, IServiceRepository
{
    private readonly ILogger<ServiceRepository> _repoLogger;

    public ServiceRepository(
        AppDbContext context,
        ILogger<ServiceRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<Service>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.BusinessId == businessId)
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    services.Count,
                    stopwatch.ElapsedMilliseconds);

                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> GetActiveByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetActiveByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.BusinessId == businessId && s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetActiveByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    services.Count,
                    stopwatch.ElapsedMilliseconds);

                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetActiveByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Service?> GetByBusinessIdAndNameAsync(Guid businessId, string serviceName, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBusinessIdAndName"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("ServiceName", serviceName))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByBusinessIdAndNameAsync START: BusinessId={BusinessId}, ServiceName={ServiceName}",
                businessId, serviceName);

            try
            {
                if (string.IsNullOrWhiteSpace(serviceName))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetByBusinessIdAndNameAsync COMPLETE: ServiceName empty, Result=null, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return null;
                }

                var service = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BusinessId == businessId &&
                                            s.Name.ToLower() == serviceName.Trim().ToLower(),
                                            cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBusinessIdAndNameAsync COMPLETE: BusinessId={BusinessId}, ServiceName={ServiceName}, Found={Found}, Duration={ElapsedMs}ms",
                    businessId,
                    serviceName,
                    service != null,
                    stopwatch.ElapsedMilliseconds);

                return service;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBusinessIdAndNameAsync FAILED: BusinessId={BusinessId}, ServiceName={ServiceName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    serviceName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByName"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByNameAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByNameAsync COMPLETE: SearchTerm empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Service>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => EF.Functions.Like(s.Name.ToLower(), $"%{normalizedSearchTerm}%"))
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByNameAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    services.Count,
                    stopwatch.ElapsedMilliseconds);

                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByNameAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> SearchByDescriptionAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByDescription"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByDescriptionAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByDescriptionAsync COMPLETE: SearchTerm empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Service>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => EF.Functions.Like(s.Description.ToLower(), $"%{normalizedSearchTerm}%"))
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByDescriptionAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    services.Count,
                    stopwatch.ElapsedMilliseconds);

                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByDescriptionAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> GetByPriceRangeAsync(Money minPrice, Money maxPrice, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByPriceRange"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("MinPrice", minPrice.Amount))
        using (LogContext.PushProperty("MaxPrice", maxPrice.Amount))
        using (LogContext.PushProperty("Currency", minPrice.Currency))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByPriceRangeAsync START: MinPrice={MinPrice}, MaxPrice={MaxPrice}, Currency={Currency}",
                minPrice.Amount, maxPrice.Amount, minPrice.Currency);

            try
            {
                if (minPrice.Currency != maxPrice.Currency)
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetByPriceRangeAsync COMPLETE: Currency mismatch, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Service>();
                }

                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.Price != null &&
                               s.Price.Currency == minPrice.Currency &&
                               s.Price.Amount >= minPrice.Amount &&
                               s.Price.Amount <= maxPrice.Amount)
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Price!.Amount)
                    .ThenBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByPriceRangeAsync COMPLETE: MinPrice={MinPrice}, MaxPrice={MaxPrice}, Currency={Currency}, Count={Count}, Duration={ElapsedMs}ms",
                    minPrice.Amount,
                    maxPrice.Amount,
                    minPrice.Currency,
                    services.Count,
                    stopwatch.ElapsedMilliseconds);

                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByPriceRangeAsync FAILED: MinPrice={MinPrice}, MaxPrice={MaxPrice}, Currency={Currency}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    minPrice.Amount,
                    maxPrice.Amount,
                    minPrice.Currency,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> GetFreeServicesAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetFreeServices"))
        using (LogContext.PushProperty("EntityType", "Service"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetFreeServicesAsync START");

            try
            {
                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.Price == null || s.Price.Amount == 0)
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetFreeServicesAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    services.Count, stopwatch.ElapsedMilliseconds);
                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetFreeServicesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<int> GetServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetServiceCountByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetServiceCountByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var count = await _dbSet
                    .Where(s => s.BusinessId == businessId)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetServiceCountByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId, count, stopwatch.ElapsedMilliseconds);
                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetServiceCountByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<int> GetActiveServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveServiceCountByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetActiveServiceCountByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var count = await _dbSet
                    .Where(s => s.BusinessId == businessId && s.IsActive)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetActiveServiceCountByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId, count, stopwatch.ElapsedMilliseconds);
                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetActiveServiceCountByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Service>> GetServicesByBusinessIdsAsync(IEnumerable<Guid> businessIds, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetServicesByBusinessIds"))
        using (LogContext.PushProperty("EntityType", "Service"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetServicesByBusinessIdsAsync START");

            try
            {
                var businessIdsList = businessIds.ToList();
                if (!businessIdsList.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation("GetServicesByBusinessIdsAsync COMPLETE: Empty input, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Service>();
                }

                var services = await _dbSet
                    .AsNoTracking()
                    .Where(s => businessIdsList.Contains(s.BusinessId))
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.BusinessId)
                    .ThenBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetServicesByBusinessIdsAsync COMPLETE: BusinessIdsCount={BusinessIdsCount}, Count={Count}, Duration={ElapsedMs}ms",
                    businessIdsList.Count, services.Count, stopwatch.ElapsedMilliseconds);
                return services;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetServicesByBusinessIdsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task DeactivateAllByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "DeactivateAllByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Service"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("DeactivateAllByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var services = await _dbSet
                    .Where(s => s.BusinessId == businessId && s.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var service in services)
                {
                    service.Deactivate();
                }

                await _context.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("DeactivateAllByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, DeactivatedCount={DeactivatedCount}, Duration={ElapsedMs}ms",
                    businessId, services.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "DeactivateAllByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }
}
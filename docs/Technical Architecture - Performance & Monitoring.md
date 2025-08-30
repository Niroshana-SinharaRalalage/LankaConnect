# LankaConnect - Performance & Monitoring
## Technical Architecture Document

**Version:** 2.0 (Implementation-Focused)  
**Last Updated:** January 2025  
**Status:** Final  
**Owner:** Platform Architecture Team  
**Target Audience:** Claude Code Agents, Development Team, DevOps Engineers

---

## 1. Executive Summary

This document provides comprehensive implementation strategies for performance optimization and monitoring in the LankaConnect platform. It includes advanced multi-layer caching, performance middleware, API optimization patterns, and real-time monitoring with actionable code implementations.

### 1.1 Document Purpose
- Provide implementation-ready performance optimization patterns
- Define advanced caching strategies with distributed locking
- Establish comprehensive monitoring and observability
- Create performance middleware pipeline
- Design automated performance testing frameworks

### 1.2 Key Performance Targets
- **API Response Time:** < 200ms (p95)
- **Database Query Time:** < 50ms (average)
- **Cache Hit Rate:** > 85% minimum
- **Concurrent Users:** 10,000 target
- **System Availability:** 99.9% uptime
- **Page Load Time:** < 2 seconds
- **SignalR Latency:** < 100ms

---

## 2. Performance Architecture

### 2.1 Core Performance Strategy Configuration
```csharp
namespace LankaConnect.Infrastructure.Performance
{
    public class PerformanceStrategy
    {
        public static readonly PerformanceTargets Targets = new()
        {
            ApiResponseTimeP95 = TimeSpan.FromMilliseconds(200),
            DatabaseQueryTimeAverage = TimeSpan.FromMilliseconds(50),
            CacheHitRateMinimum = 0.85,
            ConcurrentUsersTarget = 10000,
            AvailabilityTarget = 0.999, // 99.9%
            PageLoadTimeTarget = TimeSpan.FromSeconds(2),
            SignalRLatencyTarget = TimeSpan.FromMilliseconds(100)
        };

        public static readonly PerformancePrinciples Principles = new()
        {
            CacheFirst = true,
            AsyncByDefault = true,
            LazyLoadingForHeavyOps = true,
            ConnectionPooling = true,
            MinimalDataTransfer = true,
            BackgroundProcessingForNonCritical = true,
            MemoryEfficientCollections = true,
            BatchOperationsWhenPossible = true
        };
    }
}
```

### 2.2 Performance Middleware Pipeline

#### Performance Tracking Middleware
```csharp
public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTrackingMiddleware> _logger;
    private readonly IPerformanceMetrics _metrics;
    private readonly PerformanceSettings _settings;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var startMemory = GC.GetTotalMemory(false);

        try
        {
            using var activity = Activity.StartActivity("HttpRequest");
            activity?.SetTag("http.method", context.Request.Method);
            activity?.SetTag("http.url", context.Request.Path);
            activity?.SetTag("user.id", context.User?.Identity?.Name);

            await _next(context);

            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;

            await RecordRequestMetricsAsync(context, stopwatch.Elapsed, memoryUsed, endpoint);
            await CheckPerformanceThresholdsAsync(context, stopwatch.Elapsed, endpoint);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _metrics.RecordRequestErrorAsync(endpoint, stopwatch.Elapsed, ex.GetType().Name);
            _logger.LogError(ex, "Request failed for {Endpoint} after {Duration}ms", 
                endpoint, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task RecordRequestMetricsAsync(HttpContext context, TimeSpan duration, long memoryUsed, string endpoint)
    {
        var metrics = new RequestMetrics
        {
            Endpoint = endpoint,
            Duration = duration,
            StatusCode = context.Response.StatusCode,
            MemoryUsed = memoryUsed,
            UserId = context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        };

        await _metrics.RecordRequestAsync(metrics);

        if (duration.TotalMilliseconds > _settings.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow request detected: {Endpoint} took {Duration}ms", 
                endpoint, duration.TotalMilliseconds);
        }
    }
}
```

#### Smart Rate Limiting Middleware
```csharp
public class SmartRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitSettings _settings;

    public async Task InvokeAsync(HttpContext context)
    {
        var identifier = GetClientIdentifier(context);
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var rateLimitRule = GetRateLimitRule(context);

        if (await IsRateLimitExceededAsync(identifier, endpoint, rateLimitRule))
        {
            await HandleRateLimitExceededAsync(context, identifier, endpoint);
            return;
        }

        await RecordRequestAsync(identifier, endpoint, rateLimitRule);
        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
            return $"user:{context.User.Identity.Name}";

        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            return $"api:{apiKey}";

        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    private RateLimitRule GetRateLimitRule(HttpContext context)
    {
        var endpoint = context.Request.Path.Value?.ToLowerInvariant();

        return endpoint switch
        {
            var path when path.Contains("/api/search") => _settings.SearchEndpointRule,
            var path when path.Contains("/api/upload") => _settings.UploadEndpointRule,
            var path when path.StartsWith("/api/auth") => _settings.AuthEndpointRule,
            _ => _settings.DefaultRule
        };
    }
}
```

---

## 3. Advanced Multi-Layer Caching Implementation

### 3.1 Comprehensive Caching Service
```csharp
namespace LankaConnect.Infrastructure.Performance.Caching
{
    public interface IAdvancedCacheService : ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options, CancellationToken cancellationToken = default);
        Task<Dictionary<string, T>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
        Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, CacheOptions options, CancellationToken cancellationToken = default);
        Task<bool> TryLockAsync(string lockKey, TimeSpan lockDuration, CancellationToken cancellationToken = default);
        Task ReleaseLockAsync(string lockKey, CancellationToken cancellationToken = default);
        Task WarmupAsync(CacheWarmupScope scope, CancellationToken cancellationToken = default);
        Task<CacheStatistics> GetCacheStatisticsAsync();
        Task<CacheHealth> GetCacheHealthAsync();
    }

    public class AdvancedCacheService : IAdvancedCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<AdvancedCacheService> _logger;
        private readonly CacheSettings _settings;
        private readonly ICacheMetrics _metrics;
        private readonly SemaphoreSlim _warmupSemaphore;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _lockSemaphores;

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheKey = GenerateCacheKey(key, options);

            try
            {
                // L1 Cache (Memory) Check
                if (_memoryCache.TryGetValue(cacheKey, out T memoryValue))
                {
                    await _metrics.RecordCacheOperationAsync("memory_hit", stopwatch.Elapsed, cacheKey);
                    return memoryValue;
                }

                // L2 Cache (Distributed) Check
                var distributedValue = await GetFromDistributedCacheAsync<T>(cacheKey, cancellationToken);
                if (distributedValue != null && !EqualityComparer<T>.Default.Equals(distributedValue, default(T)))
                {
                    // Promote to L1 cache
                    var memoryOptions = CreateMemoryCacheOptions(options);
                    _memoryCache.Set(cacheKey, distributedValue, memoryOptions);

                    await _metrics.RecordCacheOperationAsync("distributed_hit", stopwatch.Elapsed, cacheKey);
                    return distributedValue;
                }

                // Cache miss - execute factory with distributed locking
                var lockKey = $"lock:{cacheKey}";
                if (await TryLockAsync(lockKey, TimeSpan.FromSeconds(30), cancellationToken))
                {
                    try
                    {
                        // Double-check after acquiring lock
                        distributedValue = await GetFromDistributedCacheAsync<T>(cacheKey, cancellationToken);
                        if (distributedValue != null && !EqualityComparer<T>.Default.Equals(distributedValue, default(T)))
                        {
                            return distributedValue;
                        }

                        // Execute factory function
                        var factoryStopwatch = Stopwatch.StartNew();
                        var value = await factory();
                        factoryStopwatch.Stop();

                        if (value != null && !EqualityComparer<T>.Default.Equals(value, default(T)))
                        {
                            await SetInBothCachesAsync(cacheKey, value, options, cancellationToken);
                        }

                        await _metrics.RecordCacheOperationAsync("factory_execution", factoryStopwatch.Elapsed, cacheKey);
                        return value;
                    }
                    finally
                    {
                        await ReleaseLockAsync(lockKey, cancellationToken);
                    }
                }
                else
                {
                    // Could not acquire lock, wait briefly and try cache again
                    await Task.Delay(50, cancellationToken);
                    distributedValue = await GetFromDistributedCacheAsync<T>(cacheKey, cancellationToken);
                    if (distributedValue != null)
                        return distributedValue;

                    // Fall back to executing factory without lock
                    return await factory();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache operation failed for key: {CacheKey}", cacheKey);
                await _metrics.RecordCacheErrorAsync("get_or_set", stopwatch.Elapsed, cacheKey, ex.GetType().Name);
                return await factory();
            }
        }

        public async Task<bool> TryLockAsync(string lockKey, TimeSpan lockDuration, CancellationToken cancellationToken = default)
        {
            try
            {
                var distributedLockKey = $"distributed_lock:{lockKey}";
                var lockValue = Guid.NewGuid().ToString();
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = lockDuration
                };

                var existingLock = await _distributedCache.GetStringAsync(distributedLockKey, cancellationToken);
                if (existingLock == null)
                {
                    await _distributedCache.SetStringAsync(distributedLockKey, lockValue, options, cancellationToken);
                    
                    // Verify we got the lock
                    var verifyLock = await _distributedCache.GetStringAsync(distributedLockKey, cancellationToken);
                    if (verifyLock == lockValue)
                    {
                        var semaphore = new SemaphoreSlim(1, 1);
                        _lockSemaphores.TryAdd(lockKey, semaphore);
                        semaphore.Wait(0);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire distributed lock: {LockKey}", lockKey);
                return false;
            }
        }

        public async Task WarmupAsync(CacheWarmupScope scope, CancellationToken cancellationToken = default)
        {
            if (!await _warmupSemaphore.WaitAsync(100, cancellationToken))
            {
                _logger.LogWarning("Cache warmup already in progress, skipping");
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Starting cache warmup with scope: {Scope}", scope);

                var warmupTasks = new List<Task>
                {
                    WarmupEssentialDataAsync(cancellationToken)
                };

                if (scope >= CacheWarmupScope.Extended)
                {
                    warmupTasks.Add(WarmupExtendedDataAsync(cancellationToken));
                }

                if (scope >= CacheWarmupScope.Complete)
                {
                    warmupTasks.Add(WarmupCompleteDataAsync(cancellationToken));
                }

                await Task.WhenAll(warmupTasks);

                stopwatch.Stop();
                _logger.LogInformation("Cache warmup completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                _warmupSemaphore.Release();
            }
        }

        private async Task SetInBothCachesAsync<T>(string key, T value, CacheOptions options, CancellationToken cancellationToken)
        {
            // Set in memory cache
            var memoryOptions = CreateMemoryCacheOptions(options);
            _memoryCache.Set(key, value, memoryOptions);

            // Set in distributed cache
            var distributedOptions = new DistributedCacheEntryOptions();
            
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
                distributedOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            
            if (options.SlidingExpiration.HasValue)
                distributedOptions.SlidingExpiration = options.SlidingExpiration;

            var jsonValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, jsonValue, distributedOptions, cancellationToken);
        }
    }

    // Cache Configuration Models
    public class CacheOptions
    {
        public static readonly CacheOptions Default = new();
        
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
        public long? Size { get; set; }
        public string KeyPrefix { get; set; }
        public CacheLevel Level { get; set; } = CacheLevel.Both;
    }

    public enum CacheWarmupScope
    {
        Essential = 1,
        Extended = 2,
        Complete = 3
    }

    public class CacheSettings
    {
        public string CacheKeyPrefix { get; set; } = "lankaconnect";
        public int DefaultExpiryHours { get; set; } = 1;
        public int MemoryCacheDefaultExpiryMinutes { get; set; } = 15;
        public int MemoryCacheMaxExpiryMinutes { get; set; } = 60;
        public bool EnableCacheMetrics { get; set; } = true;
        public bool EnableCacheWarmup { get; set; } = true;
        public CacheWarmupScope DefaultWarmupScope { get; set; } = CacheWarmupScope.Essential;
        public long MemoryCacheSizeLimit { get; set; } = 100_000_000; // 100MB
    }
}
```

### 3.2 Cache Invalidation Patterns
```csharp
// Domain event-based cache invalidation
public class ServiceUpdatedEventHandler : INotificationHandler<ServiceUpdatedEvent>
{
    private readonly IDistributedCacheService _cache;

    public async Task Handle(ServiceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Invalidate related cache entries
        var patterns = new[]
        {
            $"service:{notification.ServiceId}",
            $"services:category:{notification.CategoryId}",
            $"provider:{notification.ProviderId}:services"
        };

        foreach (var pattern in patterns)
        {
            await _cache.InvalidateAsync(pattern);
        }
    }
}

// Proactive cache refresh
public class CacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheRefreshService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<IAdvancedCacheService>();
                
                await cacheService.WarmupAsync(CacheWarmupScope.Essential, stoppingToken);
                
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache refresh failed");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

---

## 4. Database Performance Optimization

### 4.1 Query Optimization Patterns
```csharp
// Optimized repository with compiled queries
public class OptimizedServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<OptimizedServiceRepository> _logger;

    // Compiled queries for performance
    private static readonly Func<AppDbContext, Guid, string, int, int, Task<List<Service>>> 
        GetServicesByCategoryCompiled = EF.CompileAsyncQuery(
            (AppDbContext context, Guid categoryId, string location, int skip, int take) =>
                context.Services
                    .AsNoTracking()
                    .Include(s => s.Provider)
                    .Include(s => s.Reviews.Take(5))
                    .Where(s => s.CategoryId == categoryId && 
                               s.Location.Contains(location) &&
                               s.IsActive)
                    .OrderByDescending(s => s.Rating)
                    .ThenByDescending(s => s.ReviewCount)
                    .Skip(skip)
                    .Take(take)
                    .ToList());

    public async Task<PagedResult<Service>> GetServicesByCategoryAsync(
        Guid categoryId, string location, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        
        // Get total count separately for performance
        var totalCount = await _context.Services
            .Where(s => s.CategoryId == categoryId && 
                       s.Location.Contains(location) &&
                       s.IsActive)
            .CountAsync();

        var items = await GetServicesByCategoryCompiled(_context, categoryId, location, skip, pageSize);

        return new PagedResult<Service>(items, totalCount, page, pageSize);
    }

    // Batch loading to avoid N+1 queries
    public async Task<List<ServiceDetailsDto>> GetServicesWithDetailsAsync(List<Guid> serviceIds)
    {
        // Load services
        var services = await _context.Services
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync();

        // Load related data in separate queries
        var serviceIdSet = services.Select(s => s.Id).ToHashSet();
        
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => serviceIdSet.Contains(r.ServiceId))
            .GroupBy(r => r.ServiceId)
            .Select(g => new 
            { 
                ServiceId = g.Key, 
                AverageRating = g.Average(r => r.Rating),
                ReviewCount = g.Count()
            })
            .ToListAsync();
            
        var images = await _context.ServiceImages
            .AsNoTracking()
            .Where(i => serviceIdSet.Contains(i.ServiceId))
            .ToListAsync();

        // Map in memory for optimal performance
        return MapToDetailsDto(services, reviews, images);
    }

    // Split query for complex relationships
    public async Task<ServiceProviderDetails> GetProviderWithServicesAsync(Guid providerId)
    {
        // Load provider
        var provider = await _context.Providers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ContactInfo)
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null) return null;

        // Load services separately to control query complexity
        var services = await _context.Services
            .AsNoTracking()
            .Where(s => s.ProviderId == providerId && s.IsActive)
            .Select(s => new ServiceSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                BasePrice = s.BasePrice,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount
            })
            .ToListAsync();

        return new ServiceProviderDetails
        {
            Provider = provider,
            Services = services
        };
    }
}
```

### 4.2 Database Connection Optimization
```csharp
// Connection pooling and resilience configuration
public class DatabaseConfiguration
{
    public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Enable sensitive data logging only in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Add interceptors for performance monitoring
            options.AddInterceptors(serviceProvider.GetRequiredService<PerformanceDbCommandInterceptor>());
        });

        // Configure connection pooling
        services.Configure<NpgsqlDataSourceBuilder>(builder =>
        {
            builder.ConnectionStringBuilder.Pooling = true;
            builder.ConnectionStringBuilder.MinPoolSize = 5;
            builder.ConnectionStringBuilder.MaxPoolSize = 100;
            builder.ConnectionStringBuilder.ConnectionLifetime = 300; // 5 minutes
            builder.ConnectionStringBuilder.ConnectionIdleLifetime = 60; // 1 minute
        });
    }
}

// Performance monitoring interceptor
public class PerformanceDbCommandInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceDbCommandInterceptor> _logger;
    private readonly IPerformanceMetrics _metrics;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        command.CommandTimeout = 30; // Ensure timeout is set
        
        return result;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration;
        
        await _metrics.RecordDatabaseQueryAsync(
            command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)),
            duration.TotalMilliseconds);

        if (duration.TotalMilliseconds > 100)
        {
            _logger.LogWarning("Slow query detected ({Duration}ms): {Query}", 
                duration.TotalMilliseconds, 
                command.CommandText);
        }

        return result;
    }
}
```

### 4.3 Index Strategy
```csharp
// Entity configurations with performance indexes
public class ServiceEntityConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        // Composite index for common queries
        builder.HasIndex(s => new { s.CategoryId, s.Location, s.IsActive })
            .HasDatabaseName("IX_Service_Category_Location_Active")
            .IncludeProperties(s => new { s.Name, s.Rating, s.BasePrice });

        // Index for sorting
        builder.HasIndex(s => new { s.Rating, s.ReviewCount })
            .HasDatabaseName("IX_Service_Rating_ReviewCount")
            .IsDescending();

        // Full-text search index
        builder.HasIndex(s => s.Name)
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("english");

        // Partial index for active services only
        builder.HasIndex(s => s.ProviderId)
            .HasDatabaseName("IX_Service_ProviderId_Active")
            .HasFilter("[IsActive] = 1");
    }
}

// Migration for adding performance indexes
public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create covering index for service searches
        migrationBuilder.Sql(@"
            CREATE INDEX IX_Service_Search_Covering 
            ON Services (CategoryId, Location, IsActive) 
            INCLUDE (Name, Rating, BasePrice, ReviewCount)
            WHERE IsActive = true;
        ");

        // Create index for timestamp-based queries
        migrationBuilder.Sql(@"
            CREATE INDEX IX_Service_CreatedAt_Desc 
            ON Services (CreatedAt DESC)
            WHERE IsActive = true;
        ");

        // Create index for geospatial queries if using PostGIS
        migrationBuilder.Sql(@"
            CREATE INDEX IX_Service_Location_Spatial 
            ON Services USING GIST (LocationPoint);
        ");
    }
}
```

## 5. API Performance & Response Optimization

### 5.1 High-Performance Controller Base
```csharp
// Optimized API Controller Base Class
namespace LankaConnect.Api.Controllers.Base
{
    [ApiController]
    [Produces("application/json")]
    public abstract class PerformantControllerBase : ControllerBase
    {
        protected readonly ILogger _logger;
        protected readonly ICacheService _cacheService;
        protected readonly IPerformanceMetrics _metrics;
        protected readonly IMapper _mapper;

        protected async Task<IActionResult> ExecuteWithPerformanceTrackingAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var startMemory = GC.GetTotalMemory(false);

            try
            {
                var result = await operation();
                
                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                var memoryUsed = endMemory - startMemory;

                await _metrics.RecordApiOperationAsync(operationName, stopwatch.Elapsed, true, memoryUsed);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _metrics.RecordApiOperationAsync(operationName, stopwatch.Elapsed, false, 0);
                _logger.LogError(ex, "Error in {Operation}", operationName);
                return StatusCode(500, new { error = "An internal error occurred" });
            }
        }

        protected async Task<IActionResult> GetWithCacheAsync<T>(
            string cacheKey,
            Func<Task<T>> dataFactory,
            TimeSpan? cacheExpiry = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                var cacheOptions = new CacheOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiry ?? TimeSpan.FromMinutes(15)
                };

                return await _cacheService.GetOrSetAsync(cacheKey, dataFactory, cacheOptions, cancellationToken);
            }, $"cached_get_{typeof(T).Name}", cancellationToken);
        }

        protected IActionResult CreatePaginationResponse<T>(PagedResult<T> pagedResult)
        {
            Response.Headers.Add("X-Total-Count", pagedResult.TotalCount.ToString());
            Response.Headers.Add("X-Page", pagedResult.Page.ToString());
            Response.Headers.Add("X-Page-Size", pagedResult.PageSize.ToString());
            Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(new
            {
                items = pagedResult.Items,
                pagination = new
                {
                    page = pagedResult.Page,
                    pageSize = pagedResult.PageSize,
                    totalCount = pagedResult.TotalCount,
                    totalPages = pagedResult.TotalPages,
                    hasNext = pagedResult.HasNext,
                    hasPrevious = pagedResult.HasPrevious
                }
            });
        }
    }
}
```

### 5.2 Response Optimization Service
```csharp
namespace LankaConnect.Infrastructure.Performance.ResponseOptimization
{
    public interface IResponseOptimizationService
    {
        Task<T> OptimizeResponseAsync<T>(T response, ResponseOptimizationOptions options);
        Task<byte[]> CompressResponseAsync(object response, CompressionType compressionType);
        Task<PagedResult<T>> OptimizePagedResponseAsync<T>(PagedResult<T> pagedResult, PaginationOptimizationOptions options);
    }

    public class ResponseOptimizationService : IResponseOptimizationService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<ResponseOptimizationService> _logger;
        private readonly ResponseOptimizationSettings _settings;

        public async Task<T> OptimizeResponseAsync<T>(T response, ResponseOptimizationOptions options)
        {
            if (response == null) return response;

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var optimized = response;

                // Apply field selection if specified
                if (options.FieldsToInclude?.Any() == true)
                {
                    optimized = ApplyFieldSelection(optimized, options.FieldsToInclude);
                }

                // Apply data compression for large objects
                if (options.EnableDataCompression && IsLargeObject(optimized))
                {
                    optimized = await CompressDataFieldsAsync(optimized);
                }

                stopwatch.Stop();
                _logger.LogDebug("Response optimization completed in {Duration}ms", stopwatch.ElapsedMilliseconds);

                return optimized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Response optimization failed");
                return response; // Return original on error
            }
        }

        private T ApplyFieldSelection<T>(T obj, IEnumerable<string> fieldsToInclude)
        {
            // Implementation for selective field serialization
            var json = JsonSerializer.Serialize(obj);
            var jsonDocument = JsonDocument.Parse(json);
            var filteredJson = FilterJsonProperties(jsonDocument.RootElement, fieldsToInclude);
            
            return JsonSerializer.Deserialize<T>(filteredJson);
        }
    }

    // Bulk Operations Service
    public interface IBulkOperationService
    {
        Task<BulkOperationResult<TResult>> ExecuteBulkOperationAsync<TRequest, TResult>(
            IEnumerable<TRequest> requests,
            Func<TRequest, Task<TResult>> operation,
            BulkOperationOptions options = null,
            CancellationToken cancellationToken = default);
    }

    public class BulkOperationService : IBulkOperationService
    {
        public async Task<BulkOperationResult<TResult>> ExecuteBulkOperationAsync<TRequest, TResult>(
            IEnumerable<TRequest> requests,
            Func<TRequest, Task<TResult>> operation,
            BulkOperationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= BulkOperationOptions.Default;
            var requestsList = requests.ToList();
            var results = new List<TResult>();
            var errors = new List<BulkOperationError>();

            var batches = requestsList.Chunk(options.BatchSize);

            foreach (var batch in batches)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batchTasks = batch.Select(async request =>
                {
                    try
                    {
                        var result = await operation(request);
                        return new { Success = true, Result = result, Error = (Exception)null };
                    }
                    catch (Exception ex)
                    {
                        return new { Success = false, Result = default(TResult), Error = ex };
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);

                foreach (var batchResult in batchResults)
                {
                    if (batchResult.Success)
                        results.Add(batchResult.Result);
                    else
                        errors.Add(new BulkOperationError { ErrorMessage = batchResult.Error.Message });
                }

                if (options.DelayBetweenBatches > TimeSpan.Zero)
                    await Task.Delay(options.DelayBetweenBatches, cancellationToken);
            }

            return new BulkOperationResult<TResult>
            {
                Results = results,
                Errors = errors,
                TotalProcessed = requestsList.Count,
                SuccessCount = results.Count,
                ErrorCount = errors.Count
            };
        }
    }
}
```

---

## 6. Real-time Performance Monitoring

### 6.1 Application Insights Advanced Configuration
```csharp
namespace LankaConnect.Infrastructure.Monitoring
{
    public class ApplicationInsightsConfiguration
    {
        public static void ConfigureAdvancedMonitoring(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
                options.EnableAdaptiveSampling = true;
                options.EnableQuickPulseMetricStream = true;
                options.EnableDependencyTrackingTelemetryModule = true;
                options.EnablePerformanceCounterCollectionModule = true;
                options.EnableEventCounterCollectionModule = true;
                options.EnableRequestTrackingTelemetryModule = true;
            });

            // Configure adaptive sampling
            services.Configure<TelemetryConfiguration>(config =>
            {
                var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
                
                // Adaptive sampling
                builder.UseAdaptiveSampling(
                    maxTelemetryItemsPerSecond: 5,
                    excludedTypes: "Event");
                
                // Fixed-rate sampling for specific telemetry types
                builder.UseSampling(
                    10,
                    includedTypes: "Dependency");
            });

            // Custom telemetry processors
            services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
            services.AddApplicationInsightsTelemetryProcessor<CustomTelemetryProcessor>();
        }
    }

    // Advanced telemetry processor
    public class CustomTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly IConfiguration _configuration;

        public CustomTelemetryProcessor(ITelemetryProcessor next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public void Process(ITelemetry item)
        {
            // Filter out health check requests
            if (item is RequestTelemetry request && 
                (request.Url?.AbsolutePath.Contains("/health") == true ||
                 request.Url?.AbsolutePath.Contains("/metrics") == true))
            {
                return;
            }

            // Enrich dependency telemetry
            if (item is DependencyTelemetry dependency)
            {
                if (dependency.Type == "SQL")
                {
                    // Add query plan information for slow queries
                    if (dependency.Duration.TotalMilliseconds > 100)
                    {
                        dependency.Properties["SlowQuery"] = "true";
                        dependency.Properties["QueryPlan"] = "EXPLAIN ANALYZE available in logs";
                    }
                }
            }

            // Add global properties
            item.Context.GlobalProperties["ServiceName"] = "LankaConnect";
            item.Context.GlobalProperties["DeploymentId"] = Environment.GetEnvironmentVariable("DEPLOYMENT_ID") ?? "local";

            _next.Process(item);
        }
    }

    // Custom metrics service
    public interface ICustomMetricsService
    {
        void TrackApiPerformance(string endpoint, double duration, bool success);
        void TrackCachePerformance(string operation, string cacheType, bool hit, double duration);
        void TrackDatabasePerformance(string operation, double duration, int recordCount);
        void TrackBusinessMetric(string metricName, double value, Dictionary<string, string> properties = null);
        void TrackRealTimeMetric(string hubName, string method, int connectionCount, double duration);
    }

    public class CustomMetricsService : ICustomMetricsService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<CustomMetricsService> _logger;

        public CustomMetricsService(TelemetryClient telemetryClient, ILogger<CustomMetricsService> logger)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public void TrackApiPerformance(string endpoint, double duration, bool success)
        {
            var metric = _telemetryClient.GetMetric("API.Performance", "Endpoint", "Success");
            metric.TrackValue(duration, endpoint, success.ToString());

            if (duration > 1000) // Log slow APIs
            {
                _telemetryClient.TrackEvent("SlowApiCall", new Dictionary<string, string>
                {
                    ["Endpoint"] = endpoint,
                    ["Duration"] = duration.ToString(),
                    ["Success"] = success.ToString()
                });
            }
        }

        public void TrackCachePerformance(string operation, string cacheType, bool hit, double duration)
        {
            var metric = _telemetryClient.GetMetric("Cache.Performance", "Operation", "CacheType", "Hit");
            metric.TrackValue(duration, operation, cacheType, hit.ToString());

            // Track hit ratio
            var hitRatioMetric = _telemetryClient.GetMetric("Cache.HitRatio", "CacheType");
            hitRatioMetric.TrackValue(hit ? 1.0 : 0.0, cacheType);
        }

        public void TrackDatabasePerformance(string operation, double duration, int recordCount)
        {
            _telemetryClient.TrackDependency("SQL", "PostgreSQL", operation, 
                DateTimeOffset.UtcNow.AddMilliseconds(-duration), 
                TimeSpan.FromMilliseconds(duration), 
                success: duration < 100);

            // Track query performance by operation type
            var metric = _telemetryClient.GetMetric("Database.Performance", "Operation");
            metric.TrackValue(duration, operation);

            // Track records per second
            if (recordCount > 0)
            {
                var throughputMetric = _telemetryClient.GetMetric("Database.Throughput", "Operation");
                throughputMetric.TrackValue(recordCount / (duration / 1000.0), operation);
            }
        }

        public void TrackBusinessMetric(string metricName, double value, Dictionary<string, string> properties = null)
        {
            _telemetryClient.TrackMetric(metricName, value, properties);
            
            // Also log for real-time monitoring
            _logger.LogInformation("Business Metric: {MetricName} = {Value}", metricName, value);
        }

        public void TrackRealTimeMetric(string hubName, string method, int connectionCount, double duration)
        {
            var metric = _telemetryClient.GetMetric("SignalR.Performance", "Hub", "Method");
            metric.TrackValue(duration, hubName, method);

            _telemetryClient.TrackEvent("SignalR.MethodCall", new Dictionary<string, string>
            {
                ["Hub"] = hubName,
                ["Method"] = method,
                ["ConnectionCount"] = connectionCount.ToString()
            }, new Dictionary<string, double>
            {
                ["Duration"] = duration
            });
        }
    }
}
```

### 6.2 Real-time Performance Dashboard
```csharp
// SignalR Hub for real-time performance metrics
namespace LankaConnect.Infrastructure.Monitoring.Hubs
{
    public interface IPerformanceMetricsClient
    {
        Task ReceiveMetricUpdate(PerformanceMetricUpdate update);
        Task ReceiveSystemHealth(SystemHealthStatus health);
        Task ReceiveAlert(PerformanceAlert alert);
    }

    [Authorize(Roles = "Admin")]
    public class PerformanceMetricsHub : Hub<IPerformanceMetricsClient>
    {
        private readonly IPerformanceMonitoringService _monitoringService;
        private readonly ILogger<PerformanceMetricsHub> _logger;

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "performance-monitoring");
            
            // Send current metrics immediately
            var currentMetrics = await _monitoringService.GetCurrentMetricsAsync();
            await Clients.Caller.ReceiveMetricUpdate(currentMetrics);

            await base.OnConnectedAsync();
        }

        public async Task SubscribeToMetric(string metricType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"metric-{metricType}");
            _logger.LogInformation("Client {ConnectionId} subscribed to {MetricType}", 
                Context.ConnectionId, metricType);
        }

        public async Task<PerformanceSnapshot> GetPerformanceSnapshot()
        {
            return await _monitoringService.GetPerformanceSnapshotAsync();
        }
    }

    // Background service for broadcasting metrics
    public class PerformanceMetricsBroadcastService : BackgroundService
    {
        private readonly IHubContext<PerformanceMetricsHub, IPerformanceMetricsClient> _hubContext;
        private readonly IPerformanceMonitoringService _monitoringService;
        private readonly ILogger<PerformanceMetricsBroadcastService> _logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Broadcast system metrics every 5 seconds
                    var metrics = await _monitoringService.GetCurrentMetricsAsync();
                    await _hubContext.Clients.Group("performance-monitoring")
                        .ReceiveMetricUpdate(metrics);

                    // Check for alerts
                    var alerts = await _monitoringService.CheckForAlertsAsync();
                    foreach (var alert in alerts)
                    {
                        await _hubContext.Clients.Group("performance-monitoring")
                            .ReceiveAlert(alert);
                    }

                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting performance metrics");
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }
    }

    // Performance monitoring service
    public interface IPerformanceMonitoringService
    {
        Task<PerformanceMetricUpdate> GetCurrentMetricsAsync();
        Task<PerformanceSnapshot> GetPerformanceSnapshotAsync();
        Task<List<PerformanceAlert>> CheckForAlertsAsync();
        Task<SystemHealthStatus> GetSystemHealthAsync();
    }

    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public async Task<PerformanceMetricUpdate> GetCurrentMetricsAsync()
        {
            var metrics = new PerformanceMetricUpdate
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = GetMemoryUsage(),
                ActiveRequests = GetActiveRequestCount(),
                RequestsPerSecond = await GetRequestsPerSecondAsync(),
                AverageResponseTime = await GetAverageResponseTimeAsync(),
                CacheHitRatio = await GetCacheHitRatioAsync(),
                DatabaseConnections = await GetDatabaseConnectionsAsync(),
                ErrorRate = await GetErrorRateAsync()
            };

            return metrics;
        }

        public async Task<PerformanceSnapshot> GetPerformanceSnapshotAsync()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                SystemMetrics = await GetSystemMetricsAsync(),
                ApplicationMetrics = await GetApplicationMetricsAsync(),
                DatabaseMetrics = await GetDatabaseMetricsAsync(),
                CacheMetrics = await GetCacheMetricsAsync(),
                ApiMetrics = await GetApiMetricsAsync()
            };

            return snapshot;
        }

        public async Task<List<PerformanceAlert>> CheckForAlertsAsync()
        {
            var alerts = new List<PerformanceAlert>();
            var metrics = await GetCurrentMetricsAsync();

            // Check CPU usage
            if (metrics.CpuUsage > 80)
            {
                alerts.Add(new PerformanceAlert
                {
                    Level = AlertLevel.Warning,
                    Type = "CPU",
                    Message = $"High CPU usage detected: {metrics.CpuUsage:F1}%",
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check memory usage
            if (metrics.MemoryUsage > 85)
            {
                alerts.Add(new PerformanceAlert
                {
                    Level = AlertLevel.Critical,
                    Type = "Memory",
                    Message = $"Critical memory usage: {metrics.MemoryUsage:F1}%",
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check response time
            if (metrics.AverageResponseTime > 500)
            {
                alerts.Add(new PerformanceAlert
                {
                    Level = AlertLevel.Warning,
                    Type = "ResponseTime",
                    Message = $"Slow response times detected: {metrics.AverageResponseTime:F0}ms average",
                    Timestamp = DateTime.UtcNow
                });
            }

            return alerts;
        }

        private double GetCpuUsage()
        {
            // Implementation to get CPU usage
            var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
        }

        private double GetMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;
            
            return (double)workingSet / (1024 * 1024 * 1024) * 100; // Convert to percentage of available memory
        }

        private int GetActiveRequestCount()
        {
            // This would integrate with your request tracking
            return _cache.Get<int>("active_request_count");
        }
    }
}
```

## 7. Background Service Performance

### 7.1 Optimized Background Service Base
```csharp
namespace LankaConnect.Infrastructure.BackgroundServices
{
    public abstract class PerformantBackgroundServiceBase : BackgroundService
    {
        protected readonly ILogger Logger;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IPerformanceMetrics Metrics;
        private readonly PerformanceSettings _settings;

        protected PerformantBackgroundServiceBase(
            ILogger logger,
            IServiceProvider serviceProvider,
            IPerformanceMetrics metrics,
            IOptions<PerformanceSettings> settings)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            Metrics = metrics;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("{ServiceName} is starting", GetType().Name);

            // Wait for application to fully start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var executionId = Guid.NewGuid();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    using (Logger.BeginScope(new Dictionary<string, object>
                    {
                        ["ExecutionId"] = executionId,
                        ["ServiceName"] = GetType().Name
                    }))
                    {
                        Logger.LogDebug("Starting background service execution");

                        using var scope = ServiceProvider.CreateScope();
                        await ExecuteIterationAsync(scope.ServiceProvider, stoppingToken);

                        stopwatch.Stop();
                        await Metrics.RecordBackgroundServiceExecutionAsync(
                            GetType().Name, 
                            stopwatch.Elapsed, 
                            true);

                        Logger.LogDebug("Background service execution completed in {Duration}ms", 
                            stopwatch.ElapsedMilliseconds);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    Logger.LogInformation("Background service execution cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    await Metrics.RecordBackgroundServiceExecutionAsync(
                        GetType().Name, 
                        stopwatch.Elapsed, 
                        false);

                    Logger.LogError(ex, "Error in background service execution");
                    
                    // Implement exponential backoff
                    var delay = CalculateBackoffDelay();
                    await Task.Delay(delay, stoppingToken);
                }

                // Wait for next iteration
                var nextDelay = GetNextExecutionDelay();
                await Task.Delay(nextDelay, stoppingToken);
            }

            Logger.LogInformation("{ServiceName} is stopping", GetType().Name);
        }

        protected abstract Task ExecuteIterationAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken);
        
        protected virtual TimeSpan GetNextExecutionDelay() => TimeSpan.FromMinutes(5);

        private TimeSpan CalculateBackoffDelay()
        {
            // Exponential backoff with jitter
            var random = new Random();
            var baseDelay = TimeSpan.FromSeconds(5);
            var maxDelay = TimeSpan.FromMinutes(5);
            
            var exponentialDelay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, _consecutiveErrors));
            
            var jitteredDelay = TimeSpan.FromMilliseconds(
                exponentialDelay.TotalMilliseconds * (0.5 + random.NextDouble() * 0.5));
            
            return jitteredDelay > maxDelay ? maxDelay : jitteredDelay;
        }

        private int _consecutiveErrors = 0;
    }

    // Example optimized background service
    public class CacheWarmupBackgroundService : PerformantBackgroundServiceBase
    {
        public CacheWarmupBackgroundService(
            ILogger<CacheWarmupBackgroundService> logger,
            IServiceProvider serviceProvider,
            IPerformanceMetrics metrics,
            IOptions<PerformanceSettings> settings)
            : base(logger, serviceProvider, metrics, settings)
        {
        }

        protected override async Task ExecuteIterationAsync(
            IServiceProvider scopedProvider, 
            CancellationToken cancellationToken)
        {
            var cacheService = scopedProvider.GetRequiredService<IAdvancedCacheService>();
            
            await cacheService.WarmupAsync(CacheWarmupScope.Essential, cancellationToken);
        }

        protected override TimeSpan GetNextExecutionDelay() => TimeSpan.FromMinutes(30);
    }
}
```

## 8. Memory Management & Resource Optimization

### 8.1 Memory-Efficient Collections
```csharp
namespace LankaConnect.Infrastructure.Performance.Memory
{
    public class MemoryOptimizedCollectionFactory
    {
        // Use ArrayPool for temporary arrays
        public static T[] RentArray<T>(int minimumLength)
        {
            return ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public static void ReturnArray<T>(T[] array, bool clearArray = false)
        {
            ArrayPool<T>.Shared.Return(array, clearArray);
        }

        // Memory-efficient string builder pool
        public static StringBuilder RentStringBuilder(int capacity = 256)
        {
            return StringBuilderPool.Shared.Rent(capacity);
        }

        public static string ReturnStringBuilder(StringBuilder builder)
        {
            var result = builder.ToString();
            StringBuilderPool.Shared.Return(builder);
            return result;
        }
    }

    // Custom memory pool for domain objects
    public class DomainObjectPool<T> where T : class, new()
    {
        private readonly ObjectPool<T> _pool;
        private readonly ILogger<DomainObjectPool<T>> _logger;

        public DomainObjectPool(ILogger<DomainObjectPool<T>> logger)
        {
            _logger = logger;
            
            var policy = new DefaultPooledObjectPolicy<T>();
            _pool = new DefaultObjectPool<T>(policy, Environment.ProcessorCount * 2);
        }

        public T Rent()
        {
            var obj = _pool.Get();
            _logger.LogTrace("Rented {Type} from pool", typeof(T).Name);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj is IResettable resettable)
            {
                resettable.Reset();
            }
            
            _pool.Return(obj);
            _logger.LogTrace("Returned {Type} to pool", typeof(T).Name);
        }
    }

    // Memory pressure monitoring
    public class MemoryMonitoringService : IHostedService
    {
        private readonly ILogger<MemoryMonitoringService> _logger;
        private readonly IPerformanceMetrics _metrics;
        private Timer _timer;
        private long _lastGen2Count = 0;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckMemoryPressure, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        private void CheckMemoryPressure(object state)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var gen0Count = GC.CollectionCount(0);
            var gen1Count = GC.CollectionCount(1);
            var gen2Count = GC.CollectionCount(2);

            _metrics.RecordMemoryMetrics(new MemoryMetrics
            {
                TotalMemoryMB = totalMemory / (1024 * 1024),
                Gen0Collections = gen0Count,
                Gen1Collections = gen1Count,
                Gen2Collections = gen2Count
            });

            // Check for memory pressure
            if (totalMemory > 500_000_000) // 500MB
            {
                _logger.LogWarning("High memory usage detected: {MemoryMB}MB", totalMemory / (1024 * 1024));
                
                // Force garbage collection if needed
                if (gen2Count == _lastGen2Count && totalMemory > 800_000_000) // 800MB
                {
                    _logger.LogWarning("Forcing garbage collection due to memory pressure");
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            _lastGen2Count = gen2Count;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return Task.CompletedTask;
        }
    }
}
```

---

## 9. Performance Testing & Load Testing

### 9.1 Automated Performance Testing
```csharp
namespace LankaConnect.Tests.Performance
{
    public class PerformanceTestBase
    {
        protected readonly HttpClient Client;
        protected readonly ITestOutputHelper Output;
        protected readonly PerformanceCounter Counter;

        public PerformanceTestBase(ITestOutputHelper output)
        {
            Output = output;
            Counter = new PerformanceCounter();
            
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Use in-memory database for tests
                        services.AddDbContext<AppDbContext>(options =>
                            options.UseInMemoryDatabase("PerfTestDb"));
                    });
                });
                
            Client = factory.CreateClient();
        }

        protected async Task<PerformanceResult> MeasureEndpointAsync(
            string endpoint, 
            int iterations = 100)
        {
            var results = new List<double>();
            
            // Warmup
            for (int i = 0; i < 10; i++)
            {
                await Client.GetAsync(endpoint);
            }

            // Actual measurements
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await Client.GetAsync(endpoint);
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    results.Add(stopwatch.Elapsed.TotalMilliseconds);
                }
            }

            return new PerformanceResult
            {
                Endpoint = endpoint,
                AverageMs = results.Average(),
                MinMs = results.Min(),
                MaxMs = results.Max(),
                P95Ms = results.OrderBy(r => r).Skip((int)(results.Count * 0.95)).First(),
                P99Ms = results.OrderBy(r => r).Skip((int)(results.Count * 0.99)).First()
            };
        }
    }

    [Collection("Performance")]
    public class ApiPerformanceTests : PerformanceTestBase
    {
        public ApiPerformanceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task GetServices_PerformanceTest()
        {
            var result = await MeasureEndpointAsync("/api/services?category=plumbing&location=colombo");
            
            Output.WriteLine($"Average: {result.AverageMs:F2}ms");
            Output.WriteLine($"P95: {result.P95Ms:F2}ms");
            Output.WriteLine($"P99: {result.P99Ms:F2}ms");
            
            // Assert performance requirements
            Assert.True(result.P95Ms < 200, $"P95 response time {result.P95Ms}ms exceeds 200ms target");
            Assert.True(result.P99Ms < 500, $"P99 response time {result.P99Ms}ms exceeds 500ms target");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task ConcurrentRequests_PerformanceTest(int concurrentRequests)
        {
            var tasks = new List<Task<PerformanceResult>>();
            
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(Task.Run(() => MeasureEndpointAsync("/api/services", iterations: 10)));
            }

            var results = await Task.WhenAll(tasks);
            
            var overallAverage = results.Average(r => r.AverageMs);
            var overallP95 = results.Select(r => r.P95Ms).OrderBy(p => p)
                .Skip((int)(results.Length * 0.95)).First();

            Output.WriteLine($"Concurrent requests: {concurrentRequests}");
            Output.WriteLine($"Overall average: {overallAverage:F2}ms");
            Output.WriteLine($"Overall P95: {overallP95:F2}ms");
            
            // Performance should degrade gracefully
            Assert.True(overallP95 < 1000, $"P95 under load {overallP95}ms exceeds 1000ms");
        }
    }
}
```

### 9.2 Load Testing with NBomber
```csharp
namespace LankaConnect.Tests.LoadTesting
{
    public class LoadTests
    {
        [Fact]
        public void ServiceSearch_LoadTest()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

            var scenario = Scenario.Create("service_search", async context =>
            {
                var categories = new[] { "plumbing", "electrical", "cleaning", "carpentry" };
                var locations = new[] { "Colombo", "Galle", "Kandy", "Jaffna" };
                
                var category = categories[Random.Shared.Next(categories.Length)];
                var location = locations[Random.Shared.Next(locations.Length)];

                var response = await httpClient.GetAsync(
                    $"/api/services?category={category}&location={location}");

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithLoadSimulations(
                Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2)),
                Simulation.RampPerSec(rate: 0, interval: TimeSpan.FromSeconds(10), during: TimeSpan.FromMinutes(1))
            );

            NBomberRunner
                .RegisterScenarios(scenario)
                .WithWorkerPlugins(new[] { new PingPlugin() })
                .Run();
        }

        [Fact]
        public void MixedLoad_RealWorldSimulation()
        {
            var searchScenario = Scenario.Create("search", async context =>
            {
                // Search for services
                var response = await context.HttpClient.GetAsync("/api/services?category=plumbing");
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithWeight(60); // 60% of traffic

            var detailsScenario = Scenario.Create("service_details", async context =>
            {
                // Get service details
                var serviceId = Random.Shared.Next(1, 1000);
                var response = await context.HttpClient.GetAsync($"/api/services/{serviceId}");
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithWeight(30); // 30% of traffic

            var bookingScenario = Scenario.Create("create_booking", async context =>
            {
                // Create booking
                var booking = new
                {
                    serviceId = Random.Shared.Next(1, 1000),
                    date = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)),
                    duration = Random.Shared.Next(1, 4)
                };

                var response = await context.HttpClient.PostAsJsonAsync("/api/bookings", booking);
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            })
            .WithWeight(10); // 10% of traffic

            NBomberRunner
                .RegisterScenarios(searchScenario, detailsScenario, bookingScenario)
                .WithReportFolder("./load-test-reports")
                .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
                .Run();
        }
    }
}
```

### 9.3 Stress Testing
```csharp
public class StressTests
{
    [Fact]
    public async Task Database_StressTest()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IServiceRepository>();

        // Generate high load
        var tasks = new List<Task>();
        var concurrentOperations = 1000;

        for (int i = 0; i < concurrentOperations; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                await repository.GetServicesByCategoryAsync(Guid.NewGuid(), "Colombo", 1, 20);
                stopwatch.Stop();
                
                return stopwatch.ElapsedMilliseconds;
            }));
        }

        var results = await Task.WhenAll(tasks);
        
        // Analyze results
        var average = results.Average();
        var p95 = results.OrderBy(r => r).Skip((int)(results.Length * 0.95)).First();
        
        Assert.True(average < 100, $"Average query time {average}ms exceeds 100ms under stress");
        Assert.True(p95 < 200, $"P95 query time {p95}ms exceeds 200ms under stress");
    }

    [Fact]
    public async Task Cache_StressTest()
    {
        var cacheService = new AdvancedCacheService(/* dependencies */);
        var tasks = new List<Task>();
        
        // Simulate cache stampede
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await cacheService.GetOrSetAsync(
                    "shared-key",
                    async () =>
                    {
                        await Task.Delay(100); // Simulate expensive operation
                        return "expensive-data";
                    },
                    new CacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            }));
        }

        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // With proper locking, only one factory execution should occur
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            "Cache stampede protection failed - multiple factory executions detected");
    }
}
```

## 10. Health Monitoring & Diagnostics

### 10.1 Advanced Health Checks
```csharp
namespace LankaConnect.Infrastructure.HealthChecks
{
    public class HealthCheckConfiguration
    {
        public static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                // Database health check with performance monitoring
                .AddTypeActivatedCheck<DatabaseHealthCheck>(
                    "database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "critical", "database" })
                
                // Redis health check with connection monitoring
                .AddTypeActivatedCheck<RedisHealthCheck>(
                    "redis-cache",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "cache" })
                
                // Custom composite health check
                .AddTypeActivatedCheck<SystemHealthCheck>(
                    "system",
                    tags: new[] { "system" });

            // Health check UI
            services.AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(30);
                options.MaximumHistoryEntriesPerEndpoint(50);
                options.AddHealthCheckEndpoint("LankaConnect API", "/health");
            }).AddInMemoryStorage();
        }
    }

    // Advanced database health check
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Test connection
                await _context.Database.CanConnectAsync(cancellationToken);
                
                // Test query performance
                var count = await _context.Services
                    .Take(1)
                    .CountAsync(cancellationToken);
                
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                    ["ConnectionString"] = _context.Database.GetConnectionString()?.Split(';')[0] // Only server part
                };

                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    return HealthCheckResult.Degraded(
                        $"Database is slow: {stopwatch.ElapsedMilliseconds}ms", 
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Database is healthy: {stopwatch.ElapsedMilliseconds}ms response time", 
                    data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                
                return HealthCheckResult.Unhealthy(
                    "Database is not accessible",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
            }
        }
    }

    // System-wide health check
    public class SystemHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var checks = new List<Task<HealthCheckResult>>();
            
            // Check memory usage
            var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            if (memoryUsage > 500)
            {
                return HealthCheckResult.Degraded($"High memory usage: {memoryUsage}MB");
            }

            // Check disk space
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var drive in drives)
            {
                var freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                if (freeSpacePercent < 10)
                {
                    return HealthCheckResult.Degraded($"Low disk space on {drive.Name}: {freeSpacePercent:F1}%");
                }
            }

            return HealthCheckResult.Healthy("System resources are healthy", 
                new Dictionary<string, object>
                {
                    ["MemoryUsageMB"] = memoryUsage,
                    ["CPUCount"] = Environment.ProcessorCount
                });
        }
    }
}
```

## 11. Performance Monitoring Implementation

### 11.1 Startup Configuration
```csharp
// Program.cs performance configuration
var builder = WebApplication.CreateBuilder(args);

// Add performance services
builder.Services.AddSingleton<IPerformanceMetrics, PerformanceMetrics>();
builder.Services.AddSingleton<IAdvancedCacheService, AdvancedCacheService>();
builder.Services.AddSingleton<IResponseOptimizationService, ResponseOptimizationService>();
builder.Services.AddSingleton<IBulkOperationService, BulkOperationService>();

// Configure performance middleware
builder.Services.Configure<PerformanceSettings>(
    builder.Configuration.GetSection("Performance"));
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("Cache"));

// Add memory cache with size limit
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100_000_000; // 100MB
    options.CompactionPercentage = 0.25;
});

// Add distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "LankaConnect";
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Add Application Insights
ApplicationInsightsConfiguration.ConfigureAdvancedMonitoring(
    builder.Services, 
    builder.Configuration);

// Configure health checks
HealthCheckConfiguration.ConfigureHealthChecks(
    builder.Services, 
    builder.Configuration);

// Add background services
builder.Services.AddHostedService<MemoryMonitoringService>();
builder.Services.AddHostedService<CacheWarmupBackgroundService>();
builder.Services.AddHostedService<PerformanceMetricsBroadcastService>();

var app = builder.Build();

// Configure performance middleware pipeline
app.UseResponseCompression();
app.UseMiddleware<PerformanceTrackingMiddleware>();
app.UseMiddleware<SmartRateLimitingMiddleware>();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI();

// Map performance monitoring hub
app.MapHub<PerformanceMetricsHub>("/hubs/performance");

app.Run();
```

### 11.2 Configuration Settings
```json
{
  "Performance": {
    "SlowRequestThresholdMs": 1000,
    "HighMemoryThresholdBytes": 50000000,
    "MemoryPressureThresholdMB": 500,
    "EnableDetailedMetrics": true
  },
  "Cache": {
    "CacheKeyPrefix": "lankaconnect",
    "DefaultExpiryHours": 1,
    "MemoryCacheDefaultExpiryMinutes": 15,
    "MemoryCacheMaxExpiryMinutes": 60,
    "EnableCacheMetrics": true,
    "EnableCacheWarmup": true,
    "DefaultWarmupScope": "Essential",
    "MemoryCacheSizeLimit": 100000000
  },
  "RateLimit": {
    "DefaultRule": {
      "RequestLimit": 1000,
      "WindowSizeMinutes": 1
    },
    "SearchEndpointRule": {
      "RequestLimit": 100,
      "WindowSizeMinutes": 1
    },
    "UploadEndpointRule": {
      "RequestLimit": 10,
      "WindowSizeMinutes": 1
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://your-region.in.applicationinsights.azure.com/"
  }
}
```

## 12. Performance Optimization Checklist

### 12.1 Implementation Checklist
- [ ] Configure multi-layer caching with distributed locking
- [ ] Implement performance tracking middleware
- [ ] Set up smart rate limiting per endpoint
- [ ] Configure response compression (Brotli/Gzip)
- [ ] Implement database query optimization with compiled queries
- [ ] Set up Application Insights with custom telemetry
- [ ] Configure comprehensive health checks
- [ ] Implement real-time performance monitoring
- [ ] Set up automated performance testing
- [ ] Configure memory monitoring and management
- [ ] Implement background service optimization
- [ ] Set up performance alerts and dashboards

### 12.2 Performance Targets Validation
- [ ] API Response Time < 200ms (p95)
- [ ] Database Query Time < 50ms (average)
- [ ] Cache Hit Rate > 85%
- [ ] Support 10,000 concurrent users
- [ ] System Availability > 99.9%
- [ ] Page Load Time < 2 seconds
- [ ] SignalR Latency < 100ms

---

## 13. Conclusion

This comprehensive Performance & Monitoring implementation provides LankaConnect with enterprise-grade performance optimization and monitoring capabilities. The multi-layer caching with distributed locking, advanced middleware pipeline, and real-time monitoring ensure the platform can scale efficiently while maintaining optimal performance under load.

The implementation-focused approach with detailed code examples enables Claude Code agents to directly implement these patterns during development sessions, ensuring consistent performance optimization across the entire platform.
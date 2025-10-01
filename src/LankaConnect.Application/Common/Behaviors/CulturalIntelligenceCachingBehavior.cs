using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Billing;
using System.Diagnostics;
using System.Text.Json;
using BillingEndpoint = LankaConnect.Domain.Billing.BillingEndpoint;

namespace LankaConnect.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior implementing cache-aside pattern for cultural intelligence queries
/// Provides transparent caching for ICacheableQuery implementations with performance monitoring
/// </summary>
public class CulturalIntelligenceCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICulturalIntelligenceCacheService _cacheService;
    private readonly ILogger<CulturalIntelligenceCachingBehavior<TRequest, TResponse>> _logger;

    public CulturalIntelligenceCachingBehavior(
        ICulturalIntelligenceCacheService cacheService,
        ILogger<CulturalIntelligenceCachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache requests that implement ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        // Check if caching is enabled for this query
        if (!cacheableQuery.ShouldCache())
        {
            _logger.LogDebug("Caching disabled for query {QueryType}", typeof(TRequest).Name);
            return await next();
        }

        var stopwatch = Stopwatch.StartNew();
        var cacheKey = cacheableQuery.GetCacheKey();
        var cacheTtl = cacheableQuery.GetCacheTtl();

        try
        {
            // Attempt to retrieve from cache
            var cachedResult = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            
            if (cachedResult != null)
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "Cache HIT for key {CacheKey} in {ElapsedMs}ms (Query: {QueryType})",
                    cacheKey,
                    stopwatch.ElapsedMilliseconds,
                    typeof(TRequest).Name);
                
                await LogCacheMetricsAsync(cacheKey, true, stopwatch.Elapsed, cancellationToken);
                return cachedResult;
            }

            _logger.LogDebug("Cache MISS for key {CacheKey} (Query: {QueryType})", cacheKey, typeof(TRequest).Name);

            // Execute the actual query handler
            var executionStopwatch = Stopwatch.StartNew();
            var result = await next();
            executionStopwatch.Stop();

            // Cache the result if it's successful (for Result<T> types)
            await CacheResultIfSuccessfulAsync(cacheKey, result, cacheTtl, cancellationToken);

            stopwatch.Stop();
            
            _logger.LogDebug(
                "Query executed and cached for key {CacheKey} - Total: {TotalMs}ms, Execution: {ExecutionMs}ms (Query: {QueryType})",
                cacheKey,
                stopwatch.ElapsedMilliseconds,
                executionStopwatch.ElapsedMilliseconds,
                typeof(TRequest).Name);

            await LogCacheMetricsAsync(cacheKey, false, stopwatch.Elapsed, cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Cache operation failed for key {CacheKey} after {ElapsedMs}ms (Query: {QueryType}). Proceeding without cache.",
                cacheKey,
                stopwatch.ElapsedMilliseconds,
                typeof(TRequest).Name);
            
            // If caching fails, still execute the query
            return await next();
        }
    }

    private async Task CacheResultIfSuccessfulAsync(string cacheKey, TResponse result, TimeSpan cacheTtl, CancellationToken cancellationToken)
    {
        try
        {
            // For Result<T> types, only cache successful results
            if (IsResultType(typeof(TResponse)))
            {
                if (IsSuccessfulResult(result))
                {
                    await _cacheService.SetAsync(cacheKey, result, cacheTtl, cancellationToken);
                    _logger.LogDebug("Successfully cached result for key {CacheKey}", cacheKey);
                }
                else
                {
                    _logger.LogDebug("Skipping cache for unsuccessful result (key: {CacheKey})", cacheKey);
                }
            }
            else
            {
                // For non-Result types, cache all results
                await _cacheService.SetAsync(cacheKey, result, cacheTtl, cancellationToken);
                _logger.LogDebug("Successfully cached result for key {CacheKey}", cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache result for key {CacheKey}", cacheKey);
            // Don't throw - caching failure shouldn't break the query
        }
    }

    private static bool IsResultType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>) ||
               type == typeof(Result);
    }

    private static bool IsSuccessfulResult(TResponse result)
    {
        // Check if it's Result<T>
        if (result?.GetType().IsGenericType == true && 
            result.GetType().GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccessProperty = result.GetType().GetProperty("IsSuccess");
            return isSuccessProperty?.GetValue(result) as bool? == true;
        }

        // Check if it's Result
        if (result is Result plainResult)
        {
            return plainResult.IsSuccess;
        }

        // For non-Result types, always consider successful
        return true;
    }

    private async Task LogCacheMetricsAsync(string cacheKey, bool wasHit, TimeSpan duration, CancellationToken cancellationToken)
    {
        try
        {
            // Extract cultural intelligence endpoint from cache key for metrics
            var billingEndpoint = ExtractEndpointFromCacheKey(cacheKey);
            var monitoringEndpoint = CreateMonitoringEndpointFromBilling(billingEndpoint);

            // In a real implementation, you might want to aggregate these metrics
            // and periodically update a metrics store or send to monitoring system
            var metrics = await _cacheService.GetCacheMetricsAsync(monitoringEndpoint, cancellationToken);

            _logger.LogTrace(
                "Cache metrics - Key: {CacheKey}, Hit: {IsHit}, Duration: {DurationMs}ms, HitRatio: {HitRatio:P2}",
                cacheKey,
                wasHit,
                duration.TotalMilliseconds,
                metrics.HitRatio);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to log cache metrics for key {CacheKey}", cacheKey);
            // Don't throw - metrics failure shouldn't break the operation
        }
    }

    private static LankaConnect.Domain.Common.Monitoring.CulturalIntelligenceEndpoint CreateMonitoringEndpointFromBilling(BillingEndpoint billingEndpoint)
    {
        var endpointId = $"billing_{billingEndpoint.Category}_{billingEndpoint.BillingComplexity}".ToLowerInvariant();
        var monitoringEndpoint = new LankaConnect.Domain.Common.Monitoring.CulturalIntelligenceEndpoint(endpointId)
        {
            Id = endpointId,
            EndpointName = billingEndpoint.Category.ToString(),
            EndpointUrl = billingEndpoint.Path,
            EndpointType = CulturalIntelligenceEndpointType.ApiGateway,
            Status = CulturalIntelligenceEndpointStatus.Healthy,
            SupportedCulturalContexts = new List<string> { "Buddhist", "Hindu", "Sri Lankan" },
            EndpointConfiguration = new Dictionary<string, object>
            {
                ["BillingComplexity"] = billingEndpoint.BillingComplexity.ToString(),
                ["Category"] = billingEndpoint.Category.ToString()
            },
            SecurityLevel = CulturalIntelligenceSecurityLevel.Internal,
            ResponseTimeThreshold = TimeSpan.FromSeconds(5),
            MaxConcurrentConnections = 100,
            IsLoadBalanced = true,
            AssignedRegions = new List<string> { "Global" },
            LastHealthCheck = DateTime.UtcNow
        };
        return monitoringEndpoint;
    }

    private static BillingEndpoint ExtractEndpointFromCacheKey(string cacheKey)
    {
        var prefix = cacheKey.Split(':').FirstOrDefault()?.ToLowerInvariant();

        return prefix switch
        {
            "cal_buddhist" => BillingEndpoint.BuddhistCalendar("/api/buddhist-calendar"),
            "cal_hindu" => BillingEndpoint.HinduCalendar("/api/hindu-calendar"),
            "cultural_score" => BillingEndpoint.CulturalAppropriateness("/api/cultural-appropriateness"),
            "diaspora_analytics" => BillingEndpoint.DiasporaAnalytics("/api/diaspora-analytics"),
            "event_recommendations" => BillingEndpoint.EventRecommendations("/api/event-recommendations"),
            "cultural_content" => BillingEndpoint.CulturalContent("/api/cultural-content"),
            "business_directory" => BillingEndpoint.BusinessDirectory("/api/business-directory"),
            "community_engagement" => BillingEndpoint.CommunityEngagement("/api/community-engagement"),
            _ => BillingEndpoint.CulturalContent("/api/cultural-content/default")
        };
    }
}
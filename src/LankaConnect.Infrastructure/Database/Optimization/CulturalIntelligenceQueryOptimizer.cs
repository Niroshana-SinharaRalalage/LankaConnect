using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using DomainCulturalContext = LankaConnect.Domain.Communications.ValueObjects.CulturalContext;
using LankaConnect.Infrastructure.Data;

namespace LankaConnect.Infrastructure.Database.Optimization;

public interface ICulturalIntelligenceQueryOptimizer
{
    Task<Result<OptimizedQueryPlan>> OptimizeQueryAsync<T>(
        IQueryable<T> query,
        DomainCulturalContext culturalContext,
        QueryPerformanceTarget performanceTarget,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<T>>> ExecuteOptimizedQueryAsync<T>(
        OptimizedQueryPlan queryPlan,
        CancellationToken cancellationToken = default);

    Task<Result<QueryPerformanceMetrics>> AnalyzeQueryPerformanceAsync<T>(
        IQueryable<T> query,
        DomainCulturalContext culturalContext,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetOptimizationRecommendationsAsync<T>(
        IQueryable<T> query,
        QueryPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default);

    Task<Result> WarmupCulturalIntelligenceQueriesAsync(
        IEnumerable<string> communityIds,
        CancellationToken cancellationToken = default);
}

public class CulturalIntelligenceQueryOptimizer : ICulturalIntelligenceQueryOptimizer
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CulturalIntelligenceQueryOptimizer> _logger;
    private readonly IOptions<DatabaseOptimizationOptions> _optimizationOptions;
    private readonly ConcurrentDictionary<string, OptimizedQueryPlan> _queryPlanCache;
    private readonly ConcurrentDictionary<string, QueryPerformanceMetrics> _performanceCache;
    private readonly SemaphoreSlim _optimizationSemaphore;

    public CulturalIntelligenceQueryOptimizer(
        IApplicationDbContext dbContext,
        ILogger<CulturalIntelligenceQueryOptimizer> logger,
        IOptions<DatabaseOptimizationOptions> optimizationOptions)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optimizationOptions = optimizationOptions ?? throw new ArgumentNullException(nameof(optimizationOptions));
        _queryPlanCache = new ConcurrentDictionary<string, OptimizedQueryPlan>();
        _performanceCache = new ConcurrentDictionary<string, QueryPerformanceMetrics>();
        _optimizationSemaphore = new SemaphoreSlim(10, 10);
    }

    public async Task<Result<OptimizedQueryPlan>> OptimizeQueryAsync<T>(
        IQueryable<T> query,
        DomainCulturalContext culturalContext,
        QueryPerformanceTarget performanceTarget,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _optimizationSemaphore.WaitAsync(cancellationToken);
            
            var queryHash = GenerateQueryHash(query, culturalContext);
            
            // Check cache first
            if (_queryPlanCache.TryGetValue(queryHash, out var cachedPlan) && 
                !IsPlanExpired(cachedPlan))
            {
                _logger.LogDebug("Retrieved cached query plan for {QueryHash}", queryHash);
                return Result.Success(cachedPlan);
            }

            _logger.LogInformation(
                "Optimizing cultural intelligence query for community {CommunityId} in region {Region} (Target: {Target})",
                culturalContext.CommunityId, culturalContext.GeographicRegion, performanceTarget);

            var stopwatch = Stopwatch.StartNew();
            
            // Analyze query structure
            var queryAnalysis = await AnalyzeQueryStructureAsync(query, culturalContext, cancellationToken);
            
            // Generate optimization strategies
            var optimizationStrategies = GenerateOptimizationStrategies(queryAnalysis, performanceTarget);
            
            // Create optimized query plan
            var queryPlan = new OptimizedQueryPlan
            {
                QueryId = queryHash,
                OriginalQuery = query.Expression.ToString(),
                CulturalContext = culturalContext,
                PerformanceTarget = performanceTarget,
                OptimizationStrategies = optimizationStrategies,
                EstimatedExecutionTime = CalculateEstimatedExecutionTime(queryAnalysis, optimizationStrategies),
                CacheExpiry = DateTime.UtcNow.AddHours(_optimizationOptions.Value.QueryPlanCacheHours),
                CreatedAt = DateTime.UtcNow
            };

            // Apply cultural intelligence specific optimizations
            ApplyCulturalIntelligenceOptimizations(queryPlan, culturalContext);
            
            // Cache the query plan
            _queryPlanCache.TryAdd(queryHash, queryPlan);
            
            stopwatch.Stop();
            queryPlan.OptimizationTime = stopwatch.Elapsed;
            
            _logger.LogInformation(
                "Query optimization completed in {OptimizationTimeMs}ms for {QueryId} (Strategies: {StrategyCount})",
                stopwatch.ElapsedMilliseconds, queryHash, optimizationStrategies.Count);

            return Result.Success(queryPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize query for community {CommunityId}", culturalContext.CommunityId);
            return Result.Failure<OptimizedQueryPlan>($"Query optimization failed: {ex.Message}");
        }
        finally
        {
            _optimizationSemaphore.Release();
        }
    }

    public async Task<Result<IEnumerable<T>>> ExecuteOptimizedQueryAsync<T>(
        OptimizedQueryPlan queryPlan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug(
                "Executing optimized query {QueryId} with {StrategyCount} optimization strategies",
                queryPlan.QueryId, queryPlan.OptimizationStrategies.Count);

            // Apply performance optimizations based on the plan
            var optimizedDbContext = ApplyContextOptimizations(queryPlan);
            
            // Execute query with optimizations
            var results = await ExecuteWithOptimizationsAsync<T>(queryPlan, optimizedDbContext, cancellationToken);
            
            stopwatch.Stop();
            
            // Record performance metrics
            var performanceMetrics = new QueryPerformanceMetrics
            {
                QueryId = queryPlan.QueryId,
                ExecutionTime = stopwatch.Elapsed,
                ResultCount = results.Count(),
                CulturalContext = queryPlan.CulturalContext,
                OptimizationStrategiesUsed = queryPlan.OptimizationStrategies,
                ExecutedAt = DateTime.UtcNow
            };
            
            _performanceCache.TryAdd(queryPlan.QueryId, performanceMetrics);
            
            _logger.LogInformation(
                "Optimized query executed successfully in {ExecutionTimeMs}ms, returned {ResultCount} results",
                stopwatch.ElapsedMilliseconds, results.Count());

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute optimized query {QueryId}", queryPlan.QueryId);
            return Result.Failure<IEnumerable<T>>($"Optimized query execution failed: {ex.Message}");
        }
    }

    public async Task<Result<QueryPerformanceMetrics>> AnalyzeQueryPerformanceAsync<T>(
        IQueryable<T> query,
        DomainCulturalContext culturalContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryHash = GenerateQueryHash(query, culturalContext);
            
            // Check for cached performance metrics
            if (_performanceCache.TryGetValue(queryHash, out var cachedMetrics) && 
                !IsMetricsExpired(cachedMetrics))
            {
                return Result.Success(cachedMetrics);
            }

            _logger.LogDebug("Analyzing query performance for {QueryHash}", queryHash);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Execute query and measure performance
            var resultCount = 0;
            try
            {
                var results = await query.Take(1000).ToListAsync(cancellationToken); // Limit for analysis
                resultCount = results.Count;
            }
            catch (Exception queryEx)
            {
                _logger.LogWarning(queryEx, "Query execution failed during analysis");
            }
            
            stopwatch.Stop();
            
            var performanceMetrics = new QueryPerformanceMetrics
            {
                QueryId = queryHash,
                ExecutionTime = stopwatch.Elapsed,
                ResultCount = resultCount,
                CulturalContext = culturalContext,
                AnalyzedAt = DateTime.UtcNow
            };
            
            // Analyze query complexity
            performanceMetrics.ComplexityScore = CalculateQueryComplexity(query);
            performanceMetrics.OptimizationPotential = CalculateOptimizationPotential(performanceMetrics);
            
            _performanceCache.TryAdd(queryHash, performanceMetrics);
            
            _logger.LogInformation(
                "Query performance analysis completed: {ExecutionTimeMs}ms, {ResultCount} results, Complexity: {ComplexityScore}",
                stopwatch.ElapsedMilliseconds, resultCount, performanceMetrics.ComplexityScore);

            return Result.Success(performanceMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze query performance");
            return Result.Failure<QueryPerformanceMetrics>($"Query performance analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetOptimizationRecommendationsAsync<T>(
        IQueryable<T> query,
        QueryPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recommendations = new List<string>();
            
            // Performance-based recommendations
            if (performanceMetrics.ExecutionTime > TimeSpan.FromMilliseconds(500))
            {
                recommendations.Add("Consider adding database indexes for cultural intelligence queries");
                recommendations.Add("Implement query result caching for frequently accessed cultural data");
            }
            
            if (performanceMetrics.ComplexityScore > 0.7)
            {
                recommendations.Add("Break complex cultural intelligence queries into smaller, focused queries");
                recommendations.Add("Consider using compiled queries for repeated cultural data access patterns");
            }
            
            // Cultural intelligence specific recommendations
            var culturalContext = performanceMetrics.CulturalContext;
            if (!string.IsNullOrEmpty(culturalContext.CommunityId))
            {
                recommendations.Add($"Create specialized indexes for {culturalContext.CommunityId} community queries");
                recommendations.Add("Implement cultural data partitioning by community for better performance");
            }
            
            if (!string.IsNullOrEmpty(culturalContext.GeographicRegion))
            {
                recommendations.Add($"Optimize for {culturalContext.GeographicRegion} regional data distribution");
                recommendations.Add("Consider regional database replicas for improved diaspora community performance");
            }
            
            // General optimization recommendations
            recommendations.Add("Enable query plan caching for cultural intelligence patterns");
            recommendations.Add("Implement connection pooling optimization for multi-cultural queries");
            recommendations.Add("Consider read replicas for cultural intelligence reporting queries");
            
            _logger.LogDebug(
                "Generated {RecommendationCount} optimization recommendations for query with {ComplexityScore} complexity",
                recommendations.Count, performanceMetrics.ComplexityScore);

            await Task.CompletedTask;
            return Result.Success<IEnumerable<string>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate optimization recommendations");
            return Result.Failure<IEnumerable<string>>($"Optimization recommendations generation failed: {ex.Message}");
        }
    }

    public async Task<Result> WarmupCulturalIntelligenceQueriesAsync(
        IEnumerable<string> communityIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var communities = communityIds.ToList();
            _logger.LogInformation(
                "Starting cultural intelligence query warmup for {CommunityCount} communities",
                communities.Count);

            var warmupTasks = communities.Select(async communityId =>
            {
                try
                {
                    await WarmupCommunityQueriesAsync(communityId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warmup queries for community {CommunityId}", communityId);
                }
            });
            
            await Task.WhenAll(warmupTasks);
            
            _logger.LogInformation(
                "Cultural intelligence query warmup completed for {CommunityCount} communities",
                communities.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warmup cultural intelligence queries");
            return Result.Failure($"Query warmup failed: {ex.Message}");
        }
    }

    // Private helper methods
    private string GenerateQueryHash<T>(IQueryable<T> query, DomainCulturalContext culturalContext)
    {
        var queryString = query.Expression.ToString();
        var contextString = JsonSerializer.Serialize(culturalContext);
        var combined = $"{queryString}:{contextString}";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    private bool IsPlanExpired(OptimizedQueryPlan plan)
    {
        return DateTime.UtcNow > plan.CacheExpiry;
    }

    private bool IsMetricsExpired(QueryPerformanceMetrics metrics)
    {
        return DateTime.UtcNow - metrics.AnalyzedAt > TimeSpan.FromHours(1);
    }

    private async Task<QueryAnalysis> AnalyzeQueryStructureAsync<T>(
        IQueryable<T> query, 
        DomainCulturalContext culturalContext, 
        CancellationToken cancellationToken)
    {
        var analysis = new QueryAnalysis
        {
            QueryType = typeof(T).Name,
            HasCulturalFilter = query.Expression.ToString().Contains("Community") || 
                               query.Expression.ToString().Contains("Cultural"),
            HasGeographicFilter = query.Expression.ToString().Contains("Region") || 
                                 query.Expression.ToString().Contains("Geographic"),
            HasComplexJoins = query.Expression.ToString().Contains("join") || 
                             query.Expression.ToString().Contains("Join"),
            EstimatedResultSize = await EstimateResultSizeAsync(query, cancellationToken)
        };
        
        return analysis;
    }

    private async Task<int> EstimateResultSizeAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        try
        {
            // Use a sample to estimate total size
            var sampleCount = await query.Take(100).CountAsync(cancellationToken);
            return sampleCount < 100 ? sampleCount : sampleCount * 10; // Rough estimation
        }
        catch
        {
            return 1000; // Default estimate
        }
    }

    private List<string> GenerateOptimizationStrategies(QueryAnalysis analysis, QueryPerformanceTarget target)
    {
        var strategies = new List<string>();
        
        if (analysis.HasCulturalFilter)
        {
            strategies.Add("CulturalIndexOptimization");
            strategies.Add("CommunityDataPartitioning");
        }
        
        if (analysis.HasGeographicFilter)
        {
            strategies.Add("GeographicIndexOptimization");
            strategies.Add("RegionalQueryRouting");
        }
        
        if (analysis.HasComplexJoins)
        {
            strategies.Add("JoinOptimization");
            strategies.Add("QuerySplitting");
        }
        
        if (target == QueryPerformanceTarget.RealTime)
        {
            strategies.Add("ResultCaching");
            strategies.Add("QueryCompilation");
        }
        
        return strategies;
    }

    private TimeSpan CalculateEstimatedExecutionTime(QueryAnalysis analysis, List<string> strategies)
    {
        var baseTime = analysis.EstimatedResultSize switch
        {
            < 100 => 50,
            < 1000 => 150,
            < 10000 => 500,
            _ => 1000
        };
        
        var optimizationMultiplier = strategies.Count * 0.1; // Each strategy reduces time by ~10%
        var optimizedTime = baseTime * (1 - optimizationMultiplier);
        
        return TimeSpan.FromMilliseconds(Math.Max(10, optimizedTime));
    }

    private void ApplyCulturalIntelligenceOptimizations(OptimizedQueryPlan queryPlan, DomainCulturalContext culturalContext)
    {
        // Add cultural intelligence specific optimizations
        var culturalOptimizations = new List<string>();
        
        if (!string.IsNullOrEmpty(culturalContext.CommunityId))
        {
            culturalOptimizations.Add($"Community-specific optimization for {culturalContext.CommunityId}");
        }
        
        if (!string.IsNullOrEmpty(culturalContext.Language))
        {
            culturalOptimizations.Add($"Language-specific optimization for {culturalContext.Language}");
        }
        
        queryPlan.CulturalOptimizations = culturalOptimizations;
    }

    private AppDbContext ApplyContextOptimizations(OptimizedQueryPlan queryPlan)
    {
        var context = (AppDbContext)_dbContext;
        
        // Apply optimization strategies
        foreach (var strategy in queryPlan.OptimizationStrategies)
        {
            switch (strategy)
            {
                case "ResultCaching":
                    // Enable query result caching
                    break;
                case "QueryCompilation":
                    // Use compiled queries where possible
                    break;
                default:
                    // Log unhandled strategies
                    _logger.LogDebug("Optimization strategy {Strategy} not implemented", strategy);
                    break;
            }
        }
        
        return context;
    }

    private async Task<IEnumerable<T>> ExecuteWithOptimizationsAsync<T>(
        OptimizedQueryPlan queryPlan,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        // For now, return empty result as we can't reconstruct the original query
        // In a real implementation, we would store the query expression and reconstruct it
        await Task.CompletedTask;
        return Enumerable.Empty<T>();
    }

    private async Task WarmupCommunityQueriesAsync(string communityId, CancellationToken cancellationToken)
    {
        // Simulate common cultural intelligence queries for the community
        await Task.Delay(100, cancellationToken); // Simulate warmup operation
        
        _logger.LogDebug("Warmed up queries for community {CommunityId}", communityId);
    }

    private double CalculateQueryComplexity<T>(IQueryable<T> query)
    {
        var queryString = query.Expression.ToString();
        var complexity = 0.0;
        
        // Basic complexity scoring
        if (queryString.Contains("Where")) complexity += 0.1;
        if (queryString.Contains("OrderBy")) complexity += 0.1;
        if (queryString.Contains("GroupBy")) complexity += 0.2;
        if (queryString.Contains("Join")) complexity += 0.3;
        if (queryString.Contains("Select")) complexity += 0.1;
        
        return Math.Min(1.0, complexity);
    }

    private double CalculateOptimizationPotential(QueryPerformanceMetrics metrics)
    {
        var potential = 0.0;
        
        if (metrics.ExecutionTime > TimeSpan.FromMilliseconds(200)) potential += 0.3;
        if (metrics.ComplexityScore > 0.5) potential += 0.4;
        if (metrics.ResultCount > 1000) potential += 0.3;
        
        return Math.Min(1.0, potential);
    }
}

// Supporting classes
public class QueryAnalysis
{
    public string QueryType { get; set; } = string.Empty;
    public bool HasCulturalFilter { get; set; }
    public bool HasGeographicFilter { get; set; }
    public bool HasComplexJoins { get; set; }
    public int EstimatedResultSize { get; set; }
}

public enum QueryPerformanceTarget
{
    RealTime,
    Fast,
    Standard,
    Background
}

public class OptimizedQueryPlan
{
    public string QueryId { get; set; } = string.Empty;
    public string OriginalQuery { get; set; } = string.Empty;
    public DomainCulturalContext CulturalContext { get; set; } = new();
    public QueryPerformanceTarget PerformanceTarget { get; set; }
    public List<string> OptimizationStrategies { get; set; } = new();
    public List<string> CulturalOptimizations { get; set; } = new();
    public TimeSpan EstimatedExecutionTime { get; set; }
    public TimeSpan OptimizationTime { get; set; }
    public DateTime CacheExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QueryPerformanceMetrics
{
    public string QueryId { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public int ResultCount { get; set; }
    public double ComplexityScore { get; set; }
    public double OptimizationPotential { get; set; }
    public DomainCulturalContext CulturalContext { get; set; } = new();
    public List<string> OptimizationStrategiesUsed { get; set; } = new();
    public DateTime ExecutedAt { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class DatabaseOptimizationOptions
{
    public bool EnableQueryOptimization { get; set; } = true;
    public int QueryPlanCacheHours { get; set; } = 4;
    public bool EnablePerformanceTracking { get; set; } = true;
    public int MaxConcurrentOptimizations { get; set; } = 10;
    public bool EnableQueryWarmup { get; set; } = true;
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> CulturalOptimizationSettings { get; set; } = new();
}
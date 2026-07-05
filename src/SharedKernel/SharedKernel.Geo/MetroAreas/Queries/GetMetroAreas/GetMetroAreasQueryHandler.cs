using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.SharedKernel.Geo.MetroAreas.Queries.GetMetroAreas;

/// <summary>
/// Handler for GetMetroAreasQuery.
/// Phase 5C: Metro Areas API.
///
/// Phase 8 (post-prod-perf-RCA hygiene): adds <see cref="IMemoryCache"/>
/// fronting the DB query. MetroAreas change rarely (operator-managed seed
/// data) but the endpoint is hit on every event-detail page load — caching
/// removes a hot DB query that contributed to the connection-pool
/// starvation noted in <c>MASTER_TODO_PROD_PERF_RCA_2026_04_25.md</c>.
///
/// Cache strategy mirrors <see cref="ReferenceData.Services.ReferenceDataService"/>:
/// 1-hour absolute expiration, key derived from filter params so each
/// distinct query gets its own slot.
/// </summary>
public class GetMetroAreasQueryHandler : IQueryHandler<GetMetroAreasQuery, IReadOnlyList<MetroAreaDto>>
{
    private const string CACHE_KEY_PREFIX = "MetroAreas";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);
    private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = CacheExpiration,
        Priority = CacheItemPriority.High // Reference data rarely changes
    };

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetMetroAreasQueryHandler> _logger;

    public GetMetroAreasQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<GetMetroAreasQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MetroAreaDto>>> Handle(
        GetMetroAreasQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetMetroAreas"))
        using (LogContext.PushProperty("EntityType", "MetroArea"))
        {
            var stopwatch = Stopwatch.StartNew();

            var stateFilterNormalized = string.IsNullOrWhiteSpace(request.StateFilter)
                ? "*"
                : request.StateFilter.Trim().ToUpperInvariant();
            var cacheKey = $"{CACHE_KEY_PREFIX}:state={stateFilterNormalized}:active={request.ActiveOnly}";

            // Cache hit — short-circuit before touching DbContext.
            if (_cache.TryGetValue<IReadOnlyList<MetroAreaDto>>(cacheKey, out var cached) && cached is not null)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "GetMetroAreas CACHE-HIT: ActiveOnly={ActiveOnly}, StateFilter={StateFilter}, Count={Count}, Duration={ElapsedMs}ms",
                    request.ActiveOnly, request.StateFilter, cached.Count, stopwatch.ElapsedMilliseconds);
                return Result<IReadOnlyList<MetroAreaDto>>.Success(cached);
            }

            _logger.LogInformation(
                "GetMetroAreas CACHE-MISS: ActiveOnly={ActiveOnly}, StateFilter={StateFilter}",
                request.ActiveOnly, request.StateFilter);

            try
            {
                // Query metro areas from database
                var query = _context.MetroAreas.AsNoTracking().AsQueryable();

                // Apply active filter (default: true)
                if (request.ActiveOnly == true)
                {
                    query = query.Where(m => m.IsActive);
                }

                // Apply state filter if provided
                if (!string.IsNullOrWhiteSpace(request.StateFilter))
                {
                    var stateUpper = request.StateFilter.ToUpper();
                    query = query.Where(m => m.State == stateUpper);
                }

                // Execute query and order results
                var metroAreas = await query
                    .OrderBy(m => m.State)
                    .ThenBy(m => m.Name)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "GetMetroAreas: Query executed - MetroAreaCount={MetroAreaCount}",
                    metroAreas.Count);

                // Map to DTOs
                var dtos = (IReadOnlyList<MetroAreaDto>)metroAreas
                    .Select(m => _mapper.Map<MetroAreaDto>(m))
                    .ToList();

                // Populate the cache with the materialised result.
                _cache.Set(cacheKey, dtos, CacheOptions);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetMetroAreas COMPLETE: ActiveOnly={ActiveOnly}, StateFilter={StateFilter}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms, CacheKey={CacheKey}",
                    request.ActiveOnly, request.StateFilter, dtos.Count, stopwatch.ElapsedMilliseconds, cacheKey);

                return Result<IReadOnlyList<MetroAreaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetMetroAreas FAILED: Exception occurred - ActiveOnly={ActiveOnly}, StateFilter={StateFilter}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.ActiveOnly, request.StateFilter, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<IReadOnlyList<MetroAreaDto>>.Failure("Failed to fetch metro areas");
            }
        }
    }
}

using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.MetroAreas.Queries.GetMetroAreas;

/// <summary>
/// Handler for GetMetroAreasQuery
/// Phase 5C: Metro Areas API
/// Returns all active metro areas from events.metro_areas table
/// </summary>
public class GetMetroAreasQueryHandler : IQueryHandler<GetMetroAreasQuery, IReadOnlyList<MetroAreaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetMetroAreasQueryHandler> _logger;

    public GetMetroAreasQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<GetMetroAreasQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
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

            _logger.LogInformation(
                "GetMetroAreas START: ActiveOnly={ActiveOnly}, StateFilter={StateFilter}",
                request.ActiveOnly, request.StateFilter);

            try
            {
                // Query metro areas from database
                var query = _context.MetroAreas.AsQueryable();

                // Apply active filter (default: true)
                if (request.ActiveOnly == true)
                {
                    query = query.Where(m => m.IsActive);
                }

                // Apply state filter if provided
                if (!string.IsNullOrWhiteSpace(request.StateFilter))
                {
                    query = query.Where(m => m.State == request.StateFilter.ToUpper());
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
                var dtos = metroAreas.Select(m => _mapper.Map<MetroAreaDto>(m)).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetMetroAreas COMPLETE: ActiveOnly={ActiveOnly}, StateFilter={StateFilter}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.ActiveOnly, request.StateFilter, dtos.Count, stopwatch.ElapsedMilliseconds);

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

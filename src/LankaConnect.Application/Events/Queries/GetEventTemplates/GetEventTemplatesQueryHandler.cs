using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventTemplates;

/// <summary>
/// Phase 6A.8: Event Template System
/// Handler for retrieving event templates with filtering and sorting
/// </summary>
public class GetEventTemplatesQueryHandler : IQueryHandler<GetEventTemplatesQuery, IReadOnlyList<EventTemplateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetEventTemplatesQueryHandler> _logger;

    public GetEventTemplatesQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<GetEventTemplatesQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventTemplateDto>>> Handle(GetEventTemplatesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventTemplates"))
        using (LogContext.PushProperty("EntityType", "EventTemplate"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventTemplates START: Category={Category}, IsActive={IsActive}",
                request.Category, request.IsActive);

            try
            {
                // Build query with filters
                var query = _context.EventTemplates.AsQueryable();

                // Filter by category if specified
                if (request.Category.HasValue)
                {
                    query = query.Where(t => t.Category == request.Category.Value);
                }

                // Filter by active status if specified (default to active templates only)
                if (request.IsActive.HasValue)
                {
                    query = query.Where(t => t.IsActive == request.IsActive.Value);
                }
                else
                {
                    // By default, only return active templates
                    query = query.Where(t => t.IsActive);
                }

                // Order by display order, then by name
                query = query
                    .OrderBy(t => t.DisplayOrder)
                    .ThenBy(t => t.Name);

                // Execute query and map to DTOs
                var templates = await query.ToListAsync(cancellationToken);
                var templateDtos = templates.Select(t => _mapper.Map<EventTemplateDto>(t)).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventTemplates COMPLETE: Category={Category}, IsActive={IsActive}, TemplateCount={TemplateCount}, Duration={ElapsedMs}ms",
                    request.Category, request.IsActive, templateDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventTemplateDto>>.Success(templateDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventTemplates FAILED: Exception occurred - Category={Category}, IsActive={IsActive}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Category, request.IsActive, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

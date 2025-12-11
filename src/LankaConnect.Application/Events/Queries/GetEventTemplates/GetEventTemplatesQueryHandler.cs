using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetEventTemplates;

/// <summary>
/// Phase 6A.8: Event Template System
/// Handler for retrieving event templates with filtering and sorting
/// </summary>
public class GetEventTemplatesQueryHandler : IQueryHandler<GetEventTemplatesQuery, IReadOnlyList<EventTemplateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventTemplatesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventTemplateDto>>> Handle(GetEventTemplatesQuery request, CancellationToken cancellationToken)
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

        return Result<IReadOnlyList<EventTemplateDto>>.Success(templateDtos);
    }
}

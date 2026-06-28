using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventTemplates;

/// <summary>
/// Phase 6A.8: Event Template System
/// Query to retrieve event templates with optional filtering by category and active status
/// </summary>
public record GetEventTemplatesQuery(
    EventCategory? Category = null,
    bool? IsActive = null
) : IQuery<IReadOnlyList<EventTemplateDto>>;

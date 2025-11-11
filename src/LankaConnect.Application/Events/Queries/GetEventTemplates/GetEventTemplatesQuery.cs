using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetEventTemplates;

/// <summary>
/// Phase 6A.8: Event Template System
/// Query to retrieve event templates with optional filtering by category and active status
/// </summary>
public record GetEventTemplatesQuery(
    EventCategory? Category = null,
    bool? IsActive = null
) : IQuery<IReadOnlyList<EventTemplateDto>>;

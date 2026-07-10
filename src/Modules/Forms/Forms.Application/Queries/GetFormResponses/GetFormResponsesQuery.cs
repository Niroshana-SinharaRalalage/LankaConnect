using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Modules.Forms.Application.Queries.GetFormResponses;

/// <summary>
/// Gets paginated responses for a form (organizer view).
/// </summary>
public record GetFormResponsesQuery(
    Guid EventId,
    Guid FormId,
    int Page = 1,
    int PageSize = 20
) : IQuery<FormResponsesPagedDto>;

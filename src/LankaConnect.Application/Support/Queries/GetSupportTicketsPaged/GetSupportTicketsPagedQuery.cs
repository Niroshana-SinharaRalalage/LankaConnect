using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketsPaged;

/// <summary>
/// Query to get paginated list of support tickets for admin view
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record GetSupportTicketsPagedQuery : IQuery<PagedResultDto<SupportTicketDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public SupportTicketStatus? StatusFilter { get; init; }
    public SupportTicketPriority? PriorityFilter { get; init; }
    public Guid? AssignedToFilter { get; init; }
    public bool? UnassignedOnly { get; init; }
}

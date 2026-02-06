using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.Domain.Support;

/// <summary>
/// Phase 6A.89: Repository interface for SupportTicket aggregate.
/// </summary>
public interface ISupportTicketRepository
{
    /// <summary>
    /// Gets a support ticket by its ID
    /// </summary>
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a support ticket by its reference ID (e.g., CONTACT-20260127-XXXXXXXX)
    /// </summary>
    Task<SupportTicket?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated support tickets with optional filtering
    /// </summary>
    Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        SupportTicketStatus? statusFilter = null,
        SupportTicketPriority? priorityFilter = null,
        Guid? assignedToFilter = null,
        bool? unassignedOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ticket counts by status for statistics
    /// </summary>
    Task<Dictionary<SupportTicketStatus, int>> GetCountsByStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.90: Gets ticket counts by priority for statistics
    /// </summary>
    Task<Dictionary<SupportTicketPriority, int>> GetCountsByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.90: Gets count of unassigned tickets
    /// </summary>
    Task<int> GetUnassignedCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new support ticket
    /// </summary>
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing support ticket
    /// </summary>
    void Update(SupportTicket ticket);

    /// <summary>
    /// Saves all changes to the repository
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

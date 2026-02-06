namespace LankaConnect.Domain.Support;

/// <summary>
/// Phase 6A.89: Repository interface for AdminAuditLog.
/// </summary>
public interface IAdminAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry
    /// </summary>
    Task AddAsync(AdminAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated audit logs with optional filtering
    /// </summary>
    Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? adminUserFilter = null,
        string? actionFilter = null,
        Guid? targetUserFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    Task<IReadOnlyList<AdminAuditLog>> GetByTargetUserAsync(
        Guid targetUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the repository
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

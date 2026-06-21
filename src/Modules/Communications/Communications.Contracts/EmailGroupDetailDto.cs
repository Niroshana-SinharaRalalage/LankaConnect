namespace LankaConnect.Modules.Communications.Contracts;

/// <summary>
/// Cross-module read-only projection of an EmailGroup WITH the individual
/// email addresses split out of the aggregate's CSV. Returned by
/// <see cref="IEmailGroupQueries.GetByIdWithEmailsAsync"/>.
/// </summary>
/// <remarks>
/// Wave 5.4.a (2026-06-13). The aggregate stores recipients as a single
/// comma-separated <c>EmailAddresses</c> string for historical reasons
/// (Phase 6A.25). This DTO does the split + trim + de-duplication once at the
/// Contracts boundary so callers don't reimplement it.
/// </remarks>
public sealed record EmailGroupDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    IReadOnlyList<string> Emails,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public int EmailCount => Emails.Count;
}

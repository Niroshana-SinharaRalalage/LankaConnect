namespace LankaConnect.Modules.Communications.Contracts;

/// <summary>
/// Cross-module read-only projection of an EmailGroup (without member-email
/// detail). Returned by <see cref="IEmailGroupQueries.GetByIdsAsync"/>,
/// <see cref="IEmailGroupQueries.GetByIdAsync"/>, and
/// <see cref="IEmailGroupQueries.GetByOwnerAsync"/>.
/// </summary>
/// <remarks>
/// Wave 5.4.a (2026-06-13). Field surface tracks the EmailGroup aggregate's
/// public state as of W4 (Phase 6A.25). <see cref="EmailCount"/> is the count
/// of comma-separated entries in the aggregate's <c>EmailAddresses</c> CSV —
/// callers that need the individual addresses request
/// <see cref="EmailGroupDetailDto"/> via
/// <see cref="IEmailGroupQueries.GetByIdWithEmailsAsync"/>.
/// </remarks>
public sealed record EmailGroupSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    int EmailCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

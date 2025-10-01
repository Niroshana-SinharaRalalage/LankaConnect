using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Queries.GetEmailStatus;

/// <summary>
/// Query to get the status of sent emails for a user or specific email
/// </summary>
/// <param name="UserId">Optional user ID to filter emails by user</param>
/// <param name="EmailAddress">Optional email address to filter by</param>
/// <param name="EmailType">Optional email type to filter by</param>
/// <param name="Status">Optional status to filter by</param>
/// <param name="PageNumber">Page number for pagination</param>
/// <param name="PageSize">Page size for pagination</param>
/// <param name="FromDate">Optional start date to filter emails</param>
/// <param name="ToDate">Optional end date to filter emails</param>
public record GetEmailStatusQuery(
    Guid? UserId = null,
    string? EmailAddress = null,
    EmailType? EmailType = null,
    EmailStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<GetEmailStatusResponse>;

/// <summary>
/// Response containing email status information
/// </summary>
public class GetEmailStatusResponse
{
    public List<EmailStatusDto> Emails { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public GetEmailStatusResponse(List<EmailStatusDto> emails, int totalCount, int pageNumber, int pageSize)
    {
        Emails = emails;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
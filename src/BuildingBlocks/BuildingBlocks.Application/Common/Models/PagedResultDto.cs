namespace LankaConnect.BuildingBlocks.Application.Common.Models;

/// <summary>
/// Generic paged result DTO. Wave 4.6.c.3 (2026-06-24): extracted from
/// <c>LankaConnect.Modules.Identity.Application.Users.DTOs.AdminUserDto.cs</c> so consumers
/// in legacy <c>LankaConnect.Application</c> (UserMappingProfile,
/// GetSupportTicketsPagedQuery, etc.) can keep referencing it after the
/// Identity DTOs move to Identity.Application.DTOs. Belongs in Common
/// because paging is a cross-cutting concern, not Identity-specific.
/// </summary>
public record PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

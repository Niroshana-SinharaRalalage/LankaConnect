using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.DTOs;
namespace LankaConnect.Modules.Identity.Application.Queries.Users.SearchUsers;

/// <summary>
/// Phase 6A.133: Search registered users by name, email, or phone for co-organizer linking.
/// </summary>
public record SearchUsersQuery : IQuery<IReadOnlyList<UserSearchResultDto>>
{
    public string SearchTerm { get; init; } = string.Empty;

    public SearchUsersQuery(string searchTerm)
    {
        SearchTerm = searchTerm;
    }
}

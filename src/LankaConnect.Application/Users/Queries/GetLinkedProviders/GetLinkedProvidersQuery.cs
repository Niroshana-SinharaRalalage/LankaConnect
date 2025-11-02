using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Queries.GetLinkedProviders;

/// <summary>
/// Query to retrieve all linked external providers for a user
/// Epic 1 Phase 2: Multi-Provider Social Login
/// </summary>
public record GetLinkedProvidersQuery : IQuery<GetLinkedProvidersResponse>
{
    public Guid UserId { get; init; }

    public GetLinkedProvidersQuery(Guid userId)
    {
        UserId = userId;
    }
}

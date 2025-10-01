using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;

namespace LankaConnect.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery : IQuery<UserDto>
{
    public Guid UserId { get; init; }

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
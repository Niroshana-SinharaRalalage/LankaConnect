using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.DTOs;
namespace LankaConnect.Modules.Identity.Application.Queries.Users.GetUserById;

public record GetUserByIdQuery : IQuery<UserDto>
{
    public Guid UserId { get; init; }

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
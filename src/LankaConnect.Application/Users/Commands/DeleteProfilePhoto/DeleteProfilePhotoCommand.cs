using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.DeleteProfilePhoto;

/// <summary>
/// Command to delete a user's profile photo
/// </summary>
public record DeleteProfilePhotoCommand : ICommand
{
    public Guid UserId { get; init; }
}

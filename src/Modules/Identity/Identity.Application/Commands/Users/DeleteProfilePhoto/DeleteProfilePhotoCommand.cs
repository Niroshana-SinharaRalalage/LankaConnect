using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.DeleteProfilePhoto;

/// <summary>
/// Command to delete a user's profile photo
/// </summary>
public record DeleteProfilePhotoCommand : ICommand
{
    public Guid UserId { get; init; }
}

using LankaConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LankaConnect.Application.Users.Commands.UploadProfilePhoto;

/// <summary>
/// Command to upload a profile photo for a user
/// </summary>
public record UploadProfilePhotoCommand : ICommand<UploadProfilePhotoResponse>
{
    public Guid UserId { get; init; }
    public IFormFile ImageFile { get; init; } = null!;
}

/// <summary>
/// Response after successfully uploading a profile photo
/// </summary>
public record UploadProfilePhotoResponse
{
    public string PhotoUrl { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}

using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a user's profile photo is updated
/// </summary>
public record UserProfilePhotoUpdatedEvent(
    Guid UserId,
    string PhotoUrl,
    string BlobName) : DomainEvent;

using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Domain event raised when a user's profile photo is removed
/// </summary>
public record UserProfilePhotoRemovedEvent(
    Guid UserId,
    string OldPhotoUrl,
    string OldBlobName) : DomainEvent;

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.UpdateUserLocation;

/// <summary>
/// Command to update a user's location
/// Nullable properties - if all are null, location will be cleared (privacy choice)
/// </summary>
public record UpdateUserLocationCommand : ICommand
{
    public Guid UserId { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}

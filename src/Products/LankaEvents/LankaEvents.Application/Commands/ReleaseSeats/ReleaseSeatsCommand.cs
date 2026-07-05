using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ReleaseSeats;

/// <summary>
/// Releases held seats (user cancelled seat selection before checkout).
/// </summary>
public record ReleaseSeatsCommand(
    string SessionId,
    Guid UserId
) : ICommand;

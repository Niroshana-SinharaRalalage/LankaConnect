using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ReleaseSeats;

/// <summary>
/// Releases held seats (user cancelled seat selection before checkout).
/// </summary>
public record ReleaseSeatsCommand(
    string SessionId,
    Guid UserId
) : ICommand;

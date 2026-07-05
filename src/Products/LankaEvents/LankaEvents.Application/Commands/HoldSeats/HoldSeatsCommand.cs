using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.HoldSeats;

/// <summary>
/// Temporarily holds seats for a user's checkout session (10-minute hold).
/// </summary>
public record HoldSeatsCommand(
    Guid EventId,
    Guid UserId,
    string SessionId,
    List<Guid> SeatIds
) : ICommand<HoldSeatsResult>;

public record HoldSeatsResult(
    List<Guid> HeldSeatIds,
    DateTime ExpiresAt,
    string SessionId
);

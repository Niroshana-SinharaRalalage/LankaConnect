namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Represents the lifecycle status of a seat hold.
/// Holds are temporary reservations during the checkout flow.
/// </summary>
public enum SeatHoldStatus
{
    /// <summary>Seat is actively held for a user. Expires after 10 minutes.</summary>
    Active = 0,

    /// <summary>Hold expired without being confirmed. Seat is available again.</summary>
    Expired = 1,

    /// <summary>Hold was confirmed — seat has been reserved via a completed registration.</summary>
    Confirmed = 2,

    /// <summary>Hold was manually released by the user before expiry.</summary>
    Released = 3
}

using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Represents a temporary hold on a seat during the checkout flow.
/// Holds expire after 10 minutes if not confirmed.
/// A partial unique index on (seat_id) WHERE status = 'Active' prevents double-holds.
/// </summary>
public class SeatHold : LegacyBaseEntity
{
    /// <summary>Default hold duration: 10 minutes.</summary>
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    public Guid SeatId { get; private set; }
    public Guid UserId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public SeatHoldStatus Status { get; private set; }
    public DateTime HeldAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Whether this hold has expired based on current UTC time.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    // EF Core parameterless constructor
    private SeatHold() { }

    private SeatHold(
        Guid seatId,
        Guid userId,
        string sessionId)
    {
        SeatId = seatId;
        UserId = userId;
        SessionId = sessionId;
        Status = SeatHoldStatus.Active;
        HeldAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(HoldDuration);
    }

    /// <summary>
    /// Creates a new seat hold for a user's checkout session.
    /// </summary>
    public static Result<SeatHold> Create(
        Guid seatId,
        Guid userId,
        string sessionId)
    {
        if (seatId == Guid.Empty)
            return Result<SeatHold>.Failure("Seat ID is required");

        if (userId == Guid.Empty)
            return Result<SeatHold>.Failure("User ID is required");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Result<SeatHold>.Failure("Session ID is required");

        if (sessionId.Trim().Length > 100)
            return Result<SeatHold>.Failure("Session ID cannot exceed 100 characters");

        return Result<SeatHold>.Success(new SeatHold(seatId, userId, sessionId.Trim()));
    }

    /// <summary>
    /// Marks the hold as expired. Called by the cleanup service.
    /// </summary>
    public Result Expire()
    {
        if (Status != SeatHoldStatus.Active)
            return Result.Failure($"Cannot expire a hold with status '{Status}'");

        Status = SeatHoldStatus.Expired;
        return Result.Success();
    }

    /// <summary>
    /// Confirms the hold — seat is now reserved via a completed registration.
    /// </summary>
    public Result Confirm()
    {
        if (Status != SeatHoldStatus.Active)
            return Result.Failure($"Cannot confirm a hold with status '{Status}'");

        if (IsExpired)
            return Result.Failure("Cannot confirm an expired hold");

        Status = SeatHoldStatus.Confirmed;
        return Result.Success();
    }

    /// <summary>
    /// Manually releases the hold before expiry (user cancelled seat selection).
    /// </summary>
    public Result Release()
    {
        if (Status != SeatHoldStatus.Active)
            return Result.Failure($"Cannot release a hold with status '{Status}'");

        Status = SeatHoldStatus.Released;
        return Result.Success();
    }
}

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelPendingAddition;

/// <summary>
/// Command to cancel a pending attendee addition before payment completes.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record CancelPendingAdditionCommand(
    /// <summary>
    /// ID of the registration with the pending addition.
    /// </summary>
    Guid RegistrationId,

    /// <summary>
    /// User ID for authorization (optional for anonymous).
    /// </summary>
    Guid? UserId = null
) : ICommand<CancelPendingAdditionResult>;

/// <summary>
/// Result of cancelling a pending addition.
/// </summary>
public record CancelPendingAdditionResult
{
    /// <summary>
    /// Whether the cancellation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if not successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// ID of the cancelled addition (if found).
    /// </summary>
    public Guid? CancelledAdditionId { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CancelPendingAdditionResult Successful(Guid cancelledAdditionId)
    {
        return new CancelPendingAdditionResult
        {
            Success = true,
            CancelledAdditionId = cancelledAdditionId
        };
    }

    /// <summary>
    /// Creates a result indicating no pending addition found.
    /// </summary>
    public static CancelPendingAdditionResult NoPendingAddition()
    {
        return new CancelPendingAdditionResult
        {
            Success = true
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CancelPendingAdditionResult Failed(string errorMessage)
    {
        return new CancelPendingAdditionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

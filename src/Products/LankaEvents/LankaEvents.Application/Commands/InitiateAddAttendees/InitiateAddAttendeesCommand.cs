using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.CalculateAdditionPrice;
namespace LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees;

/// <summary>
/// Command to initiate adding attendees to an existing paid registration.
/// Creates a RegistrationAddition record and Stripe checkout session.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record InitiateAddAttendeesCommand(
    /// <summary>
    /// ID of the existing registration to add attendees to.
    /// </summary>
    Guid RegistrationId,

    /// <summary>
    /// New attendees to add.
    /// </summary>
    List<NewAttendeeDto> NewAttendees,

    /// <summary>
    /// URL to redirect to after successful payment.
    /// </summary>
    string SuccessUrl,

    /// <summary>
    /// URL to redirect to if user cancels.
    /// </summary>
    string CancelUrl,

    /// <summary>
    /// User ID (if authenticated, null for anonymous).
    /// </summary>
    Guid? UserId = null
) : ICommand<InitiateAddAttendeesResult>;

/// <summary>
/// Result of initiating an add attendees operation.
/// </summary>
public record InitiateAddAttendeesResult
{
    /// <summary>
    /// Whether the initiation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if not successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The RegistrationAddition ID tracking this addition.
    /// </summary>
    public Guid? RegistrationAdditionId { get; init; }

    /// <summary>
    /// The Stripe Checkout Session ID.
    /// </summary>
    public string? CheckoutSessionId { get; init; }

    /// <summary>
    /// The checkout URL to redirect the user to.
    /// </summary>
    public string? CheckoutUrl { get; init; }

    /// <summary>
    /// When the checkout session expires.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Additional amount being charged.
    /// </summary>
    public decimal? AdditionalAmount { get; init; }

    /// <summary>
    /// Currency for the amount.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Number of new attendees.
    /// </summary>
    public int NewAttendeesCount { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static InitiateAddAttendeesResult Successful(
        Guid registrationAdditionId,
        string checkoutSessionId,
        string checkoutUrl,
        DateTime expiresAt,
        decimal additionalAmount,
        string currency,
        int newAttendeesCount)
    {
        return new InitiateAddAttendeesResult
        {
            Success = true,
            RegistrationAdditionId = registrationAdditionId,
            CheckoutSessionId = checkoutSessionId,
            CheckoutUrl = checkoutUrl,
            ExpiresAt = expiresAt,
            AdditionalAmount = additionalAmount,
            Currency = currency,
            NewAttendeesCount = newAttendeesCount
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static InitiateAddAttendeesResult Failed(string errorMessage)
    {
        return new InitiateAddAttendeesResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

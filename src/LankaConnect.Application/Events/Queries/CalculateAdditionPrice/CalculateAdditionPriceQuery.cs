using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Queries.CalculateAdditionPrice;

/// <summary>
/// Query to calculate the price for adding new attendees to an existing paid registration.
/// Returns the delta amount to charge based on the event's pricing type.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record CalculateAdditionPriceQuery(
    /// <summary>
    /// ID of the existing registration to add attendees to.
    /// </summary>
    Guid RegistrationId,

    /// <summary>
    /// New attendees to add with their details.
    /// </summary>
    List<NewAttendeeDto> NewAttendees
) : IQuery<AdditionPriceResultDto>;

/// <summary>
/// DTO representing a new attendee to be added.
/// </summary>
public record NewAttendeeDto(
    /// <summary>
    /// Attendee name.
    /// </summary>
    string Name,

    /// <summary>
    /// Age category (Adult, Child, Senior, etc.)
    /// </summary>
    AgeCategory AgeCategory,

    /// <summary>
    /// Optional gender.
    /// </summary>
    Gender? Gender = null
);

/// <summary>
/// Result DTO containing pricing calculation details.
/// </summary>
public record AdditionPriceResultDto
{
    /// <summary>
    /// Registration ID.
    /// </summary>
    public Guid RegistrationId { get; init; }

    /// <summary>
    /// Event ID.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Current attendee count in the registration.
    /// </summary>
    public int CurrentAttendeeCount { get; init; }

    /// <summary>
    /// Number of new attendees being added.
    /// </summary>
    public int NewAttendeesCount { get; init; }

    /// <summary>
    /// Total attendee count after addition.
    /// </summary>
    public int TotalAttendeeCount { get; init; }

    /// <summary>
    /// Maximum attendees allowed per registration.
    /// </summary>
    public int MaxAttendeesPerRegistration { get; init; }

    /// <summary>
    /// Amount already paid for the registration.
    /// </summary>
    public decimal CurrentTotalPaid { get; init; }

    /// <summary>
    /// New total price after adding attendees.
    /// </summary>
    public decimal NewTotalPrice { get; init; }

    /// <summary>
    /// Additional amount to charge (delta).
    /// </summary>
    public decimal AdditionalAmount { get; init; }

    /// <summary>
    /// Currency for all amounts.
    /// </summary>
    public string Currency { get; init; } = null!;

    /// <summary>
    /// Whether the addition is valid (capacity, max attendees, etc.).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if IsValid is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether there is an existing pending addition.
    /// </summary>
    public bool HasPendingAddition { get; init; }

    /// <summary>
    /// Breakdown of price per new attendee (for display purposes).
    /// </summary>
    public List<AttendeePrice>? AttendeeBreakdown { get; init; }

    /// <summary>
    /// Event title for display.
    /// </summary>
    public string EventTitle { get; init; } = null!;

    /// <summary>
    /// Remaining event capacity (if applicable).
    /// </summary>
    public int? RemainingCapacity { get; init; }
}

/// <summary>
/// Price breakdown per attendee.
/// </summary>
public record AttendeePrice(
    string Name,
    AgeCategory AgeCategory,
    decimal Price
);

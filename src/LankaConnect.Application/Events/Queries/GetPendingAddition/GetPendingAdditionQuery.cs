using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Queries.GetPendingAddition;

/// <summary>
/// Query to get the pending attendee addition for a registration, if any.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record GetPendingAdditionQuery(
    /// <summary>
    /// ID of the registration to check.
    /// </summary>
    Guid RegistrationId
) : IQuery<PendingAdditionDto?>;

/// <summary>
/// DTO representing a pending attendee addition.
/// </summary>
public record PendingAdditionDto
{
    /// <summary>
    /// ID of the pending addition.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Registration this addition belongs to.
    /// </summary>
    public Guid RegistrationId { get; init; }

    /// <summary>
    /// Event this addition is for.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Current status of the addition.
    /// </summary>
    public RegistrationAdditionStatus Status { get; init; }

    /// <summary>
    /// New attendees being added.
    /// </summary>
    public List<PendingAttendeeDto> NewAttendees { get; init; } = new();

    /// <summary>
    /// Additional amount to be charged.
    /// </summary>
    public decimal AdditionalAmount { get; init; }

    /// <summary>
    /// Currency for the amount.
    /// </summary>
    public string Currency { get; init; } = null!;

    /// <summary>
    /// Stripe checkout session ID (if active).
    /// </summary>
    public string? CheckoutSessionId { get; init; }

    /// <summary>
    /// URL to resume checkout (if active).
    /// </summary>
    public string? CheckoutUrl { get; init; }

    /// <summary>
    /// When the checkout session expires.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// When the addition was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO representing an attendee in a pending addition.
/// </summary>
public record PendingAttendeeDto(
    string Name,
    AgeCategory AgeCategory,
    Gender? Gender
);

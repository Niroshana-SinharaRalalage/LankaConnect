using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;

/// <summary>
/// Command to subscribe to the newsletter
/// </summary>
/// <param name="Email">Email address for newsletter subscription</param>
/// <param name="MetroAreaId">Optional metro area ID for location-specific newsletters</param>
/// <param name="ReceiveAllLocations">Whether to receive newsletters from all locations</param>
public record SubscribeToNewsletterCommand(
    string Email,
    Guid? MetroAreaId = null,
    bool ReceiveAllLocations = false) : ICommand<SubscribeToNewsletterResponse>;

/// <summary>
/// Response for newsletter subscription command
/// </summary>
public class SubscribeToNewsletterResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid? MetroAreaId { get; init; }
    public bool ReceiveAllLocations { get; init; }
    public bool IsActive { get; init; }
    public bool IsConfirmed { get; init; }
    public string ConfirmationToken { get; init; } = string.Empty;
    public DateTime ConfirmationSentAt { get; init; }

    public SubscribeToNewsletterResponse(
        Guid id,
        string email,
        Guid? metroAreaId,
        bool receiveAllLocations,
        bool isActive,
        bool isConfirmed,
        string confirmationToken,
        DateTime confirmationSentAt)
    {
        Id = id;
        Email = email;
        MetroAreaId = metroAreaId;
        ReceiveAllLocations = receiveAllLocations;
        IsActive = isActive;
        IsConfirmed = isConfirmed;
        ConfirmationToken = confirmationToken;
        ConfirmationSentAt = confirmationSentAt;
    }
}

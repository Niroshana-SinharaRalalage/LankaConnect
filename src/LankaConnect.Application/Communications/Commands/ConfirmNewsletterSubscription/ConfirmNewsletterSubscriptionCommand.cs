using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.ConfirmNewsletterSubscription;

/// <summary>
/// Command to confirm newsletter subscription with token
/// </summary>
/// <param name="ConfirmationToken">The confirmation token from email</param>
public record ConfirmNewsletterSubscriptionCommand(
    string ConfirmationToken) : ICommand<ConfirmNewsletterSubscriptionResponse>;

/// <summary>
/// Response for newsletter subscription confirmation
/// </summary>
public class ConfirmNewsletterSubscriptionResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid? MetroAreaId { get; init; }
    public bool ReceiveAllLocations { get; init; }
    public DateTime ConfirmedAt { get; init; }

    public ConfirmNewsletterSubscriptionResponse(
        Guid id,
        string email,
        Guid? metroAreaId,
        bool receiveAllLocations,
        DateTime confirmedAt)
    {
        Id = id;
        Email = email;
        MetroAreaId = metroAreaId;
        ReceiveAllLocations = receiveAllLocations;
        ConfirmedAt = confirmedAt;
    }
}

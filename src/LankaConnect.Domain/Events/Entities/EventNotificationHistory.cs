using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Tracks manual email notifications sent by event organizers
/// Phase 6A.61: Added for Communication tab history display
/// </summary>
public class EventNotificationHistory : BaseEntity
{
    /// <summary>
    /// The event that the notification was sent for
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// User who sent the notification (event organizer)
    /// </summary>
    public Guid SentByUserId { get; private set; }

    /// <summary>
    /// When the notification was sent
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Total number of recipients (registrations + email groups + newsletter subscribers)
    /// </summary>
    public int RecipientCount { get; private set; }

    /// <summary>
    /// Number of successfully sent emails
    /// Updated by background job after processing
    /// </summary>
    public int SuccessfulSends { get; private set; }

    /// <summary>
    /// Number of failed email sends
    /// Updated by background job after processing
    /// </summary>
    public int FailedSends { get; private set; }

    // EF Core constructor
    private EventNotificationHistory()
    {
    }

    private EventNotificationHistory(Guid eventId, Guid sentByUserId, int recipientCount)
    {
        EventId = eventId;
        SentByUserId = sentByUserId;
        SentAt = DateTime.UtcNow;
        RecipientCount = recipientCount;
        SuccessfulSends = 0;
        FailedSends = 0;
    }

    /// <summary>
    /// Factory method to create a new notification history record
    /// Phase 6A.61: Called from command handler when email send is initiated
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="sentByUserId">User ID of the organizer sending the notification</param>
    /// <param name="recipientCount">Initial recipient count (placeholder, updated by background job)</param>
    public static Result<EventNotificationHistory> Create(
        Guid eventId,
        Guid sentByUserId,
        int recipientCount)
    {
        if (eventId == Guid.Empty)
            return Result<EventNotificationHistory>.Failure("Event ID is required");

        if (sentByUserId == Guid.Empty)
            return Result<EventNotificationHistory>.Failure("Sent by user ID is required");

        if (recipientCount < 0)
            return Result<EventNotificationHistory>.Failure("Recipient count cannot be negative");

        var history = new EventNotificationHistory(eventId, sentByUserId, recipientCount);
        return Result<EventNotificationHistory>.Success(history);
    }

    /// <summary>
    /// Updates the send statistics after background job processes emails
    /// Phase 6A.61: Called from EventNotificationEmailJob
    /// </summary>
    /// <param name="totalRecipients">Final recipient count</param>
    /// <param name="successful">Number of successfully sent emails</param>
    /// <param name="failed">Number of failed email sends</param>
    public void UpdateSendStatistics(int totalRecipients, int successful, int failed)
    {
        if (totalRecipients < 0)
            throw new ArgumentException("Total recipients cannot be negative", nameof(totalRecipients));

        if (successful < 0)
            throw new ArgumentException("Successful sends cannot be negative", nameof(successful));

        if (failed < 0)
            throw new ArgumentException("Failed sends cannot be negative", nameof(failed));

        RecipientCount = totalRecipients;
        SuccessfulSends = successful;
        FailedSends = failed;
        MarkAsUpdated();
    }
}

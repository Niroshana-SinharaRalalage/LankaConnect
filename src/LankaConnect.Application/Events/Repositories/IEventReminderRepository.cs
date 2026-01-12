namespace LankaConnect.Application.Events.Repositories;

/// <summary>
/// Phase 6A.71: Repository for tracking sent event reminders (idempotency)
/// </summary>
public interface IEventReminderRepository
{
    /// <summary>
    /// Check if a reminder has already been sent for this event/registration/type combination.
    /// </summary>
    Task<bool> IsReminderAlreadySentAsync(
        Guid eventId,
        Guid registrationId,
        string reminderType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record that a reminder has been sent.
    /// </summary>
    Task RecordReminderSentAsync(
        Guid eventId,
        Guid registrationId,
        string reminderType,
        string recipientEmail,
        CancellationToken cancellationToken = default);
}

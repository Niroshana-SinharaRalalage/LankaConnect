using LankaConnect.Products.LankaEvents.Contracts.DTOs;

namespace LankaConnect.Products.LankaEvents.Contracts.Repositories; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

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

    /// <summary>
    /// Phase 6A.76: Get reminder history for an event, aggregated by type and date.
    /// Returns summary records showing when reminders were sent and to how many recipients.
    /// </summary>
    Task<List<EventReminderHistoryDto>> GetReminderHistoryAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}

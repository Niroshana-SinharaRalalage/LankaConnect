using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Phase 6A.74 Part 13+: Tracks newsletter email send history with detailed recipient breakdown
/// Purpose: Display "X recipients (breakdown) ✓ Y sent ✗ Z failed" in newsletter UI
/// </summary>
public class NewsletterEmailHistory : BaseEntity
{
    /// <summary>
    /// Newsletter that was sent
    /// </summary>
    public Guid NewsletterId { get; private set; }

    /// <summary>
    /// When the email was sent
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Total unique recipients who received the email (after deduplication)
    /// </summary>
    public int TotalRecipientCount { get; private set; }

    /// <summary>
    /// Number of recipients from newsletter's own email groups
    /// </summary>
    public int NewsletterEmailGroupCount { get; private set; }

    /// <summary>
    /// Number of recipients from event's email groups (if linked to event)
    /// </summary>
    public int EventEmailGroupCount { get; private set; }

    /// <summary>
    /// Number of newsletter subscribers
    /// </summary>
    public int SubscriberCount { get; private set; }

    /// <summary>
    /// Number of event registered attendees (if linked to event)
    /// </summary>
    public int EventRegistrationCount { get; private set; }

    /// <summary>
    /// Number of emails successfully sent
    /// </summary>
    public int SuccessfulSends { get; private set; }

    /// <summary>
    /// Number of emails that failed to send
    /// </summary>
    public int FailedSends { get; private set; }

    /// <summary>
    /// Legacy: Combined email group count (newsletter + event) for backwards compatibility
    /// </summary>
    public int EmailGroupRecipientCount { get; private set; }

    /// <summary>
    /// Legacy: Subscriber count alias for backwards compatibility
    /// </summary>
    public int SubscriberRecipientCount { get; private set; }

    /// <summary>
    /// Navigation property to Newsletter
    /// </summary>
    public Newsletter Newsletter { get; private set; } = null!;

    // Private constructor for EF Core
    private NewsletterEmailHistory() { }

    /// <summary>
    /// Creates a new newsletter email history record with full breakdown
    /// </summary>
    public static NewsletterEmailHistory Create(
        Guid newsletterId,
        DateTime sentAt,
        int totalRecipientCount,
        int newsletterEmailGroupCount,
        int eventEmailGroupCount,
        int subscriberCount,
        int eventRegistrationCount,
        int successfulSends,
        int failedSends)
    {
        if (newsletterId == Guid.Empty)
            throw new ArgumentException("Newsletter ID cannot be empty", nameof(newsletterId));

        if (totalRecipientCount < 0)
            throw new ArgumentException("Total recipient count cannot be negative", nameof(totalRecipientCount));

        return new NewsletterEmailHistory
        {
            Id = Guid.NewGuid(),
            NewsletterId = newsletterId,
            SentAt = sentAt,
            TotalRecipientCount = totalRecipientCount,
            NewsletterEmailGroupCount = newsletterEmailGroupCount,
            EventEmailGroupCount = eventEmailGroupCount,
            SubscriberCount = subscriberCount,
            EventRegistrationCount = eventRegistrationCount,
            SuccessfulSends = successfulSends,
            FailedSends = failedSends,
            // Legacy fields for backwards compatibility
            EmailGroupRecipientCount = newsletterEmailGroupCount + eventEmailGroupCount,
            SubscriberRecipientCount = subscriberCount
        };
    }

    /// <summary>
    /// Legacy factory method for backwards compatibility
    /// </summary>
    public static NewsletterEmailHistory CreateLegacy(
        Guid newsletterId,
        DateTime sentAt,
        int totalRecipientCount,
        int emailGroupRecipientCount,
        int subscriberRecipientCount)
    {
        return Create(
            newsletterId,
            sentAt,
            totalRecipientCount,
            newsletterEmailGroupCount: emailGroupRecipientCount,
            eventEmailGroupCount: 0,
            subscriberCount: subscriberRecipientCount,
            eventRegistrationCount: 0,
            successfulSends: totalRecipientCount,
            failedSends: 0);
    }
}

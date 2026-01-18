using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Phase 6A.74 Part 13 Issue #1: Tracks newsletter email send history and recipient counts
/// Purpose: Display "Sent to X recipients" in newsletter UI
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
    /// Total unique recipients who received the email
    /// </summary>
    public int TotalRecipientCount { get; private set; }

    /// <summary>
    /// Number of recipients from email groups
    /// </summary>
    public int EmailGroupRecipientCount { get; private set; }

    /// <summary>
    /// Number of recipients from newsletter subscribers
    /// </summary>
    public int SubscriberRecipientCount { get; private set; }

    /// <summary>
    /// Navigation property to Newsletter
    /// </summary>
    public Newsletter Newsletter { get; private set; } = null!;

    // Private constructor for EF Core
    private NewsletterEmailHistory() { }

    /// <summary>
    /// Creates a new newsletter email history record
    /// </summary>
    public static NewsletterEmailHistory Create(
        Guid newsletterId,
        DateTime sentAt,
        int totalRecipientCount,
        int emailGroupRecipientCount,
        int subscriberRecipientCount)
    {
        if (newsletterId == Guid.Empty)
            throw new ArgumentException("Newsletter ID cannot be empty", nameof(newsletterId));

        if (totalRecipientCount < 0)
            throw new ArgumentException("Total recipient count cannot be negative", nameof(totalRecipientCount));

        if (emailGroupRecipientCount < 0)
            throw new ArgumentException("Email group recipient count cannot be negative", nameof(emailGroupRecipientCount));

        if (subscriberRecipientCount < 0)
            throw new ArgumentException("Subscriber recipient count cannot be negative", nameof(subscriberRecipientCount));

        return new NewsletterEmailHistory
        {
            Id = Guid.NewGuid(),
            NewsletterId = newsletterId,
            SentAt = sentAt,
            TotalRecipientCount = totalRecipientCount,
            EmailGroupRecipientCount = emailGroupRecipientCount,
            SubscriberRecipientCount = subscriberRecipientCount
        };
    }
}

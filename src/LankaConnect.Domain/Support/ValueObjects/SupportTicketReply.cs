using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Support.ValueObjects;

/// <summary>
/// Phase 6A.89: Represents an admin reply to a support ticket.
/// This is an owned entity within the SupportTicket aggregate root.
/// </summary>
public class SupportTicketReply : ValueObject
{
    public Guid Id { get; private set; }
    public string Content { get; private set; }
    public Guid RepliedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private SupportTicketReply()
    {
        Content = string.Empty;
    }

    public SupportTicketReply(string content, Guid repliedByUserId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Reply content cannot be empty", nameof(content));

        if (repliedByUserId == Guid.Empty)
            throw new ArgumentException("Replied by user ID is required", nameof(repliedByUserId));

        Id = Guid.NewGuid();
        Content = content.Trim();
        RepliedByUserId = repliedByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Content;
        yield return RepliedByUserId;
        yield return CreatedAt;
    }
}

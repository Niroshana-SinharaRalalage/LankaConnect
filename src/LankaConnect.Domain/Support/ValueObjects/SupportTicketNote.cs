using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Support.ValueObjects;

/// <summary>
/// Phase 6A.89: Represents an internal admin note on a support ticket.
/// This is visible only to admin users, not to the ticket submitter.
/// This is an owned entity within the SupportTicket aggregate root.
/// </summary>
public class SupportTicketNote : ValueObject
{
    public Guid Id { get; private set; }
    public string Content { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private SupportTicketNote()
    {
        Content = string.Empty;
    }

    public SupportTicketNote(string content, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Note content cannot be empty", nameof(content));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Created by user ID is required", nameof(createdByUserId));

        Id = Guid.NewGuid();
        Content = content.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Content;
        yield return CreatedByUserId;
        yield return CreatedAt;
    }
}

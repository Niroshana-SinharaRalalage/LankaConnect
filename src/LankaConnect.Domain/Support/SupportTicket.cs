using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Support.Enums;
using LankaConnect.Domain.Support.ValueObjects;

namespace LankaConnect.Domain.Support;

/// <summary>
/// Phase 6A.89: Support ticket aggregate root for handling contact form submissions.
/// Follows DDD patterns with factory method, domain methods, and domain events.
/// </summary>
public class SupportTicket : BaseEntity
{
    /// <summary>
    /// Human-readable reference ID in format: CONTACT-YYYYMMDD-XXXXXXXX
    /// </summary>
    public string ReferenceId { get; private set; }

    /// <summary>
    /// Name of the person who submitted the ticket
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Email of the person who submitted the ticket
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// Subject/title of the support request
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Original message content
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Current status of the ticket
    /// </summary>
    public SupportTicketStatus Status { get; private set; }

    /// <summary>
    /// Priority level of the ticket
    /// </summary>
    public SupportTicketPriority Priority { get; private set; }

    /// <summary>
    /// Admin user assigned to handle this ticket (nullable)
    /// </summary>
    public Guid? AssignedToUserId { get; private set; }

    // Collections (Aggregate Root pattern with private backing fields)
    private readonly List<SupportTicketReply> _replies = new();
    public IReadOnlyCollection<SupportTicketReply> Replies => _replies.AsReadOnly();

    private readonly List<SupportTicketNote> _notes = new();
    public IReadOnlyCollection<SupportTicketNote> Notes => _notes.AsReadOnly();

    // EF Core constructor
    private SupportTicket()
    {
        ReferenceId = string.Empty;
        Name = string.Empty;
        Email = null!;
        Subject = string.Empty;
        Message = string.Empty;
    }

    /// <summary>
    /// Factory method to create a new support ticket (DDD pattern).
    /// Validates all input and raises SupportTicketCreatedEvent for auto-confirmation email.
    /// </summary>
    public static Result<SupportTicket> Create(
        string name,
        Email email,
        string subject,
        string message)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<SupportTicket>.Failure("Name is required");

        if (email == null)
            return Result<SupportTicket>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(subject))
            return Result<SupportTicket>.Failure("Subject is required");

        if (string.IsNullOrWhiteSpace(message))
            return Result<SupportTicket>.Failure("Message is required");

        if (name.Trim().Length > 100)
            return Result<SupportTicket>.Failure("Name cannot exceed 100 characters");

        if (subject.Trim().Length > 200)
            return Result<SupportTicket>.Failure("Subject cannot exceed 200 characters");

        if (message.Trim().Length > 10000)
            return Result<SupportTicket>.Failure("Message cannot exceed 10000 characters");

        var ticket = new SupportTicket
        {
            ReferenceId = GenerateReferenceId(),
            Name = name.Trim(),
            Email = email,
            Subject = subject.Trim(),
            Message = message.Trim(),
            Status = SupportTicketStatus.New,
            Priority = SupportTicketPriority.Normal
        };

        // Raise domain event for auto-confirmation email
        ticket.RaiseDomainEvent(new SupportTicketCreatedEvent(
            ticket.Id,
            ticket.ReferenceId,
            ticket.Email.Value,
            ticket.Name,
            ticket.Subject));

        return Result<SupportTicket>.Success(ticket);
    }

    /// <summary>
    /// Adds an admin reply to the ticket.
    /// Automatically updates status to InProgress if ticket was New.
    /// Raises SupportTicketRepliedEvent for notification email.
    /// </summary>
    public Result AddReply(string content, Guid adminUserId)
    {
        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("Cannot reply to closed ticket");

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure("Reply content is required");

        if (content.Trim().Length > 10000)
            return Result.Failure("Reply content cannot exceed 10000 characters");

        if (adminUserId == Guid.Empty)
            return Result.Failure("Admin user ID is required");

        var reply = new SupportTicketReply(content.Trim(), adminUserId);
        _replies.Add(reply);

        // Automatically move from New to InProgress when admin replies
        if (Status == SupportTicketStatus.New)
            Status = SupportTicketStatus.InProgress;

        MarkAsUpdated();

        // Raise domain event for reply notification email
        RaiseDomainEvent(new SupportTicketRepliedEvent(
            Id,
            ReferenceId,
            Email.Value,
            Name,
            Subject,
            content.Trim(),
            adminUserId));

        return Result.Success();
    }

    /// <summary>
    /// Adds an internal note to the ticket (visible only to admins).
    /// </summary>
    public Result AddNote(string content, Guid adminUserId)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure("Note content is required");

        if (content.Trim().Length > 5000)
            return Result.Failure("Note content cannot exceed 5000 characters");

        if (adminUserId == Guid.Empty)
            return Result.Failure("Admin user ID is required");

        var note = new SupportTicketNote(content.Trim(), adminUserId);
        _notes.Add(note);

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Updates the ticket status.
    /// Raises SupportTicketStatusChangedEvent for audit logging.
    /// </summary>
    public Result UpdateStatus(SupportTicketStatus newStatus)
    {
        if (Status == newStatus)
            return Result.Failure("Status is already set to this value");

        // Business rule: Cannot reopen a closed ticket
        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("Cannot change status of a closed ticket");

        var oldStatus = Status;
        Status = newStatus;

        MarkAsUpdated();

        // Raise domain event for audit logging
        RaiseDomainEvent(new SupportTicketStatusChangedEvent(
            Id,
            ReferenceId,
            oldStatus,
            newStatus));

        return Result.Success();
    }

    /// <summary>
    /// Updates the ticket priority.
    /// </summary>
    public Result UpdatePriority(SupportTicketPriority newPriority)
    {
        if (Priority == newPriority)
            return Result.Failure("Priority is already set to this value");

        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("Cannot change priority of a closed ticket");

        Priority = newPriority;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Assigns the ticket to an admin user.
    /// Raises SupportTicketAssignedEvent for admin notification.
    /// </summary>
    public Result AssignTo(Guid adminUserId)
    {
        if (adminUserId == Guid.Empty)
            return Result.Failure("Admin user ID is required");

        if (AssignedToUserId == adminUserId)
            return Result.Failure("Ticket is already assigned to this user");

        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("Cannot assign a closed ticket");

        var previousAssignee = AssignedToUserId;
        AssignedToUserId = adminUserId;

        MarkAsUpdated();

        // Raise domain event for admin notification
        RaiseDomainEvent(new SupportTicketAssignedEvent(
            Id,
            ReferenceId,
            adminUserId,
            previousAssignee));

        return Result.Success();
    }

    /// <summary>
    /// Unassigns the ticket (removes admin assignment).
    /// </summary>
    public Result Unassign()
    {
        if (!AssignedToUserId.HasValue)
            return Result.Failure("Ticket is not assigned to anyone");

        if (Status == SupportTicketStatus.Closed)
            return Result.Failure("Cannot unassign a closed ticket");

        AssignedToUserId = null;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Generates a human-readable reference ID in format: CONTACT-YYYYMMDD-XXXXXXXX
    /// </summary>
    private static string GenerateReferenceId()
    {
        return $"CONTACT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    }
}

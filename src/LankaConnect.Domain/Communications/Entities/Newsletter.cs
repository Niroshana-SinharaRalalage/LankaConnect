using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.DomainEvents;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Newsletter Aggregate Root
/// Encapsulates newsletter/news alert business rules and lifecycle management
/// Phase 6A.74: Newsletter/News Alert Feature
///
/// Lifecycle: Draft → Active → Inactive/Sent
/// - Draft: Initial state, editable
/// - Active: Published and visible, auto-deactivates after 7 days
/// - Inactive: Deactivated (auto or manual), can be reactivated
/// - Sent: Final state after emails sent, cannot be reactivated
/// </summary>
public class Newsletter : BaseEntity, IAggregateRoot
{
    public NewsletterTitle Title { get; private set; } = null!;
    public NewsletterDescription Description { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public Guid? EventId { get; private set; }
    public NewsletterStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IncludeNewsletterSubscribers { get; private set; }

    // Collection of email group IDs (junction table pattern)
    private readonly List<Guid> _emailGroupIds = new();
    public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();

    // IAggregateRoot implementation
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public void SetVersion(byte[] version)
    {
        Version = version;
    }

    // EF Core constructor
    private Newsletter() { }

    /// <summary>
    /// Validates the current state of the aggregate
    /// </summary>
    public ValidationResult ValidateState()
    {
        var errors = new List<string>();

        if (Title == null)
            errors.Add("Title is required");

        if (Description == null)
            errors.Add("Description is required");

        if (CreatedByUserId == Guid.Empty)
            errors.Add("Creator user ID is required");

        // Business rule: Must have at least one recipient source
        if (!_emailGroupIds.Any() && !IncludeNewsletterSubscribers)
            errors.Add("Must have at least one recipient source (email groups or newsletter subscribers)");

        // Status-specific validation
        if (Status == NewsletterStatus.Active && ExpiresAt == null)
            errors.Add("Active newsletter must have expiration date");

        if (Status == NewsletterStatus.Sent && SentAt == null)
            errors.Add("Sent newsletter must have sent date");

        return errors.Any()
            ? ValidationResult.Invalid(errors)
            : ValidationResult.Valid();
    }

    /// <summary>
    /// Checks if the aggregate is in a valid state
    /// </summary>
    public bool IsValid()
    {
        return ValidateState().IsValid;
    }

    /// <summary>
    /// Factory method to create a new newsletter
    /// Creates in Draft status
    /// </summary>
    public static Result<Newsletter> Create(
        NewsletterTitle title,
        NewsletterDescription description,
        Guid createdByUserId,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId = null)
    {
        if (title == null)
            return Result<Newsletter>.Failure("Title is required");

        if (description == null)
            return Result<Newsletter>.Failure("Description is required");

        if (createdByUserId == Guid.Empty)
            return Result<Newsletter>.Failure("Creator user ID is required");

        var emailGroupIdsList = emailGroupIds?.ToList() ?? new List<Guid>();

        // Business rule: Must have at least one recipient source
        if (!emailGroupIdsList.Any() && !includeNewsletterSubscribers)
        {
            return Result<Newsletter>.Failure(
                "Must have at least one recipient source (email groups or newsletter subscribers)");
        }

        var newsletter = new Newsletter
        {
            Title = title,
            Description = description,
            CreatedByUserId = createdByUserId,
            EventId = eventId,
            Status = NewsletterStatus.Draft,
            IncludeNewsletterSubscribers = includeNewsletterSubscribers
        };

        newsletter._emailGroupIds.AddRange(emailGroupIdsList);

        // Raise domain event
        newsletter.RaiseDomainEvent(new NewsletterCreatedEvent(
            newsletter.Id,
            createdByUserId,
            title.Value,
            eventId
        ));

        return Result<Newsletter>.Success(newsletter);
    }

    /// <summary>
    /// Updates newsletter content
    /// Can only be performed when status is Draft
    /// </summary>
    public Result Update(
        NewsletterTitle title,
        NewsletterDescription description,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId = null)
    {
        if (Status != NewsletterStatus.Draft)
            return Result.Failure("Only draft newsletters can be updated");

        if (title == null)
            return Result.Failure("Title is required");

        if (description == null)
            return Result.Failure("Description is required");

        var emailGroupIdsList = emailGroupIds?.ToList() ?? new List<Guid>();

        // Business rule: Must have at least one recipient source
        if (!emailGroupIdsList.Any() && !includeNewsletterSubscribers)
        {
            return Result.Failure(
                "Must have at least one recipient source (email groups or newsletter subscribers)");
        }

        Title = title;
        Description = description;
        EventId = eventId;
        IncludeNewsletterSubscribers = includeNewsletterSubscribers;

        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIdsList);

        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Publishes the newsletter (Draft → Active)
    /// Sets expiration date to 7 days from now
    /// </summary>
    public Result Publish()
    {
        if (Status != NewsletterStatus.Draft)
            return Result.Failure($"Cannot publish newsletter with status {Status}. Only drafts can be published.");

        var now = DateTime.UtcNow;

        Status = NewsletterStatus.Active;
        PublishedAt = now;
        ExpiresAt = now.AddDays(7);

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new NewsletterPublishedEvent(
            Id,
            PublishedAt.Value,
            ExpiresAt.Value
        ));

        return Result.Success();
    }

    /// <summary>
    /// Marks newsletter as sent
    /// Transitions to final Sent state
    /// Should be called by SendNewsletterEmailJob after successful email delivery
    /// </summary>
    public Result MarkAsSent()
    {
        if (!CanSendEmail())
            return Result.Failure("Newsletter cannot be marked as sent. Must be Active and not already sent.");

        Status = NewsletterStatus.Sent;
        SentAt = DateTime.UtcNow;

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new NewsletterSentEvent(
            Id,
            SentAt.Value,
            0 // Recipient count will be set by the job
        ));

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the newsletter (Active → Inactive)
    /// Called by auto-deactivation job after 7 days or manually by user
    /// </summary>
    public Result Deactivate()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure($"Cannot deactivate newsletter with status {Status}. Only active newsletters can be deactivated.");

        Status = NewsletterStatus.Inactive;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Reactivates an inactive newsletter (Inactive → Active)
    /// Extends expiration date by 7 days from now
    /// Cannot reactivate if already sent
    /// </summary>
    public Result Reactivate()
    {
        if (Status != NewsletterStatus.Inactive)
            return Result.Failure($"Cannot reactivate newsletter with status {Status}. Only inactive newsletters can be reactivated.");

        if (SentAt.HasValue)
            return Result.Failure("Cannot reactivate newsletter that has already been sent");

        var now = DateTime.UtcNow;

        Status = NewsletterStatus.Active;
        ExpiresAt = now.AddDays(7);

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new NewsletterReactivatedEvent(
            Id,
            now,
            ExpiresAt.Value
        ));

        return Result.Success();
    }

    /// <summary>
    /// Checks if email can be sent for this newsletter
    /// Email can only be sent if status is Active AND not already sent
    /// </summary>
    public bool CanSendEmail()
    {
        return Status == NewsletterStatus.Active && !SentAt.HasValue;
    }

    /// <summary>
    /// Checks if newsletter can be edited
    /// Only drafts can be edited
    /// </summary>
    public bool CanEdit()
    {
        return Status == NewsletterStatus.Draft;
    }

    /// <summary>
    /// Checks if newsletter can be deleted
    /// Only drafts can be deleted
    /// </summary>
    public bool CanDelete()
    {
        return Status == NewsletterStatus.Draft;
    }

    /// <summary>
    /// Checks if newsletter has expired
    /// Used by auto-deactivation job
    /// </summary>
    public bool HasExpired()
    {
        return Status == NewsletterStatus.Active
            && ExpiresAt.HasValue
            && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Synchronizes email group IDs from EF Core collection
    /// Used by repository after loading from database (shadow navigation pattern)
    /// Phase 6A.74: Follows ADR-009 for list-based value objects
    /// </summary>
    public void SyncEmailGroupIdsFromEntities(IEnumerable<Guid> emailGroupIds)
    {
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIds);
    }
}

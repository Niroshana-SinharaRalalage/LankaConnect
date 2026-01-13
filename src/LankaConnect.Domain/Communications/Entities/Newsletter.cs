using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Events;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Newsletter aggregate root
/// Phase 6A.74: Newsletter/News Alert system with location targeting
/// </summary>
public class Newsletter : BaseEntity
{
    private readonly List<Guid> _emailGroupIds = new();
    private readonly List<Guid> _metroAreaIds = new();
    private List<EmailGroup> _emailGroupEntities = new(); // Shadow navigation for EF Core
    private List<MetroArea> _metroAreaEntities = new(); // Shadow navigation for EF Core

    public NewsletterTitle Title { get; private set; }
    public NewsletterDescription Description { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? EventId { get; private set; }
    public NewsletterStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IncludeNewsletterSubscribers { get; private set; }

    // Phase 6A.74 Enhancement 1: Location Targeting for Non-Event Newsletters
    public bool TargetAllLocations { get; private set; }

    public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();
    public IReadOnlyList<Guid> MetroAreaIds => _metroAreaIds.AsReadOnly();

    // EF Core constructor
    private Newsletter()
    {
        Title = null!;
        Description = null!;
    }

    private Newsletter(
        NewsletterTitle title,
        NewsletterDescription description,
        Guid createdByUserId,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId,
        IEnumerable<Guid>? metroAreaIds,
        bool targetAllLocations)
    {
        Title = title;
        Description = description;
        CreatedByUserId = createdByUserId;
        IncludeNewsletterSubscribers = includeNewsletterSubscribers;
        EventId = eventId;
        TargetAllLocations = targetAllLocations;
        Status = NewsletterStatus.Draft;

        _emailGroupIds.AddRange(emailGroupIds);
        if (metroAreaIds != null)
        {
            _metroAreaIds.AddRange(metroAreaIds);
        }
    }

    public static Result<Newsletter> Create(
        NewsletterTitle title,
        NewsletterDescription description,
        Guid createdByUserId,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId = null,
        IEnumerable<Guid>? metroAreaIds = null,
        bool targetAllLocations = false)
    {
        if (title == null)
            return Result<Newsletter>.Failure("Title is required");

        if (description == null)
            return Result<Newsletter>.Failure("Description is required");

        if (createdByUserId == Guid.Empty)
            return Result<Newsletter>.Failure("Creator user ID is required");

        var errors = new List<string>();

        // Business Rule 1: Must have at least one recipient source
        if (!emailGroupIds.Any() && !includeNewsletterSubscribers)
        {
            errors.Add("Newsletter must have at least one recipient source (email groups or newsletter subscribers)");
        }

        // Business Rule 2: Non-event newsletters with subscribers must specify location
        if (!eventId.HasValue && includeNewsletterSubscribers)
        {
            if (!targetAllLocations && (metroAreaIds == null || !metroAreaIds.Any()))
            {
                errors.Add("Non-event newsletters with newsletter subscribers must specify TargetAllLocations or at least one MetroArea");
            }
        }

        if (errors.Any())
        {
            return Result<Newsletter>.Failure(string.Join("; ", errors));
        }

        return Result<Newsletter>.Success(new Newsletter(
            title,
            description,
            createdByUserId,
            emailGroupIds,
            includeNewsletterSubscribers,
            eventId,
            metroAreaIds,
            targetAllLocations));
    }

    public Result Publish()
    {
        if (Status != NewsletterStatus.Draft)
            return Result.Failure("Only draft newsletters can be published");

        Status = NewsletterStatus.Active;
        PublishedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(7);

        return Result.Success();
    }

    public Result MarkAsSent()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can be marked as sent");

        if (SentAt.HasValue)
            return Result.Failure("Newsletter has already been sent");

        Status = NewsletterStatus.Sent;
        SentAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can be deactivated");

        Status = NewsletterStatus.Inactive;

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != NewsletterStatus.Inactive)
            return Result.Failure("Only inactive newsletters can be reactivated");

        if (SentAt.HasValue)
            return Result.Failure("Sent newsletters cannot be reactivated");

        Status = NewsletterStatus.Active;
        ExpiresAt = DateTime.UtcNow.AddDays(7);

        return Result.Success();
    }

    /// <summary>
    /// Unpublish newsletter (Active → Draft)
    /// Phase 6A.74 Part 9A: Unpublish functionality
    /// </summary>
    public Result Unpublish()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can be unpublished");

        if (SentAt.HasValue)
            return Result.Failure("Sent newsletters cannot be unpublished");

        Status = NewsletterStatus.Draft;
        PublishedAt = null;
        ExpiresAt = null;

        return Result.Success();
    }

    public Result Update(
        NewsletterTitle title,
        NewsletterDescription description,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId,
        IEnumerable<Guid>? metroAreaIds,
        bool targetAllLocations)
    {
        if (Status != NewsletterStatus.Draft)
            return Result.Failure("Only draft newsletters can be updated");

        var errors = new List<string>();

        // Business Rule 1: Must have at least one recipient source
        if (!emailGroupIds.Any() && !includeNewsletterSubscribers)
        {
            errors.Add("Newsletter must have at least one recipient source");
        }

        // Business Rule 2: Non-event newsletters with subscribers must specify location
        if (!eventId.HasValue && includeNewsletterSubscribers)
        {
            if (!targetAllLocations && (metroAreaIds == null || !metroAreaIds.Any()))
            {
                errors.Add("Non-event newsletters with subscribers must specify location targeting");
            }
        }

        if (errors.Any())
        {
            return Result.Failure(string.Join("; ", errors));
        }

        Title = title;
        Description = description;
        IncludeNewsletterSubscribers = includeNewsletterSubscribers;
        EventId = eventId;
        TargetAllLocations = targetAllLocations;

        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIds);

        _metroAreaIds.Clear();
        if (metroAreaIds != null)
        {
            _metroAreaIds.AddRange(metroAreaIds);
        }

        return Result.Success();
    }

    public bool CanSendEmail() => Status == NewsletterStatus.Active && !SentAt.HasValue;

    public bool CanDelete() => Status == NewsletterStatus.Draft;

    // Sync method for repository pattern - called by infrastructure layer
    public void SyncEmailGroupIdsFromEntities(List<Guid> emailGroupIds)
    {
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIds);
    }

    // Phase 6A.74 Enhancement 1: Sync method for metro area IDs
    public void SyncMetroAreaIdsFromEntities(List<Guid> metroAreaIds)
    {
        _metroAreaIds.Clear();
        _metroAreaIds.AddRange(metroAreaIds);
    }
}

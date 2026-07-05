using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Modules.Communications.Domain.ValueObjects;
// W5.2.d-hotfix2 (2026-06-28): The W5.1 List<object> retyping of _metroAreaEntities
// caused InvalidCastException on every newsletter create because EF Core 8's
// shadow-nav hydrator assigns a List<MetroArea> via reflection and List<T> is
// invariant. Replaced with the NewsletterMetroAreaLink junction CLR pattern
// (mirrors NewsletterEmailGroupLink from W5.4.d.1b) — no Products dep, no
// invariant-generic trap. Same physical communications.newsletter_metro_areas table.
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Newsletter aggregate root
/// Phase 6A.74: Newsletter/News Alert system with location targeting
/// </summary>
public class Newsletter : LegacyBaseEntity
{
    private readonly List<Guid> _emailGroupIds = new();
    private readonly List<Guid> _metroAreaIds = new();
    // Wave 5.4.d.1b (2026-06-22) — replaced Phase 6A.74 `_emailGroupEntities:
    // List<EmailGroup>` typed nav with this explicit junction CLR collection.
    // Same rationale as the W5.4.c.0 Event surgery: EF Core 8 cannot model a
    // typed M2M nav once EmailGroup moves to Communications.Domain. _metroAreaEntities
    // stays as-is because MetroArea is NOT moving in Wave 5.4 — it lives in
    // LankaConnect.Products.LankaEvents.Domain (moved Wave 5.1) and is reachable via the W5.0 transitional ProjectReference from Products.Domain to LankaConnect.Domain.
    private readonly List<NewsletterEmailGroupLink> _emailGroupLinks = new();
    // W5.2.d-hotfix2: NewsletterMetroAreaLink junction CLR replaces the broken
    // List<object> shadow nav from W5.1.
    private readonly List<NewsletterMetroAreaLink> _metroAreaLinks = new();

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

    /// <summary>
    /// Phase 6A.74 Part 14: Announcement-only newsletters skip Draft state and are NOT published to public page.
    /// When true: auto-activates on creation, cannot be published to /newsletters page
    /// When false: normal published newsletter behavior (Draft → Active → visible on public page)
    /// </summary>
    public bool IsAnnouncementOnly { get; private set; }

    public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();
    public IReadOnlyList<Guid> MetroAreaIds => _metroAreaIds.AsReadOnly();
    // Wave 5.4.d.1b (2026-06-22): EmailGroupLinks exposes the junction collection
    // for EF Core HasMany expression-tree binding (mirrors EventEmailGroupLinks).
    public IReadOnlyList<NewsletterEmailGroupLink> EmailGroupLinks => _emailGroupLinks.AsReadOnly();
    public IReadOnlyList<NewsletterMetroAreaLink> MetroAreaLinks => _metroAreaLinks.AsReadOnly();

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
        bool targetAllLocations,
        bool isAnnouncementOnly)
    {
        Title = title;
        Description = description;
        CreatedByUserId = createdByUserId;
        IncludeNewsletterSubscribers = includeNewsletterSubscribers;
        EventId = eventId;
        TargetAllLocations = targetAllLocations;
        IsAnnouncementOnly = isAnnouncementOnly;

        // Phase 6A.74 Part 14: Announcement-only newsletters auto-activate
        if (isAnnouncementOnly)
        {
            Status = NewsletterStatus.Active;
            ExpiresAt = DateTime.UtcNow.AddDays(7);
            // PublishedAt stays null - indicates not published to public page
        }
        else
        {
            Status = NewsletterStatus.Draft;
        }

        // Wave 5.4.d.1b (2026-06-22): post-ctor Id is already populated by
        // LegacyBaseEntity ctor before this body runs, so we can mint links here.
        _emailGroupIds.AddRange(emailGroupIds);
        foreach (var gid in emailGroupIds)
        {
            _emailGroupLinks.Add(NewsletterEmailGroupLink.Create(Id, gid));
        }
        if (metroAreaIds != null)
        {
            _metroAreaIds.AddRange(metroAreaIds);
            foreach (var mid in metroAreaIds)
            {
                _metroAreaLinks.Add(NewsletterMetroAreaLink.Create(Id, mid));
            }
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
        bool targetAllLocations = false,
        bool isAnnouncementOnly = false)
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

        // Business Rule 2 REMOVED: Phase 6A.74 Part 13 Issue #6 CRITICAL FIX
        // Location targeting is OPTIONAL - users can create newsletters without selecting locations
        // They just need at least one recipient source (email groups OR subscribers) - validated above
        // No location validation needed

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
            targetAllLocations,
            isAnnouncementOnly));
    }

    public Result Publish()
    {
        // Phase 6A.74 Part 14: Announcement-only newsletters cannot be published to public page
        if (IsAnnouncementOnly)
            return Result.Failure("Announcement-only newsletters cannot be published to the public page. They are already active for sending emails.");

        if (Status != NewsletterStatus.Draft)
            return Result.Failure("Only draft newsletters can be published");

        Status = NewsletterStatus.Active;
        PublishedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(7);

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.74 Part 14: Record that an email was sent from this newsletter.
    /// Unlike the old MarkAsSent(), this does NOT change the status - newsletter remains Active
    /// and can send unlimited emails. SentAt is only set on the first send for tracking purposes.
    /// </summary>
    public Result RecordEmailSent()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can send emails");

        // Only set SentAt on the first send (for historical tracking)
        if (!SentAt.HasValue)
        {
            SentAt = DateTime.UtcNow;
        }

        // DO NOT change status - newsletter remains Active and can send more emails
        return Result.Success();
    }

    /// <summary>
    /// DEPRECATED: Use RecordEmailSent() instead.
    /// Kept for backwards compatibility with existing code that may call it.
    /// Phase 6A.74 Part 14: This now delegates to RecordEmailSent() and does NOT lock the newsletter.
    /// </summary>
    [Obsolete("Use RecordEmailSent() instead. This method no longer changes status to Sent.")]
    public Result MarkAsSent()
    {
        return RecordEmailSent();
    }

    public Result Deactivate()
    {
        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can be deactivated");

        Status = NewsletterStatus.Inactive;

        return Result.Success();
    }

    /// <summary>
    /// Reactivate an inactive newsletter for another 7-day period.
    /// Phase 6A.74 Part 14: Removed SentAt check - newsletters can be reactivated even after sending emails.
    /// </summary>
    public Result Reactivate()
    {
        if (Status != NewsletterStatus.Inactive)
            return Result.Failure("Only inactive newsletters can be reactivated");

        // Phase 6A.74 Part 14: REMOVED SentAt check - newsletters can be reactivated and send more emails
        // Old behavior locked newsletters after first send, new behavior allows unlimited sends

        Status = NewsletterStatus.Active;
        ExpiresAt = DateTime.UtcNow.AddDays(7);

        return Result.Success();
    }

    /// <summary>
    /// Unpublish newsletter (Active → Draft)
    /// Phase 6A.74 Part 9A: Unpublish functionality
    /// Phase 6A.74 Part 14: Removed SentAt check - newsletters can be unpublished even after sending emails.
    /// Announcement-only newsletters cannot be unpublished (they were never published).
    /// </summary>
    public Result Unpublish()
    {
        // Phase 6A.74 Part 14: Announcement-only newsletters cannot be unpublished
        if (IsAnnouncementOnly)
            return Result.Failure("Announcement-only newsletters cannot be unpublished. They were never published to the public page.");

        if (Status != NewsletterStatus.Active)
            return Result.Failure("Only active newsletters can be unpublished");

        // Phase 6A.74 Part 14: REMOVED SentAt check - newsletters can be unpublished even after sending emails
        // Old behavior blocked unpublish after first send, new behavior allows it

        Status = NewsletterStatus.Draft;
        PublishedAt = null;
        ExpiresAt = null;

        return Result.Success();
    }

    /// <summary>
    /// Update newsletter content and settings.
    /// Phase 6A.74 Part 14: Removed SentAt check - newsletters can be updated even after sending emails.
    /// </summary>
    public Result Update(
        NewsletterTitle title,
        NewsletterDescription description,
        IEnumerable<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId,
        IEnumerable<Guid>? metroAreaIds,
        bool targetAllLocations)
    {
        // Phase 6A.74 Part 14: REMOVED SentAt check - newsletters can be updated even after sending emails
        // Only block updates for Inactive status (must reactivate first)
        // Note: Status == Sent should not occur anymore with new behavior, but keep for backwards compatibility
        if (Status == NewsletterStatus.Sent)
            return Result.Failure("Newsletters in Sent status cannot be updated. Please reactivate first.");

        if (Status == NewsletterStatus.Inactive)
            return Result.Failure("Inactive newsletters cannot be updated. Please reactivate first.");

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

        // Wave 5.4.d.1b (2026-06-22): mirror _emailGroupIds onto _emailGroupLinks
        // so EF persists the junction directly (no infra shadow-nav sync needed).
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIds);
        _emailGroupLinks.Clear();
        foreach (var gid in emailGroupIds)
        {
            _emailGroupLinks.Add(NewsletterEmailGroupLink.Create(Id, gid));
        }

        _metroAreaIds.Clear();
        _metroAreaLinks.Clear();
        if (metroAreaIds != null)
        {
            _metroAreaIds.AddRange(metroAreaIds);
            foreach (var mid in metroAreaIds)
            {
                _metroAreaLinks.Add(NewsletterMetroAreaLink.Create(Id, mid));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Check if newsletter can send emails.
    /// Phase 6A.74 Part 14: Removed SentAt check - newsletters can send unlimited emails while Active.
    /// </summary>
    public bool CanSendEmail() => Status == NewsletterStatus.Active;

    /// <summary>
    /// Check if newsletter can be deleted.
    /// Only Draft newsletters can be deleted. Announcement-only newsletters are never in Draft status.
    /// </summary>
    public bool CanDelete() => Status == NewsletterStatus.Draft && !IsAnnouncementOnly;

    /// <summary>
    /// Wave 5.4.d.1b (2026-06-22): Newsletter's _emailGroupLinks now hydrates
    /// directly via EF Core's HasMany binding — the previous repository-side
    /// SyncEmailGroupIdsFromEntities back-door rebuilt _emailGroupIds from a
    /// typed-nav payload that no longer exists. The call site in
    /// NewsletterRepository.GetByIdAsync now invokes
    /// <see cref="SyncEmailGroupIdsFromLinks"/> after EF populates
    /// _emailGroupLinks.
    /// </summary>
    public void SyncEmailGroupIdsFromLinks()
    {
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(_emailGroupLinks.Select(l => l.EmailGroupId));
    }

    // Phase 6A.74 Enhancement 1: Sync method for metro area IDs
    public void SyncMetroAreaIdsFromEntities(List<Guid> metroAreaIds)
    {
        _metroAreaIds.Clear();
        _metroAreaIds.AddRange(metroAreaIds);
    }
}

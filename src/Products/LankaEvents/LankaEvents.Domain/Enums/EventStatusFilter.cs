namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Issue #36: User-friendly status filter groups for event listing pages.
/// Maps to multiple EventStatus values internally for simplified filtering.
///
/// Mappings:
/// - All: Everything (context-dependent: public excludes Draft/UnderReview)
/// - Active: Published + Active (upcoming/ongoing events)
/// - Inactive: Completed + Archived + Postponed (past/paused events)
/// - Cancelled: Cancelled only
/// - Unpublished: Draft + UnderReview (organizer-only visibility)
/// </summary>
public enum EventStatusFilter
{
    /// <summary>
    /// All events. For public users: excludes Draft/UnderReview.
    /// For organizers with IncludeAllStatuses=true: includes everything.
    /// </summary>
    All = 0,

    /// <summary>
    /// Currently happening or upcoming events.
    /// Maps to: EventStatus.Published, EventStatus.Active
    /// </summary>
    Active = 1,

    /// <summary>
    /// Past or paused events.
    /// Maps to: EventStatus.Completed, EventStatus.Archived, EventStatus.Postponed
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Cancelled events only.
    /// Maps to: EventStatus.Cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Not yet published events. Only visible to organizers with proper authorization.
    /// Maps to: EventStatus.Draft, EventStatus.UnderReview
    /// Requires IncludeAllStatuses=true to have effect.
    /// </summary>
    Unpublished = 4
}

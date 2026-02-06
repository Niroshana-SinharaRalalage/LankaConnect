namespace LankaConnect.Domain.ReferenceData.Entities;

/// <summary>
/// User Role reference data
/// Phase 6A.47: Replaces UserRole enum with database-driven reference data
/// Includes business logic flags from original UserRole extension methods
/// </summary>
public class UserRoleRef : ReferenceDataBase
{
    /// <summary>
    /// Can this role manage other users?
    /// </summary>
    public bool CanManageUsers { get; private set; }

    /// <summary>
    /// Can this role create events?
    /// </summary>
    public bool CanCreateEvents { get; private set; }

    /// <summary>
    /// Can this role moderate content?
    /// </summary>
    public bool CanModerateContent { get; private set; }

    /// <summary>
    /// Can this role create business profiles?
    /// </summary>
    public bool CanCreateBusinessProfile { get; private set; }

    /// <summary>
    /// Can this role create posts?
    /// </summary>
    public bool CanCreatePosts { get; private set; }

    /// <summary>
    /// Does this role require a paid subscription?
    /// </summary>
    public bool RequiresSubscription { get; private set; }

    /// <summary>
    /// Monthly subscription price (USD)
    /// </summary>
    public decimal MonthlyPrice { get; private set; }

    /// <summary>
    /// Does role upgrade require admin approval?
    /// </summary>
    public bool RequiresApproval { get; private set; }

    private UserRoleRef(
        Guid id,
        string code,
        string name,
        int displayOrder,
        bool canManageUsers,
        bool canCreateEvents,
        bool canModerateContent,
        bool canCreateBusinessProfile,
        bool canCreatePosts,
        bool requiresSubscription,
        decimal monthlyPrice,
        bool requiresApproval,
        string? description = null)
        : base(id, code, name, displayOrder, description)
    {
        CanManageUsers = canManageUsers;
        CanCreateEvents = canCreateEvents;
        CanModerateContent = canModerateContent;
        CanCreateBusinessProfile = canCreateBusinessProfile;
        CanCreatePosts = canCreatePosts;
        RequiresSubscription = requiresSubscription;
        MonthlyPrice = monthlyPrice;
        RequiresApproval = requiresApproval;
    }

    private UserRoleRef() : base()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Factory method to create a new UserRoleRef
    /// </summary>
    public static UserRoleRef Create(
        Guid id,
        string code,
        string name,
        int displayOrder,
        bool canManageUsers = false,
        bool canCreateEvents = false,
        bool canModerateContent = false,
        bool canCreateBusinessProfile = false,
        bool canCreatePosts = false,
        bool requiresSubscription = false,
        decimal monthlyPrice = 0m,
        bool requiresApproval = false,
        string? description = null)
    {
        return new UserRoleRef(
            id,
            code,
            name,
            displayOrder,
            canManageUsers,
            canCreateEvents,
            canModerateContent,
            canCreateBusinessProfile,
            canCreatePosts,
            requiresSubscription,
            monthlyPrice,
            requiresApproval,
            description) { Id = id };
    }
}

namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// User roles for the LankaConnect platform
/// Phase 6A.0: Updated role system for Event Organizer + Business Owner model
///
/// Role Capabilities:
/// - GeneralUser: Browse events, register, forum participation, no subscription
/// - BusinessOwner: Create business profiles/ads, $10/month, requires approval
/// - EventOrganizer: Create events/posts, $10/month, requires approval
/// - EventOrganizerAndBusinessOwner: Combined capabilities, $15/month, requires approval
/// - Admin: System administrator, manages approvals and analytics
/// - AdminManager: Super admin, manages admin users and system settings
///
/// Phase 1 MVP: Only GeneralUser and EventOrganizer implemented; BusinessOwner UI available but disabled
/// Phase 2: Full BusinessOwner, Event Organizer + Business Owner support with marketplace
/// </summary>
public enum UserRole
{
    GeneralUser = 1,
    BusinessOwner = 2,
    EventOrganizer = 3,
    EventOrganizerAndBusinessOwner = 4,
    Admin = 5,
    AdminManager = 6
}

public static class UserRoleExtensions
{
    public static string ToDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.GeneralUser => "General User",
            UserRole.BusinessOwner => "Business Owner",
            UserRole.EventOrganizer => "Event Organizer",
            UserRole.EventOrganizerAndBusinessOwner => "Event Organizer + Business Owner",
            UserRole.Admin => "Administrator",
            UserRole.AdminManager => "Admin Manager",
            _ => role.ToString()
        };
    }

    public static bool CanManageUsers(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.AdminManager;
    }

    public static bool CanCreateEvents(this UserRole role)
    {
        return role == UserRole.EventOrganizer ||
               role == UserRole.EventOrganizerAndBusinessOwner ||
               role == UserRole.Admin ||
               role == UserRole.AdminManager;
    }

    public static bool CanModerateContent(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.AdminManager;
    }

    public static bool IsEventOrganizer(this UserRole role)
    {
        return role == UserRole.EventOrganizer;
    }

    public static bool IsAdmin(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.AdminManager;
    }

    public static bool RequiresSubscription(this UserRole role)
    {
        return role == UserRole.EventOrganizer ||
               role == UserRole.BusinessOwner ||
               role == UserRole.EventOrganizerAndBusinessOwner;
    }

    public static bool CanCreateBusinessProfile(this UserRole role)
    {
        return role == UserRole.BusinessOwner ||
               role == UserRole.EventOrganizerAndBusinessOwner ||
               role == UserRole.Admin ||
               role == UserRole.AdminManager;
    }

    public static bool CanCreatePosts(this UserRole role)
    {
        return role == UserRole.EventOrganizer ||
               role == UserRole.EventOrganizerAndBusinessOwner ||
               role == UserRole.Admin ||
               role == UserRole.AdminManager;
    }

    public static decimal GetMonthlySubscriptionPrice(this UserRole role)
    {
        return role switch
        {
            UserRole.EventOrganizer => 10.00m,
            UserRole.BusinessOwner => 10.00m,
            UserRole.EventOrganizerAndBusinessOwner => 15.00m,
            _ => 0.00m
        };
    }
}
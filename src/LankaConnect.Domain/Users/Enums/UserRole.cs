namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// User roles for the LankaConnect platform
/// Phase 6A.0: Updated role system for Event Organizer model
/// </summary>
public enum UserRole
{
    GeneralUser = 1,
    EventOrganizer = 2,
    Admin = 3,
    AdminManager = 4
}

public static class UserRoleExtensions
{
    public static string ToDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.GeneralUser => "General User",
            UserRole.EventOrganizer => "Event Organizer",
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
        return role == UserRole.EventOrganizer || role == UserRole.Admin || role == UserRole.AdminManager;
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
        return role == UserRole.EventOrganizer;
    }
}
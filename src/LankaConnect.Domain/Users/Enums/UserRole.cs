namespace LankaConnect.Domain.Users.Enums;

public enum UserRole
{
    User = 1,
    BusinessOwner = 2,
    Moderator = 3,
    Admin = 4
}

public static class UserRoleExtensions
{
    public static string ToDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.User => "User",
            UserRole.BusinessOwner => "Business Owner",
            UserRole.Moderator => "Moderator",
            UserRole.Admin => "Administrator",
            _ => role.ToString()
        };
    }

    public static bool CanManageUsers(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.Moderator;
    }

    public static bool CanManageBusinesses(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.Moderator || role == UserRole.BusinessOwner;
    }

    public static bool CanModerateContent(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.Moderator;
    }

    public static bool IsBusinessOwner(this UserRole role)
    {
        return role == UserRole.BusinessOwner;
    }

    public static bool IsAdmin(this UserRole role)
    {
        return role == UserRole.Admin;
    }
}
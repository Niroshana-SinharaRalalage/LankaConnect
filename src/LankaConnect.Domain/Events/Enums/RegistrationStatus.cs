namespace LankaConnect.Domain.Events.Enums;

public enum RegistrationStatus
{
    Pending = 0,
    Confirmed = 1,
    Waitlisted = 2,
    CheckedIn = 3,
    Completed = 4,
    Cancelled = 5,
    Refunded = 6
}
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing details of a single attendee
/// Lightweight object containing only name and age
/// Used in multi-attendee registration to store information for each person
/// </summary>
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public int Age { get; }

    // EF Core constructor
    private AttendeeDetails()
    {
        // Required for EF Core
        Name = null!;
    }

    private AttendeeDetails(string name, int age)
    {
        Name = name;
        Age = age;
    }

    /// <summary>
    /// Creates a new AttendeeDetails instance
    /// </summary>
    /// <param name="name">Attendee's full name</param>
    /// <param name="age">Attendee's age (1-120)</param>
    public static Result<AttendeeDetails> Create(string? name, int age)
    {
        // Validation: Name is required
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeDetails>.Failure("Name is required");

        // Validation: Age must be greater than 0
        if (age <= 0)
            return Result<AttendeeDetails>.Failure("Age must be greater than 0");

        // Validation: Age must not exceed 120
        if (age > 120)
            return Result<AttendeeDetails>.Failure("Age must not exceed 120");

        // Trim whitespace from name
        var trimmedName = name.Trim();

        return Result<AttendeeDetails>.Success(new AttendeeDetails(trimmedName, age));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Age;
    }

    public override string ToString()
    {
        return $"{Name} ({Age})";
    }
}

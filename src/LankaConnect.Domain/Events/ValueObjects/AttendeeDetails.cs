using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing details of a single attendee
/// Contains name, age category (Adult/Child), and optional gender
/// Used in multi-attendee registration to store information for each person
/// </summary>
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }
    public Gender? Gender { get; }

    // EF Core constructor
    private AttendeeDetails()
    {
        // Required for EF Core
        Name = null!;
    }

    private AttendeeDetails(string name, AgeCategory ageCategory, Gender? gender)
    {
        Name = name;
        AgeCategory = ageCategory;
        Gender = gender;
    }

    /// <summary>
    /// Creates a new AttendeeDetails instance
    /// </summary>
    /// <param name="name">Attendee's full name</param>
    /// <param name="ageCategory">Age category (Adult or Child)</param>
    /// <param name="gender">Optional gender (Male, Female, or Other)</param>
    public static Result<AttendeeDetails> Create(string? name, AgeCategory ageCategory, Gender? gender = null)
    {
        // Validation: Name is required
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeDetails>.Failure("Name is required");

        // Validation: AgeCategory must be a valid enum value
        if (!Enum.IsDefined(typeof(AgeCategory), ageCategory))
            return Result<AttendeeDetails>.Failure("Invalid age category");

        // Validation: Gender must be a valid enum value if provided
        if (gender.HasValue && !Enum.IsDefined(typeof(Gender), gender.Value))
            return Result<AttendeeDetails>.Failure("Invalid gender value");

        // Trim whitespace from name
        var trimmedName = name.Trim();

        return Result<AttendeeDetails>.Success(new AttendeeDetails(trimmedName, ageCategory, gender));
    }

    /// <summary>
    /// Creates AttendeeDetails from legacy age-based format (for data migration)
    /// Age <= 18 maps to Child, Age > 18 maps to Adult
    /// </summary>
    /// <param name="name">Attendee's full name</param>
    /// <param name="age">Attendee's age (used for category determination)</param>
    /// <param name="gender">Optional gender</param>
    public static Result<AttendeeDetails> CreateFromAge(string? name, int age, Gender? gender = null)
    {
        // Validation: Name is required
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeDetails>.Failure("Name is required");

        // Validation: Age must be valid for category determination
        if (age <= 0 || age > 120)
            return Result<AttendeeDetails>.Failure("Age must be between 1 and 120");

        // Determine age category based on age
        var ageCategory = age <= 18 ? AgeCategory.Child : AgeCategory.Adult;

        // Trim whitespace from name
        var trimmedName = name.Trim();

        return Result<AttendeeDetails>.Success(new AttendeeDetails(trimmedName, ageCategory, gender));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return AgeCategory;
        yield return Gender ?? Enums.Gender.Other; // Use default for null comparison
    }

    public override string ToString()
    {
        var genderStr = Gender.HasValue ? $", {Gender}" : "";
        return $"{Name} ({AgeCategory}{genderStr})";
    }
}

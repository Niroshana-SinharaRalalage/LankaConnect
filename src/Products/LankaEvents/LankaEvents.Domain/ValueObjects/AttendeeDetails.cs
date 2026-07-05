using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

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

    /// <summary>
    /// Optional ticket tier ID for multi-tier events.
    /// Null for SingleTier mode events.
    /// </summary>
    public Guid? TicketTierId { get; }

    /// <summary>
    /// Denormalized tier name for display (e.g., "VIP", "Basic").
    /// Null for SingleTier mode events.
    /// </summary>
    public string? TicketTierName { get; }

    /// <summary>
    /// Assigned seat ID for events with assigned seating.
    /// Null for general admission events.
    /// </summary>
    public Guid? SeatId { get; }

    /// <summary>
    /// Denormalized seat label for display (e.g., "A1", "T3-S5").
    /// Null for general admission events.
    /// </summary>
    public string? SeatLabel { get; }

    // EF Core constructor
    private AttendeeDetails()
    {
        // Required for EF Core
        Name = null!;
    }

    private AttendeeDetails(string name, AgeCategory ageCategory, Gender? gender,
        Guid? ticketTierId = null, string? ticketTierName = null,
        Guid? seatId = null, string? seatLabel = null)
    {
        Name = name;
        AgeCategory = ageCategory;
        Gender = gender;
        TicketTierId = ticketTierId;
        TicketTierName = ticketTierName;
        SeatId = seatId;
        SeatLabel = seatLabel;
    }

    /// <summary>
    /// Creates a new AttendeeDetails instance
    /// </summary>
    /// <param name="name">Attendee's full name</param>
    /// <param name="ageCategory">Age category (Adult or Child)</param>
    /// <param name="gender">Optional gender (Male, Female, or Other)</param>
    public static Result<AttendeeDetails> Create(string? name, AgeCategory ageCategory, Gender? gender = null,
        Guid? ticketTierId = null, string? ticketTierName = null,
        Guid? seatId = null, string? seatLabel = null)
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

        return Result<AttendeeDetails>.Success(new AttendeeDetails(
            trimmedName, ageCategory, gender, ticketTierId, ticketTierName?.Trim(),
            seatId, seatLabel?.Trim()));
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

    /// <summary>
    /// Phase 8 S8.1 — value-object-style seat binding. Returns a new
    /// <see cref="AttendeeDetails"/> with the seat fields populated; original
    /// is unchanged. Used by the webhook seat-binding path so application
    /// handlers don't have to repeat every other field on the 7-arg
    /// <see cref="Create"/> factory just to add a seat. Idempotent rebinds
    /// are allowed at this layer; the aggregate-level
    /// <c>Registration.ConfirmSeatAssignments</c> enforces invariants like
    /// "seat already bound" via its own checks.
    /// </summary>
    public Result<AttendeeDetails> WithSeat(Guid seatId, string? seatLabel)
    {
        if (seatId == Guid.Empty)
            return Result<AttendeeDetails>.Failure("Seat ID is required");

        if (string.IsNullOrWhiteSpace(seatLabel))
            return Result<AttendeeDetails>.Failure("Seat label is required");

        return Result<AttendeeDetails>.Success(new AttendeeDetails(
            Name,
            AgeCategory,
            Gender,
            TicketTierId,
            TicketTierName,
            seatId,
            seatLabel.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return AgeCategory;
        yield return Gender ?? Enums.Gender.Other; // Use default for null comparison
        yield return TicketTierId ?? Guid.Empty;
        yield return SeatId ?? Guid.Empty;
    }

    public override string ToString()
    {
        var genderStr = Gender.HasValue ? $", {Gender}" : "";
        return $"{Name} ({AgeCategory}{genderStr})";
    }
}

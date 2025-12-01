using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

public class AttendeeInfo : ValueObject
{
    public string Name { get; }
    public int Age { get; }
    public string Address { get; }
    public Email Email { get; }
    public PhoneNumber PhoneNumber { get; }

    // Private constructor for domain logic
    private AttendeeInfo(
        string name,
        int age,
        string address,
        Email email,
        PhoneNumber phoneNumber)
    {
        Name = name;
        Age = age;
        Address = address;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    // Parameterless constructor for EF Core
    private AttendeeInfo()
    {
        Name = string.Empty;
        Age = 0;
        Address = string.Empty;
        Email = null!;
        PhoneNumber = null!;
    }

    public static Result<AttendeeInfo> Create(
        string name,
        int age,
        string address,
        string email,
        string phoneNumber)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeInfo>.Failure("Name is required");

        // Validate age
        if (age < 1 || age > 150)
            return Result<AttendeeInfo>.Failure("Age must be between 1 and 150");

        // Validate address
        if (string.IsNullOrWhiteSpace(address))
            return Result<AttendeeInfo>.Failure("Address is required");

        // Validate and create email
        var emailResult = Email.Create(email);
        if (!emailResult.IsSuccess)
            return Result<AttendeeInfo>.Failure($"Invalid email: {emailResult.Error}");

        // Validate and create phone number
        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (!phoneResult.IsSuccess)
            return Result<AttendeeInfo>.Failure($"Invalid phone number: {phoneResult.Error}");

        return Result<AttendeeInfo>.Success(new AttendeeInfo(
            name.Trim(),
            age,
            address.Trim(),
            emailResult.Value,
            phoneResult.Value
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Age;
        yield return Address;
        yield return Email;
        yield return PhoneNumber;
    }

    public override string ToString()
    {
        return $"{Name}, Age {Age}, {Address}, {Email.Value}, {PhoneNumber.ToDisplayFormat()}";
    }
}

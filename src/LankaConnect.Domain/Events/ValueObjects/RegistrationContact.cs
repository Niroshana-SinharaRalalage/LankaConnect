using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing shared contact information for a registration
/// All attendees in a registration share the same contact details
/// </summary>
public class RegistrationContact : ValueObject
{
    // Simple email validation regex
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Email { get; }
    public string PhoneNumber { get; }
    public string? Address { get; }

    private RegistrationContact(string email, string phoneNumber, string? address)
    {
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    /// <summary>
    /// Creates a new RegistrationContact instance
    /// </summary>
    /// <param name="email">Email address (required, must be valid format)</param>
    /// <param name="phoneNumber">Phone number (required)</param>
    /// <param name="address">Physical address (optional)</param>
    public static Result<RegistrationContact> Create(string? email, string? phoneNumber, string? address)
    {
        // Validation: Email is required
        if (string.IsNullOrWhiteSpace(email))
            return Result<RegistrationContact>.Failure("Email is required");

        // Trim email
        var trimmedEmail = email.Trim();

        // Validation: Email format must be valid
        if (!EmailRegex.IsMatch(trimmedEmail))
            return Result<RegistrationContact>.Failure("Invalid email format");

        // Validation: Phone number is required
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result<RegistrationContact>.Failure("Phone number is required");

        // Trim phone number
        var trimmedPhone = phoneNumber.Trim();

        // Trim and normalize address (null if empty after trim)
        string? trimmedAddress = null;
        if (!string.IsNullOrWhiteSpace(address))
        {
            trimmedAddress = address.Trim();
        }

        return Result<RegistrationContact>.Success(
            new RegistrationContact(trimmedEmail, trimmedPhone, trimmedAddress));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return PhoneNumber;
        if (Address != null)
            yield return Address;
    }

    public override string ToString()
    {
        if (Address != null)
            return $"Email: {Email}, Phone: {PhoneNumber}, Address: {Address}";

        return $"Email: {Email}, Phone: {PhoneNumber}";
    }
}

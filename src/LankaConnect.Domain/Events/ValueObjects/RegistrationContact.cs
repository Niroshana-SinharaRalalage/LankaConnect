using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing shared contact information for a registration
/// All attendees in a registration share the same contact details
/// Phase 7A.6D: Added WhatsAppPhoneNumber and WhatsAppOptedIn for WhatsApp notification opt-in
/// </summary>
public class RegistrationContact : ValueObject
{
    // Simple email validation regex
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Phase 7A.6D: E.164 phone number format validation for WhatsApp
    private static readonly Regex E164Regex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string Email { get; }
    public string PhoneNumber { get; }
    public string? Address { get; }

    // Phase 7A.6D: WhatsApp opt-in fields stored in JSONB (no migration needed)
    public string? WhatsAppPhoneNumber { get; }
    public bool WhatsAppOptedIn { get; }

    // EF Core constructor
    private RegistrationContact()
    {
        // Required for EF Core
        Email = null!;
        PhoneNumber = null!;
        WhatsAppOptedIn = false;
    }

    private RegistrationContact(string email, string phoneNumber, string? address,
        string? whatsAppPhoneNumber, bool whatsAppOptedIn)
    {
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
        WhatsAppPhoneNumber = whatsAppPhoneNumber;
        WhatsAppOptedIn = whatsAppOptedIn;
    }

    /// <summary>
    /// Creates a new RegistrationContact instance
    /// Phase 7A.6D: Added optional WhatsApp phone number and opt-in flag
    /// </summary>
    /// <param name="email">Email address (required, must be valid format)</param>
    /// <param name="phoneNumber">Phone number (required)</param>
    /// <param name="address">Physical address (optional)</param>
    /// <param name="whatsAppPhoneNumber">WhatsApp phone in E.164 format (optional)</param>
    /// <param name="whatsAppOptedIn">Whether user opted in to WhatsApp notifications</param>
    public static Result<RegistrationContact> Create(
        string? email, string? phoneNumber, string? address,
        string? whatsAppPhoneNumber = null, bool whatsAppOptedIn = false)
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

        // Phase 7A.6D: Validate and normalize WhatsApp phone number
        string? trimmedWhatsApp = null;
        var effectiveOptedIn = false;
        if (whatsAppOptedIn && !string.IsNullOrWhiteSpace(whatsAppPhoneNumber))
        {
            trimmedWhatsApp = whatsAppPhoneNumber.Trim();
            if (!E164Regex.IsMatch(trimmedWhatsApp))
            {
                return Result<RegistrationContact>.Failure(
                    "WhatsApp phone number must be in E.164 format (e.g., +14155551234)");
            }
            effectiveOptedIn = true;
        }

        return Result<RegistrationContact>.Success(
            new RegistrationContact(trimmedEmail, trimmedPhone, trimmedAddress,
                trimmedWhatsApp, effectiveOptedIn));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return PhoneNumber;
        if (Address != null)
            yield return Address;
        yield return WhatsAppOptedIn;
        if (WhatsAppPhoneNumber != null)
            yield return WhatsAppPhoneNumber;
    }

    public override string ToString()
    {
        var waStatus = WhatsAppOptedIn ? $", WhatsApp: {WhatsAppPhoneNumber}" : "";
        if (Address != null)
            return $"Email: {Email}, Phone: {PhoneNumber}, Address: {Address}{waStatus}";

        return $"Email: {Email}, Phone: {PhoneNumber}{waStatus}";
    }
}

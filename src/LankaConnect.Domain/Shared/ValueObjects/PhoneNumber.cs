using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Shared.ValueObjects;

public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string Value { get; }
    public string CountryCode { get; }
    public string Number { get; }

    private PhoneNumber(string value, string countryCode, string number)
    {
        Value = value;
        CountryCode = countryCode;
        Number = number;
    }

    // Parameterless constructor for EF Core
    private PhoneNumber()
    {
        Value = string.Empty;
        CountryCode = string.Empty;
        Number = string.Empty;
    }

    public static Result<PhoneNumber> Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result<PhoneNumber>.Failure("Phone number cannot be empty");

        // Remove all non-digit characters except +
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");

        if (string.IsNullOrEmpty(cleaned))
            return Result<PhoneNumber>.Failure("Phone number must contain digits");

        // Validate format
        if (!PhoneRegex.IsMatch(cleaned))
            return Result<PhoneNumber>.Failure("Invalid phone number format. Must include country code (e.g., +94771234567)");

        // Extract country code and number
        string countryCode;
        string number;

        if (cleaned.StartsWith("+"))
        {
            // Find where country code ends (assuming max 3 digits for country code)
            var match = Regex.Match(cleaned, @"^\+(\d{1,3})(\d{7,})$");
            if (!match.Success)
                return Result<PhoneNumber>.Failure("Invalid phone number format");

            countryCode = match.Groups[1].Value;
            number = match.Groups[2].Value;
        }
        else
        {
            // Assume Sri Lankan number without + prefix
            if (cleaned.StartsWith("94"))
            {
                countryCode = "94";
                number = cleaned[2..];
            }
            else if (cleaned.StartsWith("0"))
            {
                countryCode = "94";
                number = cleaned[1..];
            }
            else
            {
                return Result<PhoneNumber>.Failure("Phone number must include country code or start with 0 for Sri Lankan numbers");
            }
        }

        // Validate Sri Lankan numbers specifically
        if (countryCode == "94")
        {
            if (number.Length != 9)
                return Result<PhoneNumber>.Failure("Sri Lankan phone numbers must have 9 digits after country code");

            var firstDigit = number[0];
            if (firstDigit != '7' && firstDigit != '1' && firstDigit != '2' && firstDigit != '3' && 
                firstDigit != '4' && firstDigit != '5' && firstDigit != '6' && firstDigit != '8' && firstDigit != '9')
                return Result<PhoneNumber>.Failure("Invalid Sri Lankan phone number format");
        }

        // Preserve original format for display, but store cleaned for processing
        var originalFormat = phoneNumber.Trim();
        return Result<PhoneNumber>.Success(new PhoneNumber(originalFormat, countryCode, number));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;

    public string ToDisplayFormat()
    {
        if (CountryCode == "94" && Number.Length == 9)
        {
            return $"+94-{Number[0]}{Number[1]}-{Number[2..5]}-{Number[5..]}";
        }
        
        return Value;
    }
}
using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Shared.ValueObjects;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure("Email cannot be empty");

        var trimmedEmail = email.Trim().ToLowerInvariant();

        if (trimmedEmail.Length > 254)
            return Result<Email>.Failure("Email cannot exceed 254 characters");

        if (!EmailRegex.IsMatch(trimmedEmail))
            return Result<Email>.Failure("Invalid email format");

        return Result<Email>.Success(new Email(trimmedEmail));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
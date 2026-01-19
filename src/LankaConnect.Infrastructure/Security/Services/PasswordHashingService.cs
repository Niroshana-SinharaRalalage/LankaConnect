using System.Text.RegularExpressions;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Security.Services;

public class PasswordHashingService : IPasswordHashingService
{
    private readonly ILogger<PasswordHashingService> _logger;
    private const int WorkFactor = 12; // BCrypt work factor for security

    // Password validation patterns
    private static readonly Regex HasUpperCase = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex HasLowerCase = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new(@"\d", RegexOptions.Compiled);
    private static readonly Regex HasSpecialChar = new(@"[^a-zA-Z\d\s]", RegexOptions.Compiled);

    public PasswordHashingService(ILogger<PasswordHashingService> logger)
    {
        _logger = logger;
    }

    public Result<string> HashPassword(string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return Result<string>.Failure("Password cannot be empty");
            }

            // Validate password strength before hashing
            var validationResult = ValidatePasswordStrength(password);
            if (!validationResult.IsSuccess)
            {
                return Result<string>.Failure(validationResult.Error);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
            _logger.LogInformation("Password hashed successfully");
            
            return Result<string>.Success(hashedPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            return Result<string>.Failure("Failed to hash password");
        }
    }

    public Result<bool> VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return Result<bool>.Failure("Password cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return Result<bool>.Failure("Hashed password cannot be empty");
            }

            var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            _logger.LogInformation("Password verification completed: {IsValid}", isValid);
            
            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return Result<bool>.Failure("Failed to verify password");
        }
    }

    public Result ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure("Password is required");
        }

        var errors = new List<string>();

        // Minimum length requirement
        if (password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters long");
        }

        // Maximum length requirement (to prevent DoS attacks)
        if (password.Length > 128)
        {
            errors.Add("Password must not exceed 128 characters");
        }

        // Character type requirements
        if (!HasUpperCase.IsMatch(password))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (!HasLowerCase.IsMatch(password))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (!HasDigit.IsMatch(password))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (!HasSpecialChar.IsMatch(password))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Common password checks
        if (IsCommonPassword(password))
        {
            errors.Add("Password is too common, please choose a stronger password");
        }

        // Sequential character checks
        if (HasSequentialCharacters(password))
        {
            errors.Add("Password should not contain sequential characters");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join(". ", errors);
            _logger.LogInformation("Password validation failed: {Errors}", errorMessage);
            return Result.Failure(errorMessage);
        }

        _logger.LogInformation("Password validation passed");
        return Result.Success();
    }

    private static bool IsCommonPassword(string password)
    {
        // Common passwords to reject
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "password123", "admin", "qwerty",
            "letmein", "welcome", "monkey", "1234567890", "password1",
            "abc123", "Password1", "welcome123", "admin123", "root",
            "user", "guest", "test", "demo", "temp"
        };

        return commonPasswords.Contains(password);
    }

    private static bool HasSequentialCharacters(string password)
    {
        // Check for sequential characters (both ascending and descending)
        for (int i = 0; i < password.Length - 2; i++)
        {
            var char1 = password[i];
            var char2 = password[i + 1];
            var char3 = password[i + 2];

            // Check for ascending sequence
            if (char2 == char1 + 1 && char3 == char2 + 1)
            {
                return true;
            }

            // Check for descending sequence
            if (char2 == char1 - 1 && char3 == char2 - 1)
            {
                return true;
            }
        }

        // Check for repeated characters (3 or more in a row)
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
            {
                return true;
            }
        }

        return false;
    }
}
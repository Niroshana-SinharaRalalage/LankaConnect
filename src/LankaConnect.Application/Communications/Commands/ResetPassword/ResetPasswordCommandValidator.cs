using FluentValidation;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.ResetPassword;

/// <summary>
/// Validator for ResetPasswordCommand
/// Phase 6A.101: Email is now optional - user can be looked up by token
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        // Phase 6A.101: Email is optional - only validate format if provided
        RuleFor(x => x.Email)
            .Must(BeValidEmailOrNull)
            .WithMessage("Invalid email format")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required")
            .MinimumLength(32)
            .WithMessage("Invalid token format")
            .MaximumLength(64)
            .WithMessage("Invalid token format")
            .Matches(@"^[a-zA-Z0-9]+$")
            .WithMessage("Token contains invalid characters");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one digit")
            .Matches(@"[^a-zA-Z\d\s]")
            .WithMessage("Password must contain at least one special character");
    }

    private static bool BeValidEmailOrNull(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;  // Null/empty is allowed (user found by token)

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}
using FluentValidation;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.ResetPassword;

/// <summary>
/// Validator for ResetPasswordCommand
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .Must(BeValidEmail)
            .WithMessage("Invalid email format");

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

    private static bool BeValidEmail(string email)
    {
        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}
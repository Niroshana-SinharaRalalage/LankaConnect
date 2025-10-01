using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Validator for VerifyEmailCommand
/// </summary>
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Verification token is required")
            .MinimumLength(32)
            .WithMessage("Invalid token format")
            .MaximumLength(64)
            .WithMessage("Invalid token format")
            .Matches(@"^[a-zA-Z0-9]+$")
            .WithMessage("Token contains invalid characters");
    }
}
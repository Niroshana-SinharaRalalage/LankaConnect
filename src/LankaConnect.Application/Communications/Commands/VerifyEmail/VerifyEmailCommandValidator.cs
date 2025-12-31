using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Validator for VerifyEmailCommand
/// </summary>
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        // Phase 6A.53: Removed UserId validation - token-only verification
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
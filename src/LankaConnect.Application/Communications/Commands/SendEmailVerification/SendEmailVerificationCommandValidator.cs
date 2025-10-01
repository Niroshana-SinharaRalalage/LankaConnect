using FluentValidation;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SendEmailVerification;

/// <summary>
/// Validator for SendEmailVerificationCommand
/// </summary>
public class SendEmailVerificationCommandValidator : AbstractValidator<SendEmailVerificationCommand>
{
    public SendEmailVerificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Email)
            .Must(BeValidEmailWhenProvided)
            .WithMessage("Invalid email format")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }

    private static bool BeValidEmailWhenProvided(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true; // Email is optional

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}
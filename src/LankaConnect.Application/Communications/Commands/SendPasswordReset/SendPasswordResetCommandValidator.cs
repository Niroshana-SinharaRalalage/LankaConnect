using FluentValidation;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SendPasswordReset;

/// <summary>
/// Validator for SendPasswordResetCommand
/// </summary>
public class SendPasswordResetCommandValidator : AbstractValidator<SendPasswordResetCommand>
{
    public SendPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .Must(BeValidEmail)
            .WithMessage("Invalid email format");
    }

    private static bool BeValidEmail(string email)
    {
        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}
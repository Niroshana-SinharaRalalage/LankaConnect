using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.SendWelcomeEmail;

/// <summary>
/// Validator for SendWelcomeEmailCommand
/// </summary>
public class SendWelcomeEmailCommandValidator : AbstractValidator<SendWelcomeEmailCommand>
{
    public SendWelcomeEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.TriggerType)
            .IsInEnum()
            .WithMessage("Invalid welcome email trigger type");

        RuleFor(x => x.CustomMessage)
            .MaximumLength(500)
            .WithMessage("Custom message must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomMessage));
    }
}
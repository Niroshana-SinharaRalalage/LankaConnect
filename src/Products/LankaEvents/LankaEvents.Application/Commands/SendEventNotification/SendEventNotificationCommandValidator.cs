using FluentValidation;

namespace LankaConnect.Products.LankaEvents.Application.Commands.SendEventNotification;

/// <summary>
/// Phase 6A.61: Validator for SendEventNotification command
/// </summary>
public class SendEventNotificationCommandValidator : AbstractValidator<SendEventNotificationCommand>
{
    public SendEventNotificationCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");
    }
}

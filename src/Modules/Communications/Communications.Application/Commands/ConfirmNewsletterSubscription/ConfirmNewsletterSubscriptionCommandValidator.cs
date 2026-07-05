using FluentValidation;
namespace LankaConnect.Modules.Communications.Application.Commands.ConfirmNewsletterSubscription;

/// <summary>
/// Validator for ConfirmNewsletterSubscriptionCommand
/// </summary>
public class ConfirmNewsletterSubscriptionCommandValidator : AbstractValidator<ConfirmNewsletterSubscriptionCommand>
{
    public ConfirmNewsletterSubscriptionCommandValidator()
    {
        RuleFor(x => x.ConfirmationToken)
            .NotEmpty()
            .WithMessage("Confirmation token is required")
            .MinimumLength(10)
            .WithMessage("Confirmation token is invalid");
    }
}

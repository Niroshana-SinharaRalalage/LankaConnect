using FluentValidation;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
namespace LankaConnect.Modules.Communications.Application.Commands.SubscribeToNewsletter;

/// <summary>
/// Validator for SubscribeToNewsletterCommand
/// </summary>
public class SubscribeToNewsletterCommandValidator : AbstractValidator<SubscribeToNewsletterCommand>
{
    public SubscribeToNewsletterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .Must(BeValidEmail)
            .WithMessage("Invalid email format");

        // Phase 5B: Comprehensive validation rule that allows empty arrays when ReceiveAllLocations = true
        // Removed redundant .NotEmpty() rule that incorrectly rejected empty arrays
        RuleFor(x => x)
            .Must(command => command.ReceiveAllLocations || (command.MetroAreaIds != null && command.MetroAreaIds.Any()))
            .WithMessage("Either specify metro areas or select to receive all locations");

        // Phase 7A.6C: Validate WhatsApp phone number format (E.164) when provided
        RuleFor(x => x.WhatsAppPhoneNumber)
            .Matches(@"^\+[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppPhoneNumber))
            .WithMessage("WhatsApp phone number must be in E.164 format (e.g., +14155551234)");
    }

    private static bool BeValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}

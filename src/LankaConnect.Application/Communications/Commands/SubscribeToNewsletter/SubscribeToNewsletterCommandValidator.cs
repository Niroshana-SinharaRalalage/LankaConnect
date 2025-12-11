using FluentValidation;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;

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
    }

    private static bool BeValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}

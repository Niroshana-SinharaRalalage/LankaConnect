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

        RuleFor(x => x.MetroAreaId)
            .NotEmpty()
            .WithMessage("Metro area is required when not receiving all locations")
            .When(x => !x.ReceiveAllLocations);

        RuleFor(x => x.ReceiveAllLocations)
            .Must((command, receiveAll) => receiveAll || command.MetroAreaId.HasValue)
            .WithMessage("Either specify a metro area or select to receive all locations");
    }

    private static bool BeValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}

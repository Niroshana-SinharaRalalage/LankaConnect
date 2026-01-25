using FluentValidation;

namespace LankaConnect.Application.Events.Commands.ResendAttendeeConfirmation;

/// <summary>
/// Phase 6A.X: Validator for ResendAttendeeConfirmationCommand.
/// Ensures all required IDs are provided.
/// </summary>
public class ResendAttendeeConfirmationCommandValidator : AbstractValidator<ResendAttendeeConfirmationCommand>
{
    public ResendAttendeeConfirmationCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.RegistrationId)
            .NotEmpty()
            .WithMessage("Registration ID is required");

        RuleFor(x => x.OrganizerId)
            .NotEmpty()
            .WithMessage("Organizer ID is required");
    }
}

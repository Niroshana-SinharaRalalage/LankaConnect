using FluentValidation;

namespace LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;

/// <summary>
/// Phase 6A.14: Validator for UpdateRegistrationDetailsCommand
/// Validates input before reaching the domain layer
/// </summary>
public class UpdateRegistrationDetailsCommandValidator : AbstractValidator<UpdateRegistrationDetailsCommand>
{
    public UpdateRegistrationDetailsCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Attendees)
            .NotNull()
            .WithMessage("Attendees list is required")
            .Must(a => a != null && a.Count > 0)
            .WithMessage("At least one attendee is required")
            .Must(a => a == null || a.Count <= 10)
            .WithMessage("Maximum 10 attendees per registration");

        RuleForEach(x => x.Attendees)
            .ChildRules(attendee =>
            {
                attendee.RuleFor(a => a.Name)
                    .NotEmpty()
                    .WithMessage("Attendee name is required")
                    .MaximumLength(100)
                    .WithMessage("Attendee name cannot exceed 100 characters");

                // Phase 6A.43: AgeCategory is an enum, validated by model binding
                attendee.RuleFor(a => a.AgeCategory)
                    .IsInEnum()
                    .WithMessage("Invalid age category");
            });

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .MaximumLength(30)
            .WithMessage("Phone number cannot exceed 30 characters");

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));
    }
}

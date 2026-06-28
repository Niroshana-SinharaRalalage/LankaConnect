using FluentValidation;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventOrganizerContact;

/// <summary>
/// Phase 6A.132: Validator for UpdateEventOrganizerContactCommand
/// Enforces max contacts limit and per-contact field constraints at MediatR pipeline level.
/// Domain validation (EventOrganizerContact.Create) handles deeper business rules
/// like "must have email OR phone".
/// </summary>
public class UpdateEventOrganizerContactCommandValidator
    : AbstractValidator<UpdateEventOrganizerContactCommand>
{
    private const int MaxContacts = 10;

    public UpdateEventOrganizerContactCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.Contacts)
            .NotNull()
            .WithMessage("Contacts list is required");

        When(x => x.Contacts != null, () =>
        {
            RuleFor(x => x.Contacts)
                .Must(c => c.Count <= MaxContacts)
                .WithMessage($"Maximum {MaxContacts} organizer contacts allowed");

            RuleForEach(x => x.Contacts)
                .ChildRules(contact =>
                {
                    contact.RuleFor(c => c.ContactName)
                        .NotEmpty()
                        .WithMessage("Contact name is required")
                        .MaximumLength(200)
                        .WithMessage("Contact name must be less than 200 characters");

                    contact.RuleFor(c => c.ContactEmail)
                        .MaximumLength(255)
                        .WithMessage("Email must be less than 255 characters")
                        .EmailAddress()
                        .WithMessage("Invalid email format")
                        .When(c => !string.IsNullOrWhiteSpace(c.ContactEmail));

                    contact.RuleFor(c => c.ContactPhone)
                        .MaximumLength(20)
                        .WithMessage("Phone number must be less than 20 characters")
                        .When(c => !string.IsNullOrWhiteSpace(c.ContactPhone));
                });
        });
    }
}

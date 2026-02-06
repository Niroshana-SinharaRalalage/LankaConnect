using FluentValidation;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Validator for AddOpenSignUpItemCommand
/// </summary>
public class AddOpenSignUpItemCommandValidator : AbstractValidator<AddOpenSignUpItemCommand>
{
    public AddOpenSignUpItemCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.SignUpListId)
            .NotEmpty()
            .WithMessage("Sign-up list ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.ItemName)
            .NotEmpty()
            .WithMessage("Item name is required")
            .MaximumLength(200)
            .WithMessage("Item name must not exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 500 characters");

        RuleFor(x => x.ContactName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ContactName))
            .WithMessage("Contact name must not exceed 100 characters");

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Contact email must be a valid email address")
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Contact email must not exceed 255 characters");

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Contact phone must not exceed 20 characters");
    }
}

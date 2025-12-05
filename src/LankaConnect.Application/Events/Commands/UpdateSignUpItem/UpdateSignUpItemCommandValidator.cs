using FluentValidation;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpItem;

/// <summary>
/// Validator for UpdateSignUpItemCommand
/// Phase 6A.14: Edit Sign-Up Item feature
/// </summary>
public class UpdateSignUpItemCommandValidator : AbstractValidator<UpdateSignUpItemCommand>
{
    public UpdateSignUpItemCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.SignUpListId)
            .NotEmpty()
            .WithMessage("Sign-up list ID is required");

        RuleFor(x => x.SignUpItemId)
            .NotEmpty()
            .WithMessage("Sign-up item ID is required");

        RuleFor(x => x.ItemDescription)
            .NotEmpty()
            .WithMessage("Item description is required")
            .MaximumLength(500)
            .WithMessage("Item description must not exceed 500 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes must not exceed 500 characters");
    }
}

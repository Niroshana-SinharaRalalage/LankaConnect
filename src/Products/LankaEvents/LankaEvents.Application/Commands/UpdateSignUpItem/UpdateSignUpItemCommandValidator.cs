using FluentValidation;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateSignUpItem;

/// <summary>
/// Validator for UpdateSignUpItemCommand.
/// Phase 6A.14: Edit Sign-Up Item feature
/// Phase 6A.131: Surface-level validation only. Type-matched validation (e.g. TargetQuantity for
/// quantity items, AvailableSlots for slot items) happens in the handler after the item is loaded,
/// because the authoritative type lives on the entity — the validator has no DB access.
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

        RuleFor(x => x)
            .Must(cmd => cmd.TargetQuantity.HasValue || cmd.AvailableSlots.HasValue)
            .WithMessage("Either TargetQuantity (for quantity-based items) or AvailableSlots (for slot-based items) must be provided");

        RuleFor(x => x.TargetQuantity)
            .GreaterThan(0)
            .When(x => x.TargetQuantity.HasValue)
            .WithMessage("TargetQuantity must be greater than 0");

        RuleFor(x => x.AvailableSlots)
            .GreaterThan(0)
            .When(x => x.AvailableSlots.HasValue)
            .WithMessage("AvailableSlots must be greater than 0");

        RuleFor(x => x.SuggestedPerSlot)
            .GreaterThan(0)
            .When(x => x.SuggestedPerSlot.HasValue)
            .WithMessage("SuggestedPerSlot must be greater than 0");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes must not exceed 500 characters");
    }
}

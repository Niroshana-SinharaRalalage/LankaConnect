using FluentValidation;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Commands.AddSignUpItem;

/// <summary>
/// Phase 6A.121: Validator for AddSignUpItemCommand
/// Enforces dual field constraint: exactly ONE of TargetQuantity or AvailableSlots must be populated
/// </summary>
public class AddSignUpItemCommandValidator : AbstractValidator<AddSignUpItemCommand>
{
    public AddSignUpItemCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.SignUpListId)
            .NotEmpty()
            .WithMessage("Sign-up list ID is required");

        RuleFor(x => x.ItemDescription)
            .NotEmpty()
            .WithMessage("Item description is required")
            .MaximumLength(200)
            .WithMessage("Item description must not exceed 200 characters");

        RuleFor(x => x.ItemType)
            .IsInEnum()
            .WithMessage("Item type must be Quantity or Slot");

        // Quantity-based item rules
        When(x => x.ItemType == SignUpItemType.Quantity, () =>
        {
            RuleFor(x => x.TargetQuantity)
                .NotNull()
                .WithMessage("Target quantity is required for quantity-based items")
                .GreaterThan(0)
                .WithMessage("Target quantity must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Target quantity cannot exceed 1000");

            RuleFor(x => x.AvailableSlots)
                .Null()
                .WithMessage("Available slots must not be provided for quantity-based items");
        });

        // Slot-based item rules
        When(x => x.ItemType == SignUpItemType.Slot, () =>
        {
            RuleFor(x => x.AvailableSlots)
                .NotNull()
                .WithMessage("Available slots is required for slot-based items")
                .GreaterThan(0)
                .WithMessage("Available slots must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Available slots cannot exceed 100");

            RuleFor(x => x.TargetQuantity)
                .Null()
                .WithMessage("Target quantity must not be provided for slot-based items");

            When(x => x.SuggestedPerSlot.HasValue, () =>
            {
                RuleFor(x => x.SuggestedPerSlot)
                    .GreaterThan(0)
                    .WithMessage("Suggested quantity per slot must be greater than 0")
                    .LessThanOrEqualTo(1000)
                    .WithMessage("Suggested quantity per slot cannot exceed 1000");
            });
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes must not exceed 500 characters");
    }
}

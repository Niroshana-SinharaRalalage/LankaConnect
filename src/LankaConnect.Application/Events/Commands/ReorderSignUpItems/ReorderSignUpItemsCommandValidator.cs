using FluentValidation;

namespace LankaConnect.Application.Events.Commands.ReorderSignUpItems;

/// <summary>
/// Phase 6A.132: Surface-level validation for ReorderSignUpItemsCommand.
/// Structural checks only — the domain aggregate owns the authoritative set-equality rule
/// (exact match against current item IDs) because the validator has no DB access.
/// </summary>
public class ReorderSignUpItemsCommandValidator : AbstractValidator<ReorderSignUpItemsCommand>
{
    public ReorderSignUpItemsCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.SignUpListId)
            .NotEmpty()
            .WithMessage("Sign-up list ID is required");

        RuleFor(x => x.OrderedItemIds)
            .NotNull()
            .WithMessage("Ordered item IDs are required")
            .Must(ids => ids != null && ids.Count > 0)
            .WithMessage("Ordered item IDs cannot be empty")
            .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
            .WithMessage("Ordered item IDs must all be non-empty GUIDs")
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Ordered item IDs must not contain duplicates");
    }
}

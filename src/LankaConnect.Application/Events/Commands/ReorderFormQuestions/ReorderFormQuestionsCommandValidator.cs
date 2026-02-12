using FluentValidation;

namespace LankaConnect.Application.Events.Commands.ReorderFormQuestions;

public class ReorderFormQuestionsCommandValidator : AbstractValidator<ReorderFormQuestionsCommand>
{
    public ReorderFormQuestionsCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.FormId)
            .NotEmpty()
            .WithMessage("Form ID is required");

        RuleFor(x => x.QuestionIdsInOrder)
            .NotEmpty()
            .WithMessage("Question IDs list is required");

        RuleFor(x => x.QuestionIdsInOrder)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Question IDs must not contain duplicates")
            .When(x => x.QuestionIdsInOrder != null && x.QuestionIdsInOrder.Count > 0);
    }
}

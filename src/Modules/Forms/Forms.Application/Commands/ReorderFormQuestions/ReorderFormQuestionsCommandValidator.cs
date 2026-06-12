using FluentValidation;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands.ReorderFormQuestions;

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

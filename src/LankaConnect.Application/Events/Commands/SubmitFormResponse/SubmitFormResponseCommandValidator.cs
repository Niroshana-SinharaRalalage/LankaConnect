using FluentValidation;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Application.Events.Commands.SubmitFormResponse;

public class SubmitFormResponseCommandValidator : AbstractValidator<SubmitFormResponseCommand>
{
    public SubmitFormResponseCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.FormId)
            .NotEmpty()
            .WithMessage("Form ID is required");

        RuleFor(x => x.RespondentEmail)
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters")
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.RespondentEmail))
            .WithMessage("Invalid email format");

        RuleFor(x => x.RespondentName)
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Answers)
            .NotNull()
            .WithMessage("Answers list is required");

        RuleForEach(x => x.Answers)
            .ChildRules(answer =>
            {
                answer.RuleFor(a => a.QuestionId)
                    .NotEmpty()
                    .WithMessage("Question ID is required for each answer");
            })
            .When(x => x.Answers != null);
    }
}

using FluentValidation;

namespace LankaConnect.Application.Communications.Queries.GetEmailTemplateById;

/// <summary>
/// Phase 6A.89: Validator for GetEmailTemplateByIdQuery
/// </summary>
public class GetEmailTemplateByIdQueryValidator : AbstractValidator<GetEmailTemplateByIdQuery>
{
    public GetEmailTemplateByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Template ID is required");
    }
}

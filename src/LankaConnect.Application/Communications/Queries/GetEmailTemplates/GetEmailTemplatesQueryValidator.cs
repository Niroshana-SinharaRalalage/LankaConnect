using FluentValidation;

namespace LankaConnect.Application.Communications.Queries.GetEmailTemplates;

/// <summary>
/// Validator for GetEmailTemplatesQuery
/// </summary>
public class GetEmailTemplatesQueryValidator : AbstractValidator<GetEmailTemplatesQuery>
{
    public GetEmailTemplatesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Category)
            .Must(category => category == null || IsValidCategory(category))
            .WithMessage("Category must be a valid template category if provided");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
    }

    private static bool IsValidCategory(LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory? category)
    {
        return category != null && LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory.All.Contains(category);
    }
}
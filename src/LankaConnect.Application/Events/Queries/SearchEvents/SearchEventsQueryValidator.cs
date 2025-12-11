using FluentValidation;

namespace LankaConnect.Application.Events.Queries.SearchEvents;

/// <summary>
/// Validator for SearchEventsQuery
/// Ensures search parameters are valid before execution
/// </summary>
public class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    public SearchEventsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("Search term is required")
            .MaximumLength(500)
            .WithMessage("Search term cannot exceed 500 characters")
            .Must(BeValidSearchTerm)
            .WithMessage("Search term contains invalid characters");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.StartDateFrom)
            .Must(date => !date.HasValue || date.Value >= DateTime.UtcNow.Date)
            .WithMessage("Start date filter must be in the future");
    }

    private bool BeValidSearchTerm(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        // Reject search terms with only special characters or stop words
        var normalizedTerm = searchTerm.Trim();

        // Check for excessive special characters (potential SQL injection attempt)
        var specialCharCount = normalizedTerm.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        if (specialCharCount > normalizedTerm.Length / 2)
            return false;

        return true;
    }
}

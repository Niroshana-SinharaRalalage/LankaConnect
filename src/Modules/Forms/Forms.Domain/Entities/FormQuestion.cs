using LankaConnect.Domain.Common;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Modules.Forms.Domain.Entities;

/// <summary>
/// Represents a question within a custom event form.
/// Child entity of EventForm aggregate root.
/// Supports various question types (text, choice, number, date, yes/no).
/// Options are stored as JSONB for choice-type questions (SingleChoice, MultipleChoice, Dropdown).
/// </summary>
public class FormQuestion : LegacyBaseEntity
{
    public const int MaxQuestionTextLength = 500;
    public const int MaxHelpTextLength = 300;
    public const int MaxOptionsCount = 50;

    private readonly List<QuestionOption> _options = new();

    public Guid EventFormId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public FormQuestionType QuestionType { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public string? HelpText { get; private set; }

    /// <summary>
    /// Options for choice-type questions (SingleChoice, MultipleChoice, Dropdown).
    /// Stored as JSONB in the database.
    /// </summary>
    public IReadOnlyList<QuestionOption> Options => _options.AsReadOnly();

    // EF Core constructor
    private FormQuestion()
    {
    }

    private FormQuestion(
        Guid eventFormId,
        string questionText,
        FormQuestionType questionType,
        bool isRequired,
        int sortOrder,
        string? helpText,
        List<QuestionOption> options)
    {
        EventFormId = eventFormId;
        QuestionText = questionText;
        QuestionType = questionType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        HelpText = helpText;
        _options = options;
    }

    /// <summary>
    /// Creates a new FormQuestion with validation.
    /// Options are required for choice-type questions and forbidden for others.
    /// </summary>
    public static Result<FormQuestion> Create(
        Guid eventFormId,
        string questionText,
        FormQuestionType questionType,
        bool isRequired,
        int sortOrder,
        IEnumerable<QuestionOption>? options = null,
        string? helpText = null)
    {
        if (eventFormId == Guid.Empty)
            return Result<FormQuestion>.Failure("Event form ID cannot be empty");

        if (string.IsNullOrWhiteSpace(questionText))
            return Result<FormQuestion>.Failure("Question text cannot be empty");

        questionText = questionText.Trim();

        if (questionText.Length > MaxQuestionTextLength)
            return Result<FormQuestion>.Failure($"Question text cannot exceed {MaxQuestionTextLength} characters");

        if (sortOrder < 0)
            return Result<FormQuestion>.Failure("Sort order cannot be negative");

        if (helpText != null)
        {
            helpText = helpText.Trim();
            if (helpText.Length > MaxHelpTextLength)
                return Result<FormQuestion>.Failure($"Help text cannot exceed {MaxHelpTextLength} characters");
            if (string.IsNullOrWhiteSpace(helpText))
                helpText = null;
        }

        var optionsList = options?.ToList() ?? new List<QuestionOption>();

        // Validate options based on question type
        var validationResult = ValidateOptionsForType(questionType, optionsList);
        if (validationResult.IsFailure)
            return Result<FormQuestion>.Failure(validationResult.Error);

        return Result<FormQuestion>.Success(new FormQuestion(
            eventFormId, questionText, questionType, isRequired, sortOrder, helpText, optionsList));
    }

    /// <summary>
    /// Updates question details. Safe to call even when responses exist (text/help/sort changes allowed).
    /// Changing question type or removing selected options is handled at the EventForm aggregate level.
    /// </summary>
    public Result UpdateDetails(
        string questionText,
        FormQuestionType questionType,
        bool isRequired,
        int sortOrder,
        IEnumerable<QuestionOption>? options = null,
        string? helpText = null)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            return Result.Failure("Question text cannot be empty");

        questionText = questionText.Trim();

        if (questionText.Length > MaxQuestionTextLength)
            return Result.Failure($"Question text cannot exceed {MaxQuestionTextLength} characters");

        if (sortOrder < 0)
            return Result.Failure("Sort order cannot be negative");

        if (helpText != null)
        {
            helpText = helpText.Trim();
            if (helpText.Length > MaxHelpTextLength)
                return Result.Failure($"Help text cannot exceed {MaxHelpTextLength} characters");
            if (string.IsNullOrWhiteSpace(helpText))
                helpText = null;
        }

        var optionsList = options?.ToList() ?? new List<QuestionOption>();

        var validationResult = ValidateOptionsForType(questionType, optionsList);
        if (validationResult.IsFailure)
            return validationResult;

        QuestionText = questionText;
        QuestionType = questionType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        HelpText = helpText;
        _options.Clear();
        _options.AddRange(optionsList);

        return Result.Success();
    }

    /// <summary>
    /// Updates just the sort order (used during reordering).
    /// </summary>
    public void UpdateSortOrder(int newSortOrder)
    {
        SortOrder = newSortOrder;
    }

    /// <summary>
    /// Returns true if this question type requires options (choice-based questions).
    /// </summary>
    public bool RequiresOptions() => IsChoiceType(QuestionType);

    /// <summary>
    /// Gets a specific option by its Id.
    /// </summary>
    public QuestionOption? GetOption(Guid optionId) => _options.FirstOrDefault(o => o.Id == optionId);

    private static bool IsChoiceType(FormQuestionType type) =>
        type is FormQuestionType.SingleChoice or FormQuestionType.MultipleChoice or FormQuestionType.Dropdown;

    private static Result ValidateOptionsForType(FormQuestionType type, List<QuestionOption> options)
    {
        if (IsChoiceType(type))
        {
            if (options.Count < 2)
                return Result.Failure("Choice-type questions must have at least 2 options");

            if (options.Count > MaxOptionsCount)
                return Result.Failure($"Questions cannot have more than {MaxOptionsCount} options");

            // Check for duplicate option text
            var duplicateTexts = options
                .GroupBy(o => o.Text.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTexts.Any())
                return Result.Failure($"Duplicate option text found: {string.Join(", ", duplicateTexts)}");
        }
        else
        {
            if (options.Count > 0)
                return Result.Failure($"Options are not allowed for {type} question type");
        }

        return Result.Success();
    }
}

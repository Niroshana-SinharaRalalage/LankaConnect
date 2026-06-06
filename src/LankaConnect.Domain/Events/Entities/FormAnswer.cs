using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a single answer to a form question within a response.
/// Child entity of FormResponse aggregate root.
///
/// Stores both the answer value and a snapshot of the question text at submission time,
/// preserving what the respondent saw for accurate exports even if the question is later edited.
///
/// Answer format depends on question type:
/// - ShortText/LongText: TextValue
/// - SingleChoice/MultipleChoice/Dropdown: SelectedOptionIds + SelectedOptionTextSnapshots
/// - Number: TextValue (stored as string, validated as numeric)
/// - Date: TextValue (stored as ISO date string)
/// - YesNo: BooleanValue
/// </summary>
public class FormAnswer : LegacyBaseEntity
{
    public const int MaxTextValueLength = 5000;

    private readonly List<Guid> _selectedOptionIds = new();
    private readonly List<string> _selectedOptionTextSnapshots = new();

    public Guid FormResponseId { get; private set; }
    public Guid FormQuestionId { get; private set; }

    /// <summary>
    /// Snapshot of the question text at the time the answer was submitted.
    /// Ensures exports show the exact question the respondent saw.
    /// </summary>
    public string QuestionTextSnapshot { get; private set; } = string.Empty;

    /// <summary>
    /// Text-based answer value. Used for ShortText, LongText, Number, and Date question types.
    /// </summary>
    public string? TextValue { get; private set; }

    /// <summary>
    /// Selected option IDs for choice-type questions (SingleChoice, MultipleChoice, Dropdown).
    /// References QuestionOption.Id values stored on the FormQuestion.
    /// Stored as JSONB in the database.
    /// </summary>
    public IReadOnlyList<Guid> SelectedOptionIds => _selectedOptionIds.AsReadOnly();

    /// <summary>
    /// Snapshot of selected option texts at submission time.
    /// Ensures exports show the exact option text the respondent selected.
    /// Stored as JSONB in the database.
    /// </summary>
    public IReadOnlyList<string> SelectedOptionTextSnapshots => _selectedOptionTextSnapshots.AsReadOnly();

    /// <summary>
    /// Boolean answer for YesNo question type.
    /// </summary>
    public bool? BooleanValue { get; private set; }

    // EF Core constructor
    private FormAnswer()
    {
    }

    private FormAnswer(
        Guid formResponseId,
        Guid formQuestionId,
        string questionTextSnapshot,
        string? textValue,
        List<Guid>? selectedOptionIds,
        List<string>? selectedOptionTextSnapshots,
        bool? booleanValue)
    {
        FormResponseId = formResponseId;
        FormQuestionId = formQuestionId;
        QuestionTextSnapshot = questionTextSnapshot;
        TextValue = textValue;
        if (selectedOptionIds != null) _selectedOptionIds.AddRange(selectedOptionIds);
        if (selectedOptionTextSnapshots != null) _selectedOptionTextSnapshots.AddRange(selectedOptionTextSnapshots);
        BooleanValue = booleanValue;
    }

    /// <summary>
    /// Creates a new FormAnswer. Validation of answer format against question type
    /// is done at the FormResponse aggregate level.
    /// </summary>
    public static Result<FormAnswer> Create(
        Guid formResponseId,
        Guid formQuestionId,
        string questionTextSnapshot,
        string? textValue = null,
        List<Guid>? selectedOptionIds = null,
        List<string>? selectedOptionTextSnapshots = null,
        bool? booleanValue = null)
    {
        if (formResponseId == Guid.Empty)
            return Result<FormAnswer>.Failure("Form response ID cannot be empty");

        if (formQuestionId == Guid.Empty)
            return Result<FormAnswer>.Failure("Form question ID cannot be empty");

        if (string.IsNullOrWhiteSpace(questionTextSnapshot))
            return Result<FormAnswer>.Failure("Question text snapshot cannot be empty");

        if (textValue != null && textValue.Length > MaxTextValueLength)
            return Result<FormAnswer>.Failure($"Text value cannot exceed {MaxTextValueLength} characters");

        return Result<FormAnswer>.Success(new FormAnswer(
            formResponseId,
            formQuestionId,
            questionTextSnapshot,
            textValue,
            selectedOptionIds,
            selectedOptionTextSnapshots,
            booleanValue));
    }

    /// <summary>
    /// Updates the answer values (for editing a response before deadline).
    /// </summary>
    public Result Update(
        string? textValue,
        List<Guid>? selectedOptionIds,
        List<string>? selectedOptionTextSnapshots,
        bool? booleanValue)
    {
        if (textValue != null && textValue.Length > MaxTextValueLength)
            return Result.Failure($"Text value cannot exceed {MaxTextValueLength} characters");

        TextValue = textValue;

        _selectedOptionIds.Clear();
        if (selectedOptionIds != null) _selectedOptionIds.AddRange(selectedOptionIds);

        _selectedOptionTextSnapshots.Clear();
        if (selectedOptionTextSnapshots != null) _selectedOptionTextSnapshots.AddRange(selectedOptionTextSnapshots);

        BooleanValue = booleanValue;

        return Result.Success();
    }
}

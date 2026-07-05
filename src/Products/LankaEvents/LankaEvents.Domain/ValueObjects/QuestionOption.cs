using System.Text.Json.Serialization;
using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Represents a selectable option for choice-type form questions (SingleChoice, MultipleChoice, Dropdown).
/// Stored as JSONB in the database on the FormQuestion entity.
/// Uses a stable Guid Id so that answers reference options by ID, not by index position.
/// This prevents data corruption when options are reordered or edited.
/// </summary>
public class QuestionOption : ValueObject
{
    public const int MaxTextLength = 500;

    /// <summary>
    /// Stable identifier for this option. Used by FormAnswer.SelectedOptionIds to reference selected options.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display text for this option.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Display order (0-based) within the question's option list.
    /// </summary>
    public int SortOrder { get; init; }

    // Private constructor for EF Core / JSON deserialization
    [JsonConstructor]
    private QuestionOption() { }

    private QuestionOption(Guid id, string text, int sortOrder)
    {
        Id = id;
        Text = text;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Creates a new QuestionOption with a generated Id.
    /// </summary>
    public static Result<QuestionOption> Create(string text, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<QuestionOption>.Failure("Option text cannot be empty");

        text = text.Trim();

        if (text.Length > MaxTextLength)
            return Result<QuestionOption>.Failure($"Option text cannot exceed {MaxTextLength} characters");

        if (sortOrder < 0)
            return Result<QuestionOption>.Failure("Sort order cannot be negative");

        return Result<QuestionOption>.Success(new QuestionOption(Guid.NewGuid(), text, sortOrder));
    }

    /// <summary>
    /// Creates a QuestionOption with a specific Id (for rebuilding from persistence).
    /// </summary>
    public static Result<QuestionOption> CreateWithId(Guid id, string text, int sortOrder)
    {
        if (id == Guid.Empty)
            return Result<QuestionOption>.Failure("Option Id cannot be empty");

        if (string.IsNullOrWhiteSpace(text))
            return Result<QuestionOption>.Failure("Option text cannot be empty");

        text = text.Trim();

        if (text.Length > MaxTextLength)
            return Result<QuestionOption>.Failure($"Option text cannot exceed {MaxTextLength} characters");

        if (sortOrder < 0)
            return Result<QuestionOption>.Failure("Sort order cannot be negative");

        return Result<QuestionOption>.Success(new QuestionOption(id, text, sortOrder));
    }

    public override string ToString() => $"{SortOrder}: {Text}";

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Text;
        yield return SortOrder;
    }
}

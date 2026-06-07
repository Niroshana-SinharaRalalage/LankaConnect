namespace LankaConnect.Modules.Forms.Domain.Enums;

/// <summary>
/// Types of questions available for custom event forms.
/// Each type determines how the question is rendered and what answer format is expected.
/// </summary>
public enum FormQuestionType
{
    /// <summary>
    /// Single-line text input (max 500 characters)
    /// Example: "What is your name?"
    /// </summary>
    ShortText = 0,

    /// <summary>
    /// Multi-line text area (max 5000 characters)
    /// Example: "Please describe your dietary requirements"
    /// </summary>
    LongText = 1,

    /// <summary>
    /// Radio buttons - select exactly one option
    /// Example: "Which session will you attend?" with options ["Morning", "Afternoon", "Evening"]
    /// </summary>
    SingleChoice = 2,

    /// <summary>
    /// Checkboxes - select one or more options
    /// Example: "What skills can you contribute?" with options ["Cooking", "Setup", "Cleanup"]
    /// </summary>
    MultipleChoice = 3,

    /// <summary>
    /// Dropdown select - select exactly one option
    /// Example: "Select your t-shirt size" with options ["S", "M", "L", "XL"]
    /// </summary>
    Dropdown = 4,

    /// <summary>
    /// Numeric input
    /// Example: "How many guests will you bring?"
    /// </summary>
    Number = 5,

    /// <summary>
    /// Date picker input
    /// Example: "What date are you available?"
    /// </summary>
    Date = 6,

    /// <summary>
    /// Yes/No toggle or radio buttons
    /// Example: "Will you need parking?"
    /// </summary>
    YesNo = 7
}

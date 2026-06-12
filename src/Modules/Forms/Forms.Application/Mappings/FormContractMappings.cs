using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;

namespace LankaConnect.Modules.Forms.Application.Mappings;

/// <summary>
/// 1:1 Domain → Contracts mapping helpers. Wave 5.3b (2026-06-11).
/// Keeps Contracts DTOs decoupled from Domain types so cross-module
/// consumers do not pull Forms.Domain assembly references (enforced by
/// the Wave 5.3a ArchTest rule).
/// </summary>
internal static class FormContractMappings
{
    public static FormOwnerEntityTypeDto ToContractDto(this FormOwnerEntityType source) => source switch
    {
        FormOwnerEntityType.Event => FormOwnerEntityTypeDto.Event,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source,
            $"Unknown FormOwnerEntityType '{source}' — Forms.Contracts mirror requires an update."),
    };

    public static FormStatusDto ToContractDto(this FormStatus source) => source switch
    {
        FormStatus.Draft => FormStatusDto.Draft,
        FormStatus.Active => FormStatusDto.Active,
        FormStatus.Closed => FormStatusDto.Closed,
        FormStatus.Archived => FormStatusDto.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source,
            $"Unknown FormStatus '{source}' — Forms.Contracts mirror requires an update."),
    };

    public static FormQuestionTypeDto ToContractDto(this FormQuestionType source) => source switch
    {
        FormQuestionType.ShortText => FormQuestionTypeDto.ShortText,
        FormQuestionType.LongText => FormQuestionTypeDto.LongText,
        FormQuestionType.SingleChoice => FormQuestionTypeDto.SingleChoice,
        FormQuestionType.MultipleChoice => FormQuestionTypeDto.MultipleChoice,
        FormQuestionType.Dropdown => FormQuestionTypeDto.Dropdown,
        FormQuestionType.Number => FormQuestionTypeDto.Number,
        FormQuestionType.Date => FormQuestionTypeDto.Date,
        FormQuestionType.YesNo => FormQuestionTypeDto.YesNo,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source,
            $"Unknown FormQuestionType '{source}' — Forms.Contracts mirror requires an update."),
    };

    public static FormSummaryDto ToSummaryDto(this Form form, int responseCount) => new(
        Id: form.Id,
        OwnerEntityType: form.OwnerEntityType.ToContractDto(),
        OwnerEntityId: form.OwnerEntityId,
        Title: form.Title,
        Description: form.Description,
        Status: form.Status.ToContractDto(),
        AllowMultipleResponses: form.AllowMultipleResponses,
        ResponseDeadline: form.ResponseDeadline,
        MaxResponses: form.MaxResponses,
        HasResponses: form.HasResponses,
        QuestionCount: form.Questions.Count,
        ResponseCount: responseCount,
        CreatedAt: form.CreatedAt,
        UpdatedAt: form.UpdatedAt,
        AllowAttendeesToViewResponses: form.AllowAttendeesToViewResponses);

    public static FormDetailDto ToDetailDto(this Form form, int responseCount) => new(
        Id: form.Id,
        OwnerEntityType: form.OwnerEntityType.ToContractDto(),
        OwnerEntityId: form.OwnerEntityId,
        Title: form.Title,
        Description: form.Description,
        Status: form.Status.ToContractDto(),
        AllowMultipleResponses: form.AllowMultipleResponses,
        ResponseDeadline: form.ResponseDeadline,
        MaxResponses: form.MaxResponses,
        HasResponses: form.HasResponses,
        ResponseCount: responseCount,
        CreatedAt: form.CreatedAt,
        UpdatedAt: form.UpdatedAt,
        AllowAttendeesToViewResponses: form.AllowAttendeesToViewResponses,
        Questions: form.Questions.Select(q => q.ToContractDto()).ToList());

    public static FormQuestionContractDto ToContractDto(this FormQuestion question) => new(
        Id: question.Id,
        QuestionText: question.QuestionText,
        QuestionType: question.QuestionType.ToContractDto(),
        IsRequired: question.IsRequired,
        SortOrder: question.SortOrder,
        HelpText: question.HelpText,
        Options: question.Options
            .Select(o => new QuestionOptionContractDto(o.Id, o.Text, o.SortOrder))
            .ToList());

    public static FormResponseDetailDto ToContractDto(this FormResponse response) => new(
        Id: response.Id,
        FormId: response.EventFormId,
        EventId: response.EventId,
        RespondentEmail: response.RespondentEmail,
        RespondentName: response.RespondentName,
        RespondentUserId: response.RespondentUserId,
        SubmittedAt: response.SubmittedAt,
        UpdatedAt: response.UpdatedAt,
        Answers: response.Answers.Select(a => a.ToContractDto()).ToList());

    public static FormAnswerContractDto ToContractDto(this FormAnswer answer)
    {
        var answerText = ComposeAnswerText(answer);
        return new FormAnswerContractDto(
            Id: answer.Id,
            QuestionId: answer.FormQuestionId,
            QuestionText: answer.QuestionTextSnapshot,
            // The question type isn't directly stored on FormAnswer — Forms.Application
            // callers project this from the parent FormQuestion in 5.3c. For
            // standalone GetResponseWithAnswersAsync we don't have the question
            // type at hand without a join, so we surface ShortText as a safe
            // default; consumers that need typed display values fetch the
            // parent question separately.
            QuestionType: FormQuestionTypeDto.ShortText,
            AnswerText: answerText);
    }

    private static string ComposeAnswerText(FormAnswer answer)
    {
        if (answer.BooleanValue.HasValue)
        {
            return answer.BooleanValue.Value ? "Yes" : "No";
        }

        if (answer.SelectedOptionTextSnapshots.Count > 0)
        {
            return string.Join(", ", answer.SelectedOptionTextSnapshots);
        }

        return answer.TextValue ?? string.Empty;
    }
}

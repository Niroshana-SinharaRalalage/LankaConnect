using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Queries.GetFormResponses;

public class GetFormResponsesQueryHandler : IQueryHandler<GetFormResponsesQuery, FormResponsesPagedDto>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetFormResponsesQueryHandler> _logger;

    public GetFormResponsesQueryHandler(
        IFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        ILogger<GetFormResponsesQueryHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<FormResponsesPagedDto>> Handle(GetFormResponsesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetFormResponses"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetFormResponses START: FormId={FormId}, EventId={EventId}, Page={Page}, PageSize={PageSize}",
                request.FormId, request.EventId, request.Page, request.PageSize);

            try
            {
                // Verify form belongs to event
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetFormResponses FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result<FormResponsesPagedDto>.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetFormResponses FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<FormResponsesPagedDto>.Failure("Form does not belong to the specified event");
                }

                var (responses, totalCount) = await _formResponseRepository.GetPaginatedAsync(
                    request.FormId, request.Page, request.PageSize, cancellationToken);

                var dto = new FormResponsesPagedDto
                {
                    Responses = responses.Select(r => new FormResponseDto
                    {
                        Id = r.Id,
                        EventFormId = r.EventFormId,
                        RespondentName = r.RespondentName,
                        RespondentEmail = r.RespondentEmail,
                        RespondentUserId = r.RespondentUserId,
                        SubmittedAt = r.SubmittedAt,
                        Answers = r.Answers.Select(a => new FormAnswerDto
                        {
                            Id = a.Id,
                            FormQuestionId = a.FormQuestionId,
                            QuestionTextSnapshot = a.QuestionTextSnapshot,
                            TextValue = a.TextValue,
                            SelectedOptionIds = a.SelectedOptionIds.ToList(),
                            SelectedOptionTextSnapshots = a.SelectedOptionTextSnapshots.ToList(),
                            BooleanValue = a.BooleanValue
                        }).ToList()
                    }).ToList(),
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetFormResponses COMPLETE: FormId={FormId}, ResponseCount={ResponseCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    request.FormId, dto.Responses.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return Result<FormResponsesPagedDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetFormResponses FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}

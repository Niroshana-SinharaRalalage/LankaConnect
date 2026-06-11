using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventFormDetail;

public class GetEventFormDetailQueryHandler : IQueryHandler<GetEventFormDetailQuery, EventFormDetailDto>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetEventFormDetailQueryHandler> _logger;

    public GetEventFormDetailQueryHandler(
        IFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        ILogger<GetEventFormDetailQueryHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<EventFormDetailDto>> Handle(GetEventFormDetailQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventFormDetail"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventFormDetail START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetEventFormDetail FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result<EventFormDetailDto>.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetEventFormDetail FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<EventFormDetailDto>.Failure("Form does not belong to the specified event");
                }

                var responseCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, cancellationToken);

                var dto = new EventFormDetailDto
                {
                    Id = form.Id,
                    EventId = form.EventId,
                    Title = form.Title,
                    Description = form.Description,
                    Status = form.Status,
                    AllowMultipleResponses = form.AllowMultipleResponses,
                    ResponseDeadline = form.ResponseDeadline,
                    MaxResponses = form.MaxResponses,
                    HasResponses = form.HasResponses,
                    ResponseCount = responseCount,
                    CreatedAt = form.CreatedAt,
                    UpdatedAt = form.UpdatedAt,
                    AllowAttendeesToViewResponses = form.AllowAttendeesToViewResponses,  // Phase 6A.146
                    Questions = form.Questions
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new FormQuestionDto
                        {
                            Id = q.Id,
                            QuestionText = q.QuestionText,
                            QuestionType = q.QuestionType,
                            IsRequired = q.IsRequired,
                            SortOrder = q.SortOrder,
                            HelpText = q.HelpText,
                            Options = q.Options
                                .OrderBy(o => o.SortOrder)
                                .Select(o => new QuestionOptionDto
                                {
                                    Id = o.Id,
                                    Text = o.Text,
                                    SortOrder = o.SortOrder
                                })
                                .ToList()
                        })
                        .ToList()
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventFormDetail COMPLETE: FormId={FormId}, EventId={EventId}, QuestionCount={QuestionCount}, ResponseCount={ResponseCount}, Duration={ElapsedMs}ms",
                    form.Id, form.EventId, form.Questions.Count, responseCount, stopwatch.ElapsedMilliseconds);

                return Result<EventFormDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetEventFormDetail FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}

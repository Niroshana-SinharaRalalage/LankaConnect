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

namespace LankaConnect.Application.Events.Queries.GetEventForms;

public class GetEventFormsQueryHandler : IQueryHandler<GetEventFormsQuery, List<EventFormDto>>
{
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetEventFormsQueryHandler> _logger;

    public GetEventFormsQueryHandler(
        IEventFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        ILogger<GetEventFormsQueryHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<List<EventFormDto>>> Handle(GetEventFormsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventForms"))
        using (LogContext.PushProperty("EntityType", "EventForm"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventForms START: EventId={EventId}",
                request.EventId);

            try
            {
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetEventForms FAILED: Invalid EventId - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<List<EventFormDto>>.Failure("Event ID is required");
                }

                var forms = await _eventFormRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                var dtos = new List<EventFormDto>();
                foreach (var form in forms)
                {
                    var responseCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, cancellationToken);

                    dtos.Add(new EventFormDto
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
                        QuestionCount = form.Questions.Count,
                        ResponseCount = responseCount,
                        CreatedAt = form.CreatedAt,
                        UpdatedAt = form.UpdatedAt,
                        AllowAttendeesToViewResponses = form.AllowAttendeesToViewResponses  // Phase 6A.146
                    });
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventForms COMPLETE: EventId={EventId}, FormCount={FormCount}, Duration={ElapsedMs}ms",
                    request.EventId, dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<List<EventFormDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetEventForms FAILED: Exception - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}

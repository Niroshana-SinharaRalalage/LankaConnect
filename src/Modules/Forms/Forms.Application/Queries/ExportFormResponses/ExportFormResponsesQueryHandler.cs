using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Modules.Forms.Application.Mappings;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Queries.ExportFormResponses;

/// <summary>
/// Handles export of custom form responses to CSV or Excel format.
/// Phase 6A.110: Form response export functionality
/// </summary>
public class ExportFormResponsesQueryHandler
    : IQueryHandler<ExportFormResponsesQuery, ExportResult>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ICsvExportService _csvExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ExportFormResponsesQueryHandler> _logger;

    public ExportFormResponsesQueryHandler(
        IFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        ICsvExportService csvExportService,
        IExcelExportService excelExportService,
        ILogger<ExportFormResponsesQueryHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _csvExportService = csvExportService;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportFormResponsesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportFormResponses"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("ExportFormat", request.Format))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportFormResponses START: FormId={FormId}, EventId={EventId}, Format={Format}",
                request.FormId, request.EventId, request.Format);

            try
            {
                // 1. Get form with questions (for column headers)
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);
                if (form == null)
                {
                    _logger.LogWarning("Form {FormId} not found", request.FormId);
                    return Result<ExportResult>.Failure($"Form {request.FormId} not found");
                }

                // 2. Verify form belongs to event (security check)
                if (form.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "Form {FormId} does not belong to Event {EventId}. Actual EventId: {ActualEventId}",
                        request.FormId, request.EventId, form.EventId);
                    return Result<ExportResult>.Failure("Form does not belong to event");
                }

                // 3. Check response count (10,000 limit to prevent timeouts/memory issues)
                var totalCount = await _formResponseRepository.GetCountByFormIdAsync(
                    request.FormId,
                    cancellationToken);

                const int MAX_EXPORT_LIMIT = 10000;
                if (totalCount > MAX_EXPORT_LIMIT)
                {
                    _logger.LogWarning(
                        "Export limit exceeded: FormId={FormId}, ResponseCount={ResponseCount}, Limit={Limit}",
                        request.FormId, totalCount, MAX_EXPORT_LIMIT);

                    return Result<ExportResult>.Failure(
                        $"This form has too many responses for direct export ({totalCount} responses, " +
                        $"limit: {MAX_EXPORT_LIMIT}). Please contact support for assistance with large data exports.");
                }

                // 4. Get ALL responses with answers (no pagination for export)
                var (responses, _) = await _formResponseRepository.GetPaginatedAsync(
                    request.FormId,
                    page: 1,
                    pageSize: int.MaxValue,
                    cancellationToken);

                _logger.LogInformation(
                    "Retrieved {ResponseCount} responses for export",
                    totalCount);

                // 5. Build form DTO
                var formDto = new EventFormDetailDto
                {
                    Id = form.Id,
                    EventId = form.EventId,
                    Title = form.Title,
                    Description = form.Description,
                    Status = form.Status.ToContractDto(),
                    AllowMultipleResponses = form.AllowMultipleResponses,
                    ResponseDeadline = form.ResponseDeadline,
                    MaxResponses = form.MaxResponses,
                    HasResponses = form.HasResponses,
                    ResponseCount = totalCount,
                    CreatedAt = form.CreatedAt,
                    UpdatedAt = form.UpdatedAt,
                    Questions = form.Questions
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new FormQuestionDto
                        {
                            Id = q.Id,
                            QuestionText = q.QuestionText,
                            QuestionType = q.QuestionType.ToContractDto(),
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

                // 6. Build responses DTO
                var responsesDto = new FormResponsesPagedDto
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
                    Page = 1,
                    PageSize = totalCount
                };

                // 7. Generate export based on format
                byte[] fileContent;
                string fileName;
                string contentType;

                if (request.Format == ExportFormat.Excel)
                {
                    fileContent = _excelExportService.ExportFormResponses(formDto, responsesDto);
                    fileName = $"form-{request.FormId}-responses-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }
                else // CSV
                {
                    fileContent = _csvExportService.ExportFormResponses(formDto, responsesDto);
                    fileName = $"form-{request.FormId}-responses-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                    contentType = "text/csv";
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportFormResponses COMPLETE: FormId={FormId}, ResponseCount={ResponseCount}, " +
                    "FileName={FileName}, FileSize={FileSize} bytes, Duration={ElapsedMs}ms",
                    request.FormId, totalCount, fileName, fileContent.Length, stopwatch.ElapsedMilliseconds);

                // 8. Track telemetry (log slow exports for monitoring)
                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    _logger.LogWarning(
                        "SLOW EXPORT DETECTED: FormId={FormId}, ResponseCount={ResponseCount}, " +
                        "Duration={ElapsedMs}ms, FileSize={FileSize} bytes",
                        request.FormId, totalCount, stopwatch.ElapsedMilliseconds, fileContent.Length);
                }

                return Result<ExportResult>.Success(new ExportResult
                {
                    FileContent = fileContent,
                    FileName = fileName,
                    ContentType = contentType
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "ExportFormResponses FAILED: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

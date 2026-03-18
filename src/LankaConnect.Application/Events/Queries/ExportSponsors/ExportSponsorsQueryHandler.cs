using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventSponsors;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.ExportSponsors;

/// <summary>
/// Handles exporting sponsors to Excel or CSV.
/// Reuses GetEventSponsorsQuery internally to get data.
/// </summary>
public class ExportSponsorsQueryHandler : IQueryHandler<ExportSponsorsQuery, ExportResult>
{
    private readonly IMediator _mediator;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ExportSponsorsQueryHandler> _logger;

    public ExportSponsorsQueryHandler(
        IMediator mediator,
        IExcelExportService excelExportService,
        ICsvExportService csvExportService,
        ILogger<ExportSponsorsQueryHandler> logger)
    {
        _mediator = mediator;
        _excelExportService = excelExportService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportSponsorsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportSponsors"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportSponsors START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                var sponsorsResult = await _mediator.Send(
                    new GetEventSponsorsQuery(request.EventId), cancellationToken);

                if (sponsorsResult.IsFailure)
                    return Result<ExportResult>.Failure(sponsorsResult.Error);

                var sponsors = sponsorsResult.Value;

                byte[] fileContent;
                string fileName;
                string contentType;

                switch (request.Format)
                {
                    case ExportFormat.Excel:
                        fileContent = _excelExportService.ExportSponsors(sponsors);
                        fileName = $"sponsors_{request.EventId:N}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case ExportFormat.Csv:
                        fileContent = _csvExportService.ExportSponsors(sponsors);
                        fileName = $"sponsors_{request.EventId:N}.csv";
                        contentType = "text/csv";
                        break;

                    default:
                        return Result<ExportResult>.Failure($"Export format '{request.Format}' is not supported for sponsors. Use Excel or Csv.");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportSponsors COMPLETE: EventId={EventId}, Format={Format}, FileSize={FileSize}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, fileContent.Length, stopwatch.ElapsedMilliseconds);

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

                _logger.LogError(ex,
                    "ExportSponsors FAILED: EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds);

                return Result<ExportResult>.Failure($"Failed to export sponsors: {ex.Message}");
            }
        }
    }
}

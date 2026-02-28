using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventDonations;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.ExportDonations;

/// <summary>
/// Handles exporting donations to Excel or CSV.
/// Reuses GetEventDonationsQuery internally to get data.
/// </summary>
public class ExportDonationsQueryHandler : IQueryHandler<ExportDonationsQuery, ExportResult>
{
    private readonly IMediator _mediator;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ExportDonationsQueryHandler> _logger;

    public ExportDonationsQueryHandler(
        IMediator mediator,
        IExcelExportService excelExportService,
        ICsvExportService csvExportService,
        ILogger<ExportDonationsQueryHandler> logger)
    {
        _mediator = mediator;
        _excelExportService = excelExportService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportDonationsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportDonations"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportDonations START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                // Get donation data via query
                var donationsResult = await _mediator.Send(
                    new GetEventDonationsQuery(request.EventId), cancellationToken);

                if (donationsResult.IsFailure)
                    return Result<ExportResult>.Failure(donationsResult.Error);

                var donations = donationsResult.Value;

                byte[] fileContent;
                string fileName;
                string contentType;

                switch (request.Format)
                {
                    case ExportFormat.Excel:
                        fileContent = _excelExportService.ExportDonations(donations);
                        fileName = $"donations_{request.EventId:N}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case ExportFormat.Csv:
                        fileContent = _csvExportService.ExportDonations(donations);
                        fileName = $"donations_{request.EventId:N}.csv";
                        contentType = "text/csv";
                        break;

                    default:
                        return Result<ExportResult>.Failure($"Export format '{request.Format}' is not supported for donations. Use Excel or Csv.");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportDonations COMPLETE: EventId={EventId}, Format={Format}, FileSize={FileSize}, Duration={ElapsedMs}ms",
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
                    "ExportDonations FAILED: EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds);

                return Result<ExportResult>.Failure($"Failed to export donations: {ex.Message}");
            }
        }
    }
}

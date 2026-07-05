using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventCollections;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportCollections;

/// <summary>
/// Handles exporting collections (event fund contributions) to Excel or CSV.
/// Reuses GetEventCollectionsQuery internally to get data.
/// </summary>
public class ExportCollectionsQueryHandler : IQueryHandler<ExportCollectionsQuery, ExportResult>
{
    private readonly IMediator _mediator;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ExportCollectionsQueryHandler> _logger;

    public ExportCollectionsQueryHandler(
        IMediator mediator,
        IExcelExportService excelExportService,
        ICsvExportService csvExportService,
        ILogger<ExportCollectionsQueryHandler> logger)
    {
        _mediator = mediator;
        _excelExportService = excelExportService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportCollectionsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportCollections"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportCollections START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                var collectionsResult = await _mediator.Send(
                    new GetEventCollectionsQuery(request.EventId), cancellationToken);

                if (collectionsResult.IsFailure)
                    return Result<ExportResult>.Failure(collectionsResult.Error);

                var collections = collectionsResult.Value;

                byte[] fileContent;
                string fileName;
                string contentType;

                switch (request.Format)
                {
                    case ExportFormat.Excel:
                        fileContent = _excelExportService.ExportCollections(collections);
                        fileName = $"collections_{request.EventId:N}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case ExportFormat.Csv:
                        fileContent = _csvExportService.ExportCollections(collections);
                        fileName = $"collections_{request.EventId:N}.csv";
                        contentType = "text/csv";
                        break;

                    default:
                        return Result<ExportResult>.Failure($"Export format '{request.Format}' is not supported for collections. Use Excel or Csv.");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportCollections COMPLETE: EventId={EventId}, Format={Format}, FileSize={FileSize}, Duration={ElapsedMs}ms",
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
                    "ExportCollections FAILED: EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds);

                return Result<ExportResult>.Failure($"Failed to export collections: {ex.Message}");
            }
        }
    }
}

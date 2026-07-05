using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventAddOnPurchases;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportAddOnPurchases;

/// <summary>
/// Handles exporting add-on purchases to Excel or CSV.
/// Reuses GetEventAddOnPurchasesQuery internally to get data.
/// </summary>
public class ExportAddOnPurchasesQueryHandler : IQueryHandler<ExportAddOnPurchasesQuery, ExportResult>
{
    private readonly IMediator _mediator;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ExportAddOnPurchasesQueryHandler> _logger;

    public ExportAddOnPurchasesQueryHandler(
        IMediator mediator,
        IExcelExportService excelExportService,
        ICsvExportService csvExportService,
        ILogger<ExportAddOnPurchasesQueryHandler> logger)
    {
        _mediator = mediator;
        _excelExportService = excelExportService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportAddOnPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportAddOnPurchases"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportAddOnPurchases START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                var purchasesResult = await _mediator.Send(
                    new GetEventAddOnPurchasesQuery(request.EventId), cancellationToken);

                if (purchasesResult.IsFailure)
                    return Result<ExportResult>.Failure(purchasesResult.Error);

                var purchases = purchasesResult.Value;

                byte[] fileContent;
                string fileName;
                string contentType;

                switch (request.Format)
                {
                    case ExportFormat.Excel:
                        fileContent = _excelExportService.ExportAddOnPurchases(purchases);
                        fileName = $"addon_purchases_{request.EventId:N}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case ExportFormat.Csv:
                        fileContent = _csvExportService.ExportAddOnPurchases(purchases);
                        fileName = $"addon_purchases_{request.EventId:N}.csv";
                        contentType = "text/csv";
                        break;

                    default:
                        return Result<ExportResult>.Failure($"Export format '{request.Format}' is not supported for add-on purchases. Use Excel or Csv.");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportAddOnPurchases COMPLETE: EventId={EventId}, Format={Format}, FileSize={FileSize}, Duration={ElapsedMs}ms",
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
                    "ExportAddOnPurchases FAILED: EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds);

                return Result<ExportResult>.Failure($"Failed to export add-on purchases: {ex.Message}");
            }
        }
    }
}

using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventDonations;
using LankaConnect.Application.Events.Queries.GetEventCollections;
using LankaConnect.Application.Events.Queries.GetEventSponsors;
using LankaConnect.Application.Events.Queries.GetEventAddOnPurchases;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.ExportAllFinancials;

/// <summary>
/// Handles exporting all financial data for an event.
/// Fetches attendees, donations, collections, sponsors, and add-on purchases in parallel,
/// then delegates to the export service for multi-sheet Excel or ZIP'd CSV.
/// </summary>
public class ExportAllFinancialsQueryHandler : IQueryHandler<ExportAllFinancialsQuery, ExportResult>
{
    private readonly IMediator _mediator;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ExportAllFinancialsQueryHandler> _logger;

    public ExportAllFinancialsQueryHandler(
        IMediator mediator,
        IExcelExportService excelExportService,
        ICsvExportService csvExportService,
        ILogger<ExportAllFinancialsQueryHandler> logger)
    {
        _mediator = mediator;
        _excelExportService = excelExportService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportAllFinancialsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportAllFinancials"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportAllFinancials START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                // Fetch all 5 data sources sequentially (DbContext is not thread-safe)
                var attendeesResult = await _mediator.Send(
                    new GetEventAttendeesQuery(request.EventId), cancellationToken);
                var donationsResult = await _mediator.Send(
                    new GetEventDonationsQuery(request.EventId), cancellationToken);
                var collectionsResult = await _mediator.Send(
                    new GetEventCollectionsQuery(request.EventId), cancellationToken);
                var sponsorsResult = await _mediator.Send(
                    new GetEventSponsorsQuery(request.EventId), cancellationToken);
                var addOnsResult = await _mediator.Send(
                    new GetEventAddOnPurchasesQuery(request.EventId), cancellationToken);

                // Check for failures
                if (attendeesResult.IsFailure)
                    return Result<ExportResult>.Failure($"Failed to load attendees: {attendeesResult.Error}");
                if (donationsResult.IsFailure)
                    return Result<ExportResult>.Failure($"Failed to load donations: {donationsResult.Error}");
                if (collectionsResult.IsFailure)
                    return Result<ExportResult>.Failure($"Failed to load collections: {collectionsResult.Error}");
                if (sponsorsResult.IsFailure)
                    return Result<ExportResult>.Failure($"Failed to load sponsors: {sponsorsResult.Error}");
                if (addOnsResult.IsFailure)
                    return Result<ExportResult>.Failure($"Failed to load add-on purchases: {addOnsResult.Error}");

                var allData = new AllFinancialsData
                {
                    Attendees = attendeesResult.Value,
                    Donations = donationsResult.Value,
                    Collections = collectionsResult.Value,
                    Sponsors = sponsorsResult.Value,
                    AddOnPurchases = addOnsResult.Value
                };

                byte[] fileContent;
                string fileName;
                string contentType;

                switch (request.Format)
                {
                    case ExportFormat.Excel:
                        fileContent = _excelExportService.ExportAllFinancials(allData);
                        fileName = $"all_financials_{request.EventId:N}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case ExportFormat.Csv:
                        fileContent = _csvExportService.ExportAllFinancialsZip(allData);
                        fileName = $"all_financials_{request.EventId:N}.zip";
                        contentType = "application/zip";
                        break;

                    default:
                        return Result<ExportResult>.Failure($"Export format '{request.Format}' is not supported. Use Excel or Csv.");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportAllFinancials COMPLETE: EventId={EventId}, Format={Format}, FileSize={FileSize}, Duration={ElapsedMs}ms",
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
                    "ExportAllFinancials FAILED: EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds);

                return Result<ExportResult>.Failure($"Failed to export all financials: {ex.Message}");
            }
        }
    }
}

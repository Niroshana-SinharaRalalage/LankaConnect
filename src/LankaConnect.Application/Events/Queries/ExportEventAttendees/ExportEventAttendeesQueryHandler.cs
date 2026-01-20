using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEventAttendees;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.ExportEventAttendees;

public class ExportEventAttendeesQueryHandler
    : IQueryHandler<ExportEventAttendeesQuery, ExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly IExcelExportService _excelService;
    private readonly ICsvExportService _csvService;
    private readonly IOptions<CommissionSettings> _commissionSettings;
    private readonly ILogger<GetEventAttendeesQueryHandler> _attendeesQueryLogger;
    private readonly ILogger<ExportEventAttendeesQueryHandler> _logger;

    public ExportEventAttendeesQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IRevenueCalculatorService revenueCalculatorService,
        IExcelExportService excelService,
        ICsvExportService csvService,
        IOptions<CommissionSettings> commissionSettings,
        ILogger<GetEventAttendeesQueryHandler> attendeesQueryLogger,
        ILogger<ExportEventAttendeesQueryHandler> logger)
    {
        _context = context;
        _eventRepository = eventRepository;
        _revenueCalculatorService = revenueCalculatorService;
        _excelService = excelService;
        _csvService = csvService;
        _commissionSettings = commissionSettings;
        _attendeesQueryLogger = attendeesQueryLogger;
        _logger = logger;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportEventAttendeesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ExportEventAttendees"))
        using (LogContext.PushProperty("EntityType", "Export"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ExportEventAttendees START: EventId={EventId}, Format={Format}",
                request.EventId, request.Format);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ExportEventAttendees FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<ExportResult>.Failure("Event ID is required");
                }

                // Get attendees data using existing query handler logic
                var attendeesQuery = new GetEventAttendeesQuery(request.EventId);
                var attendeesHandler = new GetEventAttendeesQueryHandler(
                    _context,
                    _eventRepository,
                    _revenueCalculatorService,
                    _commissionSettings,
                    _attendeesQueryLogger);
                var attendeesResult = await attendeesHandler.Handle(attendeesQuery, cancellationToken);

                if (!attendeesResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ExportEventAttendees FAILED: GetEventAttendees failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, attendeesResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<ExportResult>.Failure(attendeesResult.Error);
                }

                _logger.LogInformation(
                    "ExportEventAttendees: Attendees data loaded - EventId={EventId}, AttendeeCount={AttendeeCount}",
                    request.EventId, attendeesResult.Value?.Attendees?.Count ?? 0);

        var attendeesResponse = attendeesResult.Value!;

        // Get signup lists for Excel multi-sheet export
        List<SignUpListDto>? signUpListDtos = null;

        if (request.Format == ExportFormat.Excel)
        {
            // Get event with sign up lists
            var eventWithSignUps = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (eventWithSignUps != null && eventWithSignUps.SignUpLists.Any())
            {
                signUpListDtos = eventWithSignUps.SignUpLists.Select(s => new SignUpListDto
            {
                Id = s.Id,
                Category = s.Category,
                Description = s.Description ?? string.Empty,
                HasMandatoryItems = s.HasMandatoryItems,
                HasPreferredItems = s.HasPreferredItems,
                HasSuggestedItems = s.HasSuggestedItems,
                HasOpenItems = s.HasOpenItems,
                Items = s.Items.Select(i => new SignUpItemDto
                {
                    Id = i.Id,
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    RemainingQuantity = i.RemainingQuantity,
                    ItemCategory = i.ItemCategory,
                    CreatedByUserId = i.CreatedByUserId,
                    Commitments = i.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription ?? string.Empty,
                        Quantity = c.Quantity,
                        ContactName = c.ContactName,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone,
                        CommittedAt = c.CommittedAt
                    }).ToList()
                }).ToList()
            }).ToList();
            }
        }

        // Generate export based on format
        byte[] fileContent;
        string fileName;
        string contentType;

        // Phase 6A.69: Handle SignUpListsZip format (ZIP archive with multiple CSVs)
        // Phase 6A.73: Handle SignUpListsExcel format (Excel file with category sheets)
        if (request.Format == ExportFormat.SignUpListsZip || request.Format == ExportFormat.SignUpListsExcel)
        {
            // Get event with sign-up lists
            var eventWithSignUps = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

            if (eventWithSignUps == null)
            {
                return Result<ExportResult>.Failure("Event not found");
            }

            if (!eventWithSignUps.SignUpLists.Any())
            {
                return Result<ExportResult>.Failure("No signup lists found for this event");
            }

            // Map domain entities to DTOs (reuse existing mapping pattern from lines 56-85)
            var signUpListsForExport = eventWithSignUps.SignUpLists.Select(s => new SignUpListDto
            {
                Id = s.Id,
                Category = s.Category,
                Description = s.Description ?? string.Empty,
                HasMandatoryItems = s.HasMandatoryItems,
                HasPreferredItems = s.HasPreferredItems,
                HasSuggestedItems = s.HasSuggestedItems,
                HasOpenItems = s.HasOpenItems,
                Items = s.Items.Select(i => new SignUpItemDto
                {
                    Id = i.Id,
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    RemainingQuantity = i.RemainingQuantity,
                    ItemCategory = i.ItemCategory,
                    CreatedByUserId = i.CreatedByUserId,
                    Commitments = i.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription ?? string.Empty,
                        Quantity = c.Quantity,
                        ContactName = c.ContactName,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone,
                        CommittedAt = c.CommittedAt
                    }).ToList()
                }).ToList()
            }).ToList();

            // Phase 6A.73 (Revised): Both formats now return ZIP archives
            // CSV: ZIP with multiple CSV files (one per signup list + category)
            // Excel: ZIP with multiple Excel files (one per signup list, with category sheets)
            if (request.Format == ExportFormat.SignUpListsExcel)
            {
                // Phase 6A.73: Generate ZIP with Excel files (one Excel per signup list)
                // Removed "excel" from filename to prevent ASP.NET Core MIME type auto-detection
                fileContent = _excelService.ExportSignUpListsToExcelZip(signUpListsForExport, request.EventId);
                fileName = $"event-{request.EventId}-signup-lists-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
                contentType = "application/zip";
            }
            else // SignUpListsZip (CSV)
            {
                // Generate ZIP with CSV files
                fileContent = _csvService.ExportSignUpListsToZip(signUpListsForExport, request.EventId);
                fileName = $"event-{request.EventId}-signup-lists-csv-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
                contentType = "application/zip";
            }

                    _logger.LogInformation(
                        "ExportEventAttendees: SignUpLists export generated - EventId={EventId}, Format={Format}, SignUpListCount={SignUpListCount}",
                        request.EventId, request.Format, signUpListsForExport.Count);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "ExportEventAttendees COMPLETE: EventId={EventId}, Format={Format}, FileName={FileName}, FileSize={FileSize}bytes, Duration={ElapsedMs}ms",
                        request.EventId, request.Format, fileName, fileContent.Length, stopwatch.ElapsedMilliseconds);

                    return Result<ExportResult>.Success(new ExportResult
                    {
                        FileContent = fileContent,
                        FileName = fileName,
                        ContentType = contentType
                    });
                }

                if (request.Format == ExportFormat.Excel)
                {
                    fileContent = _excelService.ExportEventAttendees(
                        attendeesResponse,
                        signUpListDtos
                    );
                    fileName = $"event-{request.EventId}-attendees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    _logger.LogInformation(
                        "ExportEventAttendees: Excel export generated - EventId={EventId}, HasSignUpLists={HasSignUpLists}",
                        request.EventId, signUpListDtos != null && signUpListDtos.Any());
                }
                else
                {
                    fileContent = _csvService.ExportEventAttendees(attendeesResponse);
                    fileName = $"event-{request.EventId}-attendees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                    // Phase 6A.68 Fix: Use application/octet-stream to prevent HTTP middleware from treating CSV as text
                    // This prevents newline escaping (\n → literal \n) and ensures binary transfer to Excel
                    contentType = "application/octet-stream";

                    _logger.LogInformation(
                        "ExportEventAttendees: CSV export generated - EventId={EventId}",
                        request.EventId);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ExportEventAttendees COMPLETE: EventId={EventId}, Format={Format}, FileName={FileName}, FileSize={FileSize}bytes, Duration={ElapsedMs}ms",
                    request.EventId, request.Format, fileName, fileContent.Length, stopwatch.ElapsedMilliseconds);

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
                    "ExportEventAttendees FAILED: Exception occurred - EventId={EventId}, Format={Format}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.Format, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

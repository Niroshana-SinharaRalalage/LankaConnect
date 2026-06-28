using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Application.Common.Options;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventAttendees;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;

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
                #pragma warning disable CS0618 // Suppress obsolete warning for SignUpItemDto
            signUpListDtos = eventWithSignUps.SignUpLists.Select(s => new SignUpListDto
            {
                Id = s.Id,
                Category = s.Category,
                Description = s.Description ?? string.Empty,
                HasMandatoryItems = s.HasMandatoryItems,
                HasPreferredItems = s.HasPreferredItems,
                HasSuggestedItems = s.HasSuggestedItems,
                HasOpenItems = s.HasOpenItems,
                Items = s.Items.Select(i => (ISignUpItemDto)new SignUpItemDto
                {
                    Id = i.Id,
                    ItemDescription = i.ItemDescription,
                    // Phase 6A.121: SignUpItem now uses dual nullable fields (TargetQuantity or AvailableSlots)
                    Quantity = i.TargetQuantity ?? i.AvailableSlots ?? 0,
                    RemainingQuantity = i.ItemType == LankaConnect.Products.LankaEvents.Domain.Enums.SignUpItemType.Quantity ? i.GetRemainingQuantity() : i.GetRemainingSlots(),
                    ItemCategory = i.ItemCategory,
                    CreatedByUserId = i.CreatedByUserId,
                    Commitments = i.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription ?? string.Empty,
                        // Phase 6A.121: SignUpCommitment uses dual nullable fields (PhysicalQuantity/SlotsClaimed)
                        PhysicalQuantity = c.PhysicalQuantity,
                        SlotsClaimed = c.SlotsClaimed,
                        ContactName = c.ContactName,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone,
                        CommittedAt = c.CommittedAt
                    }).ToList()
                }).ToList()
            }).ToList();
            #pragma warning restore CS0618
            }
        }

        // Generate export based on format
        byte[] fileContent;
        string fileName;
        string contentType;

        // Phase 6A.69: Handle SignUpListsZip format (ZIP archive with multiple CSVs)
        // Phase 6A.73: Handle SignUpListsExcel format (Excel file with category sheets)
        // Phase 7D.1 Step 16: Handle VolunteersZip / VolunteersExcel (same pipeline; Kind=Volunteers filter + volunteer labels)
        var isVolunteerExport = request.Format == ExportFormat.VolunteersZip || request.Format == ExportFormat.VolunteersExcel;
        var isSignUpExport = request.Format == ExportFormat.SignUpListsZip || request.Format == ExportFormat.SignUpListsExcel;

        if (isSignUpExport || isVolunteerExport)
        {
            // Get event with sign-up lists
            var eventWithSignUps = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

            if (eventWithSignUps == null)
            {
                return Result<ExportResult>.Failure("Event not found");
            }

            // Phase 7D.1 Step 16: Volunteer exports filter to Kind=Volunteers; Items exports exclude volunteer lists
            // so the two export endpoints produce disjoint outputs and a volunteer export on an event without
            // volunteer lists returns a clear error rather than an empty zip.
            var filteredSignUpLists = isVolunteerExport
                ? eventWithSignUps.SignUpLists.Where(s => s.Kind == LankaConnect.Products.LankaEvents.Domain.Enums.SignUpKind.Volunteers).ToList()
                : eventWithSignUps.SignUpLists.Where(s => s.Kind == LankaConnect.Products.LankaEvents.Domain.Enums.SignUpKind.Items).ToList();

            if (!filteredSignUpLists.Any())
            {
                var errorMessage = isVolunteerExport
                    ? "No volunteer lists found for this event"
                    : "No signup lists found for this event";
                return Result<ExportResult>.Failure(errorMessage);
            }

            // Map domain entities to DTOs (reuse existing mapping pattern from lines 56-85)
            #pragma warning disable CS0618 // Suppress obsolete warning for SignUpItemDto
            var signUpListsForExport = filteredSignUpLists.Select(s => new SignUpListDto
            {
                Id = s.Id,
                Category = s.Category,
                Description = s.Description ?? string.Empty,
                HasMandatoryItems = s.HasMandatoryItems,
                HasPreferredItems = s.HasPreferredItems,
                HasSuggestedItems = s.HasSuggestedItems,
                HasOpenItems = s.HasOpenItems,
                Items = s.Items.Select(i => (ISignUpItemDto)new SignUpItemDto
                {
                    Id = i.Id,
                    ItemDescription = i.ItemDescription,
                    // Phase 6A.121: SignUpItem now uses dual nullable fields (TargetQuantity or AvailableSlots)
                    Quantity = i.TargetQuantity ?? i.AvailableSlots ?? 0,
                    RemainingQuantity = i.ItemType == LankaConnect.Products.LankaEvents.Domain.Enums.SignUpItemType.Quantity ? i.GetRemainingQuantity() : i.GetRemainingSlots(),
                    ItemCategory = i.ItemCategory,
                    CreatedByUserId = i.CreatedByUserId,
                    Commitments = i.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription ?? string.Empty,
                        // Phase 6A.121: SignUpCommitment uses dual nullable fields (PhysicalQuantity/SlotsClaimed)
                        PhysicalQuantity = c.PhysicalQuantity,
                        SlotsClaimed = c.SlotsClaimed,
                        ContactName = c.ContactName,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone,
                        CommittedAt = c.CommittedAt
                    }).ToList()
                }).ToList()
            }).ToList();
            #pragma warning restore CS0618

            // Phase 6A.73 (Revised): Both formats now return ZIP archives
            // CSV: ZIP with multiple CSV files (one per signup list + category)
            // Excel: ZIP with multiple Excel files (one per signup list, with category sheets)
            // Phase 7D.1 Step 16: Volunteer variants use the same pipeline with SignUpExportLabels.ForVolunteers().
            var exportLabels = isVolunteerExport
                ? SignUpExportLabels.ForVolunteers()
                : SignUpExportLabels.ForItems();
            var fileNameSlug = isVolunteerExport ? "volunteers" : "signup-lists";
            var wantsExcel = request.Format == ExportFormat.SignUpListsExcel || request.Format == ExportFormat.VolunteersExcel;

            if (wantsExcel)
            {
                // Generate ZIP with Excel files (one Excel per list)
                fileContent = _excelService.ExportSignUpListsToExcelZip(signUpListsForExport, request.EventId, exportLabels);
                fileName = $"event-{request.EventId}-{fileNameSlug}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
                contentType = "application/zip";
            }
            else // VolunteersZip or SignUpListsZip (CSV)
            {
                fileContent = _csvService.ExportSignUpListsToZip(signUpListsForExport, request.EventId, exportLabels);
                fileName = $"event-{request.EventId}-{fileNameSlug}-csv-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
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

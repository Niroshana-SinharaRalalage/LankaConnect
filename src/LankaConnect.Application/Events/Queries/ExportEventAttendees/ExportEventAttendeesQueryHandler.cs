using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEventAttendees;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.ExportEventAttendees;

public class ExportEventAttendeesQueryHandler
    : IQueryHandler<ExportEventAttendeesQuery, ExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IExcelExportService _excelService;
    private readonly ICsvExportService _csvService;

    public ExportEventAttendeesQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository,
        IExcelExportService excelService,
        ICsvExportService csvService)
    {
        _context = context;
        _eventRepository = eventRepository;
        _excelService = excelService;
        _csvService = csvService;
    }

    public async Task<Result<ExportResult>> Handle(
        ExportEventAttendeesQuery request,
        CancellationToken cancellationToken)
    {
        // Get attendees data using existing query handler logic
        var attendeesQuery = new GetEventAttendeesQuery(request.EventId);
        var attendeesHandler = new GetEventAttendeesQueryHandler(_context, _eventRepository);
        var attendeesResult = await attendeesHandler.Handle(attendeesQuery, cancellationToken);

        if (!attendeesResult.IsSuccess)
        {
            return Result<ExportResult>.Failure(attendeesResult.Error);
        }

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

        if (request.Format == ExportFormat.Excel)
        {
            fileContent = _excelService.ExportEventAttendees(
                attendeesResponse,
                signUpListDtos
            );
            fileName = $"event-{request.EventId}-attendees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }
        else
        {
            fileContent = _csvService.ExportEventAttendees(attendeesResponse);
            fileName = $"event-{request.EventId}-attendees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            // Phase 6A.68 Fix: Use application/octet-stream to prevent HTTP middleware from treating CSV as text
            // This prevents newline escaping (\n → literal \n) and ensures binary transfer to Excel
            contentType = "application/octet-stream";
        }

        return Result<ExportResult>.Success(new ExportResult
        {
            FileContent = fileContent,
            FileName = fileName,
            ContentType = contentType
        });
    }
}

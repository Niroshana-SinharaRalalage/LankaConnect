using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetEventAttendees;

public class GetEventAttendeesQueryHandler
    : IQueryHandler<GetEventAttendeesQuery, EventAttendeesResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;

    public GetEventAttendeesQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository)
    {
        _context = context;
        _eventRepository = eventRepository;
    }

    public async Task<Result<EventAttendeesResponse>> Handle(
        GetEventAttendeesQuery request,
        CancellationToken cancellationToken)
    {
        // Get event details using repository
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event == null)
        {
            return Result<EventAttendeesResponse>.Failure(
                new Error("Event.NotFound", "Event not found"));
        }

        // Single query with eager loading to avoid N+1
        var registrations = await _context.Registrations
            .AsNoTracking()
            .Include(r => r.Attendees)
            .Include(r => r.Contact)
            .Include(r => r.TotalPrice)
            .Where(r => r.EventId == request.EventId)
            .Where(r => r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        // Get ticket information separately (if Ticket entity exists)
        // For now, we'll extract from QR codes if available
        var attendeeDtos = registrations.Select(r => MapToDto(r)).ToList();

        return Result<EventAttendeesResponse>.Success(new EventAttendeesResponse
        {
            EventId = request.EventId,
            EventTitle = @event.Title,
            Attendees = attendeeDtos,
            TotalRegistrations = attendeeDtos.Count,
            TotalAttendees = attendeeDtos.Sum(a => a.TotalAttendees),
            TotalRevenue = attendeeDtos.Sum(a => a.TotalAmount ?? 0)
        });
    }

    private EventAttendeeDto MapToDto(Registration registration)
    {
        // Calculate adult/child counts
        var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
        var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);

        // Calculate gender distribution (e.g., "2M, 1F, 1O")
        var genderCounts = registration.Attendees
            .Where(a => a.Gender.HasValue)
            .GroupBy(a => a.Gender!.Value)
            .Select(g => $"{g.Count()}{GetGenderShortCode(g.Key)}")
            .ToList();

        var genderDistribution = genderCounts.Any()
            ? string.Join(", ", genderCounts)
            : string.Empty;

        // Map attendees
        var attendeeDtos = registration.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,
            Gender = a.Gender
        }).ToList();

        return new EventAttendeeDto
        {
            RegistrationId = registration.Id,
            UserId = registration.UserId,
            Status = registration.Status,
            PaymentStatus = registration.PaymentStatus,
            CreatedAt = registration.CreatedAt,

            ContactEmail = registration.Contact?.Email ?? string.Empty,
            ContactPhone = registration.Contact?.PhoneNumber ?? string.Empty,
            ContactAddress = registration.Contact?.Address,

            Attendees = attendeeDtos,
            TotalAttendees = registration.Attendees.Count,
            AdultCount = adultCount,
            ChildCount = childCount,
            GenderDistribution = genderDistribution,

            TotalAmount = registration.TotalPrice?.Amount,
            Currency = registration.TotalPrice?.Currency.ToString(),

            // Ticket info - will be enhanced when Ticket entity integration is complete
            TicketCode = null,
            QrCodeData = null,
            HasTicket = false
        };
    }

    private static string GetGenderShortCode(Gender gender)
    {
        return gender switch
        {
            Gender.Male => "M",
            Gender.Female => "F",
            Gender.Other => "O",
            _ => "?"
        };
    }
}

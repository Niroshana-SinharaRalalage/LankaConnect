using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;

public class GetUserRegistrationForEventQueryHandler
    : IQueryHandler<GetUserRegistrationForEventQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUserRegistrationForEventQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RegistrationDetailsDto?>> Handle(
        GetUserRegistrationForEventQuery request,
        CancellationToken cancellationToken)
    {
        // Only return active registrations (exclude cancelled and refunded)
        // This fixes the multi-attendee re-registration issue (Session 30)
        // Phase 6A.41: Fixed to return NEWEST registration (OrderByDescending)
        // Phase 6A.47: Added AsNoTracking() to fix JSON projection error
        var registration = await _context.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == request.EventId &&
                       r.UserId == request.UserId &&
                       r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RegistrationDetailsDto
            {
                Id = r.Id,
                EventId = r.EventId,
                UserId = r.UserId,
                Quantity = r.Quantity,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,

                // Map attendees (Session 21 multi-attendee feature)
                // Phase 6A.43: Updated to use AgeCategory instead of Age
                // Fix: Handle null Attendees for legacy registrations
                Attendees = r.Attendees != null ? r.Attendees.Select(a => new AttendeeDetailsDto
                {
                    Name = a.Name,
                    AgeCategory = a.AgeCategory,
                    Gender = a.Gender
                }).ToList() : new List<AttendeeDetailsDto>(),

                // Contact information
                ContactEmail = r.Contact != null ? r.Contact.Email : null,
                ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
                ContactAddress = r.Contact != null ? r.Contact.Address : null,

                // Payment information
                PaymentStatus = r.PaymentStatus,
                TotalPriceAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
                TotalPriceCurrency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<RegistrationDetailsDto?>.Success(registration);
    }
}

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
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
        var registration = await _context.Registrations
            .Where(r => r.EventId == request.EventId && r.UserId == request.UserId)
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
                Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
                {
                    Name = a.Name,
                    Age = a.Age
                }).ToList(),

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

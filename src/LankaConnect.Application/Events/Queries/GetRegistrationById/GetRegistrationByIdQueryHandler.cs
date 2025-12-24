using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetRegistrationById;

/// <summary>
/// Phase 6A.44: Handler to get registration details by ID
/// This allows anonymous users to view their registration details after payment
/// </summary>
public class GetRegistrationByIdQueryHandler
    : IQueryHandler<GetRegistrationByIdQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRegistrationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RegistrationDetailsDto?>> Handle(
        GetRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var registration = await _context.Registrations
            .Where(r => r.Id == request.RegistrationId)
            .Select(r => new RegistrationDetailsDto
            {
                Id = r.Id,
                EventId = r.EventId,
                UserId = r.UserId,
                Quantity = r.Quantity,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,

                // Map attendees
                Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
                {
                    Name = a.Name,
                    AgeCategory = a.AgeCategory,
                    Gender = a.Gender
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

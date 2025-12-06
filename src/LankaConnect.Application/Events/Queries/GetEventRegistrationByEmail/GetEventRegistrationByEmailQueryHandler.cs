using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;

/// <summary>
/// Handler for checking if an email has registered for an event
/// Phase 6A.15: Enhanced sign-up list UX with email validation
/// </summary>
public class GetEventRegistrationByEmailQueryHandler
    : IQueryHandler<GetEventRegistrationByEmailQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public GetEventRegistrationByEmailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(
        GetEventRegistrationByEmailQuery request,
        CancellationToken cancellationToken)
    {
        // Check if any registration exists for this event with the given email
        var exists = await _context.Registrations
            .AnyAsync(r =>
                r.EventId == request.EventId &&
                r.Contact != null &&
                r.Contact.Email == request.Email,
                cancellationToken);

        return Result<bool>.Success(exists);
    }
}

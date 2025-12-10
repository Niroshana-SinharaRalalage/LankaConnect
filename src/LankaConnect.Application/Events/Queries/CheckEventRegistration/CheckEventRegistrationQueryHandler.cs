using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.CheckEventRegistration;

/// <summary>
/// Handler for checking event registration with enhanced member detection
/// Phase 6A.23: Supports anonymous sign-up workflow with proper UX flow
///
/// Flow:
/// 1. First check if email belongs to a LankaConnect member (Users table)
/// 2. Then check if email is registered for the specific event
/// 3. Return appropriate result based on both checks
/// </summary>
public class CheckEventRegistrationQueryHandler
    : IQueryHandler<CheckEventRegistrationQuery, EventRegistrationCheckResult>
{
    private readonly IApplicationDbContext _context;

    public CheckEventRegistrationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<EventRegistrationCheckResult>> Handle(
        CheckEventRegistrationQuery request,
        CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty)
            return Result<EventRegistrationCheckResult>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<EventRegistrationCheckResult>.Failure("Email is required");

        var emailToCheck = request.Email.Trim();

        // Step 1: Check if email belongs to a LankaConnect member (User account)
        // Note: u.Email is a Value Object, must use .Value to access underlying string for EF Core translation
        // SQL Server comparison is case-insensitive by default with default collation
        var user = await _context.Users
            .Where(u => u.Email.Value == emailToCheck)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 2: Check if email is registered for this event
        // Note: RegistrationContact.Email is a plain string, but AttendeeInfo.Email is a Value Object
        // Must use .Value for AttendeeInfo.Email to enable EF Core translation
        var registration = await _context.Registrations
            .Where(r => r.EventId == request.EventId)
            .Where(r =>
                (r.Contact != null && r.Contact.Email == emailToCheck) ||
                (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value == emailToCheck))
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Return appropriate result based on checks

        if (user != null)
        {
            // Email belongs to a LankaConnect member - they should log in
            return Result<EventRegistrationCheckResult>.Success(
                EventRegistrationCheckResult.MemberAccount(
                    user.Id,
                    registration != null,
                    registration?.Id));
        }

        if (registration != null)
        {
            // Not a member but registered for event - allow anonymous commitment
            return Result<EventRegistrationCheckResult>.Success(
                EventRegistrationCheckResult.AnonymousRegistered(registration.Id));
        }

        // Not a member and not registered for event
        return Result<EventRegistrationCheckResult>.Success(
            EventRegistrationCheckResult.AnonymousNotRegistered());
    }
}

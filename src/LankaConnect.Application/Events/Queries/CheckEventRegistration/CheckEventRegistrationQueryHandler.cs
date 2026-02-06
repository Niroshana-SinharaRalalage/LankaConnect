using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<CheckEventRegistrationQueryHandler> _logger;

    public CheckEventRegistrationQueryHandler(
        IApplicationDbContext context,
        ILogger<CheckEventRegistrationQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<EventRegistrationCheckResult>> Handle(
        CheckEventRegistrationQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CheckEventRegistration"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            // Mask email for logging (show first 3 chars + domain)
            var maskedEmail = MaskEmail(request.Email);

            _logger.LogInformation(
                "CheckEventRegistration START: EventId={EventId}, Email={MaskedEmail}",
                request.EventId, maskedEmail);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CheckEventRegistration FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<EventRegistrationCheckResult>.Failure("Event ID is required");
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CheckEventRegistration FAILED: Email required - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<EventRegistrationCheckResult>.Failure("Email is required");
                }

                var emailToCheck = request.Email.Trim();

                // Step 1: Check if email belongs to a LankaConnect member (User account)
                // Note: u.Email is a Value Object, must use .Value to access underlying string for EF Core translation
                // SQL Server comparison is case-insensitive by default with default collation
                var user = await _context.Users
                    .Where(u => u.Email.Value == emailToCheck)
                    .Select(u => new { u.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                _logger.LogInformation(
                    "CheckEventRegistration: Member check completed - EventId={EventId}, IsMember={IsMember}",
                    request.EventId, user != null);

                // Step 2: Check if email is registered for this event
                // Note: RegistrationContact.Email is a plain string, but AttendeeInfo.Email is a Value Object
                // Must use .Value for AttendeeInfo.Email to enable EF Core translation
                // Phase 6A.44 FIX: Only count active registrations (exclude cancelled/refunded)
                var registration = await _context.Registrations
                    .Where(r => r.EventId == request.EventId)
                    .Where(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Refunded)
                    .Where(r =>
                        (r.Contact != null && r.Contact.Email == emailToCheck) ||
                        (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value == emailToCheck))
                    .Select(r => new { r.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                _logger.LogInformation(
                    "CheckEventRegistration: Registration check completed - EventId={EventId}, IsRegistered={IsRegistered}, RegistrationId={RegistrationId}",
                    request.EventId, registration != null, registration?.Id);

                // Step 3: Return appropriate result based on checks
                stopwatch.Stop();

                if (user != null)
                {
                    // Email belongs to a LankaConnect member - they should log in
                    _logger.LogInformation(
                        "CheckEventRegistration COMPLETE: MemberAccount - EventId={EventId}, UserId={UserId}, IsRegistered={IsRegistered}, Duration={ElapsedMs}ms",
                        request.EventId, user.Id, registration != null, stopwatch.ElapsedMilliseconds);

                    return Result<EventRegistrationCheckResult>.Success(
                        EventRegistrationCheckResult.MemberAccount(
                            user.Id,
                            registration != null,
                            registration?.Id));
                }

                if (registration != null)
                {
                    // Not a member but registered for event - allow anonymous commitment
                    _logger.LogInformation(
                        "CheckEventRegistration COMPLETE: AnonymousRegistered - EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.EventId, registration.Id, stopwatch.ElapsedMilliseconds);

                    return Result<EventRegistrationCheckResult>.Success(
                        EventRegistrationCheckResult.AnonymousRegistered(registration.Id));
                }

                // Not a member and not registered for event
                _logger.LogInformation(
                    "CheckEventRegistration COMPLETE: AnonymousNotRegistered - EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<EventRegistrationCheckResult>.Success(
                    EventRegistrationCheckResult.AnonymousNotRegistered());
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CheckEventRegistration FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Masks email for secure logging (shows first 3 chars + domain)
    /// </summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "[empty]";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "[invalid]";

        var prefix = email.Substring(0, Math.Min(3, atIndex));
        var domain = email.Substring(atIndex);
        return $"{prefix}***{domain}";
    }
}

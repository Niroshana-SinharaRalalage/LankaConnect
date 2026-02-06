using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;

/// <summary>
/// Handler for checking if an email has registered for an event
/// Phase 6A.15: Enhanced sign-up list UX with email validation
/// </summary>
public class GetEventRegistrationByEmailQueryHandler
    : IQueryHandler<GetEventRegistrationByEmailQuery, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetEventRegistrationByEmailQueryHandler> _logger;

    public GetEventRegistrationByEmailQueryHandler(
        IApplicationDbContext context,
        ILogger<GetEventRegistrationByEmailQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        GetEventRegistrationByEmailQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventRegistrationByEmail"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            // Mask email for logging (show first 3 chars + domain)
            var maskedEmail = MaskEmail(request.Email);

            _logger.LogInformation(
                "GetEventRegistrationByEmail START: EventId={EventId}, Email={MaskedEmail}",
                request.EventId, maskedEmail);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventRegistrationByEmail FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<bool>.Failure("Event ID is required");
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventRegistrationByEmail FAILED: Email required - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<bool>.Failure("Email is required");
                }

                // Check if any registration exists for this event with the given email
                var exists = await _context.Registrations
                    .AnyAsync(r =>
                        r.EventId == request.EventId &&
                        r.Contact != null &&
                        r.Contact.Email == request.Email,
                        cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventRegistrationByEmail COMPLETE: EventId={EventId}, Email={MaskedEmail}, IsRegistered={IsRegistered}, Duration={ElapsedMs}ms",
                    request.EventId, maskedEmail, exists, stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventRegistrationByEmail FAILED: Exception occurred - EventId={EventId}, Email={MaskedEmail}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, maskedEmail, stopwatch.ElapsedMilliseconds, ex.Message);

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

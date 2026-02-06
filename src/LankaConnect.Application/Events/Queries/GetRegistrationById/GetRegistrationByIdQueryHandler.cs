using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetRegistrationById;

/// <summary>
/// Phase 6A.44: Handler to get registration details by ID
/// This allows anonymous users to view their registration details after payment
/// </summary>
public class GetRegistrationByIdQueryHandler
    : IQueryHandler<GetRegistrationByIdQuery, RegistrationDetailsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetRegistrationByIdQueryHandler> _logger;

    public GetRegistrationByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetRegistrationByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<RegistrationDetailsDto?>> Handle(
        GetRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetRegistrationById"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetRegistrationById START: RegistrationId={RegistrationId}",
                request.RegistrationId);

            try
            {
                // Validate request
                if (request.RegistrationId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetRegistrationById FAILED: Invalid RegistrationId - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result<RegistrationDetailsDto?>.Failure("Registration ID is required");
                }

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

                stopwatch.Stop();

                if (registration == null)
                {
                    _logger.LogWarning(
                        "GetRegistrationById COMPLETE: Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "GetRegistrationById COMPLETE: RegistrationId={RegistrationId}, EventId={EventId}, AttendeeCount={AttendeeCount}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                        registration.Id, registration.EventId, registration.Attendees?.Count ?? 0, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);
                }

                return Result<RegistrationDetailsDto?>.Success(registration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetRegistrationById FAILED: Exception occurred - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

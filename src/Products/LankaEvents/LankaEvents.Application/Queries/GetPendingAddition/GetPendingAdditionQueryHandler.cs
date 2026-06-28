using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetPendingAddition;

/// <summary>
/// Handler for GetPendingAdditionQuery.
/// Returns the pending addition for a registration, if any.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class GetPendingAdditionQueryHandler
    : IQueryHandler<GetPendingAdditionQuery, PendingAdditionDto?>
{
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly ILogger<GetPendingAdditionQueryHandler> _logger;

    public GetPendingAdditionQueryHandler(
        IRegistrationAdditionRepository additionRepository,
        ILogger<GetPendingAdditionQueryHandler> logger)
    {
        _additionRepository = additionRepository;
        _logger = logger;
    }

    public async Task<Result<PendingAdditionDto?>> Handle(
        GetPendingAdditionQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPendingAddition"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "GetPendingAddition START: RegistrationId={RegistrationId}",
                request.RegistrationId);

            try
            {
                var pendingAddition = await _additionRepository.GetPendingByRegistrationIdAsync(
                    request.RegistrationId,
                    cancellationToken);

                stopwatch.Stop();

                if (pendingAddition == null)
                {
                    _logger.LogInformation(
                        "GetPendingAddition COMPLETE: RegistrationId={RegistrationId}, Found=false, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result<PendingAdditionDto?>.Success(null);
                }

                // Map to DTO
                var dto = new PendingAdditionDto
                {
                    Id = pendingAddition.Id,
                    RegistrationId = pendingAddition.RegistrationId,
                    EventId = pendingAddition.EventId,
                    Status = pendingAddition.Status,
                    NewAttendees = pendingAddition.NewAttendees
                        .Select(a => new PendingAttendeeDto(a.Name, a.AgeCategory, a.Gender))
                        .ToList(),
                    AdditionalAmount = pendingAddition.AdditionalAmount.Amount,
                    Currency = pendingAddition.AdditionalAmount.Currency.ToString(),
                    CheckoutSessionId = pendingAddition.StripeCheckoutSessionId,
                    ExpiresAt = pendingAddition.CheckoutExpiresAt,
                    CreatedAt = pendingAddition.CreatedAt
                };

                _logger.LogInformation(
                    "GetPendingAddition COMPLETE: RegistrationId={RegistrationId}, Found=true, AdditionId={AdditionId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.RegistrationId, pendingAddition.Id, pendingAddition.Status, stopwatch.ElapsedMilliseconds);

                return Result<PendingAdditionDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetPendingAddition FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}

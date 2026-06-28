using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetAllowedRegistrationModes;

/// <summary>
/// Phase 7E.2: Computes the set of allowed registration modes for a draft event shape.
/// Pure function — no DB access. Logs at Information level for traceability.
/// </summary>
public class GetAllowedRegistrationModesQueryHandler
    : IQueryHandler<GetAllowedRegistrationModesQuery, IReadOnlyList<RegistrationMode>>
{
    private readonly ILogger<GetAllowedRegistrationModesQueryHandler> _logger;

    public GetAllowedRegistrationModesQueryHandler(ILogger<GetAllowedRegistrationModesQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<RegistrationMode>>> Handle(
        GetAllowedRegistrationModesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var context = new RegistrationModeContext
            {
                IsFreeAttendance = request.IsFreeAttendance,
                HasSeating = request.HasSeating,
                HasNamedSeating = request.HasNamedSeating,
                RequiresAttendeeNameOnTicket = request.RequiresAttendeeNameOnTicket,
                HasDualPricing = request.HasDualPricing,
                HasGroupTiers = request.HasGroupTiers,
                HasTicketTiers = request.HasTicketTiers,
                HasIdentityBoundAddOn = request.HasIdentityBoundAddOn,
                HasMatrixPricing = request.HasMatrixPricing,
                // Phase 8X.11 — payment-mode axis (default Free for back-compat).
                PaymentMode = request.PaymentMode,
            };

            var allowed = RegistrationModeCompatibility.AllowedModes(context);

            _logger.LogInformation(
                "GetAllowedRegistrationModes: shape (Free={Free}, Seating={Seating}, DualPricing={Dual}, " +
                "Tiers={Tiers}, GroupTiers={Group}, PaymentMode={Pay}) → {Count} allowed modes: [{Modes}]",
                request.IsFreeAttendance, request.HasSeating, request.HasDualPricing,
                request.HasTicketTiers, request.HasGroupTiers, request.PaymentMode,
                allowed.Count, string.Join(",", allowed));

            return Task.FromResult(Result<IReadOnlyList<RegistrationMode>>.Success(allowed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetAllowedRegistrationModes FAILED for shape: {@Request}", request);
            return Task.FromResult(Result<IReadOnlyList<RegistrationMode>>.Failure(
                $"Failed to compute allowed registration modes: {ex.Message}"));
        }
    }
}

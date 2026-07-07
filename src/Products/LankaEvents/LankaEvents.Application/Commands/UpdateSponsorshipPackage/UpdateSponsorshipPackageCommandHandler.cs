using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — update or deactivate an existing sponsorship package.
/// Two-phase update: first <c>UpdateDetails</c> for the fields, then route
/// the IsActive flag through the domain's <c>Activate</c>/<c>Deactivate</c>
/// state-machine to preserve invariants (e.g., "already active" guard
/// returning a clear error).
/// </summary>
public class UpdateSponsorshipPackageCommandHandler : ICommandHandler<UpdateSponsorshipPackageCommand>
{
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSponsorshipPackageCommandHandler> _logger;

    public UpdateSponsorshipPackageCommandHandler(
        ISponsorshipPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSponsorshipPackageCommandHandler> logger)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSponsorshipPackageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSponsorshipPackage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PackageId", request.PackageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateSponsorshipPackage START: EventId={EventId}, PackageId={PackageId}, Name={Name}, IsActive={IsActive}",
                request.EventId, request.PackageId, request.Name, request.IsActive);

            try
            {
                var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
                if (package is null || package.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "UpdateSponsorshipPackage: package not found or wrong event - PackageId={PackageId}, EventId={EventId}",
                        request.PackageId, request.EventId);
                    return Result.NotFound($"Sponsorship package {request.PackageId} not found for event {request.EventId}");
                }

                if (!MoneyBuilder.TryParseCurrency(request.Currency, out var currency))
                    return Result.Failure($"Invalid currency: {request.Currency}");

                var priceResult = MoneyBuilder.Create(request.Price, currency);
                if (priceResult.IsFailure)
                    return Result.Failure(priceResult.Error);

                var updateResult = package.UpdateDetails(
                    request.Name,
                    request.Description,
                    priceResult.Value,
                    request.QuantityLimit,
                    request.SortOrder,
                    request.Tier,
                    request.Perks,
                    request.IncludedTicketCount);

                if (updateResult.IsFailure)
                    return Result.Failure(updateResult.Error);

                // Apply active-state transition only if it changed — Activate/Deactivate
                // raise "already X" errors if called redundantly, and we don't want a
                // no-op update to spuriously fail because of those guards.
                if (request.IsActive && !package.IsActive)
                {
                    var activateResult = package.Activate();
                    if (activateResult.IsFailure)
                        return Result.Failure(activateResult.Error);
                }
                else if (!request.IsActive && package.IsActive)
                {
                    var deactivateResult = package.Deactivate();
                    if (deactivateResult.IsFailure)
                        return Result.Failure(deactivateResult.Error);
                }

                _packageRepository.Update(package);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateSponsorshipPackage COMPLETE: PackageId={PackageId}, Duration={ElapsedMs}ms",
                    request.PackageId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateSponsorshipPackage FAILED: PackageId={PackageId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.PackageId, request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Sponsorship package update failed: {ex.Message}");
            }
        }
    }
}

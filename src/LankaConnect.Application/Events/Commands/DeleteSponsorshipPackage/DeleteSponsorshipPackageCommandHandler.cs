using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.DeleteSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — delete handler. Soft-deletes when sales exist (FK preservation),
/// hard-deletes otherwise. Even hard-delete uses the FK SetNull behaviour defined
/// in the Sponsor configuration — but since we only hard-delete when no sponsors
/// reference the package, that path is a no-op for the FK.
/// </summary>
public class DeleteSponsorshipPackageCommandHandler : ICommandHandler<DeleteSponsorshipPackageCommand>
{
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteSponsorshipPackageCommandHandler> _logger;

    public DeleteSponsorshipPackageCommandHandler(
        ISponsorshipPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteSponsorshipPackageCommandHandler> logger)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteSponsorshipPackageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteSponsorshipPackage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PackageId", request.PackageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteSponsorshipPackage START: EventId={EventId}, PackageId={PackageId}",
                request.EventId, request.PackageId);

            try
            {
                var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
                if (package is null || package.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "DeleteSponsorshipPackage: package not found or wrong event - PackageId={PackageId}, EventId={EventId}",
                        request.PackageId, request.EventId);
                    return Result.NotFound($"Sponsorship package {request.PackageId} not found for event {request.EventId}");
                }

                if (package.QuantitySold > 0)
                {
                    // Soft delete to preserve historical sponsor receipts. Idempotent
                    // when already inactive — we silently accept the no-op rather
                    // than surface a 409 to the organizer (clicking Delete twice
                    // should not look like an error).
                    if (package.IsActive)
                    {
                        var deactivateResult = package.Deactivate();
                        if (deactivateResult.IsFailure)
                            return Result.Failure(deactivateResult.Error);

                        _packageRepository.Update(package);
                        await _unitOfWork.CommitAsync(cancellationToken);
                    }

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "DeleteSponsorshipPackage SOFT-DELETED: PackageId={PackageId}, QuantitySold={QuantitySold}, Duration={ElapsedMs}ms",
                        request.PackageId, package.QuantitySold, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }

                // Hard delete — safe because no sponsor row references this package.
                _packageRepository.Remove(package);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "DeleteSponsorshipPackage HARD-DELETED: PackageId={PackageId}, Duration={ElapsedMs}ms",
                    request.PackageId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteSponsorshipPackage FAILED: PackageId={PackageId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.PackageId, request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Sponsorship package delete failed: {ex.Message}");
            }
        }
    }
}

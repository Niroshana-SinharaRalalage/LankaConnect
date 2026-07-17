using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteSponsorshipPackage;

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
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<DeleteSponsorshipPackageCommandHandler> _logger;

    public DeleteSponsorshipPackageCommandHandler(
        ISponsorshipPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<DeleteSponsorshipPackageCommandHandler> logger)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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
                        await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)
                    }

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "DeleteSponsorshipPackage SOFT-DELETED: PackageId={PackageId}, QuantitySold={QuantitySold}, Duration={ElapsedMs}ms",
                        request.PackageId, package.QuantitySold, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }

                // Hard delete — safe because no sponsor row references this package.
                _packageRepository.Remove(package);
                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

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

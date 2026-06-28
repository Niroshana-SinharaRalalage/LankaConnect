using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — organizer-facing delete. Soft-deletes (sets IsActive=false)
/// when the package has any sales (QuantitySold &gt; 0) to preserve historical
/// sponsor receipts that FK to it. Hard-deletes when no sales exist.
/// </summary>
public record DeleteSponsorshipPackageCommand(
    Guid EventId,
    Guid PackageId
) : ICommand;

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — organizer-facing update. Setting <see cref="IsActive"/> to
/// false is the canonical soft-delete path (mirrors the AddOn pattern). The
/// handler routes the active flag through <c>Activate</c> / <c>Deactivate</c>
/// AFTER applying field updates so we don't lose state-machine guards.
/// </summary>
public record UpdateSponsorshipPackageCommand(
    Guid EventId,
    Guid PackageId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? QuantityLimit,
    int SortOrder,
    string? Tier,
    IReadOnlyList<string>? Perks,
    int IncludedTicketCount,
    bool IsActive
) : ICommand;

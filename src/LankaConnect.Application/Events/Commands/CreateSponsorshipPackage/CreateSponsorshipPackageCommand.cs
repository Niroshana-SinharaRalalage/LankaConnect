using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CreateSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — organizer-facing create. Returns the new package's Id so the
/// FE can immediately re-fetch and select it. Authorization (event-organizer
/// check) is performed in the controller via VerifyOrganizerAsync.
/// </summary>
public record CreateSponsorshipPackageCommand(
    Guid EventId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? QuantityLimit,
    int SortOrder,
    string? Tier,
    IReadOnlyList<string>? Perks,
    int IncludedTicketCount
) : ICommand<Guid>;

using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Commands.UpdateSponsor;

/// <summary>
/// Phase 6A.151 — PATCH-shape command to update content fields on an existing
/// sponsor. All fields nullable: <c>null</c> means "leave unchanged".
///
/// Authz (enforced in handler):
///   - <c>ActingUserId == Sponsor.SponsorUserId</c> AND Sponsor.SponsorUserId is not null
///     → self-edit allowed (subject to state matrix)
///   - <c>Event.IsOrganizer(ActingUserId) == true</c> → organizer-edit allowed
///   - Otherwise → 403
///
/// State matrix enforcement lives inside the <c>Sponsor</c> aggregate
/// (UpdateContactFields / UpdateName / UpdateAmount / UpdateItemDetails) — the
/// handler is the orchestrator. See <see cref="LankaConnect.Domain.Events.Sponsor"/>
/// for the cell-by-cell rules.
///
/// Returns the updated <see cref="SponsorDto"/> so the FE can refresh state
/// without a follow-up GET.
/// </summary>
public record UpdateSponsorCommand(
    Guid EventId,
    Guid SponsorId,
    Guid ActingUserId,
    // PATCH fields — null = leave unchanged
    string? Name = null,
    string? Notes = null,
    string? Organization = null,
    decimal? Amount = null,
    string? Currency = null,  // Required iff Amount != null; honored from request
    string? ItemName = null,
    string? ItemDescription = null,
    decimal? EstimatedValue = null
) : ICommand<SponsorDto>;

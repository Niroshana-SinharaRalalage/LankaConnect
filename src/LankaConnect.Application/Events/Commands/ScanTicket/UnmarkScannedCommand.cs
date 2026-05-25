using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ScanTicket;

/// <summary>
/// Phase 6A.141 — admin override that reverses a prior accepted scan. Used by event
/// organizers (or higher) to recover from a wrongly-scanned ticket so the attendee
/// can re-scan and walk in. Always writes a new <c>TicketScanLog</c> row with
/// <c>scan_result = 'unmarked'</c> + the admin's stated reason, preserving the
/// forensic trail.
///
/// Auth: API endpoint should require Admin policy (event organizers do not have
/// unmark capability by default — too easy to abuse during disputes).
/// </summary>
public record UnmarkScannedCommand(
    Guid EventId,
    string TicketCode,
    Guid AdminUserId,
    string? AdminName,
    string Reason,
    string? ClientIp,
    string? UserAgent
) : ICommand<UnmarkScannedResult>;

/// <summary>Outcome of an admin-unmark attempt.</summary>
public record UnmarkScannedResult(string TicketCode, DateTime UnmarkedAt);

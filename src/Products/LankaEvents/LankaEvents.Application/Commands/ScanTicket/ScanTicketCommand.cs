using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket;

/// <summary>
/// Phase 6A.141 — paid-event ticket check-in scan command.
///
/// One command shape covers both gate-staff scan paths:
/// <list type="bullet">
///   <item><b>QR scan</b>: <see cref="QrPayload"/> is set, <see cref="TicketCode"/> is null.
///     Handler parses the payload via <c>TicketSignedPayload.TryParse</c>, verifies the HMAC
///     signature, then proceeds.</item>
///   <item><b>Manual entry</b>: <see cref="TicketCode"/> is set, <see cref="QrPayload"/> is null.
///     Handler skips signature verification (trust comes from organizer auth) and looks up
///     the ticket by code directly.</item>
/// </list>
///
/// Exactly one of the two fields must be set; setting both or neither is rejected as
/// a malformed request.
///
/// <see cref="EventId"/> is the URL-path event the scanner UI is operating on — used to
/// confirm the scanned ticket actually belongs to this event (rejects <c>wrong_event</c>
/// when a staff scans a QR from a different event).
/// </summary>
public record ScanTicketCommand(
    Guid EventId,
    Guid ScannerUserId,
    string? ScannerName,
    string? QrPayload,
    string? TicketCode,
    string? ClientIp,
    string? UserAgent
) : ICommand<ScanTicketResult>
{
    /// <summary>True iff this is a manual-entry scan (caller used the typed-code endpoint).</summary>
    public bool IsManualEntry => !string.IsNullOrWhiteSpace(TicketCode) && string.IsNullOrWhiteSpace(QrPayload);

    /// <summary>True iff this is a QR scan (caller used the camera endpoint).</summary>
    public bool IsQrScan => !string.IsNullOrWhiteSpace(QrPayload) && string.IsNullOrWhiteSpace(TicketCode);
}

using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Phase 6A.141 — Paid-Event Ticket Check-in audit log.
///
/// One row per scan attempt — accepted, rejected (any reason), or admin-unmarked.
/// Survives even if the underlying <c>Ticket</c> is later deleted (FK is nullable
/// soft-reference) so dispute-resolution queries don't lose data.
///
/// Rejected attempts where signature verification failed (forgery / malformed) have
/// no associated ticket; those rows have <see cref="TicketId"/> = null and the
/// <see cref="TicketCode"/> field is also null because we couldn't decode the payload
/// to extract one.
///
/// Construction goes through one of three factory methods to keep callers from
/// inventing inconsistent shapes:
/// <list type="bullet">
///   <item><see cref="Accepted"/> — happy path scan</item>
///   <item><see cref="Rejected"/> — any business-rule rejection (already scanned,
///         expired, invalidated, wrong event, invalid signature, etc.)</item>
///   <item><see cref="AdminUnmark"/> — admin override that reverses a prior accept</item>
/// </list>
/// </summary>
public class TicketScanLog : LegacyBaseEntity
{
    // ============================================================
    // Constants — wire-compatible string values
    // ============================================================

    // ScanResult values (stored in scan_result column as plain strings)
    public const string ScanResultAccepted = "accepted";
    public const string ScanResultRejected = "rejected";
    public const string ScanResultUnmarked = "unmarked";

    // EntryMethod values (stored in entry_method column as plain strings)
    public const string EntryMethodQr = "qr";
    public const string EntryMethodQrLegacy = "qr_legacy";
    public const string EntryMethodManual = "manual";
    public const string EntryMethodAdminUnmark = "admin_unmark";

    // RejectionReason values (stored in rejection_reason column as plain strings).
    // Kept as constants here so the API DTO and the audit row stay in sync — the
    // scanner UI keys on these to pick the human-readable reason message.
    public const string ReasonInvalidSignature = "invalid_signature";
    public const string ReasonMalformedPayload = "malformed_payload";
    public const string ReasonTicketNotFound = "ticket_not_found";
    public const string ReasonWrongEvent = "wrong_event";
    public const string ReasonExpired = "expired";
    public const string ReasonInvalidated = "invalidated";
    public const string ReasonAlreadyScanned = "already_scanned";

    // ============================================================
    // Persisted state
    // ============================================================

    /// <summary>FK to <c>Tickets.Id</c> when the scan resolved to a real ticket.
    /// Nullable because failed-signature / not-found attempts have no ticket.</summary>
    public Guid? TicketId { get; private set; }

    /// <summary>FK to <c>Events.Id</c> — always present. For invalid-signature attempts
    /// this is the event the scanner was viewing (from the URL path).</summary>
    public Guid EventId { get; private set; }

    /// <summary>The ticket code (LC-YYYY-XXXXXX) at the time of scan. Denormalized so
    /// forensic queries by code survive a later ticket-row delete. Null for
    /// failed-signature attempts because we couldn't decode the payload to extract one.</summary>
    public string? TicketCode { get; private set; }

    /// <summary>FK to <c>Users.Id</c> — the organizer / co-organizer who operated the
    /// scanner. Always present (scan endpoint requires authn).</summary>
    public Guid ScannerUserId { get; private set; }

    /// <summary>Denormalized display name of the scanner at the time of scan, so
    /// audit reports survive a later user rename.</summary>
    public string? ScannerName { get; private set; }

    /// <summary>One of <see cref="ScanResultAccepted"/> / <see cref="ScanResultRejected"/>
    /// / <see cref="ScanResultUnmarked"/>.</summary>
    public string ScanResult { get; private set; } = string.Empty;

    /// <summary>One of the <c>Reason*</c> constants when <see cref="ScanResult"/> is
    /// <see cref="ScanResultRejected"/>. Null otherwise.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>One of the <see cref="EntryMethodQr"/> / <see cref="EntryMethodQrLegacy"/>
    /// / <see cref="EntryMethodManual"/> / <see cref="EntryMethodAdminUnmark"/>.</summary>
    public string EntryMethod { get; private set; } = string.Empty;

    /// <summary>Phase 6A.141 F5: true when the QR signature verified against the
    /// PREVIOUS (rotated-out) key rather than the current one. Lets the operator
    /// see when the grace window is still being utilised; can also help bound
    /// when the previous key can safely be dropped from KV.</summary>
    public bool VerifiedWithPreviousKey { get; private set; }

    /// <summary>Client IP from <c>X-Forwarded-For</c> (Azure Front Door fronts requests).
    /// 45 chars max to accommodate IPv6. Nullable because some test paths won't have a
    /// real request context.</summary>
    public string? ClientIp { get; private set; }

    /// <summary>User-Agent header at scan time. Lets us spot anomalies (e.g. a
    /// scripted scanner that's not the official UI).</summary>
    public string? UserAgent { get; private set; }

    private TicketScanLog() { } // EF Core

    // ============================================================
    // Factories
    // ============================================================

    /// <summary>
    /// Happy-path scan — the ticket was found, the signature verified (or was legacy
    /// + manual-entry), and the atomic UPDATE successfully marked it scanned.
    /// </summary>
    public static TicketScanLog Accepted(
        Guid ticketId,
        Guid eventId,
        string ticketCode,
        Guid scannerUserId,
        string? scannerName,
        string entryMethod,
        bool verifiedWithPreviousKey,
        string? clientIp,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            throw new ArgumentException("ticketCode is required for accepted scans", nameof(ticketCode));
        if (!IsValidEntryMethod(entryMethod))
            throw new ArgumentException($"Unknown entryMethod '{entryMethod}'", nameof(entryMethod));

        return new TicketScanLog
        {
            TicketId = ticketId,
            EventId = eventId,
            TicketCode = ticketCode,
            ScannerUserId = scannerUserId,
            ScannerName = scannerName,
            ScanResult = ScanResultAccepted,
            RejectionReason = null,
            EntryMethod = entryMethod,
            VerifiedWithPreviousKey = verifiedWithPreviousKey,
            ClientIp = clientIp,
            UserAgent = userAgent,
        };
    }

    /// <summary>
    /// Rejected scan — any business-rule failure. Pass null for <paramref name="ticketId"/>
    /// and <paramref name="ticketCode"/> when the reason is <see cref="ReasonInvalidSignature"/>
    /// or <see cref="ReasonMalformedPayload"/> (couldn't decode to extract them).
    /// </summary>
    public static TicketScanLog Rejected(
        Guid? ticketId,
        Guid eventId,
        string? ticketCode,
        Guid scannerUserId,
        string? scannerName,
        string rejectionReason,
        string entryMethod,
        bool verifiedWithPreviousKey,
        string? clientIp,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("rejectionReason is required for rejected scans", nameof(rejectionReason));
        if (!IsValidEntryMethod(entryMethod))
            throw new ArgumentException($"Unknown entryMethod '{entryMethod}'", nameof(entryMethod));

        return new TicketScanLog
        {
            TicketId = ticketId,
            EventId = eventId,
            TicketCode = ticketCode,
            ScannerUserId = scannerUserId,
            ScannerName = scannerName,
            ScanResult = ScanResultRejected,
            RejectionReason = rejectionReason,
            EntryMethod = entryMethod,
            VerifiedWithPreviousKey = verifiedWithPreviousKey,
            ClientIp = clientIp,
            UserAgent = userAgent,
        };
    }

    /// <summary>
    /// Admin override reversing a prior accepted scan. The original audit row stays;
    /// this row records the reversal with the reason text given by the admin.
    /// </summary>
    public static TicketScanLog AdminUnmark(
        Guid ticketId,
        Guid eventId,
        string ticketCode,
        Guid scannerUserId,
        string? scannerName,
        string reason,
        string? clientIp,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            throw new ArgumentException("ticketCode is required for admin-unmark", nameof(ticketCode));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("reason is required for admin-unmark", nameof(reason));

        return new TicketScanLog
        {
            TicketId = ticketId,
            EventId = eventId,
            TicketCode = ticketCode,
            ScannerUserId = scannerUserId,
            ScannerName = scannerName,
            ScanResult = ScanResultUnmarked,
            RejectionReason = reason, // re-using the field to capture admin's text reason
            EntryMethod = EntryMethodAdminUnmark,
            VerifiedWithPreviousKey = false,
            ClientIp = clientIp,
            UserAgent = userAgent,
        };
    }

    private static bool IsValidEntryMethod(string entryMethod) =>
        entryMethod is EntryMethodQr or EntryMethodQrLegacy or EntryMethodManual or EntryMethodAdminUnmark;
}

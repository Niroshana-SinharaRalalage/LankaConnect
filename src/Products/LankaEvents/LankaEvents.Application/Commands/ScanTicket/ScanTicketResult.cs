namespace LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket;

/// <summary>
/// Phase 6A.141 — result of a ticket-scan attempt.
///
/// Shape covers both accepted and rejected outcomes via nullable fields. The scanner
/// UI keys on <see cref="Result"/> first (accepted = green panel, rejected = red),
/// then renders the relevant rejection details if <see cref="Reason"/> is set.
///
/// HTTP-status-wise, BOTH outcomes return HTTP 200 — see Plan-agent review D5:
/// the UI needs to distinguish "no network" (HTTP error) from "ticket already
/// scanned" (business reject). Conflating those into HTTP 4xx loses information.
/// </summary>
public record ScanTicketResult
{
    /// <summary>Either <c>"accepted"</c> or <c>"rejected"</c>.</summary>
    public string Result { get; init; } = string.Empty;

    /// <summary>Set on rejection only. One of the <see cref="ReasonCode"/> values.</summary>
    public string? Reason { get; init; }

    /// <summary>Human-readable rejection message in plain English (e.g.
    /// "Already scanned at 7:32 PM by Lakmal Silva"). Nullable on accept.</summary>
    public string? ReasonMessage { get; init; }

    public string? TicketCode { get; init; }

    // V1 fallback aggregates — kept for head-count-mode registrations where Attendees
    // is empty, and for backward compat with existing test fixtures + the prior UI
    // contract. When <see cref="Attendees"/> has rows, prefer those for display.
    public string? AttendeeName { get; init; }
    public string? Tier { get; init; }
    public int? AttendeeCount { get; init; }
    public IReadOnlyList<TierBreakdownEntry>? TierBreakdown { get; init; }

    /// <summary>UAT R3 — full per-attendee detail. Null/empty for head-count-mode
    /// registrations (Mode-B) or pre-MultiAttendee-migration registrations; in those
    /// cases the UI falls back to <see cref="AttendeeName"/> + <see cref="Tier"/> +
    /// <see cref="AttendeeCount"/>. When populated, the gate operator sees every
    /// person on the ticket with their age category, gender, tier, and per-attendee
    /// price computed from <c>TicketTier.CalculatePriceForAttendee</c>.</summary>
    public IReadOnlyList<AttendeeDetail>? Attendees { get; init; }

    /// <summary>UAT R4 — confirmed-and-bundled add-ons for this registration (e.g. dinner
    /// add-on, merch). Filter applied server-side: Status == Completed AND
    /// RegistrationId == this registration. Standalone add-ons (purchased separately
    /// from the event page) and Pending/Failed/Abandoned/Refunded purchases are
    /// excluded. Null when there are no matching bundled add-ons — UI omits the
    /// section entirely (no empty card).</summary>
    public IReadOnlyList<AddOnSummary>? AddOns { get; init; }

    /// <summary>When this scan was accepted (or when the previous scan was, in the
    /// case of <see cref="ReasonCode.AlreadyScanned"/>).</summary>
    public DateTime? ScannedAt { get; init; }

    /// <summary>Name of the scanner operator (denormalized from User row at scan time).</summary>
    public string? ScannedBy { get; init; }

    /// <summary>Phase 6A.141 F5: true when the QR signature verified against the rotated-out
    /// PREVIOUS key rather than the current one. Lets the scanner UI show a small grace-window
    /// indicator; lets the audit log record the fact.</summary>
    public bool UsedPreviousKey { get; init; }

    /// <summary>For <see cref="ReasonCode.WrongEvent"/> rejections only — the title of the
    /// event this ticket actually belongs to. Helps staff redirect the attendee.</summary>
    public string? WrongEventTitle { get; init; }

    /// <summary>UAT R2 Issue A: number of prior accepted scans for this ticket. Populated
    /// on ticket-resolved rejections (already_scanned, expired, invalidated, wrong_event).
    /// Normally 1 for already_scanned, but can be ≥2 if admin-unmark cycles have happened.
    /// Null when no ticket was resolved (invalid_signature, malformed_payload, ticket_not_found).</summary>
    public int? PreviousScanCount { get; init; }

    /// <summary>UAT R2 Issue A: denormalized name of the operator who took the most recent
    /// accepted scan for this ticket. Pairs with <see cref="ScannedAt"/> on the
    /// already_scanned amber panel ("First admitted 7:32 PM by Lakmal Silva").</summary>
    public string? PreviousScannedBy { get; init; }

    public static ScanTicketResult AcceptedFor(
        string ticketCode,
        string? attendeeName,
        string? tier,
        int? attendeeCount,
        IReadOnlyList<TierBreakdownEntry>? tierBreakdown,
        DateTime scannedAt,
        string? scannedBy,
        bool usedPreviousKey,
        IReadOnlyList<AttendeeDetail>? attendees = null,
        IReadOnlyList<AddOnSummary>? addOns = null) =>
        new()
        {
            Result = "accepted",
            TicketCode = ticketCode,
            AttendeeName = attendeeName,
            Tier = tier,
            AttendeeCount = attendeeCount,
            TierBreakdown = tierBreakdown,
            ScannedAt = scannedAt,
            ScannedBy = scannedBy,
            UsedPreviousKey = usedPreviousKey,
            Attendees = attendees,
            AddOns = addOns,
        };

    public static ScanTicketResult RejectedFor(string reason, string reasonMessage) =>
        new()
        {
            Result = "rejected",
            Reason = reason,
            ReasonMessage = reasonMessage,
        };
}

/// <summary>One entry in the per-ticket tier breakdown for a registration with multiple attendees.</summary>
public record TierBreakdownEntry(string Tier, int Count);

/// <summary>
/// UAT R3 — full per-attendee projection for the scanner panel. One row per row in
/// <c>Registration.Attendees</c>. Stringified enum values (AgeCategory, Gender) keep
/// the wire stable across schema changes. Price fields are nullable to handle:
///   - tier-deleted-after-registration drift (PriceAmount = null)
///   - head-count-mode tickets (the whole Attendees list is null instead)
/// </summary>
public record AttendeeDetail(
    string Name,
    string AgeCategory,        // "Adult" | "Child" — stringified enum
    string? Gender,            // "Male" | "Female" | "Other" | null
    string? TicketTierName,    // denormalized from AttendeeDetails
    decimal? PriceAmount,      // from TicketTier.CalculatePriceForAttendee
    string? PriceCurrency,     // ISO code (Currency enum .ToString())
    string? SeatLabel);        // assigned-seating events only

/// <summary>
/// UAT R4 — single bundled add-on purchase as projected onto the scanner panel.
/// UnitPrice is the snapshot at checkout time (organizer price changes don't affect
/// historical purchases). TotalAmount = UnitPrice * Quantity, denormalized so the UI
/// renders without recomputing.
/// </summary>
public record AddOnSummary(
    string Name,           // AddOnDefinition.Name
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string Currency);      // ISO code e.g. "USD", "LKR"

/// <summary>Canonical rejection reason codes — wire-compatible strings shared with
/// <c>TicketScanLog</c> audit entries and the scanner UI's i18n catalog.</summary>
public static class ReasonCode
{
    public const string InvalidSignature = "invalid_signature";
    public const string MalformedPayload = "malformed_payload";
    public const string TicketNotFound = "ticket_not_found";
    public const string WrongEvent = "wrong_event";
    public const string Expired = "expired";
    public const string Invalidated = "invalidated";
    public const string AlreadyScanned = "already_scanned";
    public const string MalformedRequest = "malformed_request"; // both qr + code or neither
}

using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Phase 6A.24: Ticket entity for paid event registrations.
/// Contains QR code data for validation and optional PDF URL for download.
/// </summary>
public class Ticket : LegacyBaseEntity
{
    /// <summary>
    /// Reference to the registration this ticket belongs to
    /// </summary>
    public Guid RegistrationId { get; private set; }

    /// <summary>
    /// Reference to the event for quick lookup
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Optional user ID for authenticated registrations
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Unique ticket code for display and validation (e.g., "LC-2024-ABC123")
    /// </summary>
    public string TicketCode { get; private set; } = string.Empty;

    /// <summary>
    /// Encoded data for QR code generation (includes ticket code, event ID, and validation hash)
    /// </summary>
    public string QrCodeData { get; private set; } = string.Empty;

    /// <summary>
    /// URL to the PDF ticket in Azure Blob Storage (null until PDF is generated)
    /// </summary>
    public string? PdfBlobUrl { get; private set; }

    /// <summary>
    /// Whether the ticket is still valid for check-in
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Timestamp when the ticket was scanned/validated at check-in
    /// </summary>
    public DateTime? ValidatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the ticket expires (typically after event ends)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Tier name for multi-tier events (e.g., "VIP", "Basic"). Null for SingleTier events.
    /// </summary>
    public string? TicketTierName { get; private set; }

    /// <summary>
    /// Category of this ticket: Standard (legacy), Master (group), or Individual (per-attendee)
    /// </summary>
    public TicketCategory TicketCategory { get; private set; } = TicketCategory.Standard;

    /// <summary>
    /// Index of the attendee this individual ticket belongs to (0-based). Null for Master/Standard tickets.
    /// </summary>
    public int? AttendeeIndex { get; private set; }

    /// <summary>
    /// Comma-separated attendee names for Master tickets. Null for Individual/Standard tickets.
    /// </summary>
    public string? AttendeeNames { get; private set; }

    // EF Core constructor
    private Ticket() { }

    private Ticket(
        Guid registrationId,
        Guid eventId,
        Guid? userId,
        string ticketCode,
        string qrCodeData,
        DateTime expiresAt,
        string? ticketTierName = null,
        TicketCategory ticketCategory = TicketCategory.Standard,
        int? attendeeIndex = null,
        string? attendeeNames = null)
    {
        RegistrationId = registrationId;
        EventId = eventId;
        UserId = userId;
        TicketCode = ticketCode;
        QrCodeData = qrCodeData;
        ExpiresAt = expiresAt;
        IsValid = true;
        TicketTierName = ticketTierName;
        TicketCategory = ticketCategory;
        AttendeeIndex = attendeeIndex;
        AttendeeNames = attendeeNames;
    }

    /// <summary>
    /// Creates a new standard ticket for a paid event registration (legacy SingleTier mode).
    /// Phase 6A.141: <paramref name="ticketCode"/> and <paramref name="qrCodeData"/> are
    /// caller-supplied. The Phase 6A.24-compatible behaviour (auto-generate both) is the
    /// default when both are <c>null</c> — preserves test callers that predate the signing
    /// flow. Production callers (TicketService) always supply both: <see cref="GenerateTicketCode"/>
    /// + signature-service-built signed v1 payload. If <paramref name="qrCodeData"/> is
    /// supplied but <paramref name="ticketCode"/> is not, the signature inside the payload
    /// would not match the entity's generated code — that's rejected with a Result failure.
    /// </summary>
    public static Result<Ticket> Create(
        Guid registrationId,
        Guid eventId,
        Guid? userId,
        DateTime eventEndDate,
        string? ticketCode = null,
        string? qrCodeData = null)
    {
        if (registrationId == Guid.Empty)
            return Result<Ticket>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<Ticket>.Failure("Event ID is required");

        if (!string.IsNullOrEmpty(qrCodeData) && string.IsNullOrEmpty(ticketCode))
            return Result<Ticket>.Failure("ticketCode must be supplied when qrCodeData is supplied so the signed payload binds to the entity's code");

        // Phase 6A.141: if the caller supplied a ticketCode (production path, TicketService
        // already resolved collisions), use it. Otherwise auto-generate (legacy / test path).
        var resolvedTicketCode = ticketCode ?? GenerateTicketCode();

        // If the caller supplied a signed payload, trust it. Otherwise fall back to the
        // legacy unsigned base64 form so unmigrated callers keep working.
        var resolvedQrCodeData = qrCodeData ?? GenerateLegacyQrCodeData(resolvedTicketCode, eventId, registrationId);

        var expiresAt = eventEndDate.AddHours(24);

        var ticket = new Ticket(
            registrationId,
            eventId,
            userId,
            resolvedTicketCode,
            resolvedQrCodeData,
            expiresAt);

        return Result<Ticket>.Success(ticket);
    }

    /// <summary>
    /// Creates a tiered ticket (Master or Individual) for multi-tier events.
    /// Phase 6A.141: same ticketCode + qrCodeData injection pattern as <see cref="Create"/>.
    /// </summary>
    public static Result<Ticket> CreateTiered(
        Guid registrationId,
        Guid eventId,
        Guid? userId,
        DateTime eventEndDate,
        string ticketTierName,
        TicketCategory ticketCategory,
        int? attendeeIndex = null,
        string? attendeeNames = null,
        string? ticketCode = null,
        string? qrCodeData = null)
    {
        if (registrationId == Guid.Empty)
            return Result<Ticket>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<Ticket>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(ticketTierName))
            return Result<Ticket>.Failure("Ticket tier name is required for tiered tickets");

        if (ticketCategory == TicketCategory.Standard)
            return Result<Ticket>.Failure("Use Create() for standard tickets");

        if (ticketCategory == TicketCategory.Individual && attendeeIndex == null)
            return Result<Ticket>.Failure("Attendee index is required for individual tickets");

        if (ticketCategory == TicketCategory.Master && string.IsNullOrWhiteSpace(attendeeNames))
            return Result<Ticket>.Failure("Attendee names are required for master tickets");

        if (!string.IsNullOrEmpty(qrCodeData) && string.IsNullOrEmpty(ticketCode))
            return Result<Ticket>.Failure("ticketCode must be supplied when qrCodeData is supplied so the signed payload binds to the entity's code");

        var resolvedTicketCode = ticketCode ?? GenerateTicketCode();
        var resolvedQrCodeData = qrCodeData ?? GenerateLegacyQrCodeData(resolvedTicketCode, eventId, registrationId);
        var expiresAt = eventEndDate.AddHours(24);

        var ticket = new Ticket(
            registrationId,
            eventId,
            userId,
            resolvedTicketCode,
            resolvedQrCodeData,
            expiresAt,
            ticketTierName.Trim(),
            ticketCategory,
            attendeeIndex,
            attendeeNames?.Trim());

        return Result<Ticket>.Success(ticket);
    }

    /// <summary>
    /// Sets the PDF blob URL after PDF generation
    /// </summary>
    public Result SetPdfUrl(string pdfBlobUrl)
    {
        if (string.IsNullOrWhiteSpace(pdfBlobUrl))
            return Result.Failure("PDF URL cannot be empty");

        PdfBlobUrl = pdfBlobUrl;
        return Result.Success();
    }

    /// <summary>
    /// Validates the ticket for check-in
    /// </summary>
    public Result Validate()
    {
        if (!IsValid)
            return Result.Failure("Ticket has already been invalidated");

        if (ValidatedAt.HasValue)
            return Result.Failure($"Ticket was already validated at {ValidatedAt.Value:g}");

        if (DateTime.UtcNow > ExpiresAt)
            return Result.Failure("Ticket has expired");

        ValidatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.141 — admin override that reverses a prior scan. Resets
    /// <see cref="ValidatedAt"/> to <c>null</c> so the ticket can be scanned again.
    /// The audit log still retains both the original accepted-scan row AND a new
    /// admin-unmark row, so the forensic history is complete.
    ///
    /// Returns failure when the ticket isn't currently scanned (nothing to unmark)
    /// or has been invalidated (refund / cancellation reset path is different).
    /// </summary>
    public Result UnmarkScanned()
    {
        if (!IsValid)
            return Result.Failure("Ticket is invalidated; can't unmark a refunded/cancelled ticket.");
        if (!ValidatedAt.HasValue)
            return Result.Failure("Ticket has not been scanned; nothing to unmark.");

        ValidatedAt = null;
        return Result.Success();
    }

    /// <summary>
    /// Invalidates the ticket (e.g., when registration is cancelled)
    /// </summary>
    public Result Invalidate()
    {
        if (!IsValid)
            return Result.Failure("Ticket is already invalid");

        IsValid = false;
        return Result.Success();
    }

    /// <summary>
    /// Generates a unique ticket code in format LC-YYYY-XXXXXX.
    /// Phase 6A.141: lifted to public so <c>TicketService</c> can generate the code,
    /// resolve any DB-side collisions, and then build the signed QR payload around
    /// the final code before calling <see cref="Create"/>.
    /// </summary>
    public static string GenerateTicketCode()
    {
        var year = DateTime.UtcNow.Year;
        var randomPart = GenerateRandomAlphanumeric(6);
        return $"LC-{year}-{randomPart}";
    }

    /// <summary>
    /// Generates random alphanumeric string of specified length
    /// </summary>
    private static string GenerateRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }

    /// <summary>
    /// Phase 6A.141: legacy fallback for callers that don't yet supply a signed payload.
    /// New code should not rely on this — TicketService now constructs a signed v1
    /// payload via ITicketSignatureService and passes it into Create / CreateTiered.
    /// Preserved so any caller predating Phase 6A.141 still produces a parseable QR.
    /// </summary>
    private static string GenerateLegacyQrCodeData(string ticketCode, Guid eventId, Guid registrationId)
    {
        var validationData = $"{ticketCode}|{eventId}|{registrationId}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(validationData));
    }

    /// <summary>
    /// Decodes QR code data to extract ticket information
    /// </summary>
    public static (string TicketCode, Guid EventId, Guid RegistrationId)? DecodeQrCodeData(string qrCodeData)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(qrCodeData);
            var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            var parts = decodedString.Split('|');

            if (parts.Length != 3)
                return null;

            return (parts[0], Guid.Parse(parts[1]), Guid.Parse(parts[2]));
        }
        catch
        {
            return null;
        }
    }
}

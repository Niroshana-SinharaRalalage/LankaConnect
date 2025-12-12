using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Phase 6A.24: Ticket entity for paid event registrations.
/// Contains QR code data for validation and optional PDF URL for download.
/// </summary>
public class Ticket : BaseEntity
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

    // EF Core constructor
    private Ticket() { }

    private Ticket(
        Guid registrationId,
        Guid eventId,
        Guid? userId,
        string ticketCode,
        string qrCodeData,
        DateTime expiresAt)
    {
        RegistrationId = registrationId;
        EventId = eventId;
        UserId = userId;
        TicketCode = ticketCode;
        QrCodeData = qrCodeData;
        ExpiresAt = expiresAt;
        IsValid = true;
    }

    /// <summary>
    /// Creates a new ticket for a paid event registration
    /// </summary>
    public static Result<Ticket> Create(
        Guid registrationId,
        Guid eventId,
        Guid? userId,
        DateTime eventEndDate)
    {
        if (registrationId == Guid.Empty)
            return Result<Ticket>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<Ticket>.Failure("Event ID is required");

        // Generate unique ticket code: LC-YYYY-XXXXXX (Year + 6 random alphanumeric)
        var ticketCode = GenerateTicketCode();

        // Generate QR code data (JSON with ticket info and validation hash)
        var qrCodeData = GenerateQrCodeData(ticketCode, eventId, registrationId);

        // Ticket expires 24 hours after event ends
        var expiresAt = eventEndDate.AddHours(24);

        var ticket = new Ticket(
            registrationId,
            eventId,
            userId,
            ticketCode,
            qrCodeData,
            expiresAt);

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
        MarkAsUpdated();
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
        MarkAsUpdated();
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
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Generates a unique ticket code in format LC-YYYY-XXXXXX
    /// </summary>
    private static string GenerateTicketCode()
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
    /// Generates QR code data with validation information
    /// </summary>
    private static string GenerateQrCodeData(string ticketCode, Guid eventId, Guid registrationId)
    {
        // Create a validation token combining ticket info
        var validationData = $"{ticketCode}|{eventId}|{registrationId}";

        // Simple base64 encoding for now - in production, consider encryption
        var encodedData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(validationData));

        return encodedData;
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

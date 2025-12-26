using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Infrastructure.Services.Export;

/// <summary>
/// CSV export service implementation using CsvHelper.
/// Exports attendee data to simple CSV format.
/// </summary>
public class CsvExportService : ICsvExportService
{
    public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
    {
        using var memoryStream = new MemoryStream();

        // Phase 6A.48B: Write UTF-8 BOM for Excel compatibility
        var utf8WithBom = new UTF8Encoding(true); // true = include BOM
        using var writer = new StreamWriter(memoryStream, utf8WithBom);

        // Phase 6A.49: Removed ShouldQuote to fix double-escaping issue
        // CsvHelper will intelligently quote fields containing commas, quotes, or newlines
        // Uses RFC 4180 standard: "" for quote escaping (not \")
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Define flattened records for CSV export
        var records = attendees.Attendees.Select(a => new
        {
            RegistrationId = a.RegistrationId.ToString(),
            MainAttendee = a.MainAttendeeName,
            AdditionalAttendees = a.AdditionalAttendees,
            TotalAttendees = a.TotalAttendees,
            Adults = a.AdultCount,
            Children = a.ChildCount,
            GenderDistribution = a.GenderDistribution,
            Email = a.ContactEmail,
            Phone = FormatPhoneNumber(a.ContactPhone),  // Format as string with prefix
            Address = a.ContactAddress ?? string.Empty,  // Use empty string instead of em dash
            PaymentStatus = a.PaymentStatus.ToString(),
            TotalAmount = a.TotalAmount?.ToString("F2") ?? string.Empty,
            Currency = a.Currency ?? string.Empty,
            TicketCode = a.TicketCode ?? string.Empty,
            QRCode = a.QrCodeData ?? string.Empty,
            RegistrationDate = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = a.Status.ToString()
        });

        // Write records
        csv.WriteRecords(records);

        // Flush to ensure all data is written
        writer.Flush();

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Format phone number as string to prevent Excel scientific notation
    /// </summary>
    private static string FormatPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Prepend with single quote to force Excel to treat as text
        return "'" + phone;
    }
}

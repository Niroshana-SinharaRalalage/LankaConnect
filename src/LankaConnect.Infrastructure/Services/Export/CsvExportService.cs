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
        // Phase 6A.XX: Set NewLine to CRLF for Excel compatibility on Windows
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            NewLine = "\r\n"  // Excel requires Windows line endings (CRLF)
        });

        // Define flattened records for CSV export
        // Phase 6A.63: Fixed CSV export to properly display attendee data
        // - Compute AdditionalAttendees as comma-separated names
        // - Compute GenderDistribution as readable string
        // - Remove phone number prefix (Excel handles numbers better in CSV)
        var records = attendees.Attendees.Select(a => new
        {
            RegistrationId = a.RegistrationId.ToString(),
            MainAttendee = a.Attendees.FirstOrDefault()?.Name ?? "Unknown",
            AdditionalAttendees = a.TotalAttendees > 1
                ? string.Join(", ", a.Attendees.Skip(1).Select(att => att.Name))
                : string.Empty,
            TotalAttendees = a.TotalAttendees,
            Adults = a.AdultCount,
            Children = a.ChildCount,
            MaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Male),
            FemaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Female),
            GenderDistribution = GetGenderDistribution(a.Attendees),
            Email = a.ContactEmail,
            Phone = a.ContactPhone ?? string.Empty,  // No prefix, let Excel handle it
            Address = a.ContactAddress ?? string.Empty,
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
    /// Generate gender distribution string (e.g., "2 Male, 1 Female")
    /// </summary>
    private static string GetGenderDistribution(List<AttendeeDetailsDto> attendees)
    {
        var genderCounts = attendees
            .Where(a => a.Gender.HasValue)
            .GroupBy(a => a.Gender!.Value)
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        return genderCounts.Any() ? string.Join(", ", genderCounts) : string.Empty;
    }
}

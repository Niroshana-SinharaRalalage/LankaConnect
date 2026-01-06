using System.Text;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Infrastructure.Services.Export;

/// <summary>
/// CSV export service implementation.
/// Exports attendee data to simple CSV format with manual string building.
/// Phase 6A.66: Replaced CsvHelper with manual CSV building to fix Excel line ending issues.
/// </summary>
public class CsvExportService : ICsvExportService
{
    public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
    {
        // Phase 6A.66: Build CSV manually like the working signup list export
        // This guarantees CRLF line endings that Excel recognizes
        var csv = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        csv.Append('\uFEFF');

        // Header row with explicit CRLF
        csv.Append("RegistrationId,MainAttendee,AdditionalAttendees,TotalAttendees,Adults,Children,MaleCount,FemaleCount,GenderDistribution,Email,Phone,Address,PaymentStatus,TotalAmount,Currency,TicketCode,QRCode,RegistrationDate,Status\r\n");

        // Data rows
        foreach (var a in attendees.Attendees)
        {
            var mainAttendee = a.Attendees.FirstOrDefault()?.Name ?? "Unknown";
            var additionalAttendees = a.TotalAttendees > 1
                ? string.Join(", ", a.Attendees.Skip(1).Select(att => att.Name))
                : string.Empty;
            var maleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Male);
            var femaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Female);
            var genderDistribution = GetGenderDistribution(a.Attendees);

            // Escape fields that might contain commas or quotes
            csv.Append($"\"{EscapeCsvField(a.RegistrationId.ToString())}\",");
            csv.Append($"\"{EscapeCsvField(mainAttendee)}\",");
            csv.Append($"\"{EscapeCsvField(additionalAttendees)}\",");
            csv.Append($"{a.TotalAttendees},");
            csv.Append($"{a.AdultCount},");
            csv.Append($"{a.ChildCount},");
            csv.Append($"{maleCount},");
            csv.Append($"{femaleCount},");
            csv.Append($"\"{EscapeCsvField(genderDistribution)}\",");
            csv.Append($"\"{EscapeCsvField(a.ContactEmail)}\",");
            csv.Append($"\"{EscapeCsvField(a.ContactPhone ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.ContactAddress ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.PaymentStatus.ToString())}\",");
            csv.Append($"\"{EscapeCsvField(a.TotalAmount?.ToString("F2") ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.Currency ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.TicketCode ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.QrCodeData ?? string.Empty)}\",");
            csv.Append($"\"{EscapeCsvField(a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))}\",");
            csv.Append($"\"{EscapeCsvField(a.Status.ToString())}\"");
            csv.Append("\r\n"); // Explicit CRLF
        }

        // Convert to UTF-8 bytes with BOM
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Escape CSV field by replacing double quotes with two double quotes (RFC 4180)
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        return field?.Replace("\"", "\"\"") ?? string.Empty;
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

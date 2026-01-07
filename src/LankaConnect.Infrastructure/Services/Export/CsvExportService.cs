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
        // Phase 6A.66: Build CSV manually exactly like signup list export
        // UTF-8 BOM for Excel compatibility
        var csv = new StringBuilder();
        csv.Append('\uFEFF');

        // Header row - use LF only (like signup list client-side export that works)
        csv.Append("RegistrationId,MainAttendee,AdditionalAttendees,TotalAttendees,Adults,Children,MaleCount,FemaleCount,GenderDistribution,Email,Phone,Address,PaymentStatus,TotalAmount,Currency,TicketCode,QRCode,RegistrationDate,Status\n");

        // Data rows
        foreach (var a in attendees.Attendees)
        {
            var mainAttendee = a.Attendees.FirstOrDefault()?.Name ?? "Unknown";
            var additionalAttendees = a.TotalAttendees > 1
                ? string.Join(", ", a.Attendees.Skip(1).Select(att => att.Name))
                : "";
            var maleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Male);
            var femaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Female);
            var genderDistribution = GetGenderDistribution(a.Attendees);

            // Build row - use helper to properly quote fields
            csv.Append(QuoteField(a.RegistrationId.ToString()));
            csv.Append(QuoteField(mainAttendee));
            csv.Append(QuoteField(additionalAttendees));
            csv.Append(a.TotalAttendees.ToString()).Append(',');
            csv.Append(a.AdultCount.ToString()).Append(',');
            csv.Append(a.ChildCount.ToString()).Append(',');
            csv.Append(maleCount.ToString()).Append(',');
            csv.Append(femaleCount.ToString()).Append(',');
            csv.Append(QuoteField(genderDistribution));
            csv.Append(QuoteField(a.ContactEmail));
            csv.Append(QuoteField(a.ContactPhone ?? ""));
            csv.Append(QuoteField(a.ContactAddress ?? ""));
            csv.Append(QuoteField(a.PaymentStatus.ToString()));
            csv.Append(QuoteField(a.TotalAmount?.ToString("F2") ?? ""));
            csv.Append(QuoteField(a.Currency ?? ""));
            csv.Append(QuoteField(a.TicketCode ?? ""));
            csv.Append(QuoteField(a.QrCodeData ?? ""));
            csv.Append(QuoteField(a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            csv.Append(QuoteField(a.Status.ToString(), isLast: true));
            csv.Append('\n');
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
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

        return genderCounts.Any() ? string.Join(", ", genderCounts) : "";
    }

    /// <summary>
    /// Properly quote and escape a CSV field value.
    /// RFC 4180 compliant: fields containing comma, quote, or newline must be quoted.
    /// Quotes inside fields must be escaped as double quotes ("").
    /// </summary>
    private static string QuoteField(string value, bool isLast = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            // Empty field - just return comma (or nothing if last field)
            return isLast ? "" : ",";
        }

        // Escape any quotes in the value by doubling them (RFC 4180)
        var escaped = value.Replace("\"", "\"\"");

        // Always quote string fields for consistency (safer than selective quoting)
        var quoted = $"\"{escaped}\"";

        // Add comma separator unless it's the last field
        return isLast ? quoted : quoted + ",";
    }
}

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

            // Build row - quote fields that need it
            csv.Append($"\"{a.RegistrationId}\",");
            csv.Append($"\"{mainAttendee}\",");
            csv.Append($"\"{additionalAttendees}\",");
            csv.Append($"{a.TotalAttendees},");
            csv.Append($"{a.AdultCount},");
            csv.Append($"{a.ChildCount},");
            csv.Append($"{maleCount},");
            csv.Append($"{femaleCount},");
            csv.Append($"\"{genderDistribution}\",");
            csv.Append($"\"{a.ContactEmail}\",");
            csv.Append($"\"{a.ContactPhone ?? ""}\",");
            csv.Append($"\"{a.ContactAddress ?? ""}\",");
            csv.Append($"\"{a.PaymentStatus}\",");
            csv.Append($"\"{a.TotalAmount?.ToString("F2") ?? ""}\",");
            csv.Append($"\"{a.Currency ?? ""}\",");
            csv.Append($"\"{a.TicketCode ?? ""}\",");
            csv.Append($"\"{a.QrCodeData ?? ""}\",");
            csv.Append($"\"{a.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
            csv.Append($"\"{a.Status}\"");
            csv.Append("\n");  // Use LF only (like signup list client-side export that works)
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
}

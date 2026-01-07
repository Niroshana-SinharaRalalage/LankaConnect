using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Infrastructure.Services.Export;

/// <summary>
/// CSV export service implementation using CsvHelper library.
/// Phase 6A.68 (Option 2): Restored CsvHelper for RFC 4180 compliant CSV generation.
/// This provides robust handling of special characters, quotes, and newlines.
/// </summary>
public class CsvExportService : ICsvExportService
{
    public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
    {
        // Phase 6A.68: Use CsvHelper for RFC 4180 compliant CSV generation
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // RFC 4180 compliant settings
            NewLine = "\n",  // Use LF for cross-platform compatibility
            ShouldQuote = args => true,  // Always quote fields for safety
            TrimOptions = TrimOptions.Trim,
            UseNewObjectForNullReferenceMembers = false
        });

        // Write header
        csv.WriteField("RegistrationId");
        csv.WriteField("MainAttendee");
        csv.WriteField("AdditionalAttendees");
        csv.WriteField("TotalAttendees");
        csv.WriteField("Adults");
        csv.WriteField("Children");
        csv.WriteField("MaleCount");
        csv.WriteField("FemaleCount");
        csv.WriteField("GenderDistribution");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Address");
        csv.WriteField("PaymentStatus");
        csv.WriteField("TotalAmount");
        csv.WriteField("Currency");
        csv.WriteField("TicketCode");
        csv.WriteField("QRCode");
        csv.WriteField("RegistrationDate");
        csv.WriteField("Status");
        csv.NextRecord();

        // Write data rows
        foreach (var a in attendees.Attendees)
        {
            var mainAttendee = a.Attendees.FirstOrDefault()?.Name ?? "Unknown";
            var additionalAttendees = a.TotalAttendees > 1
                ? string.Join(", ", a.Attendees.Skip(1).Select(att => att.Name))
                : "";
            var maleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Male);
            var femaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Female);
            var genderDistribution = GetGenderDistribution(a.Attendees);

            csv.WriteField(a.RegistrationId.ToString());
            csv.WriteField(mainAttendee);
            csv.WriteField(additionalAttendees);
            csv.WriteField(a.TotalAttendees);
            csv.WriteField(a.AdultCount);
            csv.WriteField(a.ChildCount);
            csv.WriteField(maleCount);
            csv.WriteField(femaleCount);
            csv.WriteField(genderDistribution);
            csv.WriteField(a.ContactEmail);
            csv.WriteField(a.ContactPhone ?? "");
            csv.WriteField(a.ContactAddress ?? "");
            csv.WriteField(a.PaymentStatus.ToString());
            csv.WriteField(a.TotalAmount?.ToString("F2") ?? "");
            csv.WriteField(a.Currency ?? "");
            csv.WriteField(a.TicketCode ?? "");
            csv.WriteField(a.QrCodeData ?? "");
            csv.WriteField(a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            csv.WriteField(a.Status.ToString());
            csv.NextRecord();
        }

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

        return genderCounts.Any() ? string.Join(", ", genderCounts) : "";
    }
}

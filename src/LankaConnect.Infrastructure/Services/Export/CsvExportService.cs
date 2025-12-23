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
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Define flattened records for CSV export
        var records = attendees.Attendees.Select(a => new
        {
            RegistrationId = a.RegistrationId,
            MainAttendee = a.MainAttendeeName,
            AdditionalAttendees = a.AdditionalAttendees,
            TotalAttendees = a.TotalAttendees,
            Adults = a.AdultCount,
            Children = a.ChildCount,
            GenderDistribution = a.GenderDistribution,
            Email = a.ContactEmail,
            Phone = a.ContactPhone,
            Address = a.ContactAddress ?? "—",
            PaymentStatus = a.PaymentStatus.ToString(),
            TotalAmount = a.TotalAmount?.ToString("F2") ?? "—",
            Currency = a.Currency ?? "—",
            TicketCode = a.TicketCode ?? "—",
            QRCode = a.QrCodeData ?? "—",
            RegistrationDate = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = a.Status.ToString()
        });

        // Write records
        csv.WriteRecords(records);

        // Flush to ensure all data is written
        writer.Flush();

        return memoryStream.ToArray();
    }
}

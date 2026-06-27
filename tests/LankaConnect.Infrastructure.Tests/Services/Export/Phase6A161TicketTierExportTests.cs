using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Infrastructure.Services.Export;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Phase 6A.161 — the attendee CSV and Excel exports gain a trailing "Ticket Tier" column,
/// populated from <see cref="EventAttendeeDto.TicketTierSummary"/>. Tests assert the column is
/// appended (never inserted), is present in BOTH writers, renders the summary for tiered
/// registrations, and degrades to "—" for untiered/legacy ones.
/// </summary>
public class Phase6A161TicketTierExportTests
{
    private readonly CsvExportService _csv = new();
    private readonly ExcelExportService _excel = new(NullLogger<ExcelExportService>.Instance);

    private static EventAttendeesResponse BuildResponse(bool isFreeEvent = false) => new()
    {
        EventId = Guid.NewGuid(),
        EventTitle = "Tiered Event",
        IsFreeEvent = isFreeEvent,
        Attendees = new List<EventAttendeeDto>
        {
            // Mixed-tier registration → "VIP, General"
            new()
            {
                RegistrationId = Guid.NewGuid(),
                TotalAttendees = 2,
                AdultCount = 2,
                ContactEmail = "mixed@example.com",
                ContactPhone = "+1000",
                PaymentStatus = PaymentStatus.Completed,
                TotalAmount = 100m,
                Currency = "USD",
                Status = RegistrationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                Attendees = new()
                {
                    new() { Name = "VIP Person", AgeCategory = AgeCategory.Adult, TicketTierName = "VIP" },
                    new() { Name = "Reg Person", AgeCategory = AgeCategory.Adult, TicketTierName = "General" }
                }
            },
            // Untiered / legacy registration → "—"
            new()
            {
                RegistrationId = Guid.NewGuid(),
                TotalAttendees = 1,
                AdultCount = 1,
                ContactEmail = "legacy@example.com",
                ContactPhone = "+2000",
                PaymentStatus = PaymentStatus.Completed,
                TotalAmount = 50m,
                Currency = "USD",
                Status = RegistrationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                Attendees = new()
                {
                    new() { Name = "Legacy Person", AgeCategory = AgeCategory.Adult, TicketTierName = null }
                }
            }
        }
    };

    [Fact]
    public void Csv_Should_AppendTicketTierHeaderAsLastColumn()
    {
        var csv = Encoding.UTF8.GetString(_csv.ExportEventAttendees(BuildResponse()));
        var headerLine = csv.Split('\n', StringSplitOptions.None)[0];

        headerLine.Should().Contain("TicketTier");
        // Appended last: TicketTier comes after Status in the header.
        headerLine.IndexOf("TicketTier", StringComparison.Ordinal)
            .Should().BeGreaterThan(headerLine.IndexOf("Status", StringComparison.Ordinal));
    }

    [Fact]
    public void Csv_Should_RenderTierSummary_AndEmDashForUntiered()
    {
        var csv = Encoding.UTF8.GetString(_csv.ExportEventAttendees(BuildResponse()));

        csv.Should().Contain("VIP, General", "mixed-tier registration shows the joined summary");
        csv.Should().Contain("—", "untiered/legacy registration degrades to an em-dash");
    }

    [Fact]
    public void Excel_Should_AppendTicketTierHeaderAsLastColumn()
    {
        var headers = ReadHeaders(_excel.ExportEventAttendees(BuildResponse()));

        headers.Should().Contain("Ticket Tier");
        headers.IndexOf("Ticket Tier").Should().Be(headers.Count - 1, "tier column is appended last");
    }

    [Fact]
    public void Excel_Should_RenderTierSummary_AndEmDashForUntiered()
    {
        var values = ReadColumnValues(_excel.ExportEventAttendees(BuildResponse()), "Ticket Tier");

        values.Should().Contain("VIP, General");
        values.Should().Contain("—");
    }

    [Fact]
    public void CsvAndExcel_Should_BothExposeTierColumn_ForParity()
    {
        // Both writers must agree the tier column exists — guards against drift where one
        // gets the column and the other doesn't.
        var csvHeader = Encoding.UTF8.GetString(_csv.ExportEventAttendees(BuildResponse()))
            .Split('\n', StringSplitOptions.None)[0];
        var excelHeaders = ReadHeaders(_excel.ExportEventAttendees(BuildResponse()));

        csvHeader.Should().Contain("TicketTier");
        excelHeaders.Should().Contain("Ticket Tier");
    }

    [Fact]
    public void FreeEvent_Should_StillExportTierColumn()
    {
        // Tiers can exist on free events ($0 adult price); the column is unconditional.
        var csvHeader = Encoding.UTF8.GetString(_csv.ExportEventAttendees(BuildResponse(isFreeEvent: true)))
            .Split('\n', StringSplitOptions.None)[0];

        csvHeader.Should().Contain("TicketTier");
    }

    private static List<string> ReadHeaders(byte[] xlsxBytes)
    {
        using var ms = new MemoryStream(xlsxBytes);
        using var wb = new XLWorkbook(ms);
        var sheet = wb.Worksheet("Registrations");
        var lastCol = sheet.Row(1).LastCellUsed().Address.ColumnNumber;
        var headers = new List<string>();
        for (int c = 1; c <= lastCol; c++)
            headers.Add(sheet.Cell(1, c).GetString());
        return headers;
    }

    private static List<string> ReadColumnValues(byte[] xlsxBytes, string headerName)
    {
        using var ms = new MemoryStream(xlsxBytes);
        using var wb = new XLWorkbook(ms);
        var sheet = wb.Worksheet("Registrations");
        var lastCol = sheet.Row(1).LastCellUsed().Address.ColumnNumber;
        int target = -1;
        for (int c = 1; c <= lastCol; c++)
            if (sheet.Cell(1, c).GetString() == headerName) { target = c; break; }
        target.Should().BeGreaterThan(0, $"column '{headerName}' must exist");

        var values = new List<string>();
        var lastRow = sheet.LastRowUsed().RowNumber();
        for (int r = 2; r <= lastRow; r++)
            values.Add(sheet.Cell(r, target).GetString());
        return values;
    }
}

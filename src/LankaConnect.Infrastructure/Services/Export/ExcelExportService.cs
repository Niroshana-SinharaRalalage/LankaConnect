using ClosedXML.Excel;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Services.Export;

/// <summary>
/// Excel export service implementation using ClosedXML.
/// Creates multi-sheet Excel workbooks with attendee data and signup lists.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public byte[] ExportEventAttendees(
        EventAttendeesResponse attendees,
        List<SignUpListDto>? signUpLists = null)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Registrations with all attendee data
        CreateRegistrationsSheet(workbook, attendees);

        // Sheet 2-4: Signup Lists (if exist) - categorized by Mandatory, Suggested, Open
        if (signUpLists?.Any() == true)
        {
            CreateSignUpListSheets(workbook, signUpLists);
        }

        // Convert workbook to byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateRegistrationsSheet(
        IXLWorkbook workbook,
        EventAttendeesResponse data)
    {
        var sheet = workbook.Worksheets.Add("Registrations");

        // Define headers
        var headers = new[]
        {
            "Registration ID",
            "Main Attendee",
            "Additional Attendees",
            "Total Attendees",
            "Adults",
            "Children",
            "Gender Distribution",
            "Email",
            "Phone",
            "Address",
            "Payment Status",
            "Total Amount",
            "Currency",
            "Ticket Code",
            "QR Code",
            "Registration Date",
            "Status"
        };

        // Write headers
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        // Style header row
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Write data rows
        int row = 2;
        foreach (var attendee in data.Attendees)
        {
            int col = 1;
            sheet.Cell(row, col++).Value = attendee.RegistrationId.ToString();
            sheet.Cell(row, col++).Value = attendee.MainAttendeeName;
            sheet.Cell(row, col++).Value = attendee.AdditionalAttendees;
            sheet.Cell(row, col++).Value = attendee.TotalAttendees;
            sheet.Cell(row, col++).Value = attendee.AdultCount;
            sheet.Cell(row, col++).Value = attendee.ChildCount;
            sheet.Cell(row, col++).Value = attendee.GenderDistribution;
            sheet.Cell(row, col++).Value = attendee.ContactEmail;
            sheet.Cell(row, col++).Value = attendee.ContactPhone;
            sheet.Cell(row, col++).Value = attendee.ContactAddress ?? "—";
            sheet.Cell(row, col++).Value = attendee.PaymentStatus.ToString();

            // Format currency values
            if (attendee.TotalAmount.HasValue)
            {
                sheet.Cell(row, col).Value = attendee.TotalAmount.Value;
                sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                sheet.Cell(row, col).Value = "—";
            }
            col++;

            sheet.Cell(row, col++).Value = attendee.Currency ?? "—";
            sheet.Cell(row, col++).Value = attendee.TicketCode ?? "—";
            sheet.Cell(row, col++).Value = attendee.QrCodeData ?? "—";

            // Format date
            sheet.Cell(row, col).Value = attendee.CreatedAt;
            sheet.Cell(row, col).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            col++;

            sheet.Cell(row, col++).Value = attendee.Status.ToString();

            row++;
        }

        // Add summary row
        row++;
        sheet.Cell(row, 1).Value = "TOTALS";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 4).Value = data.TotalAttendees;
        sheet.Cell(row, 4).Style.Font.Bold = true;

        if (data.TotalRevenue.HasValue && data.TotalRevenue.Value > 0)
        {
            sheet.Cell(row, 12).Value = data.TotalRevenue.Value;
            sheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, 12).Style.Font.Bold = true;
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }

    private void CreateSignUpListSheets(
        IXLWorkbook workbook,
        List<SignUpListDto> signUpLists)
    {
        // Group items by category
        var categorizedItems = new Dictionary<string, List<(SignUpListDto List, SignUpItemDto Item)>>
        {
            ["Mandatory"] = new(),
            ["Suggested"] = new(),
            ["Open"] = new()
        };

        foreach (var list in signUpLists)
        {
            foreach (var item in list.Items)
            {
                var categoryName = item.ItemCategory switch
                {
                    SignUpItemCategory.Mandatory => "Mandatory",
                    SignUpItemCategory.Suggested => "Suggested",
                    SignUpItemCategory.Open => "Open",
                    _ => "Open"
                };

                categorizedItems[categoryName].Add((list, item));
            }
        }

        // Create a sheet for each category that has items
        foreach (var (categoryName, items) in categorizedItems)
        {
            if (items.Any())
            {
                CreateSignUpSheet(workbook, $"{categoryName} Items", items);
            }
        }
    }

    private void CreateSignUpSheet(
        IXLWorkbook workbook,
        string sheetName,
        List<(SignUpListDto List, SignUpItemDto Item)> items)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        // Define headers
        var headers = new[]
        {
            "Signup List",
            "Item Description",
            "Requested Quantity",
            "Committed Quantity",
            "Remaining Quantity",
            "Commitments"
        };

        // Write headers
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        // Style header row
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Write data rows
        int row = 2;
        foreach (var (list, item) in items)
        {
            int col = 1;

            sheet.Cell(row, col++).Value = list.Category;
            sheet.Cell(row, col++).Value = item.ItemDescription;
            sheet.Cell(row, col++).Value = item.Quantity;

            var committedQty = item.Quantity - item.RemainingQuantity;
            sheet.Cell(row, col++).Value = committedQty;
            sheet.Cell(row, col++).Value = item.RemainingQuantity;

            // Format commitment details
            var commitmentDetails = item.Commitments
                .Select(c => $"{c.ContactName ?? "Anonymous"} ({c.Quantity}) - {c.ContactEmail ?? c.ContactPhone ?? "N/A"}")
                .ToList();

            sheet.Cell(row, col++).Value = commitmentDetails.Any()
                ? string.Join("; ", commitmentDetails)
                : "No commitments yet";

            row++;
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }
}

using ClosedXML.Excel;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services.Export;

/// <summary>
/// Excel export service implementation using ClosedXML.
/// Creates multi-sheet Excel workbooks with attendee data and signup lists.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger;
    }
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

    /// <summary>
    /// Phase 6A.73 (Revised): Exports signup lists to ZIP archive containing multiple Excel files.
    /// Creates one Excel file per signup list, each with sheets for different categories.
    /// Uses grouped format where each item shows once with commitments listed below (matching CSV export).
    /// </summary>
    public byte[] ExportSignUpListsToExcelZip(List<SignUpListDto> signUpLists, Guid eventId)
    {
        if (signUpLists == null || !signUpLists.Any())
            throw new ArgumentException("No signup lists to export", nameof(signUpLists));

        _logger.LogInformation(
            "Phase 6A.73: Starting Excel ZIP export for event {EventId} - {ListCount} signup lists",
            eventId,
            signUpLists.Count);

        try
        {
            using var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                // Create one Excel file per signup list
                foreach (var signUpList in signUpLists)
                {
                    // Create Excel workbook for this signup list
                    using var workbook = new XLWorkbook();

                    // Group items by category within this signup list
                    var categorizedItems = new Dictionary<string, List<SignUpItemDto>>
                    {
                        ["Mandatory"] = new(),
                        ["Suggested"] = new(),
                        ["Open"] = new()
                    };

                    foreach (var item in signUpList.Items)
                    {
                        var categoryName = item.ItemCategory switch
                        {
                            SignUpItemCategory.Mandatory => "Mandatory",
                            SignUpItemCategory.Suggested => "Suggested",
                            SignUpItemCategory.Open => "Open",
                            _ => "Open"
                        };

                        categorizedItems[categoryName].Add(item);
                    }

                    // Create a sheet for each category that has items
                    foreach (var (categoryName, items) in categorizedItems)
                    {
                        if (items.Any())
                        {
                            CreateGroupedSignUpSheet(workbook, $"{categoryName} Items", items);
                        }
                    }

                    // Save workbook to memory first, then add to ZIP as complete file
                    byte[] excelBytes;
                    using (var excelMemoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(excelMemoryStream);

                        // CRITICAL: Reset stream position to beginning before reading
                        // ClosedXML leaves the stream position at EOF after SaveAs()
                        excelMemoryStream.Position = 0;
                        excelBytes = excelMemoryStream.ToArray();

                        _logger.LogInformation(
                            "Phase 6A.73: Saved Excel workbook for signup list '{Category}' - {ByteCount} bytes",
                            signUpList.Category,
                            excelBytes.Length);
                    }

                    // Generate filename: "Food-and-Drinks.xlsx"
                    var sanitizedFileName = SanitizeFileName(signUpList.Category);
                    var fileName = $"{sanitizedFileName}.xlsx";

                    // Phase 6A.73 Fix: Write XLSX directly to ZIP entry without additional compression
                    // XLSX files are already ZIP-compressed internally (Open XML format)
                    // Store without compression to avoid double-compression issues
                    var entry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.NoCompression);
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(excelBytes, 0, excelBytes.Length);
                        entryStream.Flush();

                        _logger.LogInformation(
                            "Phase 6A.73: Added '{FileName}' to ZIP archive - {ByteCount} bytes",
                            fileName,
                            excelBytes.Length);
                    }
                }
            }

            var zipBytes = zipStream.ToArray();
            _logger.LogInformation(
                "Phase 6A.73: Successfully created Excel ZIP archive for event {EventId} - {ZipSize} bytes total",
                eventId,
                zipBytes.Length);

            return zipBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Phase 6A.73: Failed to create Excel ZIP export for event {EventId}",
                eventId);
            throw;
        }
    }

    /// <summary>
    /// Phase 6A.73 (Revised): Create Excel sheet with grouped format (matching CSV export).
    /// Each item shows once with commitments listed below, with blank item columns for additional commitments.
    /// </summary>
    private void CreateGroupedSignUpSheet(
        IXLWorkbook workbook,
        string sheetName,
        List<SignUpItemDto> items)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        // Headers matching CSV format (no "Signup List" column since each file is for one signup list)
        var headers = new[]
        {
            "Item Description",
            "Requested Quantity",
            "Remaining Quantity",
            "Contact Name",
            "Contact Email",
            "Contact Phone",
            "Quantity Committed"
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

        // Write data rows - grouped format
        int row = 2;
        foreach (var item in items)
        {
            if (!item.Commitments.Any())
            {
                // Zero commitments - single row with placeholders
                WriteGroupedItemRow(sheet, row, item, null);
                row++;
            }
            else
            {
                // First row: Item header with first commitment
                WriteGroupedItemRow(sheet, row, item, item.Commitments.First());
                row++;

                // Subsequent rows: Additional commitments (if any)
                foreach (var commitment in item.Commitments.Skip(1))
                {
                    WriteGroupedCommitmentOnlyRow(sheet, row, commitment);
                    row++;
                }
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }

    /// <summary>
    /// Phase 6A.73 (Revised): Write item row with full details (item + optional commitment).
    /// Format: Item Description | Requested Quantity | Remaining Quantity | Contact Name | Contact Email | Contact Phone | Quantity Committed
    /// </summary>
    private static void WriteGroupedItemRow(
        IXLWorksheet sheet,
        int row,
        SignUpItemDto item,
        SignUpCommitmentDto? commitment)
    {
        int col = 1;

        // Item information
        sheet.Cell(row, col++).Value = item.ItemDescription;
        sheet.Cell(row, col++).Value = item.Quantity;
        sheet.Cell(row, col++).Value = item.RemainingQuantity;

        // Contact information (use em dash for missing data)
        sheet.Cell(row, col++).Value = commitment?.ContactName ?? "—";
        sheet.Cell(row, col++).Value = commitment?.ContactEmail ?? "—";

        // Phone number with apostrophe prefix (prevents Excel auto-formatting)
        var phone = string.IsNullOrWhiteSpace(commitment?.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        sheet.Cell(row, col++).Value = phone;

        sheet.Cell(row, col++).Value = commitment?.Quantity ?? 0;
    }

    /// <summary>
    /// Phase 6A.73 (Revised): Write commitment-only row (blank item columns, commitment data only).
    /// Format: [blank] | [blank] | [blank] | Contact Name | Contact Email | Contact Phone | Quantity Committed
    /// </summary>
    private static void WriteGroupedCommitmentOnlyRow(
        IXLWorksheet sheet,
        int row,
        SignUpCommitmentDto commitment)
    {
        int col = 1;

        // Blank item columns (item already shown in previous row)
        sheet.Cell(row, col++).Value = "";  // Item Description
        sheet.Cell(row, col++).Value = "";  // Requested Quantity
        sheet.Cell(row, col++).Value = "";  // Remaining Quantity

        // Contact information
        sheet.Cell(row, col++).Value = commitment.ContactName;
        sheet.Cell(row, col++).Value = commitment.ContactEmail;

        // Phone number with apostrophe prefix
        var phone = string.IsNullOrWhiteSpace(commitment.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        sheet.Cell(row, col++).Value = phone;

        sheet.Cell(row, col++).Value = commitment.Quantity;
    }

    /// <summary>
    /// Phase 6A.73: Sanitize filename for ZIP entry (remove invalid characters).
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

        // Replace spaces with hyphens for cleaner filenames
        sanitized = sanitized.Replace(" ", "-");

        return sanitized;
    }

    private void CreateRegistrationsSheet(
        IXLWorkbook workbook,
        EventAttendeesResponse data)
    {
        var sheet = workbook.Worksheets.Add("Registrations");

        // Phase 6A.71: Build headers dynamically based on event type (free vs paid)
        var headersList = new List<string>
        {
            "Main Attendee",
            "Additional Attendees",
            "Total Attendees",
            "Adults",
            "Children",
            "Male Count",
            "Female Count",
            "Gender Distribution",
            "Email",
            "Phone",
            "Address"
        };

        // Phase 6A.71: Only include payment/amount columns for paid events
        if (!data.IsFreeEvent)
        {
            headersList.Add("Payment Status");
            headersList.Add("Gross Amount");

            // Phase 6A.X: Add detailed revenue breakdown columns
            if (data.HasRevenueBreakdown)
            {
                headersList.Add("Sales Tax");
                headersList.Add("Tax Rate");
                headersList.Add("Stripe Fee");
                headersList.Add("Platform Commission");
            }

            headersList.Add("Net Amount");
            headersList.Add("Currency");
            // Phase 6A.X: Ticket Code column for paid events only
            headersList.Add("Ticket Code");
        }

        headersList.AddRange(new[]
        {
            "Registration Date",
            "Status"
        });

        var headers = headersList.ToArray();

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
            // Phase 6A.68: Removed RegistrationId, added Male/Female counts
            sheet.Cell(row, col++).Value = attendee.MainAttendeeName;
            sheet.Cell(row, col++).Value = attendee.AdditionalAttendees;
            sheet.Cell(row, col++).Value = attendee.TotalAttendees;
            sheet.Cell(row, col++).Value = attendee.AdultCount;
            sheet.Cell(row, col++).Value = attendee.ChildCount;

            // Calculate male and female counts from attendees
            var maleCount = attendee.Attendees?.Count(a => a.Gender == Gender.Male) ?? 0;
            var femaleCount = attendee.Attendees?.Count(a => a.Gender == Gender.Female) ?? 0;
            sheet.Cell(row, col++).Value = maleCount;
            sheet.Cell(row, col++).Value = femaleCount;

            sheet.Cell(row, col++).Value = attendee.GenderDistribution;
            sheet.Cell(row, col++).Value = attendee.ContactEmail;
            sheet.Cell(row, col++).Value = attendee.ContactPhone;
            sheet.Cell(row, col++).Value = attendee.ContactAddress ?? "—";

            // Phase 6A.71: Conditionally write payment/amount columns for paid events only
            if (!data.IsFreeEvent)
            {
                sheet.Cell(row, col++).Value = attendee.PaymentStatus.ToString();

                // Gross Amount
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

                // Phase 6A.X: Write breakdown columns if available
                if (data.HasRevenueBreakdown)
                {
                    // Sales Tax
                    if (attendee.SalesTaxAmount.HasValue)
                    {
                        sheet.Cell(row, col).Value = attendee.SalesTaxAmount.Value;
                        sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "—";
                    }
                    col++;

                    // Tax Rate
                    if (attendee.SalesTaxRate > 0)
                    {
                        sheet.Cell(row, col).Value = $"{attendee.SalesTaxRate * 100:F2}%";
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "—";
                    }
                    col++;

                    // Stripe Fee
                    if (attendee.StripeFeeAmount.HasValue)
                    {
                        sheet.Cell(row, col).Value = attendee.StripeFeeAmount.Value;
                        sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "—";
                    }
                    col++;

                    // Platform Commission
                    if (attendee.PlatformCommissionAmount.HasValue)
                    {
                        sheet.Cell(row, col).Value = attendee.PlatformCommissionAmount.Value;
                        sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "—";
                    }
                    col++;
                }

                // Net Amount (organizer payout)
                if (attendee.NetAmount.HasValue)
                {
                    sheet.Cell(row, col).Value = attendee.NetAmount.Value;
                    sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                }
                else
                {
                    sheet.Cell(row, col).Value = "—";
                }
                col++;

                sheet.Cell(row, col++).Value = attendee.Currency ?? "—";

                // Phase 6A.X: Ticket Code column for paid events only
                sheet.Cell(row, col++).Value = attendee.TicketCode ?? "—";
            }

            // Format date
            sheet.Cell(row, col).Value = attendee.CreatedAt;
            sheet.Cell(row, col).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            col++;

            sheet.Cell(row, col++).Value = attendee.Status.ToString();

            row++;
        }

        // Phase 6A.71: Add summary row with dynamic column positioning
        row++;
        sheet.Cell(row, 1).Value = "TOTALS";
        sheet.Cell(row, 1).Style.Font.Bold = true;

        // Total Attendees column (column 3)
        sheet.Cell(row, 3).Value = data.TotalAttendees;
        sheet.Cell(row, 3).Style.Font.Bold = true;

        // Phase 6A.X: Revenue totals (only for paid events)
        if (!data.IsFreeEvent)
        {
            int col = 12; // Start after Address (column 11)

            // PaymentStatus column - skip
            col++;

            // Gross Amount
            if (data.GrossRevenue > 0)
            {
                sheet.Cell(row, col).Value = data.GrossRevenue;
                sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, col).Style.Font.Bold = true;
            }
            col++;

            // Phase 6A.X: Breakdown totals (if available)
            if (data.HasRevenueBreakdown)
            {
                // Sales Tax
                if (data.TotalSalesTax > 0)
                {
                    sheet.Cell(row, col).Value = data.TotalSalesTax;
                    sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    sheet.Cell(row, col).Style.Font.Bold = true;
                }
                col++;

                // Tax Rate
                if (data.AverageTaxRate > 0)
                {
                    sheet.Cell(row, col).Value = $"{data.AverageTaxRate * 100:F2}%";
                    sheet.Cell(row, col).Style.Font.Bold = true;
                }
                col++;

                // Stripe Fees
                if (data.TotalStripeFees > 0)
                {
                    sheet.Cell(row, col).Value = data.TotalStripeFees;
                    sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    sheet.Cell(row, col).Style.Font.Bold = true;
                }
                col++;

                // Platform Commission
                if (data.TotalPlatformCommission > 0)
                {
                    sheet.Cell(row, col).Value = data.TotalPlatformCommission;
                    sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    sheet.Cell(row, col).Style.Font.Bold = true;
                }
                col++;
            }

            // Net Amount (organizer payout)
            var netPayout = data.TotalOrganizerPayout > 0 ? data.TotalOrganizerPayout : data.NetRevenue;
            if (netPayout > 0)
            {
                sheet.Cell(row, col).Value = netPayout;
                sheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, col).Style.Font.Bold = true;
            }
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

        // Phase 6A.49: Updated headers to include user contact fields
        var headers = new[]
        {
            "Signup List",
            "Item Description",
            "Requested Quantity",
            "User Name",
            "Phone",
            "Email",
            "Quantity Committed",
            "Remaining"
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

        // Phase 6A.49: Expand rows - each commitment gets its own row
        int row = 2;
        foreach (var (list, item) in items)
        {
            if (!item.Commitments.Any())
            {
                // No commitments yet - show item with empty user fields
                int col = 1;
                sheet.Cell(row, col++).Value = list.Category;
                sheet.Cell(row, col++).Value = item.ItemDescription;
                sheet.Cell(row, col++).Value = item.Quantity;
                sheet.Cell(row, col++).Value = "—"; // No user name
                sheet.Cell(row, col++).Value = "—"; // No phone
                sheet.Cell(row, col++).Value = "—"; // No email
                sheet.Cell(row, col++).Value = 0;   // Nothing committed
                sheet.Cell(row, col++).Value = item.RemainingQuantity;
                row++;
            }
            else
            {
                // Each commitment gets its own row
                foreach (var commitment in item.Commitments)
                {
                    int col = 1;
                    sheet.Cell(row, col++).Value = list.Category;
                    sheet.Cell(row, col++).Value = item.ItemDescription;
                    sheet.Cell(row, col++).Value = item.Quantity;
                    sheet.Cell(row, col++).Value = commitment.ContactName ?? "Anonymous";

                    // Format phone with apostrophe prefix (same as CSV)
                    var phoneValue = string.IsNullOrWhiteSpace(commitment.ContactPhone)
                        ? "—"
                        : "'" + commitment.ContactPhone;
                    sheet.Cell(row, col++).Value = phoneValue;

                    sheet.Cell(row, col++).Value = commitment.ContactEmail ?? "—";
                    sheet.Cell(row, col++).Value = commitment.Quantity;
                    sheet.Cell(row, col++).Value = item.RemainingQuantity;
                    row++;
                }
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }
}

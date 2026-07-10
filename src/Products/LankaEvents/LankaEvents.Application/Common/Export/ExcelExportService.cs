using ClosedXML.Excel;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Common.Export; // Wave 6.5.f Day 5 slot A (2026-07-10): moved from LC.Infrastructure/Services/Export/ per architect Option C — pure DTO-in/byte-out transformers belong in Application, not Infrastructure.

/// <summary>
/// Excel export service implementation using ClosedXML.
/// Creates multi-sheet Excel workbooks with attendee data and signup lists.
///
/// Wave 6.a.1 (2026-07-01): also implements <see cref="IFormResponseExporter"/>
/// so Forms.Application can consume form-response Excel export via a narrow
/// Contracts port without importing Products.LankaEvents.Application.Common.
/// </summary>
public class ExcelExportService : IExcelExportService, IFormResponseExporter
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Wave 6.a.1 IFormResponseExporter -- Excel branch.
    /// </summary>
    public byte[] ExportFormResponsesToExcel(EventFormDetailDto form, FormResponsesPagedDto responses)
        => ExportFormResponses(form, responses);

    /// <summary>
    /// Wave 6.a.1 IFormResponseExporter -- CSV branch is a not-supported no-op on
    /// the Excel service; callers should resolve IFormResponseExporter via
    /// CsvExportService for CSV output.
    /// </summary>
    byte[] IFormResponseExporter.ExportFormResponsesToCsv(EventFormDetailDto form, FormResponsesPagedDto responses)
        => throw new NotSupportedException("ExcelExportService does not support CSV; register CsvExportService as IFormResponseExporter for CSV exports.");

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
    public byte[] ExportSignUpListsToExcelZip(List<SignUpListDto> signUpLists, Guid eventId, SignUpExportLabels? labels = null)
    {
        if (signUpLists == null || !signUpLists.Any())
            throw new ArgumentException("No signup lists to export", nameof(signUpLists));

        var columnLabels = labels ?? SignUpExportLabels.ForItems();

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
                    var categorizedItems = new Dictionary<string, List<ISignUpItemDto>>
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
                            CreateGroupedSignUpSheet(workbook, $"{categoryName} Items", items, columnLabels);
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
#pragma warning disable CS0618 // Suppress obsolete warning for SignUpItemDto
    private void CreateGroupedSignUpSheet(
        IXLWorkbook workbook,
        string sheetName,
        List<ISignUpItemDto> items,
        SignUpExportLabels columnLabels)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        // Phase 7D.1 Step 15: Headers sourced from label set (default: Items; volunteer exports override).
        var headers = new[]
        {
            columnLabels.ItemDescription,
            columnLabels.RequestedQuantity,
            columnLabels.RemainingQuantity,
            columnLabels.ContactName,
            columnLabels.ContactEmail,
            columnLabels.ContactPhone,
            columnLabels.QuantityCommitted
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
        ISignUpItemDto item,
        SignUpCommitmentDto? commitment)
    {
        int col = 1;

        // Item information
        sheet.Cell(row, col++).Value = item.ItemDescription;

        // Phase 6A.121: Handle both quantity-based and slot-based items
        var quantity = item switch
        {
            QuantityBasedItemDto qItem => qItem.TargetQuantity,
            SlotBasedItemDto sItem => sItem.TotalSlots,
            SignUpItemDto legacyItem => legacyItem.Quantity,
            _ => 0
        };
        var remaining = item switch
        {
            QuantityBasedItemDto qItem => qItem.RemainingQuantity,
            SlotBasedItemDto sItem => sItem.RemainingSlots,
            SignUpItemDto legacyItem => legacyItem.RemainingQuantity,
            _ => 0
        };

        sheet.Cell(row, col++).Value = quantity;
        sheet.Cell(row, col++).Value = remaining;

        // Contact information (use em dash for missing data)
        sheet.Cell(row, col++).Value = commitment?.ContactName ?? "—";
        sheet.Cell(row, col++).Value = commitment?.ContactEmail ?? "—";

        // Phone number with apostrophe prefix (prevents Excel auto-formatting)
        var phone = string.IsNullOrWhiteSpace(commitment?.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        sheet.Cell(row, col++).Value = phone;

        // Phase 6A.121: Use dual nullable fields (PhysicalQuantity or SlotsClaimed)
        sheet.Cell(row, col++).Value = commitment?.PhysicalQuantity ?? commitment?.SlotsClaimed ?? 0;
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

        // Phase 6A.121: Use dual nullable fields (PhysicalQuantity or SlotsClaimed)
        sheet.Cell(row, col++).Value = commitment.PhysicalQuantity ?? commitment.SlotsClaimed ?? 0;
    }
#pragma warning restore CS0618

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
            "Status",
            // Phase 6A.161: Ticket tier(s) — appended LAST to preserve existing column order.
            // Unconditional so free-event tiers ($0 adult price) still export.
            "Ticket Tier"
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

            // Phase 7E.8: counts come from the DTO (Mode A: SQL projection of r.Attendees;
            // Mode B: post-processing override from HeadCount.Demographics).
            sheet.Cell(row, col++).Value = attendee.MaleCount;
            sheet.Cell(row, col++).Value = attendee.FemaleCount;

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

            // Phase 6A.161: Ticket tier summary (computed, never null — "—" when no tier).
            sheet.Cell(row, col++).Value = attendee.TicketTierSummary;

            row++;
        }

        // Phase 6A.71: Add summary row with dynamic column positioning
        // Phase 6A.161 note: the TOTALS row writes only specific columns; the appended
        // Ticket Tier column is intentionally left blank here (matches the CSV summary row).
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
        var categorizedItems = new Dictionary<string, List<(SignUpListDto List, ISignUpItemDto Item)>>
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

#pragma warning disable CS0618 // Suppress obsolete warning for SignUpItemDto
    private void CreateSignUpSheet(
        IXLWorkbook workbook,
        string sheetName,
        List<(SignUpListDto List, ISignUpItemDto Item)> items)
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

                // Phase 6A.121: Handle both quantity-based and slot-based items
                var quantity = item switch
                {
                    QuantityBasedItemDto qItem => qItem.TargetQuantity,
                    SlotBasedItemDto sItem => sItem.TotalSlots,
                    SignUpItemDto legacyItem => legacyItem.Quantity,
                    _ => 0
                };
                var remaining = item switch
                {
                    QuantityBasedItemDto qItem => qItem.RemainingQuantity,
                    SlotBasedItemDto sItem => sItem.RemainingSlots,
                    SignUpItemDto legacyItem => legacyItem.RemainingQuantity,
                    _ => 0
                };

                sheet.Cell(row, col++).Value = quantity;
                sheet.Cell(row, col++).Value = "—"; // No user name
                sheet.Cell(row, col++).Value = "—"; // No phone
                sheet.Cell(row, col++).Value = "—"; // No email
                sheet.Cell(row, col++).Value = 0;   // Nothing committed
                sheet.Cell(row, col++).Value = remaining;
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

                    // Phase 6A.121: Handle both quantity-based and slot-based items
                    var quantity = item switch
                    {
                        QuantityBasedItemDto qItem => qItem.TargetQuantity,
                        SlotBasedItemDto sItem => sItem.TotalSlots,
                        SignUpItemDto legacyItem => legacyItem.Quantity,
                        _ => 0
                    };
                    var remaining = item switch
                    {
                        QuantityBasedItemDto qItem => qItem.RemainingQuantity,
                        SlotBasedItemDto sItem => sItem.RemainingSlots,
                        SignUpItemDto legacyItem => legacyItem.RemainingQuantity,
                        _ => 0
                    };

                    sheet.Cell(row, col++).Value = quantity;
                    sheet.Cell(row, col++).Value = commitment.ContactName ?? "Anonymous";

                    // Format phone with apostrophe prefix (same as CSV)
                    var phoneValue = string.IsNullOrWhiteSpace(commitment.ContactPhone)
                        ? "—"
                        : "'" + commitment.ContactPhone;
                    sheet.Cell(row, col++).Value = phoneValue;

                    sheet.Cell(row, col++).Value = commitment.ContactEmail ?? "—";
                    // Phase 6A.121: Use dual nullable fields (PhysicalQuantity or SlotsClaimed)
                    sheet.Cell(row, col++).Value = commitment.PhysicalQuantity ?? commitment.SlotsClaimed ?? 0;
                    sheet.Cell(row, col++).Value = remaining;
                    row++;
                }
            }
        }
#pragma warning restore CS0618

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }

    /// <summary>
    /// Exports custom form responses to Excel format.
    /// Single sheet with responses in rows, questions as columns.
    /// Phase 6A.110: Form response export functionality
    /// </summary>
    public byte[] ExportFormResponses(EventFormDetailDto form, FormResponsesPagedDto responses)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Form Responses");

        // 1. Build header columns
        var headers = new List<string>
        {
            "Respondent Name",
            "Respondent Email",
            "Submitted Date"
        };

        var sortedQuestions = form.Questions.OrderBy(q => q.SortOrder).ToList();
        foreach (var question in sortedQuestions)
        {
            headers.Add($"Q{question.SortOrder + 1}: {question.QuestionText}");
        }

        // 2. Write header row
        for (int i = 0; i < headers.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
        }

        // 3. Style header row (bold, light blue, centered, frozen)
        var headerRange = sheet.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // 4. Write data rows
        int rowIndex = 2;
        foreach (var response in responses.Responses)
        {
            int colIndex = 1;

            // Respondent info
            sheet.Cell(rowIndex, colIndex++).Value = response.RespondentName ?? "Anonymous";
            sheet.Cell(rowIndex, colIndex++).Value = response.RespondentEmail ?? "—";

            // Submitted date with Excel date format
            var dateCell = sheet.Cell(rowIndex, colIndex++);
            dateCell.Value = response.SubmittedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

            // Answers for each question
            foreach (var question in sortedQuestions)
            {
                var answer = response.Answers.FirstOrDefault(a => a.FormQuestionId == question.Id);
                sheet.Cell(rowIndex, colIndex++).Value = FormatAnswerForExcelExport(answer);
            }

            rowIndex++;
        }

        // 5. Auto-fit columns for readability
        sheet.Columns().AdjustToContents();

        // 6. Freeze header row (allows scrolling data while keeping headers visible)
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Formats a form answer for Excel export.
    /// Same logic as CSV but optimized for Excel cells.
    /// </summary>
    private static string FormatAnswerForExcelExport(FormAnswerDto? answer)
    {
        if (answer == null)
            return "—";

        if (answer.BooleanValue.HasValue)
            return answer.BooleanValue.Value ? "Yes" : "No";

        if (answer.SelectedOptionTextSnapshots?.Any() == true)
            return string.Join(", ", answer.SelectedOptionTextSnapshots);

        if (!string.IsNullOrWhiteSpace(answer.TextValue))
            return answer.TextValue;

        return "—";
    }

    /// <summary>
    /// Collection Feature: Exports event fund collections to Excel format.
    /// </summary>
    public byte[] ExportCollections(EventCollectionsResponse collections)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Collections");

        var headers = new[] { "Contributor Name", "Email", "Phone", "Amount", "Currency", "Status", "Notes", "Stripe Fee", "Platform Commission", "Organizer Payout", "Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int rowIndex = 2;
        foreach (var c in collections.Collections)
        {
            int col = 1;
            sheet.Cell(rowIndex, col++).Value = c.ContributorName;
            sheet.Cell(rowIndex, col++).Value = c.ContributorEmail;
            sheet.Cell(rowIndex, col++).Value = c.ContributorPhone ?? "";

            var amountCell = sheet.Cell(rowIndex, col++);
            amountCell.Value = c.Amount;
            amountCell.Style.NumberFormat.Format = "#,##0.00";

            sheet.Cell(rowIndex, col++).Value = c.Currency;
            sheet.Cell(rowIndex, col++).Value = c.Status;
            sheet.Cell(rowIndex, col++).Value = c.ContributorNotes ?? "";

            var feeCell = sheet.Cell(rowIndex, col++);
            if (c.StripeFeeAmount.HasValue) { feeCell.Value = c.StripeFeeAmount.Value; feeCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { feeCell.Value = "—"; }

            var commCell = sheet.Cell(rowIndex, col++);
            if (c.PlatformCommissionAmount.HasValue) { commCell.Value = c.PlatformCommissionAmount.Value; commCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { commCell.Value = "—"; }

            var payoutCell = sheet.Cell(rowIndex, col++);
            if (c.OrganizerPayoutAmount.HasValue) { payoutCell.Value = c.OrganizerPayoutAmount.Value; payoutCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { payoutCell.Value = "—"; }

            var dateCell = sheet.Cell(rowIndex, col++);
            dateCell.Value = c.PaymentCompletedAt ?? c.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Sponsor Feature: Exports sponsorships to Excel format.
    /// </summary>
    public byte[] ExportSponsors(EventSponsorsResponse sponsors)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sponsors");

        var headers = new[] { "Sponsor Name", "Email", "Phone", "Organization", "Type", "Amount", "Currency", "Status", "Item Name", "Item Description", "Estimated Value", "Stripe Fee", "Platform Commission", "Organizer Payout", "Notes", "Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int rowIndex = 2;
        foreach (var s in sponsors.Sponsors)
        {
            int col = 1;
            sheet.Cell(rowIndex, col++).Value = s.SponsorName;
            sheet.Cell(rowIndex, col++).Value = s.SponsorEmail;
            sheet.Cell(rowIndex, col++).Value = s.SponsorPhone ?? "";
            sheet.Cell(rowIndex, col++).Value = s.SponsorOrganization ?? "";
            sheet.Cell(rowIndex, col++).Value = s.SponsorType;

            if (s.Amount.HasValue)
            {
                var amountCell = sheet.Cell(rowIndex, col);
                amountCell.Value = s.Amount.Value;
                amountCell.Style.NumberFormat.Format = "#,##0.00";
            }
            else { sheet.Cell(rowIndex, col).Value = "—"; }
            col++;

            sheet.Cell(rowIndex, col++).Value = s.Currency ?? "";
            sheet.Cell(rowIndex, col++).Value = s.Status;
            sheet.Cell(rowIndex, col++).Value = s.ItemName ?? "";
            sheet.Cell(rowIndex, col++).Value = s.ItemDescription ?? "";

            if (s.EstimatedValue.HasValue)
            {
                var evCell = sheet.Cell(rowIndex, col);
                evCell.Value = s.EstimatedValue.Value;
                evCell.Style.NumberFormat.Format = "#,##0.00";
            }
            else { sheet.Cell(rowIndex, col).Value = "—"; }
            col++;

            var feeCell = sheet.Cell(rowIndex, col++);
            if (s.StripeFeeAmount.HasValue) { feeCell.Value = s.StripeFeeAmount.Value; feeCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { feeCell.Value = "—"; }

            var commCell = sheet.Cell(rowIndex, col++);
            if (s.PlatformCommissionAmount.HasValue) { commCell.Value = s.PlatformCommissionAmount.Value; commCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { commCell.Value = "—"; }

            var payoutCell = sheet.Cell(rowIndex, col++);
            if (s.OrganizerPayoutAmount.HasValue) { payoutCell.Value = s.OrganizerPayoutAmount.Value; payoutCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { payoutCell.Value = "—"; }

            sheet.Cell(rowIndex, col++).Value = s.SponsorNotes ?? "";

            var dateCell = sheet.Cell(rowIndex, col++);
            dateCell.Value = s.PaymentCompletedAt ?? s.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Add-On Feature: Exports add-on purchases to Excel format.
    /// </summary>
    public byte[] ExportAddOnPurchases(EventAddOnPurchasesResponse purchases)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Add-On Purchases");

        var headers = new[] { "Add-On Name", "Buyer Name", "Email", "Phone", "Quantity", "Unit Price", "Total Amount", "Currency", "Status", "Stripe Fee", "Platform Commission", "Organizer Payout", "Bundled with Registration", "Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int rowIndex = 2;
        foreach (var p in purchases.Purchases)
        {
            int col = 1;
            sheet.Cell(rowIndex, col++).Value = p.AddOnName;
            sheet.Cell(rowIndex, col++).Value = p.BuyerName;
            sheet.Cell(rowIndex, col++).Value = p.BuyerEmail;
            sheet.Cell(rowIndex, col++).Value = p.BuyerPhone ?? "";
            sheet.Cell(rowIndex, col++).Value = p.Quantity;

            var upCell = sheet.Cell(rowIndex, col++);
            upCell.Value = p.UnitPrice;
            upCell.Style.NumberFormat.Format = "#,##0.00";

            var taCell = sheet.Cell(rowIndex, col++);
            taCell.Value = p.TotalAmount;
            taCell.Style.NumberFormat.Format = "#,##0.00";

            sheet.Cell(rowIndex, col++).Value = p.Currency;
            sheet.Cell(rowIndex, col++).Value = p.Status;

            var feeCell = sheet.Cell(rowIndex, col++);
            if (p.StripeFeeAmount.HasValue) { feeCell.Value = p.StripeFeeAmount.Value; feeCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { feeCell.Value = "—"; }

            var commCell = sheet.Cell(rowIndex, col++);
            if (p.PlatformCommissionAmount.HasValue) { commCell.Value = p.PlatformCommissionAmount.Value; commCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { commCell.Value = "—"; }

            var payoutCell = sheet.Cell(rowIndex, col++);
            if (p.OrganizerPayoutAmount.HasValue) { payoutCell.Value = p.OrganizerPayoutAmount.Value; payoutCell.Style.NumberFormat.Format = "#,##0.00"; }
            else { payoutCell.Value = "—"; }

            sheet.Cell(rowIndex, col++).Value = p.RegistrationId.HasValue ? "Yes" : "No";

            var dateCell = sheet.Cell(rowIndex, col++);
            dateCell.Value = p.PaymentCompletedAt ?? p.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Donation Feature: Exports donations to Excel format.
    /// </summary>
    public byte[] ExportDonations(EventDonationsResponse donations)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Donations");

        // Headers
        var headers = new[] { "Donor Name", "Email", "Phone", "Amount", "Currency", "Status", "Notes", "Date", "Bundled" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int rowIndex = 2;
        foreach (var d in donations.Donations)
        {
            int col = 1;
            sheet.Cell(rowIndex, col++).Value = d.DonorName;
            sheet.Cell(rowIndex, col++).Value = d.DonorEmail;
            sheet.Cell(rowIndex, col++).Value = d.DonorPhone ?? "";
            sheet.Cell(rowIndex, col++).Value = d.Amount;
            sheet.Cell(rowIndex, col++).Value = d.Currency;
            sheet.Cell(rowIndex, col++).Value = d.Status;
            sheet.Cell(rowIndex, col++).Value = d.DonorNotes ?? "";

            var dateCell = sheet.Cell(rowIndex, col++);
            if (d.PaymentCompletedAt.HasValue)
            {
                dateCell.Value = d.PaymentCompletedAt.Value;
                dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            }
            else
            {
                dateCell.Value = d.CreatedAt;
                dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            }

            sheet.Cell(rowIndex, col++).Value = d.IsBundled ? "Yes" : "No";
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports all financial data as a multi-sheet Excel workbook.
    /// Reuses individual sheet-creation logic from existing export methods.
    /// </summary>
    public byte[] ExportAllFinancials(AllFinancialsData data)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Attendees (reuse existing registration sheet logic)
        CreateRegistrationsSheet(workbook, data.Attendees);

        // Sheet 2: Donations
        CreateDonationsSheet(workbook, data.Donations);

        // Sheet 3: Collections
        CreateCollectionsSheet(workbook, data.Collections);

        // Sheet 4: Sponsors
        CreateSponsorsSheet(workbook, data.Sponsors);

        // Sheet 5: Add-On Purchases
        CreateAddOnPurchasesSheet(workbook, data.AddOnPurchases);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void CreateDonationsSheet(IXLWorkbook workbook, EventDonationsResponse donations)
    {
        var sheet = workbook.Worksheets.Add("Donations");

        var headers = new[] { "Donor Name", "Email", "Phone", "Amount", "Currency", "Status", "Notes", "Date", "Bundled" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var d in donations.Donations)
        {
            int col = 1;
            sheet.Cell(row, col++).Value = d.DonorName;
            sheet.Cell(row, col++).Value = d.DonorEmail;
            sheet.Cell(row, col++).Value = d.DonorPhone ?? "";
            sheet.Cell(row, col++).Value = d.Amount;
            sheet.Cell(row, col++).Value = d.Currency;
            sheet.Cell(row, col++).Value = d.Status;
            sheet.Cell(row, col++).Value = d.DonorNotes ?? "";
            var dateCell = sheet.Cell(row, col++);
            dateCell.Value = d.PaymentCompletedAt ?? d.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            sheet.Cell(row, col++).Value = d.IsBundled ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);
    }

    private static void CreateCollectionsSheet(IXLWorkbook workbook, EventCollectionsResponse collections)
    {
        var sheet = workbook.Worksheets.Add("Collections");

        var headers = new[] { "Contributor Name", "Email", "Phone", "Amount", "Currency", "Status", "Notes", "Stripe Fee", "Platform Commission", "Organizer Payout", "Date" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var c in collections.Collections)
        {
            int col = 1;
            sheet.Cell(row, col++).Value = c.ContributorName;
            sheet.Cell(row, col++).Value = c.ContributorEmail;
            sheet.Cell(row, col++).Value = c.ContributorPhone ?? "";
            var amtCell = sheet.Cell(row, col++); amtCell.Value = c.Amount; amtCell.Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, col++).Value = c.Currency;
            sheet.Cell(row, col++).Value = c.Status;
            sheet.Cell(row, col++).Value = c.ContributorNotes ?? "";
            var feeCell = sheet.Cell(row, col++);
            if (c.StripeFeeAmount.HasValue) { feeCell.Value = c.StripeFeeAmount.Value; feeCell.Style.NumberFormat.Format = "#,##0.00"; } else { feeCell.Value = "—"; }
            var commCell = sheet.Cell(row, col++);
            if (c.PlatformCommissionAmount.HasValue) { commCell.Value = c.PlatformCommissionAmount.Value; commCell.Style.NumberFormat.Format = "#,##0.00"; } else { commCell.Value = "—"; }
            var payoutCell = sheet.Cell(row, col++);
            if (c.OrganizerPayoutAmount.HasValue) { payoutCell.Value = c.OrganizerPayoutAmount.Value; payoutCell.Style.NumberFormat.Format = "#,##0.00"; } else { payoutCell.Value = "—"; }
            var dateCell = sheet.Cell(row, col++);
            dateCell.Value = c.PaymentCompletedAt ?? c.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);
    }

    private static void CreateSponsorsSheet(IXLWorkbook workbook, EventSponsorsResponse sponsors)
    {
        var sheet = workbook.Worksheets.Add("Sponsors");

        var headers = new[] { "Sponsor Name", "Email", "Phone", "Organization", "Type", "Amount", "Currency", "Status", "Item Name", "Item Description", "Estimated Value", "Notes", "Date" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var s in sponsors.Sponsors)
        {
            int col = 1;
            sheet.Cell(row, col++).Value = s.SponsorName;
            sheet.Cell(row, col++).Value = s.SponsorEmail;
            sheet.Cell(row, col++).Value = s.SponsorPhone ?? "";
            sheet.Cell(row, col++).Value = s.SponsorOrganization ?? "";
            sheet.Cell(row, col++).Value = s.SponsorType;
            var amtCell2 = sheet.Cell(row, col++);
            if (s.Amount.HasValue) { amtCell2.Value = s.Amount.Value; amtCell2.Style.NumberFormat.Format = "#,##0.00"; } else { amtCell2.Value = "—"; }
            sheet.Cell(row, col++).Value = s.Currency ?? "";
            sheet.Cell(row, col++).Value = s.Status;
            sheet.Cell(row, col++).Value = s.ItemName ?? "";
            sheet.Cell(row, col++).Value = s.ItemDescription ?? "";
            var estCell = sheet.Cell(row, col++);
            if (s.EstimatedValue.HasValue) { estCell.Value = s.EstimatedValue.Value; estCell.Style.NumberFormat.Format = "#,##0.00"; } else { estCell.Value = "—"; }
            sheet.Cell(row, col++).Value = s.SponsorNotes ?? "";
            var dateCell = sheet.Cell(row, col++);
            dateCell.Value = s.PaymentCompletedAt ?? s.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);
    }

    private static void CreateAddOnPurchasesSheet(IXLWorkbook workbook, EventAddOnPurchasesResponse purchases)
    {
        var sheet = workbook.Worksheets.Add("Add-On Purchases");

        var headers = new[] { "Add-On Name", "Buyer Name", "Email", "Phone", "Quantity", "Unit Price", "Total Amount", "Currency", "Status", "Bundled with Registration", "Date" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var p in purchases.Purchases)
        {
            int col = 1;
            sheet.Cell(row, col++).Value = p.AddOnName;
            sheet.Cell(row, col++).Value = p.BuyerName;
            sheet.Cell(row, col++).Value = p.BuyerEmail;
            sheet.Cell(row, col++).Value = p.BuyerPhone ?? "";
            sheet.Cell(row, col++).Value = p.Quantity;
            var upCell = sheet.Cell(row, col++); upCell.Value = p.UnitPrice; upCell.Style.NumberFormat.Format = "#,##0.00";
            var taCell = sheet.Cell(row, col++); taCell.Value = p.TotalAmount; taCell.Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, col++).Value = p.Currency;
            sheet.Cell(row, col++).Value = p.Status;
            sheet.Cell(row, col++).Value = p.RegistrationId.HasValue ? "Yes" : "No";
            var dateCell = sheet.Cell(row, col++);
            dateCell.Value = p.PaymentCompletedAt ?? p.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);
    }
}

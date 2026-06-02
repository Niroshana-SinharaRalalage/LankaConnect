using System.Globalization;
using System.IO.Compression;
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

        // Write header (Phase 6A.68: Removed RegistrationId - not needed by organizers)
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

        // Phase 6A.71: Conditionally include payment/amount columns only for paid events
        if (!attendees.IsFreeEvent)
        {
            csv.WriteField("PaymentStatus");
            csv.WriteField("GrossAmount");

            // Phase 6A.X: Add detailed revenue breakdown columns
            if (attendees.HasRevenueBreakdown)
            {
                csv.WriteField("SalesTax");
                csv.WriteField("TaxRate");
                csv.WriteField("StripeFee");
                csv.WriteField("PlatformCommission");
            }

            csv.WriteField("NetAmount");
            csv.WriteField("Currency");
            // Phase 6A.X: Ticket Code column for paid events only
            csv.WriteField("TicketCode");
        }

        csv.WriteField("RegistrationDate");
        csv.WriteField("Status");
        // Phase 6A.161: Ticket tier(s) — appended LAST so existing positional column order is
        // untouched. Unconditional (tiers can exist on free events too, e.g. $0 adult price).
        csv.WriteField("TicketTier");
        csv.NextRecord();

        // Phase 6A.71: Calculate totals for summary row with NET revenue (after commission)
        var totalAttendeeCount = attendees.Attendees.Sum(a => a.TotalAttendees);
        var grossRevenue = attendees.Attendees
            .Where(a => a.TotalAmount.HasValue)
            .Sum(a => a.TotalAmount!.Value);
        var netRevenue = attendees.NetRevenue;  // Organizer's payout after 5% commission
        var commissionAmount = attendees.CommissionAmount;
        var currency = attendees.Attendees.FirstOrDefault(a => !string.IsNullOrEmpty(a.Currency))?.Currency ?? "USD";

        // Write data rows
        foreach (var a in attendees.Attendees)
        {
            // Phase 7E.8: source these from the DTO so Mode-B registrations export the
            // lead name / "+N attendees" / demographic counts that the post-processing
            // pass populated. Mode A still gets the existing per-attendee shape since
            // MainAttendeeName / AdditionalAttendees fall back to the Attendees list.
            var additionalAttendeesCsv = a.AdditionalAttendees == "—" ? "" : a.AdditionalAttendees;

            // Phase 6A.68: Removed RegistrationId from export
            csv.WriteField(a.MainAttendeeName);
            csv.WriteField(additionalAttendeesCsv);
            csv.WriteField(a.TotalAttendees);
            csv.WriteField(a.AdultCount);
            csv.WriteField(a.ChildCount);
            csv.WriteField(a.MaleCount);
            csv.WriteField(a.FemaleCount);
            csv.WriteField(a.GenderDistribution);
            csv.WriteField(a.ContactEmail);
            csv.WriteField(a.ContactPhone ?? "");
            csv.WriteField(a.ContactAddress ?? "");

            // Phase 6A.71: Conditionally write payment/amount columns only for paid events
            if (!attendees.IsFreeEvent)
            {
                csv.WriteField(a.PaymentStatus.ToString());
                csv.WriteField(a.TotalAmount?.ToString("F2") ?? "");  // Gross amount

                // Phase 6A.X: Write breakdown columns if available
                if (attendees.HasRevenueBreakdown)
                {
                    csv.WriteField(a.SalesTaxAmount?.ToString("F2") ?? "");
                    csv.WriteField(a.SalesTaxRate > 0 ? $"{a.SalesTaxRate * 100:F2}%" : "");
                    csv.WriteField(a.StripeFeeAmount?.ToString("F2") ?? "");
                    csv.WriteField(a.PlatformCommissionAmount?.ToString("F2") ?? "");
                }

                csv.WriteField(a.NetAmount?.ToString("F2") ?? "");  // Net amount (organizer payout)
                csv.WriteField(a.Currency ?? "");
                // Phase 6A.X: Ticket Code column for paid events only
                csv.WriteField(a.TicketCode ?? "");
            }

            csv.WriteField(a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            csv.WriteField(a.Status.ToString());
            // Phase 6A.161: Ticket tier summary (computed, never null — "—" when no tier).
            csv.WriteField(a.TicketTierSummary);
            csv.NextRecord();
        }

        // Phase 6A.71: Add summary totals row with NET revenue (after commission)
        if (attendees.Attendees.Any())
        {
            // Empty row for separation
            csv.NextRecord();

            // Summary row - NET Revenue (organizer's payout after 5% fee)
            csv.WriteField("TOTAL");
            csv.WriteField("");  // AdditionalAttendees
            csv.WriteField(totalAttendeeCount);  // Total attendees across all registrations
            csv.WriteField("");  // Adults
            csv.WriteField("");  // Children
            csv.WriteField("");  // MaleCount
            csv.WriteField("");  // FemaleCount
            csv.WriteField("");  // GenderDistribution
            csv.WriteField("");  // Email
            csv.WriteField("");  // Phone
            csv.WriteField("");  // Address

            // Phase 6A.71: Conditionally write payment/amount columns in summary row
            if (!attendees.IsFreeEvent)
            {
                csv.WriteField("");  // PaymentStatus
                csv.WriteField(grossRevenue > 0 ? $"{grossRevenue:F2}" : "0.00");  // Gross revenue

                // Phase 6A.X: Write breakdown totals if available
                if (attendees.HasRevenueBreakdown)
                {
                    csv.WriteField(attendees.TotalSalesTax > 0 ? $"{attendees.TotalSalesTax:F2}" : "0.00");
                    csv.WriteField(attendees.AverageTaxRate > 0 ? $"{attendees.AverageTaxRate * 100:F2}%" : "");
                    csv.WriteField(attendees.TotalStripeFees > 0 ? $"{attendees.TotalStripeFees:F2}" : "0.00");
                    csv.WriteField(attendees.TotalPlatformCommission > 0 ? $"{attendees.TotalPlatformCommission:F2}" : "0.00");
                }

                csv.WriteField(attendees.TotalOrganizerPayout > 0
                    ? $"{attendees.TotalOrganizerPayout:F2}"
                    : (netRevenue > 0 ? $"{netRevenue:F2}" : "0.00"));  // Net revenue (use breakdown if available, otherwise legacy)
                csv.WriteField(currency);  // Currency
                csv.WriteField("");  // TicketCode
            }

            csv.WriteField("");  // RegistrationDate
            csv.WriteField("");  // Status
            csv.WriteField("");  // Phase 6A.161: TicketTier — blank in the TOTAL summary row
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Phase 6A.69: Exports event signup lists to ZIP archive containing multiple CSV files.
    /// Each CSV represents one SignUpList-ItemCategory combination.
    /// </summary>
    public byte[] ExportSignUpListsToZip(List<SignUpListDto> signUpLists, Guid eventId, SignUpExportLabels? labels = null)
    {
        if (signUpLists == null || !signUpLists.Any())
            throw new ArgumentException("No signup lists to export", nameof(signUpLists));

        var columnLabels = labels ?? SignUpExportLabels.ForItems();

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Group items by (SignUpList.Category, ItemCategory) combination
            // Phase 6A.69 (Revised): Filter out "Preferred" category (deprecated, no longer in use)
#pragma warning disable CS0618 // Type or member is obsolete
            var groupedItems = signUpLists
                .SelectMany(list => list.Items
                    .Where(item => item.ItemCategory != Domain.Events.Enums.SignUpItemCategory.Preferred)  // Exclude Preferred category
                    .Select(item => new
                    {
                        SignUpList = list,
                        Item = item
                    }))
                .GroupBy(x => new
                {
                    SignUpListCategory = x.SignUpList.Category,
                    ItemCategory = x.Item.ItemCategory
                });
#pragma warning restore CS0618 // Type or member is obsolete

            // Track filenames to prevent duplicates (Excel error: "can't open two workbooks with same name")
            var fileNameCounter = new Dictionary<string, int>();

            foreach (var group in groupedItems)
            {
                // Generate CSV filename: "Food-Drinks-Mandatory.csv"
                var baseFileName = SanitizeFileName(
                    $"{group.Key.SignUpListCategory}-{group.Key.ItemCategory}"
                );

                // Ensure unique filenames by adding counter if duplicate exists
                string fileName;
                if (fileNameCounter.ContainsKey(baseFileName))
                {
                    fileNameCounter[baseFileName]++;
                    fileName = $"{baseFileName}-{fileNameCounter[baseFileName]}.csv";
                }
                else
                {
                    fileNameCounter[baseFileName] = 1;
                    fileName = $"{baseFileName}.csv";
                }

                // Create ZIP entry
                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    NewLine = "\n",  // LF for cross-platform compatibility
                    ShouldQuote = args => true,  // Always quote for safety
                    TrimOptions = TrimOptions.Trim,
                    UseNewObjectForNullReferenceMembers = false
                });

                // Phase 6A.69 - Revised CSV format: Grouped layout
                // Phase 7D.1 Step 15: Headers sourced from label set (default: Items; volunteer exports override).
                csv.WriteField(columnLabels.ItemDescription);
                csv.WriteField(columnLabels.RequestedQuantity);
                csv.WriteField(columnLabels.RemainingQuantity);
                csv.WriteField(columnLabels.ContactName);
                csv.WriteField(columnLabels.ContactEmail);
                csv.WriteField(columnLabels.ContactPhone);
                csv.WriteField(columnLabels.QuantityCommitted);
                csv.NextRecord();

                // Write data rows - grouped by item
                foreach (var itemData in group)
                {
                    var item = itemData.Item;

                    if (!item.Commitments.Any())
                    {
                        // Zero commitments - single row with placeholders
                        WriteItemRow(csv, item, null);
                    }
                    else
                    {
                        // First row: Item header with first commitment
                        WriteItemRow(csv, item, item.Commitments.First());

                        // Subsequent rows: Additional commitments (if any)
                        foreach (var commitment in item.Commitments.Skip(1))
                        {
                            WriteCommitmentOnlyRow(csv, commitment);
                        }
                    }
                }

                writer.Flush();
            }
        }

        return zipStream.ToArray();
    }

    /// <summary>
    /// Phase 6A.69 (Revised): Write item row with full details (item + optional commitment).
    /// Format: Item Description | Requested Quantity | Remaining Quantity | Contact Name | Contact Email | Contact Phone | Quantity Committed
    /// </summary>
#pragma warning disable CS0618 // Suppress obsolete warning for SignUpItemDto
    private static void WriteItemRow(
        CsvWriter csv,
        ISignUpItemDto item,
        SignUpCommitmentDto? commitment)
    {
        // Item information
        csv.WriteField(item.ItemDescription);

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

        csv.WriteField(quantity);
        csv.WriteField(remaining);

        // Contact information (use em dash for missing data)
        csv.WriteField(commitment?.ContactName ?? "—");
        csv.WriteField(commitment?.ContactEmail ?? "—");

        // Phone number with apostrophe prefix (prevents Excel auto-formatting to scientific notation)
        var phone = string.IsNullOrWhiteSpace(commitment?.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        csv.WriteField(phone);

        // Phase 6A.121: Use dual nullable fields (PhysicalQuantity or SlotsClaimed)
        csv.WriteField(commitment?.PhysicalQuantity ?? commitment?.SlotsClaimed ?? 0);
        csv.NextRecord();
    }
#pragma warning restore CS0618

    /// <summary>
    /// Phase 6A.69 (Revised): Write commitment-only row (blank item columns, commitment data only).
    /// Used for additional commitments after the first one.
    /// Format: [blank] | [blank] | [blank] | Contact Name | Contact Email | Contact Phone | Quantity Committed
    /// </summary>
    private static void WriteCommitmentOnlyRow(
        CsvWriter csv,
        SignUpCommitmentDto commitment)
    {
        // Blank item columns (item already shown in previous row)
        csv.WriteField("");  // Item Description
        csv.WriteField("");  // Requested Quantity
        csv.WriteField("");  // Remaining Quantity

        // Contact information
        csv.WriteField(commitment.ContactName);
        csv.WriteField(commitment.ContactEmail);

        // Phone number with apostrophe prefix
        var phone = string.IsNullOrWhiteSpace(commitment.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        csv.WriteField(phone);

        // Phase 6A.121: Use dual nullable fields (PhysicalQuantity or SlotsClaimed)
        csv.WriteField(commitment.PhysicalQuantity ?? commitment.SlotsClaimed ?? 0);
        csv.NextRecord();
    }

    /// <summary>
    /// Phase 6A.69: Sanitize filename for ZIP entry (remove invalid characters).
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

    /// <summary>
    /// Exports custom form responses to CSV format.
    /// One row per response, questions as columns (horizontal layout).
    /// Phase 6A.110: Form response export functionality
    /// </summary>
    public byte[] ExportFormResponses(EventFormDetailDto form, FormResponsesPagedDto responses)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",  // LF for cross-platform compatibility
            ShouldQuote = args => true,  // Always quote fields (handles special chars)
            TrimOptions = TrimOptions.Trim
        });

        // 1. Write header row
        csv.WriteField("Respondent Name");
        csv.WriteField("Respondent Email");
        csv.WriteField("Submitted Date");

        // Sort questions by SortOrder to match form display order
        var sortedQuestions = form.Questions.OrderBy(q => q.SortOrder).ToList();
        foreach (var question in sortedQuestions)
        {
            // Include question number for clarity (Q1, Q2, etc.)
            csv.WriteField($"Q{question.SortOrder + 1}: {question.QuestionText}");
        }
        csv.NextRecord();

        // 2. Write data rows
        foreach (var response in responses.Responses)
        {
            // Respondent info
            csv.WriteField(response.RespondentName ?? "Anonymous");
            csv.WriteField(response.RespondentEmail ?? "—");
            csv.WriteField(response.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            // Answers for each question
            foreach (var question in sortedQuestions)
            {
                var answer = response.Answers.FirstOrDefault(a => a.FormQuestionId == question.Id);
                csv.WriteField(FormatAnswerForCsvExport(answer));
            }
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Formats a form answer for CSV export.
    /// Handles text, boolean, single choice, multiple choice answers.
    /// </summary>
    private static string FormatAnswerForCsvExport(FormAnswerDto? answer)
    {
        if (answer == null)
            return "—";  // Em dash for unanswered questions

        // Boolean: "Yes" or "No" (user-friendly)
        if (answer.BooleanValue.HasValue)
            return answer.BooleanValue.Value ? "Yes" : "No";

        // Multiple choice: "Option 1, Option 2" (comma-separated, uses snapshots)
        if (answer.SelectedOptionTextSnapshots?.Any() == true)
            return string.Join(", ", answer.SelectedOptionTextSnapshots);

        // Text/Number/Date (single text value)
        if (!string.IsNullOrWhiteSpace(answer.TextValue))
            return answer.TextValue;

        return "—";
    }

    /// <summary>
    /// Collection Feature: Exports event fund collections to CSV format.
    /// </summary>
    public byte[] ExportCollections(EventCollectionsResponse collections)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",
            ShouldQuote = args => true,
            TrimOptions = TrimOptions.Trim
        });

        csv.WriteField("Contributor Name");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Amount");
        csv.WriteField("Currency");
        csv.WriteField("Status");
        csv.WriteField("Notes");
        csv.WriteField("Stripe Fee");
        csv.WriteField("Platform Commission");
        csv.WriteField("Organizer Payout");
        csv.WriteField("Date");
        csv.NextRecord();

        foreach (var c in collections.Collections)
        {
            csv.WriteField(c.ContributorName);
            csv.WriteField(c.ContributorEmail);
            csv.WriteField(c.ContributorPhone ?? "");
            csv.WriteField(c.Amount.ToString("F2"));
            csv.WriteField(c.Currency);
            csv.WriteField(c.Status);
            csv.WriteField(c.ContributorNotes ?? "");
            csv.WriteField(c.StripeFeeAmount?.ToString("F2") ?? "");
            csv.WriteField(c.PlatformCommissionAmount?.ToString("F2") ?? "");
            csv.WriteField(c.OrganizerPayoutAmount?.ToString("F2") ?? "");
            csv.WriteField((c.PaymentCompletedAt ?? c.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"));
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Sponsor Feature: Exports sponsorships to CSV format.
    /// </summary>
    public byte[] ExportSponsors(EventSponsorsResponse sponsors)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",
            ShouldQuote = args => true,
            TrimOptions = TrimOptions.Trim
        });

        csv.WriteField("Sponsor Name");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Organization");
        csv.WriteField("Type");
        csv.WriteField("Amount");
        csv.WriteField("Currency");
        csv.WriteField("Status");
        csv.WriteField("Item Name");
        csv.WriteField("Item Description");
        csv.WriteField("Estimated Value");
        csv.WriteField("Stripe Fee");
        csv.WriteField("Platform Commission");
        csv.WriteField("Organizer Payout");
        csv.WriteField("Notes");
        csv.WriteField("Date");
        csv.NextRecord();

        foreach (var s in sponsors.Sponsors)
        {
            csv.WriteField(s.SponsorName);
            csv.WriteField(s.SponsorEmail);
            csv.WriteField(s.SponsorPhone ?? "");
            csv.WriteField(s.SponsorOrganization ?? "");
            csv.WriteField(s.SponsorType);
            csv.WriteField(s.Amount?.ToString("F2") ?? "");
            csv.WriteField(s.Currency ?? "");
            csv.WriteField(s.Status);
            csv.WriteField(s.ItemName ?? "");
            csv.WriteField(s.ItemDescription ?? "");
            csv.WriteField(s.EstimatedValue?.ToString("F2") ?? "");
            csv.WriteField(s.StripeFeeAmount?.ToString("F2") ?? "");
            csv.WriteField(s.PlatformCommissionAmount?.ToString("F2") ?? "");
            csv.WriteField(s.OrganizerPayoutAmount?.ToString("F2") ?? "");
            csv.WriteField(s.SponsorNotes ?? "");
            csv.WriteField((s.PaymentCompletedAt ?? s.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"));
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Add-On Feature: Exports add-on purchases to CSV format.
    /// </summary>
    public byte[] ExportAddOnPurchases(EventAddOnPurchasesResponse purchases)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",
            ShouldQuote = args => true,
            TrimOptions = TrimOptions.Trim
        });

        csv.WriteField("Add-On Name");
        csv.WriteField("Buyer Name");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Quantity");
        csv.WriteField("Unit Price");
        csv.WriteField("Total Amount");
        csv.WriteField("Currency");
        csv.WriteField("Status");
        csv.WriteField("Stripe Fee");
        csv.WriteField("Platform Commission");
        csv.WriteField("Organizer Payout");
        csv.WriteField("Bundled with Registration");
        csv.WriteField("Date");
        csv.NextRecord();

        foreach (var p in purchases.Purchases)
        {
            csv.WriteField(p.AddOnName);
            csv.WriteField(p.BuyerName);
            csv.WriteField(p.BuyerEmail);
            csv.WriteField(p.BuyerPhone ?? "");
            csv.WriteField(p.Quantity);
            csv.WriteField(p.UnitPrice.ToString("F2"));
            csv.WriteField(p.TotalAmount.ToString("F2"));
            csv.WriteField(p.Currency);
            csv.WriteField(p.Status);
            csv.WriteField(p.StripeFeeAmount?.ToString("F2") ?? "");
            csv.WriteField(p.PlatformCommissionAmount?.ToString("F2") ?? "");
            csv.WriteField(p.OrganizerPayoutAmount?.ToString("F2") ?? "");
            csv.WriteField(p.RegistrationId.HasValue ? "Yes" : "No");
            csv.WriteField((p.PaymentCompletedAt ?? p.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"));
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Donation Feature: Exports donations to CSV format.
    /// </summary>
    public byte[] ExportDonations(EventDonationsResponse donations)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",
            ShouldQuote = args => true,
            TrimOptions = TrimOptions.Trim
        });

        // Header row
        csv.WriteField("Donor Name");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Amount");
        csv.WriteField("Currency");
        csv.WriteField("Status");
        csv.WriteField("Notes");
        csv.WriteField("Date");
        csv.WriteField("Bundled with Registration");
        csv.NextRecord();

        // Data rows
        foreach (var d in donations.Donations)
        {
            csv.WriteField(d.DonorName);
            csv.WriteField(d.DonorEmail);
            csv.WriteField(d.DonorPhone ?? "");
            csv.WriteField(d.Amount.ToString("F2"));
            csv.WriteField(d.Currency);
            csv.WriteField(d.Status);
            csv.WriteField(d.DonorNotes ?? "");
            csv.WriteField(d.PaymentCompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")
                ?? d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            csv.WriteField(d.IsBundled ? "Yes" : "No");
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Exports all financial data as a ZIP archive containing 5 CSV files.
    /// Reuses individual CSV export methods to generate each file.
    /// </summary>
    public byte[] ExportAllFinancialsZip(AllFinancialsData data)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // 1. Attendees CSV
            var attendeesBytes = ExportEventAttendees(data.Attendees);
            AddToZip(archive, "attendees.csv", attendeesBytes);

            // 2. Donations CSV
            var donationsBytes = ExportDonations(data.Donations);
            AddToZip(archive, "donations.csv", donationsBytes);

            // 3. Collections CSV
            var collectionsBytes = ExportCollections(data.Collections);
            AddToZip(archive, "collections.csv", collectionsBytes);

            // 4. Sponsors CSV
            var sponsorsBytes = ExportSponsors(data.Sponsors);
            AddToZip(archive, "sponsors.csv", sponsorsBytes);

            // 5. Add-On Purchases CSV
            var addOnsBytes = ExportAddOnPurchases(data.AddOnPurchases);
            AddToZip(archive, "addon_purchases.csv", addOnsBytes);
        }

        return zipStream.ToArray();
    }

    private static void AddToZip(ZipArchive archive, string fileName, byte[] content)
    {
        var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(content, 0, content.Length);
    }
}

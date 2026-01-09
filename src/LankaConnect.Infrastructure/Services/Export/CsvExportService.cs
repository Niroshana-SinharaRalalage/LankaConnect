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
        csv.WriteField("PaymentStatus");
        csv.WriteField("TotalAmount");
        csv.WriteField("Currency");
        csv.WriteField("TicketCode");
        csv.WriteField("QRCode");
        csv.WriteField("RegistrationDate");
        csv.WriteField("Status");
        csv.NextRecord();

        // Calculate totals for summary row
        var totalAttendeeCount = attendees.Attendees.Sum(a => a.TotalAttendees);
        var totalAmount = attendees.Attendees
            .Where(a => a.TotalAmount.HasValue)
            .Sum(a => a.TotalAmount!.Value);
        var currency = attendees.Attendees.FirstOrDefault(a => !string.IsNullOrEmpty(a.Currency))?.Currency ?? "USD";

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

            // Phase 6A.68: Removed RegistrationId from export
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

        // Phase 6A.68: Add summary totals row at the bottom
        if (attendees.Attendees.Any())
        {
            // Empty row for separation
            csv.NextRecord();

            // Summary row
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
            csv.WriteField("");  // PaymentStatus
            csv.WriteField(totalAmount.ToString("F2"));  // Total amount collected
            csv.WriteField(currency);  // Currency
            csv.WriteField("");  // TicketCode
            csv.WriteField("");  // QRCode
            csv.WriteField("");  // RegistrationDate
            csv.WriteField("");  // Status
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Phase 6A.69: Exports event signup lists to ZIP archive containing multiple CSV files.
    /// Each CSV represents one SignUpList-ItemCategory combination.
    /// </summary>
    public byte[] ExportSignUpListsToZip(List<SignUpListDto> signUpLists, Guid eventId)
    {
        if (signUpLists == null || !signUpLists.Any())
            throw new ArgumentException("No signup lists to export", nameof(signUpLists));

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
                // Write CSV headers (removed "Sign-up List" and "Committed At")
                csv.WriteField("Item Description");
                csv.WriteField("Requested Quantity");
                csv.WriteField("Remaining Quantity");
                csv.WriteField("Contact Name");
                csv.WriteField("Contact Email");
                csv.WriteField("Contact Phone");
                csv.WriteField("Quantity Committed");
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
    private static void WriteItemRow(
        CsvWriter csv,
        SignUpItemDto item,
        SignUpCommitmentDto? commitment)
    {
        // Item information
        csv.WriteField(item.ItemDescription);
        csv.WriteField(item.Quantity);
        csv.WriteField(item.RemainingQuantity);

        // Contact information (use em dash for missing data)
        csv.WriteField(commitment?.ContactName ?? "—");
        csv.WriteField(commitment?.ContactEmail ?? "—");

        // Phone number with apostrophe prefix (prevents Excel auto-formatting to scientific notation)
        var phone = string.IsNullOrWhiteSpace(commitment?.ContactPhone)
            ? "—"
            : "'" + commitment.ContactPhone;
        csv.WriteField(phone);

        csv.WriteField(commitment?.Quantity ?? 0);
        csv.NextRecord();
    }

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

        csv.WriteField(commitment.Quantity);
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

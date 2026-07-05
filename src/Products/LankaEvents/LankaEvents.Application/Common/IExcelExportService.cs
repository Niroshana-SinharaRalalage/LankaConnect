using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Contracts;
namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Service for exporting data to Excel format.
/// Implementation resides in Infrastructure layer.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports event attendees and optional signup lists to multi-sheet Excel file.
    /// </summary>
    /// <param name="attendees">Event attendee data</param>
    /// <param name="signUpLists">Optional signup lists for additional sheets</param>
    /// <returns>Excel file content as byte array</returns>
    byte[] ExportEventAttendees(
        EventAttendeesResponse attendees,
        List<SignUpListDto>? signUpLists = null);

    /// <summary>
    /// Phase 6A.73 (Revised): Exports signup lists to ZIP archive containing multiple Excel files.
    /// Creates one Excel file per signup list, each with sheets for different categories (Mandatory, Suggested, Open).
    /// Uses grouped format where each item shows once with commitments listed below.
    /// </summary>
    /// <param name="signUpLists">Signup lists to export</param>
    /// <param name="eventId">Event ID for filename generation</param>
    /// <param name="labels">Optional column-label override (defaults to Items labels). Phase 7D.1 Step 15.</param>
    /// <returns>ZIP archive content as byte array</returns>
    byte[] ExportSignUpListsToExcelZip(List<SignUpListDto> signUpLists, Guid eventId, SignUpExportLabels? labels = null);

    /// <summary>
    /// Exports custom form responses to Excel format.
    /// Single sheet with responses in rows, questions as columns.
    /// Phase 6A.110: Form response export functionality
    /// </summary>
    /// <param name="form">Form detail with questions (sorted by SortOrder)</param>
    /// <param name="responses">All responses with answers</param>
    /// <returns>Excel file content as byte array</returns>
    byte[] ExportFormResponses(EventFormDetailDto form, FormResponsesPagedDto responses);

    /// <summary>
    /// Donation Feature: Exports donations to Excel format.
    /// </summary>
    byte[] ExportDonations(EventDonationsResponse donations);

    /// <summary>
    /// Collection Feature: Exports event fund collections to Excel format.
    /// </summary>
    byte[] ExportCollections(EventCollectionsResponse collections);

    /// <summary>
    /// Sponsor Feature: Exports sponsorships to Excel format.
    /// </summary>
    byte[] ExportSponsors(EventSponsorsResponse sponsors);

    /// <summary>
    /// Add-On Feature: Exports add-on purchases to Excel format.
    /// </summary>
    byte[] ExportAddOnPurchases(EventAddOnPurchasesResponse purchases);

    /// <summary>
    /// Exports all financial data as a multi-sheet Excel workbook.
    /// Sheets: Attendees, Donations, Collections, Sponsors, Add-On Purchases.
    /// </summary>
    byte[] ExportAllFinancials(AllFinancialsData data);
}

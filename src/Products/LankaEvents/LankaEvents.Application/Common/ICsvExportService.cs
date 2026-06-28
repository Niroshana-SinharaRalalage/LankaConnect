using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Service for exporting data to CSV format.
/// Implementation resides in Infrastructure layer.
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Exports event attendees to CSV format.
    /// </summary>
    /// <param name="attendees">Event attendee data</param>
    /// <returns>CSV file content as byte array</returns>
    byte[] ExportEventAttendees(EventAttendeesResponse attendees);

    /// <summary>
    /// Exports event signup lists to ZIP archive containing multiple CSV files.
    /// Phase 6A.69: Each CSV represents one SignUpList-ItemCategory combination.
    /// </summary>
    /// <param name="signUpLists">Signup list data with items and commitments</param>
    /// <param name="eventId">Event ID for filename generation</param>
    /// <param name="labels">Optional column-label override (defaults to Items labels). Phase 7D.1 Step 15.</param>
    /// <returns>ZIP file content as byte array</returns>
    byte[] ExportSignUpListsToZip(List<SignUpListDto> signUpLists, Guid eventId, SignUpExportLabels? labels = null);

    /// <summary>
    /// Exports custom form responses to CSV format.
    /// One row per response, questions as columns (horizontal layout).
    /// Phase 6A.110: Form response export functionality
    /// </summary>
    /// <param name="form">Form detail with questions (sorted by SortOrder)</param>
    /// <param name="responses">All responses with answers</param>
    /// <returns>CSV file content as byte array</returns>
    byte[] ExportFormResponses(EventFormDetailDto form, FormResponsesPagedDto responses);

    /// <summary>
    /// Donation Feature: Exports donations to CSV format.
    /// </summary>
    byte[] ExportDonations(EventDonationsResponse donations);

    /// <summary>
    /// Collection Feature: Exports event fund collections to CSV format.
    /// </summary>
    byte[] ExportCollections(EventCollectionsResponse collections);

    /// <summary>
    /// Sponsor Feature: Exports sponsorships to CSV format.
    /// </summary>
    byte[] ExportSponsors(EventSponsorsResponse sponsors);

    /// <summary>
    /// Add-On Feature: Exports add-on purchases to CSV format.
    /// </summary>
    byte[] ExportAddOnPurchases(EventAddOnPurchasesResponse purchases);

    /// <summary>
    /// Exports all financial data as a ZIP archive containing 5 CSV files.
    /// Files: attendees.csv, donations.csv, collections.csv, sponsors.csv, addon_purchases.csv.
    /// </summary>
    byte[] ExportAllFinancialsZip(AllFinancialsData data);
}

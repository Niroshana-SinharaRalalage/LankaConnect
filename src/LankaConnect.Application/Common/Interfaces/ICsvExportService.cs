using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Common.Interfaces;

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
    /// <returns>ZIP file content as byte array</returns>
    byte[] ExportSignUpListsToZip(List<SignUpListDto> signUpLists, Guid eventId);
}

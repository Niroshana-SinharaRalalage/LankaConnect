using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Common.Interfaces;

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
    /// <returns>ZIP archive content as byte array</returns>
    byte[] ExportSignUpListsToExcelZip(List<SignUpListDto> signUpLists, Guid eventId);
}

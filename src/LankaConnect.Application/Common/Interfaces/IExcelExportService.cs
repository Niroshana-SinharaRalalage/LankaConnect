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
    /// Phase 6A.73: Exports signup lists only to Excel file with category-based sheets.
    /// </summary>
    /// <param name="signUpLists">Signup lists to export</param>
    /// <returns>Excel file content as byte array</returns>
    byte[] ExportSignUpListsToExcel(List<SignUpListDto> signUpLists);
}

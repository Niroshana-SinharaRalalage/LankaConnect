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
}

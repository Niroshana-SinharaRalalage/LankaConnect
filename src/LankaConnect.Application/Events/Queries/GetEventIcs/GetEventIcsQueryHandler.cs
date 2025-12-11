using System.Text;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetEventIcs;

/// <summary>
/// Handler for GetEventIcsQuery
/// Generates ICS (iCalendar) format file for calendar integration
/// Compatible with Google Calendar, Apple Calendar, Outlook, and other iCalendar-compliant applications
/// </summary>
public class GetEventIcsQueryHandler : IQueryHandler<GetEventIcsQuery, string>
{
    private readonly IEventRepository _eventRepository;

    public GetEventIcsQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Result<string>> Handle(GetEventIcsQuery request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<string>.Failure("Event not found");

        // Build ICS content
        var icsContent = BuildIcsContent(@event);

        return Result<string>.Success(icsContent);
    }

    /// <summary>
    /// Builds ICS (iCalendar) format content from event data
    /// </summary>
    private string BuildIcsContent(Event @event)
    {
        var ics = new StringBuilder();

        // Calendar header
        ics.AppendLine("BEGIN:VCALENDAR");
        ics.AppendLine("VERSION:2.0");
        ics.AppendLine("PRODID:-//LankaConnect//Event//EN");
        ics.AppendLine("CALSCALE:GREGORIAN");
        ics.AppendLine("METHOD:PUBLISH");

        // Event details
        ics.AppendLine("BEGIN:VEVENT");
        ics.AppendLine($"UID:event-{@event.Id}@lankaconnect.com");
        ics.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        ics.AppendLine($"DTSTART:{@event.StartDate:yyyyMMddTHHmmssZ}");
        ics.AppendLine($"DTEND:{@event.EndDate:yyyyMMddTHHmmssZ}");
        ics.AppendLine($"SUMMARY:{EscapeIcsText(@event.Title.Value)}");
        ics.AppendLine($"DESCRIPTION:{EscapeIcsText(@event.Description.Value)}");

        // Add location if available
        if (@event.Location != null)
        {
            var locationText = BuildLocationText(@event.Location);
            ics.AppendLine($"LOCATION:{EscapeIcsText(locationText)}");

            // Add geographic coordinates if available
            if (@event.Location.Coordinates != null)
            {
                ics.AppendLine($"GEO:{@event.Location.Coordinates.Latitude};{@event.Location.Coordinates.Longitude}");
            }
        }

        // Add organizer
        ics.AppendLine($"ORGANIZER;CN=Event Organizer:MAILTO:events@lankaconnect.com");

        // Event status
        ics.AppendLine($"STATUS:{MapEventStatus(@event.Status)}");

        // Add URL to event page
        ics.AppendLine($"URL:https://lankaconnect.com/events/{@event.Id}");

        // Add categories
        ics.AppendLine($"CATEGORIES:{@event.Category}");

        // Reminder: 1 hour before event
        ics.AppendLine("BEGIN:VALARM");
        ics.AppendLine("TRIGGER:-PT1H");
        ics.AppendLine("ACTION:DISPLAY");
        ics.AppendLine("DESCRIPTION:Reminder: Event starts in 1 hour");
        ics.AppendLine("END:VALARM");

        ics.AppendLine("END:VEVENT");
        ics.AppendLine("END:VCALENDAR");

        return ics.ToString();
    }

    /// <summary>
    /// Builds location text from EventLocation value object
    /// </summary>
    private string BuildLocationText(Domain.Events.ValueObjects.EventLocation location)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(location.Address.Street))
            parts.Add(location.Address.Street);

        if (!string.IsNullOrWhiteSpace(location.Address.City))
            parts.Add(location.Address.City);

        if (!string.IsNullOrWhiteSpace(location.Address.State))
            parts.Add(location.Address.State);

        if (!string.IsNullOrWhiteSpace(location.Address.ZipCode))
            parts.Add(location.Address.ZipCode);

        if (!string.IsNullOrWhiteSpace(location.Address.Country))
            parts.Add(location.Address.Country);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Escapes special characters in ICS text fields
    /// </summary>
    private string EscapeIcsText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(";", "\\;")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    /// <summary>
    /// Maps EventStatus to ICS status values
    /// </summary>
    private string MapEventStatus(Domain.Events.Enums.EventStatus status)
    {
        return status switch
        {
            Domain.Events.Enums.EventStatus.Published => "CONFIRMED",
            Domain.Events.Enums.EventStatus.Active => "CONFIRMED",
            Domain.Events.Enums.EventStatus.Cancelled => "CANCELLED",
            Domain.Events.Enums.EventStatus.Postponed => "TENTATIVE",
            _ => "TENTATIVE"
        };
    }
}

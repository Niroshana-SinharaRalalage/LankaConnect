using System.Diagnostics;
using System.Text;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventIcs;

/// <summary>
/// Handler for GetEventIcsQuery
/// Generates ICS (iCalendar) format file for calendar integration
/// Compatible with Google Calendar, Apple Calendar, Outlook, and other iCalendar-compliant applications
/// </summary>
public class GetEventIcsQueryHandler : IQueryHandler<GetEventIcsQuery, string>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetEventIcsQueryHandler> _logger;

    public GetEventIcsQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetEventIcsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(GetEventIcsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventIcs"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventIcs START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventIcs FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<string>.Failure("Event ID is required");
                }

                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventIcs FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<string>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "GetEventIcs: Event loaded - EventId={EventId}, Title={Title}, Status={Status}, HasLocation={HasLocation}",
                    @event.Id, @event.Title.Value, @event.Status, @event.Location != null);

                // Phase 8YA.2: TBD events have no DTSTART/DTEND. The .ics format has no
                // "Date TBD" representation, so the only correct response is failure.
                // Controller surfaces this as 422 Unprocessable Entity (architect-locked).
                if (!@event.StartDate.HasValue || !@event.EndDate.HasValue)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventIcs FAILED: Event has TBD dates - EventId={EventId}, " +
                        "StartDateNull={StartNull}, EndDateNull={EndNull}, Duration={ElapsedMs}ms",
                        request.EventId, !@event.StartDate.HasValue, !@event.EndDate.HasValue,
                        stopwatch.ElapsedMilliseconds);

                    return Result<string>.Failure(
                        "Calendar export is not available for events with unconfirmed dates (Date TBD). " +
                        "The organiser must set start and end dates before this event can be added to a calendar.");
                }

                // Build ICS content
                var icsContent = BuildIcsContent(@event);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventIcs COMPLETE: EventId={EventId}, ContentLength={ContentLength}chars, Duration={ElapsedMs}ms",
                    request.EventId, icsContent.Length, stopwatch.ElapsedMilliseconds);

                return Result<string>.Success(icsContent);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventIcs FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Builds ICS (iCalendar) format content from event data.
    /// Caller (Handle) MUST ensure StartDate.HasValue AND EndDate.HasValue before
    /// invoking — Phase 8YA.2 returns Failure for TBD events upstream.
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
        // Caller-checked: dates are guaranteed non-null at this point.
        ics.AppendLine($"DTSTART:{@event.StartDate!.Value:yyyyMMddTHHmmssZ}");
        ics.AppendLine($"DTEND:{@event.EndDate!.Value:yyyyMMddTHHmmssZ}");
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
    private string BuildLocationText(LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventLocation location)
    {
        var parts = new List<string>();

        // Wave 9.h.10.6 F31a: EventLocation.Address is nullable in practice — the EF Core
        // private constructor sets `Address = null!` and any event created without the
        // flat LocationAddress/City/... fields ends up with a hydrated EventLocation whose
        // Address is null. Pre-fix this threw NRE at BuildLocationText → 500 for every
        // .ics export on such events. Fall back to Name / empty when Address is unavailable.
        if (location.Address == null)
        {
            if (!string.IsNullOrWhiteSpace(location.Name))
                parts.Add(location.Name);
            return string.Join(", ", parts);
        }

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
    private string MapEventStatus(LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus status)
    {
        return status switch
        {
            LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published => "CONFIRMED",
            LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Active => "CONFIRMED",
            LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Cancelled => "CANCELLED",
            LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Postponed => "TENTATIVE",
            _ => "TENTATIVE"
        };
    }
}

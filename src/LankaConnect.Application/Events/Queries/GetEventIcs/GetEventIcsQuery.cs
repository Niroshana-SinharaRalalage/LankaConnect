using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.GetEventIcs;

/// <summary>
/// Query to generate ICS (iCalendar) format for an event
/// Used for "Add to Calendar" functionality with Google Calendar, Apple Calendar, Outlook
/// </summary>
public record GetEventIcsQuery(Guid EventId) : IQuery<string>;

using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventIcs;

/// <summary>
/// Query to generate ICS (iCalendar) format for an event
/// Used for "Add to Calendar" functionality with Google Calendar, Apple Calendar, Outlook
/// </summary>
public record GetEventIcsQuery(Guid EventId) : IQuery<string>;

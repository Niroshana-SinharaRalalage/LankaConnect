using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventFormDetail;

/// <summary>
/// Gets a specific form with its questions for detail view.
/// </summary>
public record GetEventFormDetailQuery(Guid EventId, Guid FormId) : IQuery<EventFormDetailDto>;

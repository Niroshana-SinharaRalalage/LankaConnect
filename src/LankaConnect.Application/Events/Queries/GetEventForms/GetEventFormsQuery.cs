using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventForms;

/// <summary>
/// Gets all forms for a specific event (summary view without questions).
/// </summary>
public record GetEventFormsQuery(Guid EventId) : IQuery<List<EventFormDto>>;

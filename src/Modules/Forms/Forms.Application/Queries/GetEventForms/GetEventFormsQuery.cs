using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Modules.Forms.Application.Queries.GetEventForms;

/// <summary>
/// Gets all forms for a specific event (summary view without questions).
/// </summary>
public record GetEventFormsQuery(Guid EventId) : IQuery<List<EventFormDto>>;

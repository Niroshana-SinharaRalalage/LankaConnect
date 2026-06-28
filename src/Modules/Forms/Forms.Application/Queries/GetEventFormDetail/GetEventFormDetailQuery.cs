using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Modules.Forms.Application.Queries.GetEventFormDetail;

/// <summary>
/// Gets a specific form with its questions for detail view.
/// </summary>
public record GetEventFormDetailQuery(Guid EventId, Guid FormId) : IQuery<EventFormDetailDto>;

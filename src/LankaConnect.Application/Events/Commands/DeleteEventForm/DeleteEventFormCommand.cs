using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Application.Events.Commands.DeleteEventForm;

/// <summary>
/// Deletes an event form. Only allowed if the form has no responses.
/// </summary>
public record DeleteEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;

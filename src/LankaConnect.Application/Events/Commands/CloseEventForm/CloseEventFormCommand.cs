using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Application.Events.Commands.CloseEventForm;

/// <summary>
/// Closes an event form (Active -> Closed). No more responses accepted.
/// </summary>
public record CloseEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;

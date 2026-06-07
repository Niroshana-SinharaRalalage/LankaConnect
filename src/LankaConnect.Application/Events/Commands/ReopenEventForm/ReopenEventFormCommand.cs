using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Application.Events.Commands.ReopenEventForm;

/// <summary>
/// Reopens a closed event form (Closed -> Active).
/// </summary>
public record ReopenEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;

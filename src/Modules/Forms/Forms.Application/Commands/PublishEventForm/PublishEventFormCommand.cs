using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands.PublishEventForm;

/// <summary>
/// Publishes an event form (Draft -> Active).
/// Form must have at least one question to be published.
/// </summary>
public record PublishEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateAddOnDefinition;

/// <summary>
/// Updates an existing add-on definition for an event.
/// Organizer-facing command to modify name, description, price, stock limit, and active status.
/// </summary>
public record UpdateAddOnDefinitionCommand(
    Guid EventId,
    Guid DefinitionId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? QuantityLimit,
    int SortOrder,
    bool IsActive
) : ICommand;

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateAddOnDefinition;

/// <summary>
/// Creates a new add-on definition (purchasable item) for an event.
/// Organizer-facing command. Returns the new definition ID.
/// </summary>
public record CreateAddOnDefinitionCommand(
    Guid EventId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? QuantityLimit,
    int SortOrder
) : ICommand<Guid>;  // Returns definition ID

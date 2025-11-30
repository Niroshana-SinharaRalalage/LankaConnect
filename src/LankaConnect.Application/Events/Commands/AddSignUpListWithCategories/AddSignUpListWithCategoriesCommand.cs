using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AddSignUpListWithCategories;

public record AddSignUpListWithCategoriesCommand(
    Guid EventId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems
) : ICommand;

using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateCollectionConfig;

/// <summary>
/// Updates the collection (event fund) configuration for an event.
/// Organizer-facing command to enable/disable and configure collection settings.
/// </summary>
public record UpdateCollectionConfigCommand(
    Guid EventId,
    bool IsEnabled,
    decimal? GoalAmount,
    bool ShowProgress,
    List<decimal>? SuggestedAmounts,
    bool AllowCustomAmount,
    decimal? MinAmount,
    decimal? MaxAmount,
    string? CollectionMessage,
    bool ShowContributorCount
) : ICommand;

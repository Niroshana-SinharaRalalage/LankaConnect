using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ReorderSignUpItems;

/// <summary>
/// Phase 6A.132: Organizer-initiated drag-drop reorder of a sign-up list's items.
/// <paramref name="OrderedItemIds"/> is the full, post-reorder sequence (index 0 = first).
/// The aggregate rejects any submission that is not an exact set match of the current
/// items so the frontend must refetch on conflict rather than silently dropping items.
/// </summary>
public record ReorderSignUpItemsCommand(
    Guid EventId,
    Guid SignUpListId,
    IReadOnlyList<Guid> OrderedItemIds
) : ICommand;

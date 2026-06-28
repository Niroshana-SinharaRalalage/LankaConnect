using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteLayout;

/// <summary>
/// Slice 5 Chunk 9: DELETE /api/venue-layouts/{id}. Hard-deletes the layout
/// aggregate (zones, tables, decorations, seats cascade via FK).
///
/// <para>
/// Safety gates (in order):
/// </para>
/// <list type="number">
///   <item>Authorization (two-branch: event-attached vs template).</item>
///   <item>Optimistic concurrency — caller-supplied <c>If-Match</c> RowVersion must match the current <c>layout.RowVersion</c>.</item>
///   <item>Structural edit guard — no layout seat may be held or reserved (returns <c>StructuralEditRejected</c> → HTTP 422).</item>
///   <item>If the layout is attached to an event, <c>Event.DisableAssignedSeating()</c> must succeed — rejects when the event has live registrations.</item>
/// </list>
/// </summary>
public record DeleteLayoutCommand(
    Guid LayoutId,
    uint ExpectedRowVersion
) : ICommand;

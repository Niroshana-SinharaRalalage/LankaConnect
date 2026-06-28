using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Slice 5 Chunk 13: named-metric emission for the seating-layout surface.
///
/// Per architect decision, Slices 5-8 emit a fixed set of 6 named metrics. This interface
/// owns the two metrics that belong to the Slice 5 mutation surface:
///
/// <list type="bullet">
///   <item><c>layout.created</c> (tags: <c>LayoutType</c>, <c>FromPreset</c>)</item>
///   <item><c>layout.structural_edit_rejected</c> (tags: <c>LayoutId</c>, <c>Reason</c>)</item>
/// </list>
///
/// Slice 6 adds <c>layout.preset_selected</c>. Slice 7 adds
/// <c>seatpicker.selection_completed</c>. Slice 8 adds
/// <c>layout.canvas_editor_opened</c> and <c>layout.canvas_editor_saved</c>.
///
/// Emission channel: structured Serilog log events with a stable template. Azure Container Apps
/// log analytics queries pick them up by <c>MetricName</c> property.
/// </summary>
public interface ILayoutMetrics
{
    /// <summary>
    /// Fires when a venue layout has been successfully created and committed. For Slice 5 this is
    /// called from <c>CreateVenueLayoutCommandHandler</c> with <paramref name="fromPreset"/> always
    /// <c>false</c>; Slice 6's preset command will call with <c>true</c>.
    /// </summary>
    void LayoutCreated(LayoutType layoutType, bool fromPreset);

    /// <summary>
    /// Fires when a structural-mutation endpoint rejects an edit. The <paramref name="reason"/>
    /// tag distinguishes the rejection path so a dashboard can separate user-visible friction
    /// (<c>SeatsReserved</c>) from security (<c>AuthFailed</c>) and race conditions
    /// (<c>ConcurrencyConflict</c>).
    /// </summary>
    void StructuralEditRejected(Guid layoutId, StructuralEditRejectionReason reason);

    /// <summary>
    /// Slice 6: fires when an organizer picks a preset from the preset-library modal
    /// and the preset layout is successfully committed. <paramref name="presetId"/> is
    /// the stable preset identifier (e.g. <c>theater-classic</c>) — low cardinality,
    /// safe to use as a dashboard grouping tag.
    /// </summary>
    void PresetSelected(string presetId);

    /// <summary>
    /// Slice 7 S7.8: fires when a registrant has finished picking seats in the
    /// SeatPicker and confirms the selection. The client measures the elapsed
    /// wall-clock time from SeatPicker mount to confirm, and reports it back via
    /// a small metrics endpoint. Drives the seating-UX-friction dashboard.
    /// </summary>
    void SeatPickerSelectionCompleted(Guid eventId, int attendeeCount, long timeToCompleteMs);

    /// <summary>
    /// Slice 8 S8.1: fires when an organizer opens the canvas editor modal.
    /// Pairs with <c>LayoutCanvasEditorSaved</c> — dashboard ratio
    /// <c>opened/saved</c> measures editor abandonment per architect spec.
    /// </summary>
    void LayoutCanvasEditorOpened(Guid layoutId);

    /// <summary>
    /// Slice 8 S8.8: fires when an organizer saves a canvas-editor session.
    /// <paramref name="changesCount"/> is the number of mutations the client
    /// included in the atomic <c>PUT /batch</c> payload.
    /// </summary>
    void LayoutCanvasEditorSaved(Guid layoutId, int changesCount);

    /// <summary>
    /// Phase 7H: fires when a canvas-editor save fails before a successful
    /// commit. <paramref name="reason"/> is one of a fixed set of low-
    /// cardinality tags (<c>not_found</c>, <c>auth_failed</c>,
    /// <c>concurrency_conflict</c>, <c>structural_edit_rejected</c>,
    /// <c>validation_failed</c>, <c>unknown</c>) so the dashboard can split
    /// "user-visible friction" from "system errors". Architect §S6 dashboard
    /// alerts on save error rate > 5% in 5 min.
    /// </summary>
    void LayoutCanvasEditorSaveFailed(Guid layoutId, string reason);
}

/// <summary>
/// The three rejection reasons the Slice 5 mutation surface can emit for a structural edit.
/// Mapped to snake_case tag values by the emitter so log queries match the architect's spec.
/// </summary>
public enum StructuralEditRejectionReason
{
    /// <summary>Structural guard found held or reserved seats on the affected set.</summary>
    SeatsReserved,

    /// <summary>Authorization service refused the caller (wrong user / missing admin role).</summary>
    AuthFailed,

    /// <summary>Optimistic-concurrency check (<c>RowVersion</c> mismatch) failed at commit.</summary>
    ConcurrencyConflict,
}

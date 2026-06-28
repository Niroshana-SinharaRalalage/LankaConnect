using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Slice 5 Chunk 13: Serilog-backed emitter for <see cref="ILayoutMetrics"/>.
///
/// Uses a stable template so Azure Container Apps log-analytics queries can match on
/// <c>MetricName</c> directly. The reason-tag enum is projected to the snake_case strings
/// from the architect's spec (<c>seats_reserved</c>, <c>auth_failed</c>, <c>concurrency_conflict</c>).
/// </summary>
public class LayoutMetrics : ILayoutMetrics
{
    private readonly ILogger<LayoutMetrics> _logger;

    public LayoutMetrics(ILogger<LayoutMetrics> logger)
    {
        _logger = logger;
    }

    public void LayoutCreated(LayoutType layoutType, bool fromPreset)
    {
        _logger.LogInformation(
            "Metric {MetricName} LayoutType={LayoutType} FromPreset={FromPreset}",
            "layout.created",
            layoutType.ToString(),
            fromPreset);
    }

    public void StructuralEditRejected(Guid layoutId, StructuralEditRejectionReason reason)
    {
        _logger.LogInformation(
            "Metric {MetricName} LayoutId={LayoutId} Reason={Reason}",
            "layout.structural_edit_rejected",
            layoutId,
            ToTag(reason));
    }

    public void PresetSelected(string presetId)
    {
        _logger.LogInformation(
            "Metric {MetricName} PresetId={PresetId}",
            "layout.preset_selected",
            presetId);
    }

    public void SeatPickerSelectionCompleted(Guid eventId, int attendeeCount, long timeToCompleteMs)
    {
        _logger.LogInformation(
            "Metric {MetricName} EventId={EventId} AttendeeCount={AttendeeCount} TimeToCompleteMs={TimeToCompleteMs}",
            "seatpicker.selection_completed",
            eventId,
            attendeeCount,
            timeToCompleteMs);
    }

    public void LayoutCanvasEditorOpened(Guid layoutId)
    {
        _logger.LogInformation(
            "Metric {MetricName} LayoutId={LayoutId}",
            "layout.canvas_editor_opened",
            layoutId);
    }

    public void LayoutCanvasEditorSaved(Guid layoutId, int changesCount)
    {
        _logger.LogInformation(
            "Metric {MetricName} LayoutId={LayoutId} ChangesCount={ChangesCount}",
            "layout.canvas_editor_saved",
            layoutId,
            changesCount);
    }

    public void LayoutCanvasEditorSaveFailed(Guid layoutId, string reason)
    {
        // Reason is a low-cardinality string fixed by the calling code (one
        // of: not_found, auth_failed, concurrency_conflict,
        // structural_edit_rejected, validation_failed, unknown). Dashboard
        // uses this tag to split user-friction from system errors.
        _logger.LogInformation(
            "Metric {MetricName} LayoutId={LayoutId} Reason={Reason}",
            "canvas_editor.save_failed",
            layoutId,
            reason ?? "unknown");
    }

    private static string ToTag(StructuralEditRejectionReason reason) => reason switch
    {
        StructuralEditRejectionReason.SeatsReserved => "seats_reserved",
        StructuralEditRejectionReason.AuthFailed => "auth_failed",
        StructuralEditRejectionReason.ConcurrencyConflict => "concurrency_conflict",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown rejection reason"),
    };
}

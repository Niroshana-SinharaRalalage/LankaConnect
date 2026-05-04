using FluentAssertions;
using LankaConnect.Application.Events.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 7H — pins the structured-log contract for the Phase 7H additions
/// to <see cref="LayoutMetrics"/>. The pre-existing 6 metrics
/// (<c>layout.created</c>, <c>layout.preset_selected</c>,
/// <c>layout.canvas_editor_opened/saved</c>, <c>layout.structural_edit_rejected</c>,
/// <c>seatpicker.selection_completed</c>) are exercised end-to-end by the
/// command-handler test suites; this file covers only the new
/// <c>canvas_editor.save_failed</c> emission.
/// </summary>
public class LayoutMetricsTests
{
    private readonly Mock<ILogger<LayoutMetrics>> _logger = new();
    private readonly LayoutMetrics _sut;

    public LayoutMetricsTests()
    {
        _sut = new LayoutMetrics(_logger.Object);
    }

    [Fact]
    public void LayoutCanvasEditorSaveFailed_EmitsStructuredLog_WithExpectedMetricNameAndReasonTag()
    {
        var layoutId = Guid.NewGuid();

        _sut.LayoutCanvasEditorSaveFailed(layoutId, reason: "concurrency_conflict");

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("canvas_editor.save_failed")
                    && state.ToString()!.Contains(layoutId.ToString())
                    && state.ToString()!.Contains("concurrency_conflict")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

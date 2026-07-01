namespace LankaConnect.Products.LankaEvents.Contracts;

/// <summary>
/// Cross-boundary port for exporting Form responses. Consumed by
/// <c>Modules.Forms.Application</c>; implemented in <c>LankaConnect.Infrastructure</c>
/// alongside the broader CSV / Excel export services (which are internal to
/// Products.LankaEvents.Application and NOT exposed cross-boundary).
///
/// Wave 6.a.1 (2026-07-01): introduced to resolve Rule 9 boundary violation.
/// Previously Forms.Application referenced ICsvExportService + IExcelExportService
/// directly from Products.LankaEvents.Application.Common; those interfaces have
/// too many Product-internal signatures (EventAttendeesResponse, SignUpListDto,
/// AllFinancialsData, etc.) to publish as Contracts. This narrower port
/// publishes ONLY the form-response export surface Forms actually needs.
/// </summary>
public interface IFormResponseExporter
{
    /// <summary>
    /// Exports form responses to CSV. One row per response, questions as columns.
    /// </summary>
    byte[] ExportFormResponsesToCsv(EventFormDetailDto form, FormResponsesPagedDto responses);

    /// <summary>
    /// Exports form responses to Excel. Same layout as CSV.
    /// </summary>
    byte[] ExportFormResponsesToExcel(EventFormDetailDto form, FormResponsesPagedDto responses);
}

using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Products.LankaEvents.Application.Common.Export;

/// <summary>
/// Day 6 hotfix (2026-07-10, post-Consult #18 deploy attempt): facade that unifies
/// <see cref="IFormResponseExporter"/> against the two independent export
/// implementations. <see cref="CsvExportService"/> throws NotSupportedException
/// on the Excel branch and vice versa, so injecting either directly makes
/// `Forms.ExportFormResponsesQueryHandler` half-broken. This facade delegates
/// each branch to the correct concrete impl.
/// </summary>
public sealed class FormResponseExporterFacade : IFormResponseExporter
{
    private readonly ICsvExportService _csv;
    private readonly IExcelExportService _excel;

    public FormResponseExporterFacade(ICsvExportService csv, IExcelExportService excel)
    {
        _csv = csv;
        _excel = excel;
    }

    public byte[] ExportFormResponsesToCsv(EventFormDetailDto form, FormResponsesPagedDto responses)
        => ((IFormResponseExporter)_csv).ExportFormResponsesToCsv(form, responses);

    public byte[] ExportFormResponsesToExcel(EventFormDetailDto form, FormResponsesPagedDto responses)
        => ((IFormResponseExporter)_excel).ExportFormResponsesToExcel(form, responses);
}

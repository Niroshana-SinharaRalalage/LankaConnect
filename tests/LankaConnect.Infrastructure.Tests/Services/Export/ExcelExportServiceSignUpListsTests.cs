using ClosedXML.Excel;
using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Infrastructure.Services.Export;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Compression;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Phase 7D.1 Step 15: Excel export header-label parameterization for volunteer lists.
/// Verifies that <see cref="ExcelExportService.ExportSignUpListsToExcelZip"/> uses the
/// caller-supplied label set (and falls back to the Items defaults when none is given).
/// </summary>
public class ExcelExportServiceSignUpListsTests
{
    private readonly ExcelExportService _service;

    public ExcelExportServiceSignUpListsTests()
    {
        _service = new ExcelExportService(NullLogger<ExcelExportService>.Instance);
    }

    [Fact]
    public void ExportSignUpListsToExcelZip_Should_UseVolunteerHeaders_WhenVolunteerLabelsProvided()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = CreateSampleSignUpList("Food Committee", "Cook rice");
        var volunteerLabels = SignUpExportLabels.ForVolunteers();

        // Act
        var zipBytes = _service.ExportSignUpListsToExcelZip(signUpLists, eventId, volunteerLabels);

        // Assert — extract the xlsx, open the first worksheet, inspect row 1
        var headers = ReadFirstSheetHeaders(zipBytes);

        headers.Should().Contain("Volunteer Role");
        headers.Should().Contain("Volunteers Needed");
        headers.Should().Contain("Volunteers Remaining");
        headers.Should().Contain("Volunteer Name");
        headers.Should().Contain("Volunteer Email");
        headers.Should().Contain("Volunteer Phone");
        headers.Should().Contain("Committed");

        // Regression guard: Items labels must not appear in a volunteer export
        headers.Should().NotContain("Item Description");
        headers.Should().NotContain("Contact Name");
    }

    [Fact]
    public void ExportSignUpListsToExcelZip_Should_UseItemHeaders_WhenLabelsOmitted()
    {
        // Arrange — no labels argument → default to ForItems()
        var eventId = Guid.NewGuid();
        var signUpLists = CreateSampleSignUpList("Potluck", "Bring dessert");

        // Act
        var zipBytes = _service.ExportSignUpListsToExcelZip(signUpLists, eventId);

        // Assert
        var headers = ReadFirstSheetHeaders(zipBytes);

        headers.Should().Contain("Item Description");
        headers.Should().Contain("Requested Quantity");
        headers.Should().Contain("Remaining Quantity");
        headers.Should().Contain("Contact Name");
        headers.Should().Contain("Contact Email");
        headers.Should().Contain("Contact Phone");
        headers.Should().Contain("Quantity Committed");
    }

    private static List<SignUpListDto> CreateSampleSignUpList(string category, string itemDescription)
    {
        return new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = category,
                Items = new List<ISignUpItemDto>
                {
                    new QuantityBasedItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = itemDescription,
                        TargetQuantity = 5,
                        CommittedQuantity = 2,
                        RemainingQuantity = 3,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "Alice",
                                ContactEmail = "alice@example.com",
                                ContactPhone = "+1-555-0001",
                                PhysicalQuantity = 2,
                                CommittedAt = DateTime.UtcNow
                            }
                        }
                    }
                }
            }
        };
    }

    private static List<string> ReadFirstSheetHeaders(byte[] zipBytes)
    {
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var xlsxEntry = archive.Entries.First(e => e.Name.EndsWith(".xlsx"));
        using var xlsxStream = xlsxEntry.Open();
        using var memoryCopy = new MemoryStream();
        xlsxStream.CopyTo(memoryCopy);
        memoryCopy.Position = 0;

        using var workbook = new XLWorkbook(memoryCopy);
        var sheet = workbook.Worksheets.First();

        var headers = new List<string>();
        for (int col = 1; col <= 7; col++)
        {
            headers.Add(sheet.Cell(1, col).GetString());
        }
        return headers;
    }
}

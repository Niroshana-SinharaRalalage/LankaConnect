using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Infrastructure.Services.Export;
using System.IO.Compression;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Phase 7D.1 Step 15: CSV export header-label parameterization for volunteer lists.
/// Companion to <see cref="ExcelExportServiceSignUpListsTests"/>; the pre-existing
/// CsvExportServiceSignUpListsTests.cs is excluded from compilation (uses obsolete
/// SignUpItemDto API), so these tests live in a separate file using the current
/// ISignUpItemDto / QuantityBasedItemDto API.
/// </summary>
public class CsvExportServiceVolunteerLabelsTests
{
    private readonly CsvExportService _service = new();

    [Fact]
    public void ExportSignUpListsToZip_Should_UseVolunteerHeaders_WhenVolunteerLabelsProvided()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = CreateSampleSignUpList("Food Committee", "Cook rice");
        var volunteerLabels = SignUpExportLabels.ForVolunteers();

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId, volunteerLabels);

        // Assert
        var csvContent = ReadFirstCsvEntry(zipBytes);

        csvContent.Should().Contain("Volunteer Role");
        csvContent.Should().Contain("Volunteers Needed");
        csvContent.Should().Contain("Volunteers Remaining");
        csvContent.Should().Contain("Volunteer Name");
        csvContent.Should().Contain("Volunteer Email");
        csvContent.Should().Contain("Volunteer Phone");
        csvContent.Should().Contain("Committed");

        // Regression guard: Items labels must not leak into volunteer exports
        csvContent.Should().NotContain("Item Description");
        csvContent.Should().NotContain("Requested Quantity");
        csvContent.Should().NotContain("Contact Name");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_UseItemHeaders_WhenLabelsOmitted()
    {
        // Arrange — no labels argument → default to ForItems()
        var eventId = Guid.NewGuid();
        var signUpLists = CreateSampleSignUpList("Potluck", "Bring dessert");

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        var csvContent = ReadFirstCsvEntry(zipBytes);

        csvContent.Should().Contain("Item Description");
        csvContent.Should().Contain("Requested Quantity");
        csvContent.Should().Contain("Remaining Quantity");
        csvContent.Should().Contain("Contact Name");
        csvContent.Should().Contain("Contact Email");
        csvContent.Should().Contain("Contact Phone");
        csvContent.Should().Contain("Quantity Committed");
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

    private static string ReadFirstCsvEntry(byte[] zipBytes)
    {
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}

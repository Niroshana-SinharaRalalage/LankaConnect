using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Infrastructure.Services.Export;
using System.IO.Compression;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Unit tests for Phase 6A.69: Sign-Up List ZIP Export functionality.
/// Tests verify ZIP archive generation with multiple CSV files for signup lists.
/// </summary>
public class CsvExportServiceSignUpListsTests
{
    private readonly CsvExportService _service;

    public CsvExportServiceSignUpListsTests()
    {
        _service = new CsvExportService();
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_ThrowException_WhenSignUpListsIsNull()
    {
        // Arrange
        List<SignUpListDto>? nullLists = null;
        var eventId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.ExportSignUpListsToZip(nullLists!, eventId));

        exception.Message.Should().Contain("No signup lists to export");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_ThrowException_WhenSignUpListsIsEmpty()
    {
        // Arrange
        var emptyLists = new List<SignUpListDto>();
        var eventId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.ExportSignUpListsToZip(emptyLists, eventId));

        exception.Message.Should().Contain("No signup lists to export");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_CreateMultipleCsvFiles_WhenMultipleCategoriesExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = CreateTestSignUpLists();

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        zipBytes.Should().NotBeEmpty();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        // Should have 3 CSV files (Food & Drinks has & character, Decorations-Open)
        archive.Entries.Count.Should().Be(3);

        var fileNames = archive.Entries.Select(e => e.Name).ToList();
        fileNames.Should().Contain("Food-&-Drinks-Mandatory.csv");
        fileNames.Should().Contain("Food-&-Drinks-Suggested.csv");
        fileNames.Should().Contain("Decorations-Open.csv");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_IncludeUtf8Bom_InEachCsvFile()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = CreateTestSignUpLists();

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            var buffer = new byte[3];
            entryStream.Read(buffer, 0, 3);

            // Verify UTF-8 BOM (0xEF, 0xBB, 0xBF)
            buffer[0].Should().Be(0xEF, $"first byte of {entry.Name} should be UTF-8 BOM");
            buffer[1].Should().Be(0xBB, $"second byte of {entry.Name} should be UTF-8 BOM");
            buffer[2].Should().Be(0xBF, $"third byte of {entry.Name} should be UTF-8 BOM");
        }
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_IncludeCorrectHeaders_InCsvFiles()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = CreateTestSignUpLists();

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        var csvContent = reader.ReadToEnd();

        // Verify headers
        csvContent.Should().Contain("Sign-up List");
        csvContent.Should().Contain("Item Description");
        csvContent.Should().Contain("Requested Quantity");
        csvContent.Should().Contain("Contact Name");
        csvContent.Should().Contain("Contact Email");
        csvContent.Should().Contain("Contact Phone");
        csvContent.Should().Contain("Quantity Committed");
        csvContent.Should().Contain("Committed At");
        csvContent.Should().Contain("Remaining Quantity");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_ShowPlaceholders_ForItemsWithZeroCommitments()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Test List",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "No Commitment Item",
                        Quantity = 5,
                        RemainingQuantity = 5,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>() // Empty commitments
                    }
                }
            }
        };

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        var csvContent = reader.ReadToEnd();

        // Verify placeholder (em dash —) appears for missing contact info
        csvContent.Should().Contain("—");
        csvContent.Should().Contain("No Commitment Item");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_PrefixPhoneNumbers_WithApostrophe()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Test List",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Test Item",
                        Quantity = 2,
                        RemainingQuantity = 0,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "John Doe",
                                ContactEmail = "john@example.com",
                                ContactPhone = "+1-555-123-4567",
                                Quantity = 2,
                                CommittedAt = DateTime.UtcNow
                            }
                        }
                    }
                }
            }
        };

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        var csvContent = reader.ReadToEnd();

        // Phone should be prefixed with apostrophe to prevent Excel auto-formatting
        csvContent.Should().Contain("'+1-555-123-4567");
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_ExpandMultipleCommitments_ToSeparateRows()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Food",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Rice",
                        Quantity = 5,
                        RemainingQuantity = 0,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "User 1",
                                ContactEmail = "user1@example.com",
                                ContactPhone = "+1111111111",
                                Quantity = 2,
                                CommittedAt = DateTime.UtcNow
                            },
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "User 2",
                                ContactEmail = "user2@example.com",
                                ContactPhone = "+2222222222",
                                Quantity = 3,
                                CommittedAt = DateTime.UtcNow
                            }
                        }
                    }
                }
            }
        };

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        var csvContent = reader.ReadToEnd();

        // Both users should appear on separate rows
        csvContent.Should().Contain("User 1");
        csvContent.Should().Contain("User 2");

        // Count lines (header + 2 data rows = 3 lines)
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(3);
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_SanitizeFilenames_RemovingInvalidCharacters()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var signUpLists = new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Food & Drinks",  // Contains & character
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Test",
                        Quantity = 1,
                        RemainingQuantity = 1,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>()
                    }
                }
            }
        };

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();

        // Filename should have spaces replaced with hyphens
        entry.Name.Should().Contain("Food-&-Drinks-Mandatory.csv");

        // Should not contain invalid path characters
        var invalidChars = Path.GetInvalidFileNameChars();
        entry.Name.Should().NotContainAny(invalidChars.Select(c => c.ToString()));
    }

    [Fact]
    public void ExportSignUpListsToZip_Should_FormatTimestamps_AsIso8601()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var committedAt = new DateTime(2026, 1, 7, 14, 30, 0, DateTimeKind.Utc);
        var signUpLists = new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Test",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Test Item",
                        Quantity = 1,
                        RemainingQuantity = 0,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "Test User",
                                ContactEmail = "test@example.com",
                                Quantity = 1,
                                CommittedAt = committedAt
                            }
                        }
                    }
                }
            }
        };

        // Act
        var zipBytes = _service.ExportSignUpListsToZip(signUpLists, eventId);

        // Assert
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.First();
        using var reader = new StreamReader(entry.Open());
        var csvContent = reader.ReadToEnd();

        // Timestamp should be in yyyy-MM-dd HH:mm:ss format
        csvContent.Should().Contain("2026-01-07 14:30:00");
    }

    // Helper method to create test data
    private static List<SignUpListDto> CreateTestSignUpLists()
    {
        return new List<SignUpListDto>
        {
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Food & Drinks",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Rice (10kg)",
                        Quantity = 5,
                        RemainingQuantity = 2,
                        ItemCategory = SignUpItemCategory.Mandatory,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "John Smith",
                                ContactEmail = "john@example.com",
                                ContactPhone = "+1-555-123-4567",
                                Quantity = 3,
                                CommittedAt = DateTime.UtcNow
                            }
                        }
                    },
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Beverages",
                        Quantity = 10,
                        RemainingQuantity = 10,
                        ItemCategory = SignUpItemCategory.Suggested,
                        Commitments = new List<SignUpCommitmentDto>() // No commitments
                    }
                }
            },
            new SignUpListDto
            {
                Id = Guid.NewGuid(),
                Category = "Decorations",
                Items = new List<SignUpItemDto>
                {
                    new SignUpItemDto
                    {
                        Id = Guid.NewGuid(),
                        ItemDescription = "Balloons",
                        Quantity = 20,
                        RemainingQuantity = 10,
                        ItemCategory = SignUpItemCategory.Open,
                        Commitments = new List<SignUpCommitmentDto>
                        {
                            new SignUpCommitmentDto
                            {
                                Id = Guid.NewGuid(),
                                UserId = Guid.NewGuid(),
                                ContactName = "Jane Doe",
                                ContactEmail = "jane@example.com",
                                ContactPhone = "+1-555-987-6543",
                                Quantity = 10,
                                CommittedAt = DateTime.UtcNow
                            }
                        }
                    }
                }
            }
        };
    }
}

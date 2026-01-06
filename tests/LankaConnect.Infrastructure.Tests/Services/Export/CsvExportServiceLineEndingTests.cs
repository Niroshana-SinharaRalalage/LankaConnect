using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Infrastructure.Services.Export;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Tests to verify CSV export uses correct line endings (LF only) for Excel compatibility.
/// Phase 6A.67: Changed to LF only (\n) to match working signup list client-side export.
/// CRLF (\r\n) was causing Excel to display all data in single row when served over HTTP.
/// </summary>
public class CsvExportServiceLineEndingTests
{
    private readonly CsvExportService _service;

    public CsvExportServiceLineEndingTests()
    {
        _service = new CsvExportService();
    }

    [Fact]
    public void ExportEventAttendees_Should_UseUnixLineEndings_ForExcelCompatibility()
    {
        // Arrange
        var attendees = new EventAttendeesResponse
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Test Event",
            Attendees = new List<EventAttendeeDto>
            {
                new EventAttendeeDto
                {
                    RegistrationId = Guid.NewGuid(),
                    TotalAttendees = 2,
                    AdultCount = 1,
                    ChildCount = 1,
                    ContactEmail = "test1@example.com",
                    ContactPhone = "+1234567890",
                    ContactAddress = "123 Test St",
                    PaymentStatus = PaymentStatus.Completed,
                    TotalAmount = 100.00m,
                    Currency = "USD",
                    TicketCode = "TICK-001",
                    QrCodeData = "QR-001",
                    Status = RegistrationStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    Attendees = new List<AttendeeDetailsDto>
                    {
                        new AttendeeDetailsDto
                        {
                            Name = "John Doe",
                            Gender = Gender.Male,
                            AgeCategory = AgeCategory.Adult
                        },
                        new AttendeeDetailsDto
                        {
                            Name = "Jane Doe",
                            Gender = Gender.Female,
                            AgeCategory = AgeCategory.Child
                        }
                    }
                },
                new EventAttendeeDto
                {
                    RegistrationId = Guid.NewGuid(),
                    TotalAttendees = 1,
                    AdultCount = 1,
                    ChildCount = 0,
                    ContactEmail = "test2@example.com",
                    ContactPhone = "+9876543210",
                    ContactAddress = "456 Test Ave",
                    PaymentStatus = PaymentStatus.Pending,
                    TotalAmount = 50.00m,
                    Currency = "USD",
                    TicketCode = "TICK-002",
                    QrCodeData = "QR-002",
                    Status = RegistrationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    Attendees = new List<AttendeeDetailsDto>
                    {
                        new AttendeeDetailsDto
                        {
                            Name = "Bob Smith",
                            Gender = Gender.Male,
                            AgeCategory = AgeCategory.Adult
                        }
                    }
                }
            }
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);
        var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

        // Assert - Verify LF line endings (matching signup list export that works)
        csvString.Should().Contain("\n", "CSV should use Unix line endings (LF) to match working signup list export");
        csvString.Should().NotContain("\r\n", "should NOT use CRLF - LF only like signup list");
        csvString.Should().NotContain("\n\r", "line endings should be LF, not LFCR");

        // Verify line count - should have header + 2 data rows
        var lines = csvString.Split('\n', StringSplitOptions.None);
        lines.Length.Should().BeGreaterThan(2, "should have header row and at least 2 data rows");

        // Verify header is on first line
        lines[0].Should().Contain("RegistrationId", "header should be on first line");
        lines[0].Should().Contain("MainAttendee", "header should contain MainAttendee column");

        // Verify data rows exist
        lines[1].Should().Contain("John Doe", "first data row should contain first attendee");
        lines[2].Should().Contain("Bob Smith", "second data row should contain second attendee");
    }

    [Fact]
    public void ExportEventAttendees_Should_StartWithUtf8Bom()
    {
        // Arrange
        var attendees = new EventAttendeesResponse
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Test Event",
            Attendees = new List<EventAttendeeDto>
            {
                new EventAttendeeDto
                {
                    RegistrationId = Guid.NewGuid(),
                    TotalAttendees = 1,
                    AdultCount = 1,
                    ChildCount = 0,
                    ContactEmail = "test@example.com",
                    ContactPhone = "+1234567890",
                    PaymentStatus = PaymentStatus.Completed,
                    TotalAmount = 100.00m,
                    Currency = "USD",
                    Status = RegistrationStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    Attendees = new List<AttendeeDetailsDto>
                    {
                        new AttendeeDetailsDto
                        {
                            Name = "Test User",
                            Gender = Gender.Male,
                            AgeCategory = AgeCategory.Adult
                        }
                    }
                }
            }
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);

        // Assert - Verify UTF-8 BOM (0xEF, 0xBB, 0xBF)
        csvBytes.Length.Should().BeGreaterThan(3, "CSV should have content");
        csvBytes[0].Should().Be(0xEF, "first byte should be UTF-8 BOM first byte");
        csvBytes[1].Should().Be(0xBB, "second byte should be UTF-8 BOM second byte");
        csvBytes[2].Should().Be(0xBF, "third byte should be UTF-8 BOM third byte");
    }

    [Fact]
    public void ExportEventAttendees_Should_HaveCorrectByteSequenceForLineEndings()
    {
        // Arrange
        var attendees = new EventAttendeesResponse
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Test Event",
            Attendees = new List<EventAttendeeDto>
            {
                new EventAttendeeDto
                {
                    RegistrationId = Guid.NewGuid(),
                    TotalAttendees = 1,
                    AdultCount = 1,
                    ChildCount = 0,
                    ContactEmail = "test@example.com",
                    ContactPhone = "+1234567890",
                    PaymentStatus = PaymentStatus.Completed,
                    TotalAmount = 100.00m,
                    Currency = "USD",
                    Status = RegistrationStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    Attendees = new List<AttendeeDetailsDto>
                    {
                        new AttendeeDetailsDto
                        {
                            Name = "Test User",
                            Gender = Gender.Male,
                            AgeCategory = AgeCategory.Adult
                        }
                    }
                }
            }
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);

        // Assert - Find LF bytes (0x0A) in the byte array (matching signup list export)
        bool foundLf = false;
        for (int i = 0; i < csvBytes.Length; i++)
        {
            if (csvBytes[i] == 0x0A)
            {
                foundLf = true;
                break;
            }
        }

        foundLf.Should().BeTrue("CSV should contain LF byte (0x0A) for line endings");

        // Verify we don't have CRLF (0x0D 0x0A) - should be LF only
        for (int i = 0; i < csvBytes.Length - 1; i++)
        {
            if (csvBytes[i] == 0x0D && csvBytes[i + 1] == 0x0A)
            {
                Assert.Fail($"Found CRLF at position {i} - should be LF only to match signup list export");
            }
        }
    }

    [Fact]
    public void ExportEventAttendees_WithMultipleRows_Should_SeparateEachRowWithLf()
    {
        // Arrange - Create 5 attendees to ensure multiple rows
        var attendees = new EventAttendeesResponse
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Test Event",
            Attendees = Enumerable.Range(1, 5).Select(i => new EventAttendeeDto
            {
                RegistrationId = Guid.NewGuid(),
                TotalAttendees = 1,
                AdultCount = 1,
                ChildCount = 0,
                ContactEmail = $"test{i}@example.com",
                ContactPhone = $"+123456789{i}",
                PaymentStatus = PaymentStatus.Completed,
                TotalAmount = 100.00m * i,
                Currency = "USD",
                Status = RegistrationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                Attendees = new List<AttendeeDetailsDto>
                {
                    new AttendeeDetailsDto
                    {
                        Name = $"User {i}",
                        Gender = Gender.Male,
                        AgeCategory = AgeCategory.Adult
                    }
                }
            }).ToList()
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);
        var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

        // Assert - Split by LF only (matching signup list export)
        var lines = csvString.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Should have 1 header + 5 data rows = 6 lines
        lines.Length.Should().Be(6, "should have header row and 5 data rows");

        // Verify each expected user is in the output
        for (int i = 1; i <= 5; i++)
        {
            csvString.Should().Contain($"User {i}", $"should contain attendee {i}");
        }
    }
}

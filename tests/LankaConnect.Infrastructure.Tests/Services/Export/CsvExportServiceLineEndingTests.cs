using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Infrastructure.Services.Export;

namespace LankaConnect.Infrastructure.Tests.Services.Export;

/// <summary>
/// Tests to verify CSV export uses correct line endings (CRLF) for Excel compatibility.
/// Phase 6A.XX: Investigation showed CsvHelper was using Unix line endings (\n) instead of Windows (\r\n)
/// </summary>
public class CsvExportServiceLineEndingTests
{
    private readonly CsvExportService _service;

    public CsvExportServiceLineEndingTests()
    {
        _service = new CsvExportService();
    }

    [Fact]
    public void ExportEventAttendees_Should_UseWindowsLineEndings_ForExcelCompatibility()
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
                    PaymentStatus = PaymentStatus.Paid,
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
                            Age = 30,
                            IsChild = false
                        },
                        new AttendeeDetailsDto
                        {
                            Name = "Jane Doe",
                            Gender = Gender.Female,
                            Age = 8,
                            IsChild = true
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
                            Age = 25,
                            IsChild = false
                        }
                    }
                }
            }
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);
        var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

        // Assert - Verify CRLF line endings
        csvString.Should().Contain("\r\n", "CSV should use Windows line endings (CRLF) for Excel compatibility");
        csvString.Should().NotContain("\n\r", "line endings should be CRLF, not LFCR");

        // Verify line count - should have header + 2 data rows + final CRLF
        var lines = csvString.Split(new[] { "\r\n" }, StringSplitOptions.None);
        lines.Length.Should().BeGreaterThan(2, "should have header row and at least 2 data rows");

        // Verify header is on first line
        lines[0].Should().Contain("RegistrationId", "header should be on first line");
        lines[0].Should().Contain("MainAttendee", "header should contain MainAttendee column");

        // Verify data rows exist
        lines[1].Should().Contain("John Doe", "first data row should contain first attendee");
        lines[2].Should().Contain("Bob Smith", "second data row should contain second attendee");

        // Additional verification: Check that we don't have Unix line endings
        var unixLineCount = csvString.Split('\n').Length - 1;
        var windowsLineCount = csvString.Split(new[] { "\r\n" }, StringSplitOptions.None).Length - 1;

        windowsLineCount.Should().Be(unixLineCount,
            "every LF (\\n) should be part of a CRLF (\\r\\n), not standalone Unix line endings");
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
                    PaymentStatus = PaymentStatus.Paid,
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
                            Age = 30,
                            IsChild = false
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
                    PaymentStatus = PaymentStatus.Paid,
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
                            Age = 30,
                            IsChild = false
                        }
                    }
                }
            }
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);

        // Assert - Find CRLF bytes (0x0D 0x0A) in the byte array
        bool foundCrlf = false;
        for (int i = 0; i < csvBytes.Length - 1; i++)
        {
            if (csvBytes[i] == 0x0D && csvBytes[i + 1] == 0x0A)
            {
                foundCrlf = true;
                break;
            }
        }

        foundCrlf.Should().BeTrue("CSV should contain CRLF byte sequence (0x0D 0x0A)");

        // Verify we don't have standalone LF (0x0A) without preceding CR (0x0D)
        for (int i = 0; i < csvBytes.Length; i++)
        {
            if (csvBytes[i] == 0x0A) // LF
            {
                if (i == 0)
                {
                    Assert.Fail("Found LF at start of file without preceding CR");
                }
                else if (csvBytes[i - 1] != 0x0D) // Check if previous byte is CR
                {
                    Assert.Fail($"Found standalone LF at position {i} without preceding CR");
                }
            }
        }
    }

    [Fact]
    public void ExportEventAttendees_WithMultipleRows_Should_SeparateEachRowWithCrlf()
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
                PaymentStatus = PaymentStatus.Paid,
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
                        Age = 20 + i,
                        IsChild = false
                    }
                }
            }).ToList()
        };

        // Act
        var csvBytes = _service.ExportEventAttendees(attendees);
        var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

        // Assert
        var lines = csvString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Should have 1 header + 5 data rows = 6 lines
        lines.Length.Should().Be(6, "should have header row and 5 data rows");

        // Verify each expected user is in the output
        for (int i = 1; i <= 5; i++)
        {
            csvString.Should().Contain($"User {i}", $"should contain attendee {i}");
        }
    }
}

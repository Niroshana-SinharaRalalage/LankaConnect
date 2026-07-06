using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
namespace LankaConnect.Modules.Communications.Contracts.Tests.Email.Contracts;

/// <summary>
/// Wave 9.h.10.5 F23 tests for SupportTicketEmailParams.
///
/// Regression tests for the anonymous-contact-form scenario: contact-form
/// tickets can be submitted without authentication (ContactController is
/// [AllowAnonymous]) and CreateSupportTicketCommandHandler intentionally
/// passes userId=Guid.Empty in that path. The old Validate() hard-rejected
/// Guid.Empty which killed every template-support-ticket-confirmation
/// dispatch for anonymous submissions (Pass 1 probe evidence: 4 VALIDATION-FAIL
/// with errors="UserId is required" over an 8-minute window).
/// </summary>
public class SupportTicketEmailParamsTests
{
    [Fact]
    public void Validate_WithEmptyUserId_ShouldAcceptAnonymousSubmission()
    {
        // Arrange -- construct a valid confirmation params EXCEPT UserId=Empty
        var emailParams = SupportTicketEmailParams.CreateConfirmation(
            userId: Guid.Empty, // Wave 9.h.10.5 F23: anonymous contact form
            userName: "Anonymous Submitter",
            userEmail: "anon@example.com",
            ticketId: Guid.NewGuid(),
            ticketNumber: "T-000001",
            subject: "Contact form question",
            category: "General",
            priority: "Normal",
            message: "Anonymous message body",
            createdAt: DateTime.UtcNow,
            ticketUrl: "https://example.com/tickets/T-000001");

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue("Guid.Empty UserId must be accepted for anonymous contact-form submissions");
        errors.Should().NotContain(e => e.Contains("UserId"), "UserId is no longer required after Wave 9.h.10.5 F23");
    }

    [Fact]
    public void Validate_WithNonEmptyUserId_ShouldAcceptAuthenticatedSubmission()
    {
        // Arrange -- authenticated user submits a ticket
        var emailParams = SupportTicketEmailParams.CreateConfirmation(
            userId: Guid.NewGuid(),
            userName: "Authenticated User",
            userEmail: "user@example.com",
            ticketId: Guid.NewGuid(),
            ticketNumber: "T-000002",
            subject: "Authenticated question",
            category: "General",
            priority: "Normal",
            message: "Authenticated message body",
            createdAt: DateTime.UtcNow,
            ticketUrl: "https://example.com/tickets/T-000002");

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingUserName_ShouldStillFail()
    {
        // Arrange -- confirm other required fields still rejected properly
        var emailParams = SupportTicketEmailParams.CreateConfirmation(
            userId: Guid.NewGuid(),
            userName: "", // required, must fail
            userEmail: "user@example.com",
            ticketId: Guid.NewGuid(),
            ticketNumber: "T-000003",
            subject: "Q3 test",
            category: "General",
            priority: "Normal",
            message: "body",
            createdAt: DateTime.UtcNow,
            ticketUrl: "https://example.com/tickets/T-000003");

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse("UserName is still required after F23 (only UserId was relaxed)");
        errors.Should().Contain(e => e.Contains("UserName is required"));
    }

    [Fact]
    public void ToDictionary_DoesNotEmitUserIdKey_ConfirmsTemplateBodyUnaffected()
    {
        // Arrange -- verify F23 is safe: template rendering doesn't need UserId
        // key. The old Validate() hard-rejected Guid.Empty even though the
        // rendered template doesn't reference {{UserId}} anywhere. Relaxing the
        // validator therefore cannot cause an unreplaced-placeholder rendering
        // failure. This test locks that invariant in place.
        var emailParams = SupportTicketEmailParams.CreateConfirmation(
            userId: Guid.Empty,
            userName: "Test",
            userEmail: "test@example.com",
            ticketId: Guid.NewGuid(),
            ticketNumber: "T-000004",
            subject: "template body check",
            category: "General",
            priority: "Normal",
            message: "body",
            createdAt: DateTime.UtcNow,
            ticketUrl: "https://example.com/tickets/T-000004");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().NotContainKey("UserId",
            "ToDictionary must not emit UserId -- template body does not reference {{UserId}}, so relaxing the validator is a safe change");
    }
}

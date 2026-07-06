using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
namespace LankaConnect.Modules.Communications.Contracts.Tests.Email.Contracts;

/// <summary>
/// Phase 7D.1 Phase C: Tests for volunteer-specific template switching on
/// <see cref="SignupCommitmentEmailParams"/>.
/// </summary>
public class SignupCommitmentEmailParamsVolunteerTests
{
    [Fact]
    public void AsVolunteerConfirmation_Sets_VolunteerCommitmentConfirmation_Template()
    {
        var p = new SignupCommitmentEmailParams().AsConfirmation();

        p.AsVolunteerConfirmation();

        p.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.VolunteerCommitmentConfirmation);
        p.TemplateName.Should().Be("template-volunteer-commitment-confirmation");
    }

    [Fact]
    public void AsVolunteerCancellation_Sets_VolunteerCommitmentCancellation_Template()
    {
        var p = new SignupCommitmentEmailParams().AsCancellation();

        p.AsVolunteerCancellation();

        p.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.VolunteerCommitmentCancellation);
        p.TemplateName.Should().Be("template-volunteer-commitment-cancellation");
    }

    [Fact]
    public void AsConfirmation_DefaultRoute_StillReturns_SignupListTemplate()
    {
        // Regression guard: non-volunteer callers remain on the original template.
        var p = SignupCommitmentEmailParams.CreateConfirmation(
            userId: Guid.NewGuid(),
            userName: "Alice",
            userEmail: "alice@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Picnic",
            signupItem: "Drinks",
            quantity: 1,
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "UTC",
            eventLocation: "Park",
            eventDetailsUrl: "https://example.com/e");

        p.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.SignupCommitmentConfirmation);
    }
}

using FluentAssertions;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications;

/// <summary>
/// TDD tests for WhatsAppTemplate entity.
/// Tests creation, approval workflow, rejection, resubmission, and computed properties.
/// </summary>
public class WhatsAppTemplateTests
{
    private readonly List<string> _sampleParams = new() { "UserName", "EventName", "EventDate" };

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Template_With_Pending_Status()
    {
        var template = WhatsAppTemplate.Create(
            templateName: "event_confirmation",
            displayName: "Event Confirmation",
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "Hello {{1}}, you are registered for {{2}} on {{3}}.",
            parameterNames: _sampleParams,
            language: "en",
            headerText: "Registration Confirmed",
            footerText: "LankaConnect");

        template.Should().NotBeNull();
        template.TemplateName.Should().Be("event_confirmation");
        template.DisplayName.Should().Be("Event Confirmation");
        template.Category.Should().Be(WhatsAppTemplateCategory.Utility);
        template.BodyText.Should().Contain("Hello {{1}}");
        template.Language.Should().Be("en");
        template.Status.Should().Be(WhatsAppTemplateStatus.Pending);
        template.HeaderText.Should().Be("Registration Confirmed");
        template.FooterText.Should().Be("LankaConnect");
        template.ParameterCount.Should().Be(3);
        template.MetaTemplateId.Should().BeNull();
        template.ApprovedAt.Should().BeNull();
        template.RejectedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Should_Serialize_ParameterNames_To_JSON()
    {
        var template = WhatsAppTemplate.Create(
            "test", "Test", WhatsAppTemplateCategory.Utility,
            "Body", _sampleParams);

        template.ParameterNames.Should().NotBeNullOrEmpty();
        template.ParameterNames.Should().Contain("UserName");
        template.ParameterNames.Should().Contain("EventName");
        template.ParameterNames.Should().Contain("EventDate");
        // Verify it's valid JSON (array format)
        template.ParameterNames.Should().StartWith("[");
        template.ParameterNames.Should().EndWith("]");
    }

    [Fact]
    public void Create_With_Empty_ParameterNames_Should_Set_Count_To_Zero()
    {
        var template = WhatsAppTemplate.Create(
            "no_params", "No Params", WhatsAppTemplateCategory.Marketing,
            "Static body text", new List<string>());

        template.ParameterCount.Should().Be(0);
    }

    [Fact]
    public void Create_With_Default_Language_Should_Be_English()
    {
        var template = WhatsAppTemplate.Create(
            "test", "Test", WhatsAppTemplateCategory.Utility,
            "Body", _sampleParams);

        template.Language.Should().Be("en");
    }

    #endregion

    #region MarkApproved Tests

    [Fact]
    public void MarkApproved_WithValidMetaId_Should_Set_Status_To_Approved()
    {
        var template = CreatePendingTemplate();
        var metaId = "meta-template-abc123";

        template.MarkApproved(metaId);

        template.Status.Should().Be(WhatsAppTemplateStatus.Approved);
        template.MetaTemplateId.Should().Be(metaId);
        template.ApprovedAt.Should().NotBeNull();
        template.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        template.RejectedAt.Should().BeNull();
        template.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void MarkApproved_WithEmptyMetaId_Should_Throw_ArgumentException()
    {
        var template = CreatePendingTemplate();

        var act = () => template.MarkApproved("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Meta template ID*");
    }

    [Fact]
    public void MarkApproved_WithWhitespaceMetaId_Should_Throw_ArgumentException()
    {
        var template = CreatePendingTemplate();

        var act = () => template.MarkApproved("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Meta template ID*");
    }

    [Fact]
    public void MarkApproved_After_Rejection_Should_Clear_Rejection_Data()
    {
        var template = CreatePendingTemplate();
        template.MarkRejected("Policy violation");

        template.MarkApproved("meta-template-xyz");

        template.Status.Should().Be(WhatsAppTemplateStatus.Approved);
        template.RejectedAt.Should().BeNull();
        template.RejectionReason.Should().BeNull();
    }

    #endregion

    #region MarkRejected Tests

    [Fact]
    public void MarkRejected_Should_Set_Status_To_Rejected_With_Reason()
    {
        var template = CreatePendingTemplate();
        var reason = "Template body contains prohibited content";

        template.MarkRejected(reason);

        template.Status.Should().Be(WhatsAppTemplateStatus.Rejected);
        template.RejectionReason.Should().Be(reason);
        template.RejectedAt.Should().NotBeNull();
        template.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        template.ApprovedAt.Should().BeNull();
        template.MetaTemplateId.Should().BeNull();
    }

    #endregion

    #region Resubmit Tests

    [Fact]
    public void Resubmit_Should_Reset_To_Pending_Status()
    {
        var template = CreatePendingTemplate();
        template.MarkRejected("Policy violation");

        template.Resubmit();

        template.Status.Should().Be(WhatsAppTemplateStatus.Pending);
        template.RejectedAt.Should().BeNull();
        template.RejectionReason.Should().BeNull();
        template.ApprovedAt.Should().BeNull();
        template.MetaTemplateId.Should().BeNull();
    }

    #endregion

    #region GetParameterNames Tests

    [Fact]
    public void GetParameterNames_Should_Deserialize_JSON_Correctly()
    {
        var template = WhatsAppTemplate.Create(
            "test", "Test", WhatsAppTemplateCategory.Utility,
            "Body", _sampleParams);

        var names = template.GetParameterNames();

        names.Should().HaveCount(3);
        names.Should().ContainInOrder("UserName", "EventName", "EventDate");
    }

    [Fact]
    public void GetParameterNames_With_Empty_List_Should_Return_Empty()
    {
        var template = WhatsAppTemplate.Create(
            "test", "Test", WhatsAppTemplateCategory.Utility,
            "Body", new List<string>());

        var names = template.GetParameterNames();

        names.Should().BeEmpty();
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void IsApproved_Should_Return_True_Only_When_Status_Is_Approved()
    {
        var template = CreatePendingTemplate();

        template.IsApproved.Should().BeFalse();

        template.MarkApproved("meta-123");
        template.IsApproved.Should().BeTrue();

        template.MarkRejected("reason");
        template.IsApproved.Should().BeFalse();
    }

    [Fact]
    public void IsUsable_Should_Return_True_Only_When_Approved_And_Has_MetaTemplateId()
    {
        var template = CreatePendingTemplate();

        // Pending - not usable
        template.IsUsable.Should().BeFalse();

        // Approved with MetaTemplateId - usable
        template.MarkApproved("meta-template-abc");
        template.IsUsable.Should().BeTrue();

        // Rejected - not usable
        template.MarkRejected("reason");
        template.IsUsable.Should().BeFalse();
    }

    [Fact]
    public void IsUsable_When_Pending_Should_Return_False()
    {
        var template = CreatePendingTemplate();
        template.IsUsable.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private WhatsAppTemplate CreatePendingTemplate()
    {
        return WhatsAppTemplate.Create(
            templateName: "test_template",
            displayName: "Test Template",
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "Hello {{1}}, your event {{2}} is confirmed.",
            parameterNames: _sampleParams);
    }

    #endregion
}

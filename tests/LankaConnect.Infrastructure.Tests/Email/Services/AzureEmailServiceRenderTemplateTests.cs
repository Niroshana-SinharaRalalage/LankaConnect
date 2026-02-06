using System.Reflection;
using FluentAssertions;
using LankaConnect.Infrastructure.Email.Services;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Email.Services;

/// <summary>
/// Phase 6A.75: Unit tests for the RenderTemplateContent method in AzureEmailService.
/// These tests specifically cover the nested {{#if}} conditional processing logic.
/// </summary>
public class AzureEmailServiceRenderTemplateTests
{
    private readonly MethodInfo _renderTemplateContentMethod;

    public AzureEmailServiceRenderTemplateTests()
    {
        // Get the private static RenderTemplateContent method via reflection
        _renderTemplateContentMethod = typeof(AzureEmailService)
            .GetMethod("RenderTemplateContent", BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private string RenderTemplateContent(string template, Dictionary<string, object> parameters)
    {
        return (string)_renderTemplateContentMethod.Invoke(null, new object[] { template, parameters })!;
    }

    #region Basic Placeholder Tests

    [Fact]
    public void RenderTemplateContent_WithSimplePlaceholder_ShouldReplace()
    {
        // Arrange
        var template = "Hello {{Name}}!";
        var parameters = new Dictionary<string, object> { ["Name"] = "John" };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Hello John!");
    }

    [Fact]
    public void RenderTemplateContent_WithMultiplePlaceholders_ShouldReplaceAll()
    {
        // Arrange
        var template = "{{Greeting}} {{Name}}, welcome to {{Place}}!";
        var parameters = new Dictionary<string, object>
        {
            ["Greeting"] = "Hello",
            ["Name"] = "John",
            ["Place"] = "LankaConnect"
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Hello John, welcome to LankaConnect!");
    }

    [Fact]
    public void RenderTemplateContent_WithTripleBracePlaceholder_ShouldReplace()
    {
        // Arrange
        var template = "Content: {{{HtmlContent}}}";
        var parameters = new Dictionary<string, object> { ["HtmlContent"] = "<b>Bold</b>" };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Content: <b>Bold</b>");
    }

    #endregion

    #region Simple Conditional Tests

    [Fact]
    public void RenderTemplateContent_WithTruthyIfCondition_ShouldIncludeContent()
    {
        // Arrange
        var template = "Before {{#if ShowSection}}INCLUDED{{/if}} After";
        var parameters = new Dictionary<string, object> { ["ShowSection"] = true };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Before INCLUDED After");
    }

    [Fact]
    public void RenderTemplateContent_WithFalsyIfCondition_ShouldExcludeContent()
    {
        // Arrange
        var template = "Before {{#if ShowSection}}EXCLUDED{{/if}} After";
        var parameters = new Dictionary<string, object> { ["ShowSection"] = false };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Before  After");
    }

    [Fact]
    public void RenderTemplateContent_WithStringCondition_EmptyString_ShouldBeFalsy()
    {
        // Arrange
        var template = "{{#if Location}}Location: {{Location}}{{/if}}";
        var parameters = new Dictionary<string, object> { ["Location"] = "" };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void RenderTemplateContent_WithStringCondition_NonEmptyString_ShouldBeTruthy()
    {
        // Arrange
        var template = "{{#if Location}}Location: {{Location}}{{/if}}";
        var parameters = new Dictionary<string, object> { ["Location"] = "New York" };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Location: New York");
    }

    [Fact]
    public void RenderTemplateContent_WithMissingConditionParameter_ShouldBeFalsy()
    {
        // Arrange
        var template = "{{#if MissingParam}}Should not appear{{/if}}OK";
        var parameters = new Dictionary<string, object>();

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("OK");
    }

    #endregion

    #region Nested Conditional Tests (Phase 6A.75)

    [Fact]
    public void RenderTemplateContent_WithNestedConditions_BothTruthy_ShouldIncludeBoth()
    {
        // Arrange
        var template = "{{#if Outer}}OUTER-START {{#if Inner}}INNER{{/if}} OUTER-END{{/if}}";
        var parameters = new Dictionary<string, object>
        {
            ["Outer"] = true,
            ["Inner"] = true
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("OUTER-START INNER OUTER-END");
    }

    [Fact]
    public void RenderTemplateContent_WithNestedConditions_OuterTruthyInnerFalsy_ShouldIncludeOuterOnly()
    {
        // Arrange
        var template = "{{#if Outer}}OUTER-START {{#if Inner}}INNER{{/if}} OUTER-END{{/if}}";
        var parameters = new Dictionary<string, object>
        {
            ["Outer"] = true,
            ["Inner"] = false
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("OUTER-START  OUTER-END");
    }

    [Fact]
    public void RenderTemplateContent_WithNestedConditions_OuterFalsy_ShouldExcludeEntireBlock()
    {
        // Arrange
        var template = "{{#if Outer}}OUTER-START {{#if Inner}}INNER{{/if}} OUTER-END{{/if}}";
        var parameters = new Dictionary<string, object>
        {
            ["Outer"] = false,
            ["Inner"] = true
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void RenderTemplateContent_WithMultipleNestedConditions_ShouldProcessAllCorrectly()
    {
        // Arrange - This mirrors the actual newsletter template structure
        var template = @"{{#if EventId}}
EVENT SECTION
{{#if EventLocation}}
Location: {{EventLocation}}
{{/if}}
{{#if HasSignUpLists}}
Sign-up Lists Available
{{/if}}
END EVENT
{{/if}}";

        var parameters = new Dictionary<string, object>
        {
            ["EventId"] = "event-123",
            ["EventLocation"] = "New York",
            ["HasSignUpLists"] = true
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Contain("EVENT SECTION");
        result.Should().Contain("Location: New York");
        result.Should().Contain("Sign-up Lists Available");
        result.Should().Contain("END EVENT");
    }

    [Fact]
    public void RenderTemplateContent_WithMultipleNestedConditions_SomeFalsy_ShouldExcludeCorrectSections()
    {
        // Arrange
        var template = @"{{#if EventId}}
EVENT SECTION
{{#if EventLocation}}
Location: {{EventLocation}}
{{/if}}
{{#if HasSignUpLists}}
Sign-up Lists Available
{{/if}}
END EVENT
{{/if}}";

        var parameters = new Dictionary<string, object>
        {
            ["EventId"] = "event-123",
            ["EventLocation"] = "", // Empty string - falsy
            ["HasSignUpLists"] = false
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Contain("EVENT SECTION");
        result.Should().NotContain("Location:");
        result.Should().NotContain("Sign-up Lists Available");
        result.Should().Contain("END EVENT");
    }

    [Fact]
    public void RenderTemplateContent_WithDeeplyNestedConditions_ShouldProcessCorrectly()
    {
        // Arrange - 3 levels of nesting
        var template = "{{#if Level1}}L1-START {{#if Level2}}L2-START {{#if Level3}}L3{{/if}} L2-END{{/if}} L1-END{{/if}}";
        var parameters = new Dictionary<string, object>
        {
            ["Level1"] = true,
            ["Level2"] = true,
            ["Level3"] = true
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("L1-START L2-START L3 L2-END L1-END");
    }

    [Fact]
    public void RenderTemplateContent_WithSiblingConditions_ShouldProcessIndependently()
    {
        // Arrange - Multiple conditionals at the same level (not nested)
        var template = "{{#if A}}A-CONTENT{{/if}} | {{#if B}}B-CONTENT{{/if}} | {{#if C}}C-CONTENT{{/if}}";
        var parameters = new Dictionary<string, object>
        {
            ["A"] = true,
            ["B"] = false,
            ["C"] = true
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("A-CONTENT |  | C-CONTENT");
    }

    #endregion

    #region Real-World Newsletter Template Tests

    [Fact]
    public void RenderTemplateContent_WithActualNewsletterTemplate_EventLinked_ShouldShowEventSection()
    {
        // Arrange - Simplified version of the actual newsletter HTML template
        var template = @"<div class=""newsletter"">
  <h2>{{NewsletterTitle}}</h2>
  <div>{{{NewsletterContent}}}</div>
  {{#if EventId}}
  <div class=""event-section"">
    <h3>Related Event</h3>
    <p>{{EventTitle}}</p>
    {{#if EventLocation}}
    <p>Location: {{EventLocation}}</p>
    {{/if}}
    <p>Date: {{EventDate}}</p>
    <a href=""{{EventDetailsUrl}}"">View Event Details</a>
    {{#if HasSignUpLists}}
    <a href=""{{SignUpListsUrl}}"">View Sign-up Lists</a>
    {{/if}}
  </div>
  {{/if}}
  <a href=""{{DashboardUrl}}"">Visit LankaConnect</a>
</div>";

        var parameters = new Dictionary<string, object>
        {
            ["NewsletterTitle"] = "Test Newsletter",
            ["NewsletterContent"] = "<p>This is <strong>HTML</strong> content.</p>",
            ["EventId"] = "550e8400-e29b-41d4-a716-446655440000",
            ["EventTitle"] = "Sri Lanka Community Meetup",
            ["EventLocation"] = "Queens, NY",
            ["EventDate"] = "January 25, 2026 at 2:00 PM",
            ["EventDetailsUrl"] = "https://lankaconnect.com/events/123",
            ["HasSignUpLists"] = true,
            ["SignUpListsUrl"] = "https://lankaconnect.com/events/123/signup-lists",
            ["DashboardUrl"] = "https://lankaconnect.com/dashboard"
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Contain("<h2>Test Newsletter</h2>");
        result.Should().Contain("<p>This is <strong>HTML</strong> content.</p>");
        result.Should().Contain("Related Event");
        result.Should().Contain("Sri Lanka Community Meetup");
        result.Should().Contain("Location: Queens, NY");
        result.Should().Contain("January 25, 2026 at 2:00 PM");
        result.Should().Contain("View Event Details");
        result.Should().Contain("View Sign-up Lists");
    }

    [Fact]
    public void RenderTemplateContent_WithActualNewsletterTemplate_NoEvent_ShouldHideEventSection()
    {
        // Arrange
        var template = @"<div class=""newsletter"">
  <h2>{{NewsletterTitle}}</h2>
  <div>{{{NewsletterContent}}}</div>
  {{#if EventId}}
  <div class=""event-section"">
    <h3>Related Event</h3>
    <p>{{EventTitle}}</p>
    {{#if EventLocation}}
    <p>Location: {{EventLocation}}</p>
    {{/if}}
  </div>
  {{/if}}
  <a href=""{{DashboardUrl}}"">Visit LankaConnect</a>
</div>";

        var parameters = new Dictionary<string, object>
        {
            ["NewsletterTitle"] = "General Announcement",
            ["NewsletterContent"] = "<p>No event linked.</p>",
            ["EventId"] = "", // Empty - no event linked
            ["DashboardUrl"] = "https://lankaconnect.com/dashboard"
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Contain("<h2>General Announcement</h2>");
        result.Should().Contain("<p>No event linked.</p>");
        result.Should().NotContain("Related Event");
        result.Should().NotContain("event-section");
    }

    [Fact]
    public void RenderTemplateContent_WithActualNewsletterTemplate_EventNoLocation_ShouldHideLocationOnly()
    {
        // Arrange
        var template = @"{{#if EventId}}
<div class=""event"">
  <h3>{{EventTitle}}</h3>
  {{#if EventLocation}}
  <p class=""location"">Location: {{EventLocation}}</p>
  {{/if}}
  <p class=""date"">Date: {{EventDate}}</p>
</div>
{{/if}}";

        var parameters = new Dictionary<string, object>
        {
            ["EventId"] = "event-123",
            ["EventTitle"] = "Online Webinar",
            ["EventLocation"] = "", // No location (online event)
            ["EventDate"] = "February 1, 2026"
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Contain("Online Webinar");
        result.Should().Contain("Date: February 1, 2026");
        result.Should().NotContain("Location:");
        result.Should().NotContain("location");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RenderTemplateContent_WithEmptyTemplate_ShouldReturnEmpty()
    {
        // Arrange
        var template = "";
        var parameters = new Dictionary<string, object> { ["Name"] = "John" };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void RenderTemplateContent_WithNullValue_ShouldReplaceWithEmpty()
    {
        // Arrange
        var template = "Hello {{Name}}!";
        var parameters = new Dictionary<string, object> { ["Name"] = null! };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Hello !");
    }

    [Fact]
    public void RenderTemplateContent_WithUnmatchedOpenTag_ShouldNotCrash()
    {
        // Arrange
        var template = "{{#if Unmatched}}Content without closing tag";
        var parameters = new Dictionary<string, object> { ["Unmatched"] = true };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert - Should not throw and should leave the template mostly intact
        result.Should().NotBeNull();
    }

    [Fact]
    public void RenderTemplateContent_WithUnmatchedCloseTag_ShouldNotCrash()
    {
        // Arrange
        var template = "Content with extra {{/if}}";
        var parameters = new Dictionary<string, object>();

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert - Should leave unmatched close tag as-is
        result.Should().Contain("{{/if}}");
    }

    [Fact]
    public void RenderTemplateContent_WithSpecialCharactersInValues_ShouldHandleCorrectly()
    {
        // Arrange
        var template = "{{Message}}";
        var parameters = new Dictionary<string, object>
        {
            ["Message"] = "Hello \"World\" with <special> & 'characters'"
        };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Hello \"World\" with <special> & 'characters'");
    }

    [Fact]
    public void RenderTemplateContent_WithLegacySyntax_ShouldStillWork()
    {
        // Arrange - Legacy {{#variable}}...{{/variable}} syntax
        var template = "{{#ShowSection}}Legacy Section{{/ShowSection}}";
        var parameters = new Dictionary<string, object> { ["ShowSection"] = true };

        // Act
        var result = RenderTemplateContent(template, parameters);

        // Assert
        result.Should().Be("Legacy Section");
    }

    #endregion
}

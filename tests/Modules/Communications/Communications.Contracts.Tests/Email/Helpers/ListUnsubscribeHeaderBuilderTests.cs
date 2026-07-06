using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
namespace LankaConnect.Modules.Communications.Contracts.Tests.Email.Helpers;

public class ListUnsubscribeHeaderBuilderTests
{
    [Fact]
    public void BuildHeaders_WithValidUrl_ReturnsBothHeaders()
    {
        // Arrange
        var url = "https://api.lankaconnect.app/api/newsletter/unsubscribe?token=abc123def456";

        // Act
        var headers = ListUnsubscribeHeaderBuilder.BuildHeaders(url);

        // Assert
        headers.Should().HaveCount(2);
        headers.Should().ContainKey("List-Unsubscribe");
        headers.Should().ContainKey("List-Unsubscribe-Post");
    }

    [Fact]
    public void BuildHeaders_ListUnsubscribe_WrapsUrlInAngleBrackets()
    {
        // Arrange
        var url = "https://api.lankaconnect.app/api/newsletter/unsubscribe?token=abc123";

        // Act
        var headers = ListUnsubscribeHeaderBuilder.BuildHeaders(url);

        // Assert
        headers["List-Unsubscribe"].Should().Be($"<{url}>");
    }

    [Fact]
    public void BuildHeaders_ListUnsubscribePost_IsOneClickValue()
    {
        // Arrange
        var url = "https://api.lankaconnect.app/api/newsletter/unsubscribe?token=abc123";

        // Act
        var headers = ListUnsubscribeHeaderBuilder.BuildHeaders(url);

        // Assert
        headers["List-Unsubscribe-Post"].Should().Be("List-Unsubscribe=One-Click");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildHeaders_WithNullOrEmptyUrl_ReturnsEmptyDictionary(string? url)
    {
        // Act
        var headers = ListUnsubscribeHeaderBuilder.BuildHeaders(url);

        // Assert
        headers.Should().BeEmpty();
    }

    [Fact]
    public void BuildHeaders_UsesConstantHeaderNames()
    {
        // Arrange
        var url = "https://example.com/unsubscribe?token=test";

        // Act
        var headers = ListUnsubscribeHeaderBuilder.BuildHeaders(url);

        // Assert - verify constant names match expected RFC header names
        ListUnsubscribeHeaderBuilder.ListUnsubscribeHeader.Should().Be("List-Unsubscribe");
        ListUnsubscribeHeaderBuilder.ListUnsubscribePostHeader.Should().Be("List-Unsubscribe-Post");
        ListUnsubscribeHeaderBuilder.OneClickValue.Should().Be("List-Unsubscribe=One-Click");
    }
}

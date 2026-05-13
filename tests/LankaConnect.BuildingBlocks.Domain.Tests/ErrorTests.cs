using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void None_HasEmptyCodeAndMessage()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
        Error.None.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Constructed_IsNotNone()
    {
        var error = new Error("Test.Code", "Test message");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message");
        error.IsNone.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsBracketedCodeAndMessage()
    {
        var error = new Error("Module.Subject.Reason", "Something failed");

        error.ToString().Should().Be("[Module.Subject.Reason] Something failed");
    }

    [Fact]
    public void ToString_OnNone_ReturnsNonePlaceholder()
    {
        Error.None.ToString().Should().Be("(none)");
    }

    [Fact]
    public void Records_AreValueEqualByCodeAndMessage()
    {
        var a = new Error("Same", "Same");
        var b = new Error("Same", "Same");
        var c = new Error("Different", "Same");

        a.Should().Be(b);
        a.Should().NotBe(c);
        (a == b).Should().BeTrue();
        (a == c).Should().BeFalse();
    }

    [Theory]
    [InlineData(nameof(Error.NullValue), "Error.NullValue")]
    [InlineData(nameof(Error.NotFound), "Error.NotFound")]
    [InlineData(nameof(Error.Validation), "Error.Validation")]
    [InlineData(nameof(Error.Conflict), "Error.Conflict")]
    [InlineData(nameof(Error.Forbidden), "Error.Forbidden")]
    public void WellKnownErrors_HaveExpectedCodes(string field, string expectedCode)
    {
        var actualCode = field switch
        {
            nameof(Error.NullValue) => Error.NullValue.Code,
            nameof(Error.NotFound) => Error.NotFound.Code,
            nameof(Error.Validation) => Error.Validation.Code,
            nameof(Error.Conflict) => Error.Conflict.Code,
            nameof(Error.Forbidden) => Error.Forbidden.Code,
            _ => string.Empty,
        };

        actualCode.Should().Be(expectedCode);
    }
}

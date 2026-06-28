using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

public class CanvasConfigTests
{
    [Fact]
    public void Default_Should_Produce_1200x800_At_Scale_1_White()
    {
        var canvas = CanvasConfig.Default();

        canvas.Width.Should().Be(1200);
        canvas.Height.Should().Be(800);
        canvas.Scale.Should().Be(1.0);
        canvas.BackgroundColor.Should().Be("#ffffff");
    }

    [Fact]
    public void Create_WithValidArgs_Should_Succeed()
    {
        var result = CanvasConfig.Create(2000, 1500, 1.25, "#101020");

        result.IsSuccess.Should().BeTrue();
        var canvas = result.Value;
        canvas.Width.Should().Be(2000);
        canvas.Height.Should().Be(1500);
        canvas.Scale.Should().Be(1.25);
        canvas.BackgroundColor.Should().Be("#101020");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(10_001)]
    public void Create_WithOutOfRangeWidth_Should_Fail(int width)
    {
        var result = CanvasConfig.Create(width, 800, 1.0, "#ffffff");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("width");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(10_001)]
    public void Create_WithOutOfRangeHeight_Should_Fail(int height)
    {
        var result = CanvasConfig.Create(1200, height, 1.0, "#ffffff");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("height");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.05)]
    [InlineData(10.01)]
    public void Create_WithOutOfRangeScale_Should_Fail(double scale)
    {
        var result = CanvasConfig.Create(1200, 800, scale, "#ffffff");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("scale");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("red")]
    [InlineData("#xyz")]
    [InlineData("#12345")]
    [InlineData("ffffff")]
    public void Create_WithInvalidColor_Should_Fail(string color)
    {
        var result = CanvasConfig.Create(1200, 800, 1.0, color);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("#fff")]
    [InlineData("#FFF")]
    [InlineData("#ffffff")]
    [InlineData("#FFFFFFAA")]
    public void Create_WithValidShorthandColors_Should_Succeed(string color)
    {
        var result = CanvasConfig.Create(1200, 800, 1.0, color);

        result.IsSuccess.Should().BeTrue();
        result.Value.BackgroundColor.Should().Be(color);
    }

    [Fact]
    public void Equality_Should_Match_On_All_Fields()
    {
        var a = CanvasConfig.Create(1200, 800, 1.0, "#ffffff").Value;
        var b = CanvasConfig.Create(1200, 800, 1.0, "#ffffff").Value;
        var c = CanvasConfig.Create(1200, 800, 1.0, "#000000").Value;

        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}

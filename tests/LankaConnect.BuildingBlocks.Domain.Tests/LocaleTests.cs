using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Locale;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class LocaleTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("si-LK")]
    [InlineData("ta-LK")]
    [InlineData("en-GB")]
    public void StaticInstances_HaveExpectedTags(string tag)
    {
        var locale = tag switch
        {
            "en-US" => Locale.EnUs,
            "si-LK" => Locale.SiLk,
            "ta-LK" => Locale.TaLk,
            "en-GB" => Locale.EnGb,
            _ => throw new InvalidOperationException($"unexpected tag {tag}"),
        };

        locale.Tag.Should().Be(tag);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-CA")]
    [InlineData("ja-JP")]
    public void FromTag_KnownCulture_ReturnsLocale(string tag)
    {
        var locale = Locale.FromTag(tag);
        locale.Tag.Should().Be(tag);
    }

    [Fact]
    public void FromTag_UnknownCulture_Throws()
    {
        Action act = () => Locale.FromTag("xx-XX");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a known BCP 47 / .NET culture identifier*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromTag_NullOrEmpty_Throws(string? tag)
    {
        Action act = () => Locale.FromTag(tag!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFromTag_KnownCulture_ReturnsSome()
    {
        var maybe = Locale.TryFromTag("en-US");
        maybe.HasValue.Should().BeTrue();
        maybe.Value.Tag.Should().Be("en-US");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx-XX")]
    public void TryFromTag_Unknown_ReturnsNone(string? tag)
    {
        Locale.TryFromTag(tag).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ToCultureInfo_ReturnsMatchingCulture()
    {
        var locale = Locale.EnUs;
        var culture = locale.ToCultureInfo();

        culture.Name.Should().Be("en-US");
    }

    [Fact]
    public void Equality_SameTag_AreEqual()
    {
        var a = Locale.FromTag("en-US");
        var b = Locale.FromTag("en-US");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentTags_NotEqual()
    {
        Locale.EnUs.Should().NotBe(Locale.SiLk);
    }

    [Fact]
    public void ToString_ReturnsTag()
    {
        Locale.EnUs.ToString().Should().Be("en-US");
    }
}

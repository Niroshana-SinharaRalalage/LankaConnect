using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive unit tests for SponsorConfiguration value object.
/// Covers creation, sponsor type validation, min amount rules, and ValidateMoneyAmount().
/// </summary>
public class SponsorConfigurationTests
{
    #region Create() - Enabled with Valid Data

    [Fact]
    public void Create_EnabledWithMoneyOnly_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: 25.00m,
            sponsorMessage: "Sponsor our event!",
            showSponsorList: true);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeTrue();
        config.AcceptMoneySponsors.Should().BeTrue();
        config.AcceptItemSponsors.Should().BeFalse();
        config.MinSponsorAmount.Should().Be(25.00m);
        config.SponsorMessage.Should().Be("Sponsor our event!");
        config.ShowSponsorList.Should().BeTrue();
    }

    [Fact]
    public void Create_EnabledWithItemOnly_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: false,
            acceptItemSponsors: true,
            minSponsorAmount: null,
            sponsorMessage: null,
            showSponsorList: false);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.AcceptMoneySponsors.Should().BeFalse();
        config.AcceptItemSponsors.Should().BeTrue();
    }

    [Fact]
    public void Create_EnabledWithBothTypes_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: true,
            minSponsorAmount: 10.00m,
            sponsorMessage: "All sponsors welcome!",
            showSponsorList: true);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.AcceptMoneySponsors.Should().BeTrue();
        config.AcceptItemSponsors.Should().BeTrue();
    }

    #endregion

    #region Create() - Disabled

    [Fact]
    public void Create_Disabled_ShouldReturnDisabledConfig()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: false,
            acceptMoneySponsors: true,
            acceptItemSponsors: true,
            minSponsorAmount: 50.00m,
            sponsorMessage: "ignored",
            showSponsorList: true);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeFalse();
        config.AcceptMoneySponsors.Should().BeFalse();
        config.AcceptItemSponsors.Should().BeFalse();
    }

    [Fact]
    public void Disabled_ShouldReturnConfigWithAllDefaults()
    {
        var config = SponsorConfiguration.Disabled();

        config.IsEnabled.Should().BeFalse();
        config.AcceptMoneySponsors.Should().BeFalse();
        config.AcceptItemSponsors.Should().BeFalse();
        config.MinSponsorAmount.Should().BeNull();
        config.SponsorMessage.Should().BeNull();
        config.ShowSponsorList.Should().BeFalse();
    }

    #endregion

    #region Neither Money Nor Item Accepted

    [Fact]
    public void Create_NeitherMoneyNorItem_ShouldFail()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: false,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one sponsor type");
    }

    #endregion

    #region MinSponsorAmount Validation

    [Fact]
    public void Create_WithMinSponsorAmountWhenMoneyNotAccepted_ShouldFail()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: false,
            acceptItemSponsors: true,
            minSponsorAmount: 10.00m,
            sponsorMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("money sponsors are accepted");
    }

    [Fact]
    public void Create_WithMinSponsorAmountBelowMinimum_ShouldFail()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: 0.50m, // Below MINIMUM_SPONSOR_AMOUNT (1.00)
            sponsorMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void Create_WithMinSponsorAmountAtExactMinimum_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: 1.00m,
            sponsorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.MinSponsorAmount.Should().Be(1.00m);
    }

    [Fact]
    public void Create_WithNullMinSponsorAmountAndMoneyAccepted_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.MinSponsorAmount.Should().BeNull();
    }

    #endregion

    #region Message Validation

    [Fact]
    public void Create_WithMessageExceeding500Chars_ShouldFail()
    {
        var longMessage = new string('x', 501);

        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: longMessage);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    [Fact]
    public void Create_WithMessageExactly500Chars_ShouldSucceed()
    {
        var message = new string('x', 500);

        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimMessage()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: "  Sponsor us!  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.SponsorMessage.Should().Be("Sponsor us!");
    }

    [Fact]
    public void Create_WithNullMessage_ShouldSucceed()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true,
            acceptMoneySponsors: true,
            acceptItemSponsors: false,
            minSponsorAmount: null,
            sponsorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.SponsorMessage.Should().BeNull();
    }

    #endregion

    #region ValidateMoneyAmount()

    [Fact]
    public void ValidateMoneyAmount_ValidAmount_ShouldSucceed()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: 10.00m, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(50.00m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMoneyAmount_BelowMinSponsorAmount_ShouldFail()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: 25.00m, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(10.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void ValidateMoneyAmount_BelowSystemMinimum_ShouldFail()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(0.50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Fact]
    public void ValidateMoneyAmount_WhenDisabled_ShouldFail()
    {
        var config = SponsorConfiguration.Disabled();

        var result = config.ValidateMoneyAmount(100.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public void ValidateMoneyAmount_WhenMoneyNotAccepted_ShouldFail()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: false, acceptItemSponsors: true,
            minSponsorAmount: null, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(50.00m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not accepted");
    }

    [Fact]
    public void ValidateMoneyAmount_AtExactMinSponsorAmount_ShouldSucceed()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: 25.00m, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(25.00m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMoneyAmount_AtExactSystemMinimum_ShouldSucceed()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        var result = config.ValidateMoneyAmount(1.00m);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Phase 6A.143 — Sponsor banner image fields (WithImage / WithoutImage)

    [Fact]
    public void Create_WithoutImageParams_LeavesImageFieldsNull()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        config.SponsorImageUrl.Should().BeNull();
        config.SponsorImageBlobName.Should().BeNull();
    }

    [Fact]
    public void Create_WithImageUrlAndBlob_PopulatesBoth()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://blob.example.com/sponsors/banner.png",
            sponsorImageBlobName: "banner_abc.png").Value;

        config.SponsorImageUrl.Should().Be("https://blob.example.com/sponsors/banner.png");
        config.SponsorImageBlobName.Should().Be("banner_abc.png");
    }

    [Fact]
    public void Create_WithUrlButNoBlob_Fails()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://blob.example.com/x.png",
            sponsorImageBlobName: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("together");
    }

    [Fact]
    public void Create_WithBlobButNoUrl_Fails()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: null,
            sponsorImageBlobName: "x.png");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void WithImage_ReturnsNewVoWithImageSet_PreservesOtherFields()
    {
        var original = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: 25m, sponsorMessage: "Sponsor message",
            showSponsorList: true).Value;

        var result = original.WithImage(
            "https://blob.example.com/sponsors/new.png",
            "new_banner.png");

        result.IsSuccess.Should().BeTrue();
        var updated = result.Value;
        updated.SponsorImageUrl.Should().Be("https://blob.example.com/sponsors/new.png");
        updated.SponsorImageBlobName.Should().Be("new_banner.png");
        updated.IsEnabled.Should().BeTrue();
        updated.AcceptMoneySponsors.Should().BeTrue();
        updated.MinSponsorAmount.Should().Be(25m);
        updated.SponsorMessage.Should().Be("Sponsor message");
        updated.ShowSponsorList.Should().BeTrue();
        original.SponsorImageUrl.Should().BeNull("VO immutability — original unchanged");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithImage_RejectsEmptyUrl(string? badUrl)
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        var result = config.WithImage(badUrl!, "blob.png");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("URL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithImage_RejectsEmptyBlobName(string? badBlob)
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        var result = config.WithImage("https://blob.example.com/x.png", badBlob!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("blob");
    }

    [Fact]
    public void WithoutImage_ReturnsNewVoWithImageCleared_PreservesOtherFields()
    {
        var withImage = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: 25m, sponsorMessage: "Sponsor msg",
            showSponsorList: true,
            sponsorImageUrl: "https://blob.example.com/banner.png",
            sponsorImageBlobName: "banner.png").Value;

        var cleared = withImage.WithoutImage();

        cleared.SponsorImageUrl.Should().BeNull();
        cleared.SponsorImageBlobName.Should().BeNull();
        cleared.MinSponsorAmount.Should().Be(25m);
        cleared.SponsorMessage.Should().Be("Sponsor msg");
        cleared.ShowSponsorList.Should().BeTrue();
        withImage.SponsorImageUrl.Should().NotBeNull("VO immutability — original unchanged");
    }

    [Fact]
    public void Equality_IncludesImageFields_ChangesWhenImageDiffers()
    {
        // Architect F13: image fields must participate in equality or EF change-tracking
        // silently misses banner updates.
        var configA = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://a.example.com/x.png",
            sponsorImageBlobName: "a.png").Value;
        var configB = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://b.example.com/y.png",
            sponsorImageBlobName: "b.png").Value;

        configA.Equals(configB).Should().BeFalse(
            "different image URLs must produce non-equal VOs so EF detects the change");
    }

    [Fact]
    public void Equality_SameImageFields_AreEqual()
    {
        var configA = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://x.example.com/x.png",
            sponsorImageBlobName: "x.png").Value;
        var configB = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            sponsorImageUrl: "https://x.example.com/x.png",
            sponsorImageBlobName: "x.png").Value;

        configA.Equals(configB).Should().BeTrue();
    }

    #endregion
}

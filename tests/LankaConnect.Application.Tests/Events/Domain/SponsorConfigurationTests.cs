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

    #region Phase 6A.145 — MinAmountForSponsorImage threshold

    [Fact]
    public void Create_WithoutThreshold_LeavesFieldNull_FeatureDisabled()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        config.MinAmountForSponsorImage.Should().BeNull(
            "default null = sponsor-image feature OFF for this event");
    }

    [Fact]
    public void Create_WithThreshold_PopulatesField()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 100m).Value;

        config.MinAmountForSponsorImage.Should().Be(100m);
    }

    [Fact]
    public void Create_WithThresholdBelowSystemMinimum_Fails()
    {
        var result = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 0.50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least");
    }

    [Theory]
    [InlineData(100.0, 100.0, true)]   // exactly at threshold qualifies
    [InlineData(100.0, 150.0, true)]   // above threshold qualifies
    [InlineData(100.0, 99.99, false)]  // just below threshold denied
    [InlineData(100.0, null, false)]   // null amount denied
    public void QualifiesForImage_AppliesThreshold(double threshold, double? contribution, bool expected)
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: (decimal)threshold).Value;

        var contributionDecimal = contribution.HasValue ? (decimal?)((decimal)contribution.Value) : null;
        config.QualifiesForImage(contributionDecimal).Should().Be(expected);
    }

    [Fact]
    public void QualifiesForImage_WhenThresholdNull_AlwaysFalse()
    {
        var config = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null).Value;

        config.QualifiesForImage(100_000m).Should().BeFalse(
            "feature is OFF until organizer sets a threshold");
    }

    [Fact]
    public void Equality_IncludesThreshold_ChangesWhenThresholdDiffers()
    {
        var configA = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 100m).Value;
        var configB = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 250m).Value;

        configA.Equals(configB).Should().BeFalse(
            "EF change-tracking must detect threshold updates");
    }

    [Fact]
    public void Equality_SameThreshold_AreEqual()
    {
        var configA = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 100m).Value;
        var configB = SponsorConfiguration.Create(
            isEnabled: true, acceptMoneySponsors: true, acceptItemSponsors: false,
            minSponsorAmount: null, sponsorMessage: null,
            showSponsorList: false,
            minAmountForSponsorImage: 100m).Value;

        configA.Equals(configB).Should().BeTrue();
    }

    #endregion
}

using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 6A.156 — TDD unit tests for the SponsorshipPackage aggregate.
///
/// SponsorshipPackage is the catalogue half of the new packaged-sponsorship
/// feature (mirrors AddOnDefinition's catalogue role for the add-on system).
/// Buyers don't appear here — they appear on the Sponsor aggregate with
/// SponsorshipPackageId set. The two-aggregate split is what makes packages
/// possible at all: the original flat Sponsor model could not express
/// tiers/perks/stock-caps/included-tickets without it.
///
/// Covers: Create / UpdateDetails / Deactivate / Activate / HasAvailableStock /
/// RemainingStock / SetImage / ClearImage, plus the package-specific axes
/// (Tier, Perks list bounds, IncludedTicketCount range, zero-price recognition
/// packages).
/// </summary>
public class SponsorshipPackageTests
{
    #region Test Helpers

    private static readonly Guid ValidEventId = Guid.NewGuid();
    private const string ValidName = "Gold Sponsor";
    private const string ValidDescription = "Top-tier sponsorship with stage mention";
    private const string ValidTier = "Gold";
    private const int ValidSortOrder = 1;
    private const int ValidIncludedTicketCount = 5;

    private static Money ValidPrice() => Money.Create(500m, Currency.USD).Value;

    private static IReadOnlyList<string> ValidPerks() => new List<string>
    {
        "Logo on event banner",
        "5 complimentary tickets",
        "Mention during opening remarks"
    };

    private static SponsorshipPackage CreateValidPackage(
        Guid? eventId = null,
        string name = ValidName,
        string? description = ValidDescription,
        Money? price = null,
        int? quantityLimit = 5,
        int sortOrder = ValidSortOrder,
        string? tier = ValidTier,
        IReadOnlyList<string>? perks = null,
        int includedTicketCount = ValidIncludedTicketCount)
    {
        var result = SponsorshipPackage.Create(
            eventId ?? ValidEventId,
            name,
            description,
            price ?? ValidPrice(),
            quantityLimit,
            sortOrder,
            tier,
            perks ?? ValidPerks(),
            includedTicketCount);

        result.IsSuccess.Should().BeTrue($"helper expected success but got: {result.Error}");
        return result.Value;
    }

    #endregion

    #region Create – Success Cases

    [Fact]
    public void Create_WithAllValidFields_ShouldSucceed()
    {
        var eventId = Guid.NewGuid();
        var price = Money.Create(250m, Currency.USD).Value;
        var perks = new List<string> { "Logo on banner", "2 tickets" };

        var result = SponsorshipPackage.Create(
            eventId, "Silver Sponsor", "Mid-tier package",
            price, quantityLimit: 10, sortOrder: 2,
            tier: "Silver", perks: perks, includedTicketCount: 2);

        result.IsSuccess.Should().BeTrue();
        var pkg = result.Value;
        pkg.EventId.Should().Be(eventId);
        pkg.Name.Should().Be("Silver Sponsor");
        pkg.Description.Should().Be("Mid-tier package");
        pkg.Price.Should().Be(price);
        pkg.QuantityLimit.Should().Be(10);
        pkg.QuantitySold.Should().Be(0);
        pkg.IsActive.Should().BeTrue();
        pkg.SortOrder.Should().Be(2);
        pkg.Tier.Should().Be("Silver");
        pkg.Perks.Should().BeEquivalentTo(perks);
        pkg.IncludedTicketCount.Should().Be(2);
        pkg.ImageUrl.Should().BeNull();
        pkg.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullQuantityLimit_ShouldSucceed_Unlimited()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, "Bronze", null, ValidPrice(),
            quantityLimit: null, sortOrder: 0,
            tier: "Bronze", perks: null, includedTicketCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuantityLimit.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldSucceed()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, "Basic", null, ValidPrice(), 10, 0, null, null, 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTier_ShouldSucceed()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, tier: null, perks: ValidPerks(), includedTicketCount: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullPerks_ShouldSucceed_TreatedAsEmptyList()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, perks: null, includedTicketCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Perks.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyPerks_ShouldSucceed()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, perks: new List<string>(), includedTicketCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Perks.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldSucceed_RecognitionOnlyPackages()
    {
        var zeroPrice = Money.Create(0m, Currency.USD).Value;

        var result = SponsorshipPackage.Create(
            ValidEventId, "Community Partner", null, zeroPrice,
            quantityLimit: null, sortOrder: 0,
            tier: "Community", perks: new List<string> { "Logo on website" },
            includedTicketCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Price.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithZeroIncludedTicketCount_ShouldSucceed()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), includedTicketCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludedTicketCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithMaxIncludedTicketCount_ShouldSucceed()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(),
            includedTicketCount: SponsorshipPackage.MAX_INCLUDED_TICKET_COUNT);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludedTicketCount.Should().Be(SponsorshipPackage.MAX_INCLUDED_TICKET_COUNT);
    }

    [Fact]
    public void Create_WithMaxPerks_ShouldSucceed()
    {
        var maxPerks = Enumerable.Range(1, SponsorshipPackage.MAX_PERKS_COUNT)
            .Select(i => $"Perk {i}")
            .ToList();

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, maxPerks, ValidIncludedTicketCount);

        result.IsSuccess.Should().BeTrue();
        result.Value.Perks.Count.Should().Be(SponsorshipPackage.MAX_PERKS_COUNT);
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, "  Trimmed Name  ", "  Trimmed Desc  ", ValidPrice(),
            10, 0, "  Gold  ", new List<string> { "  Perk one  " }, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Trimmed Name");
        result.Value.Description.Should().Be("Trimmed Desc");
        result.Value.Tier.Should().Be("Gold");
        result.Value.Perks[0].Should().Be("Perk one");
    }

    [Fact]
    public void Create_ShouldFilterEmptyAndWhitespacePerks()
    {
        var perks = new List<string> { "Real perk", "", "   ", "Another real perk" };

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, perks, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Perks.Should().HaveCount(2);
        result.Value.Perks.Should().BeEquivalentTo(new[] { "Real perk", "Another real perk" });
    }

    #endregion

    #region Create – Failure Cases

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            Guid.Empty, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, "", ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name is required");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, "   ", ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name is required");
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ShouldFail()
    {
        var longName = new string('A', SponsorshipPackage.MAX_NAME_LENGTH + 1);

        var result = SponsorshipPackage.Create(
            ValidEventId, longName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_NAME_LENGTH}");
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaxLength_ShouldFail()
    {
        var longDesc = new string('B', SponsorshipPackage.MAX_DESCRIPTION_LENGTH + 1);

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, longDesc, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_DESCRIPTION_LENGTH}");
    }

    [Fact]
    public void Create_WithTierExceedingMaxLength_ShouldFail()
    {
        var longTier = new string('T', SponsorshipPackage.MAX_TIER_LENGTH + 1);

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, longTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_TIER_LENGTH}");
    }

    [Fact]
    public void Create_WithNullPrice_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, null!,
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("price is required");
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldFail()
    {
        var negativePrice = new Money(-5m, Currency.USD);

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, negativePrice,
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("price cannot be negative");
    }

    [Fact]
    public void Create_WithZeroQuantityLimit_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            quantityLimit: 0, sortOrder: 0,
            tier: ValidTier, perks: ValidPerks(), includedTicketCount: ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity limit must be greater than zero");
    }

    [Fact]
    public void Create_WithNegativeQuantityLimit_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            quantityLimit: -1, sortOrder: 0,
            tier: ValidTier, perks: ValidPerks(), includedTicketCount: ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity limit must be greater than zero");
    }

    [Fact]
    public void Create_WithNegativeIncludedTicketCount_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), includedTicketCount: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Included ticket count");
    }

    [Fact]
    public void Create_WithIncludedTicketCountExceedingMax_ShouldFail()
    {
        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(),
            includedTicketCount: SponsorshipPackage.MAX_INCLUDED_TICKET_COUNT + 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_INCLUDED_TICKET_COUNT}");
    }

    [Fact]
    public void Create_WithTooManyPerks_ShouldFail()
    {
        var tooManyPerks = Enumerable.Range(1, SponsorshipPackage.MAX_PERKS_COUNT + 1)
            .Select(i => $"Perk {i}")
            .ToList();

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, tooManyPerks, ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_PERKS_COUNT}");
    }

    [Fact]
    public void Create_WithPerkExceedingMaxLength_ShouldFail()
    {
        var longPerk = new string('P', SponsorshipPackage.MAX_PERK_LENGTH + 1);
        var perks = new List<string> { "Normal perk", longPerk };

        var result = SponsorshipPackage.Create(
            ValidEventId, ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, perks, ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{SponsorshipPackage.MAX_PERK_LENGTH}");
    }

    #endregion

    #region UpdateDetails – Success Cases

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateAllFields()
    {
        var pkg = CreateValidPackage();
        var newPrice = Money.Create(750m, Currency.USD).Value;
        var newPerks = new List<string> { "Updated perk A", "Updated perk B" };

        var result = pkg.UpdateDetails(
            "Platinum Sponsor", "Top-of-the-top",
            newPrice, quantityLimit: 3, sortOrder: 0,
            tier: "Platinum", perks: newPerks, includedTicketCount: 10);

        result.IsSuccess.Should().BeTrue();
        pkg.Name.Should().Be("Platinum Sponsor");
        pkg.Description.Should().Be("Top-of-the-top");
        pkg.Price.Should().Be(newPrice);
        pkg.QuantityLimit.Should().Be(3);
        pkg.SortOrder.Should().Be(0);
        pkg.Tier.Should().Be("Platinum");
        pkg.Perks.Should().BeEquivalentTo(newPerks);
        pkg.IncludedTicketCount.Should().Be(10);
    }

    [Fact]
    public void UpdateDetails_WithNullQuantityLimit_ShouldSetUnlimited()
    {
        var pkg = CreateValidPackage(quantityLimit: 10);

        var result = pkg.UpdateDetails(
            ValidName, ValidDescription, ValidPrice(),
            quantityLimit: null, sortOrder: 0,
            tier: ValidTier, perks: ValidPerks(), includedTicketCount: ValidIncludedTicketCount);

        result.IsSuccess.Should().BeTrue();
        pkg.QuantityLimit.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldNotAffectIsActive()
    {
        var pkg = CreateValidPackage();
        pkg.Deactivate();
        pkg.IsActive.Should().BeFalse();

        pkg.UpdateDetails(ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        pkg.IsActive.Should().BeFalse();
    }

    #endregion

    #region UpdateDetails – Failure Cases

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldFail()
    {
        var pkg = CreateValidPackage();

        var result = pkg.UpdateDetails(
            "", ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithNegativeIncludedTicketCount_ShouldFail()
    {
        var pkg = CreateValidPackage();

        var result = pkg.UpdateDetails(
            ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, ValidPerks(), includedTicketCount: -2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Included ticket count");
    }

    [Fact]
    public void UpdateDetails_WithTooManyPerks_ShouldFail()
    {
        var pkg = CreateValidPackage();
        var tooMany = Enumerable.Range(1, SponsorshipPackage.MAX_PERKS_COUNT + 1)
            .Select(i => $"P{i}")
            .ToList();

        var result = pkg.UpdateDetails(
            ValidName, ValidDescription, ValidPrice(),
            10, 0, ValidTier, tooMany, ValidIncludedTicketCount);

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Deactivate / Activate

    [Fact]
    public void Deactivate_WhenActive_ShouldSucceed()
    {
        var pkg = CreateValidPackage();

        var result = pkg.Deactivate();

        result.IsSuccess.Should().BeTrue();
        pkg.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldFail()
    {
        var pkg = CreateValidPackage();
        pkg.Deactivate();

        var result = pkg.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already inactive");
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSucceed()
    {
        var pkg = CreateValidPackage();
        pkg.Deactivate();

        var result = pkg.Activate();

        result.IsSuccess.Should().BeTrue();
        pkg.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldFail()
    {
        var pkg = CreateValidPackage();

        var result = pkg.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
    }

    #endregion

    #region HasAvailableStock / RemainingStock

    [Fact]
    public void HasAvailableStock_WithNoLimit_ShouldAlwaysReturnTrue()
    {
        var pkg = CreateValidPackage(quantityLimit: null);

        pkg.HasAvailableStock(1).Should().BeTrue();
        pkg.HasAvailableStock(int.MaxValue).Should().BeTrue();
    }

    [Fact]
    public void HasAvailableStock_WithinLimit_ShouldReturnTrue()
    {
        var pkg = CreateValidPackage(quantityLimit: 5);

        pkg.HasAvailableStock(5).Should().BeTrue();
    }

    [Fact]
    public void HasAvailableStock_ExceedingLimit_ShouldReturnFalse()
    {
        var pkg = CreateValidPackage(quantityLimit: 3);

        pkg.HasAvailableStock(4).Should().BeFalse();
    }

    [Fact]
    public void HasAvailableStock_DefaultQuantityIsOne()
    {
        var pkg = CreateValidPackage(quantityLimit: 1);

        pkg.HasAvailableStock().Should().BeTrue();
    }

    [Fact]
    public void RemainingStock_WithNoLimit_ShouldReturnNull()
    {
        var pkg = CreateValidPackage(quantityLimit: null);

        pkg.RemainingStock.Should().BeNull();
    }

    [Fact]
    public void RemainingStock_WithLimit_ShouldReturnLimitMinusSold()
    {
        var pkg = CreateValidPackage(quantityLimit: 5);

        pkg.RemainingStock.Should().Be(5);
    }

    #endregion

    #region Image (SetImage / ClearImage)

    [Fact]
    public void SetImage_WithValidUrlAndBlobName_Succeeds()
    {
        var pkg = CreateValidPackage();

        var result = pkg.SetImage(
            "https://blob.example.com/packages/gold.png",
            "abc_gold-tier.png");

        result.IsSuccess.Should().BeTrue();
        pkg.ImageUrl.Should().Be("https://blob.example.com/packages/gold.png");
        pkg.ImageBlobName.Should().Be("abc_gold-tier.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetImage_WithEmptyUrl_Fails(string? badUrl)
    {
        var pkg = CreateValidPackage();

        var result = pkg.SetImage(badUrl!, "blob.png");

        result.IsFailure.Should().BeTrue();
        pkg.ImageUrl.Should().BeNull("rejected SetImage must NOT mutate the entity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetImage_WithEmptyBlobName_Fails(string? badBlob)
    {
        var pkg = CreateValidPackage();

        var result = pkg.SetImage("https://blob.example.com/x.png", badBlob!);

        result.IsFailure.Should().BeTrue();
        pkg.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void SetImage_ReplacingExistingImage_OverwritesBoth()
    {
        var pkg = CreateValidPackage();
        pkg.SetImage("https://blob.example.com/old.png", "old.png");

        pkg.SetImage("https://blob.example.com/new.png", "new.png");

        pkg.ImageUrl.Should().Be("https://blob.example.com/new.png");
        pkg.ImageBlobName.Should().Be("new.png");
    }

    [Fact]
    public void ClearImage_RemovesBothFields()
    {
        var pkg = CreateValidPackage();
        pkg.SetImage("https://blob.example.com/x.png", "x.png");

        var result = pkg.ClearImage();

        result.IsSuccess.Should().BeTrue();
        pkg.ImageUrl.Should().BeNull();
        pkg.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void ClearImage_WhenNoImage_IsIdempotent()
    {
        var pkg = CreateValidPackage();

        var result = pkg.ClearImage();

        result.IsSuccess.Should().BeTrue();
        pkg.ImageUrl.Should().BeNull();
    }

    #endregion

    #region Lifecycle Integration

    [Fact]
    public void Create_PreservesEventIdAcrossUpdates()
    {
        var eventId = Guid.NewGuid();
        var pkg = CreateValidPackage(eventId: eventId);

        pkg.UpdateDetails("New name", null, ValidPrice(), 10, 0, "Platinum", null, 0);

        pkg.EventId.Should().Be(eventId);
    }

    [Fact]
    public void NewPackage_StartsWithZeroQuantitySold()
    {
        var pkg = CreateValidPackage();

        pkg.QuantitySold.Should().Be(0);
    }

    [Fact]
    public void NewPackage_StartsActive()
    {
        var pkg = CreateValidPackage();

        pkg.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewPackage_HasNullImageFields()
    {
        var pkg = CreateValidPackage();

        pkg.ImageUrl.Should().BeNull();
        pkg.ImageBlobName.Should().BeNull();
    }

    #endregion
}

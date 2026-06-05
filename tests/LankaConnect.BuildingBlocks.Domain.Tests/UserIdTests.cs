using LankaConnect.SharedKernel.Identity;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

/// <summary>W1G verification: UserId typed identifier value object.</summary>
public sealed class UserIdTests
{
    [Fact]
    public void From_WithValidGuid_ReturnsUserId()
    {
        var guid = Guid.NewGuid();

        var userId = UserId.From(guid);

        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_Throws()
    {
        var act = () => UserId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*UserId cannot be Guid.Empty*");
    }

    [Fact]
    public void NewId_ReturnsDistinctIdsAcrossCalls()
    {
        var a = UserId.NewId();
        var b = UserId.NewId();

        a.Value.Should().NotBe(Guid.Empty);
        b.Value.Should().NotBe(Guid.Empty);
        a.Value.Should().NotBe(b.Value);
    }

    [Fact]
    public void Equality_TwoUserIdsWithSameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = UserId.From(guid);
        var b = UserId.From(guid);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_TwoDistinctUserIds_AreNotEqual()
    {
        var a = UserId.NewId();
        var b = UserId.NewId();

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var userId = UserId.From(guid);

        Guid extracted = userId;

        extracted.Should().Be(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var userId = UserId.From(guid);

        userId.ToString().Should().Be(guid.ToString());
    }
}

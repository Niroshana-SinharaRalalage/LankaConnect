using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class MaybeTests
{
    [Fact]
    public void None_HasNoValue()
    {
        Maybe<int>.None.HasValue.Should().BeFalse();
        Maybe<int>.None.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Some_HasValue()
    {
        var some = Maybe<int>.Some(42);

        some.HasValue.Should().BeTrue();
        some.IsEmpty.Should().BeFalse();
        some.Value.Should().Be(42);
    }

    [Fact]
    public void Some_NullValue_Throws()
    {
        Action act = () => Maybe<string>.Some(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void None_AccessingValue_Throws()
    {
        Action act = () => _ = Maybe<int>.None.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty (None)*");
    }

    [Fact]
    public void From_Null_ReturnsNone()
    {
        Maybe<string>.From(null).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void From_NonNull_ReturnsSome()
    {
        var some = Maybe<string>.From("hello");

        some.HasValue.Should().BeTrue();
        some.Value.Should().Be("hello");
    }

    [Fact]
    public void GetValueOrDefault_None_ReturnsFallback()
    {
        Maybe<int>.None.GetValueOrDefault(99).Should().Be(99);
    }

    [Fact]
    public void GetValueOrDefault_Some_ReturnsValue()
    {
        Maybe<int>.Some(7).GetValueOrDefault(99).Should().Be(7);
    }

    [Fact]
    public void Match_None_InvokesOnNone()
    {
        var result = Maybe<int>.None.Match(
            onSome: v => $"v={v}",
            onNone: () => "empty");

        result.Should().Be("empty");
    }

    [Fact]
    public void Match_Some_InvokesOnSome()
    {
        var result = Maybe<int>.Some(5).Match(
            onSome: v => $"v={v}",
            onNone: () => "empty");

        result.Should().Be("v=5");
    }

    [Fact]
    public void Map_None_PassesThroughAsNone()
    {
        var mapped = Maybe<int>.None.Map(x => x * 2);

        mapped.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Map_Some_TransformsValue()
    {
        var mapped = Maybe<int>.Some(5).Map(x => x * 2);

        mapped.HasValue.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Bind_None_ShortCircuits()
    {
        var bindRan = false;
        var result = Maybe<int>.None.Bind(x =>
        {
            bindRan = true;
            return Maybe<string>.Some("never");
        });

        bindRan.Should().BeFalse();
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Bind_Some_ChainsToInnerMaybe()
    {
        var result = Maybe<int>.Some(5).Bind(x => Maybe<string>.Some($"v={x}"));

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be("v=5");
    }

    [Fact]
    public void Equality_NoneEqualsNone()
    {
        Maybe<int>.None.Should().Be(Maybe<int>.None);
        (Maybe<int>.None == Maybe<int>.None).Should().BeTrue();
    }

    [Fact]
    public void Equality_SomeEqualsSome_WithSameValue()
    {
        Maybe<int>.Some(3).Should().Be(Maybe<int>.Some(3));
        (Maybe<int>.Some(3) == Maybe<int>.Some(3)).Should().BeTrue();
    }

    [Fact]
    public void Equality_SomeNotEqualToNone()
    {
        (Maybe<int>.Some(3) == Maybe<int>.None).Should().BeFalse();
        (Maybe<int>.Some(3) != Maybe<int>.None).Should().BeTrue();
    }

    [Fact]
    public void Equality_SomeNotEqualToDifferentValue()
    {
        (Maybe<int>.Some(3) == Maybe<int>.Some(4)).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_NoneIsStable()
    {
        Maybe<int>.None.GetHashCode().Should().Be(Maybe<int>.None.GetHashCode());
    }

    [Fact]
    public void ToString_SomeFormatsValue()
    {
        Maybe<int>.Some(7).ToString().Should().Be("Some(7)");
    }

    [Fact]
    public void ToString_NoneIsNone()
    {
        Maybe<int>.None.ToString().Should().Be("None");
    }
}

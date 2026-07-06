using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class ValueObjectTests
{
    private sealed class Point : ValueObject
    {
        public int X { get; }
        public int Y { get; }
        public Point(int x, int y) { X = x; Y = y; }
        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return X;
            yield return Y;
        }
    }

    private sealed class Color : ValueObject
    {
        public string Name { get; }
        public Color(string name) { Name = name; }
        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }

    [Fact]
    public void Equality_SameComponents_AreEqual()
    {
        var a = new Point(1, 2);
        var b = new Point(1, 2);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentComponents_NotEqual()
    {
        var a = new Point(1, 2);
        var b = new Point(1, 3);

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentTypes_NotEqual()
    {
        var point = new Point(1, 2);
        var color = new Color("red");

        ((ValueObject)point).Equals(color).Should().BeFalse();
    }

    [Fact]
    public void Equality_ReferenceSame_AreEqual()
    {
        var a = new Point(1, 2);

        a.Equals(a).Should().BeTrue();
    }

    [Fact]
    public void Equality_AgainstNull_NotEqual()
    {
        var a = new Point(1, 2);

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void NullSafe_BothNull_AreEqual()
    {
        Point? a = null;
        Point? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void NullSafe_OneNull_NotEqual()
    {
        Point? a = null;
        var b = new Point(1, 2);

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_DifferentComponents_GenerallyDifferent()
    {
        // Hash collisions are technically allowed, but for these simple cases
        // they should remain distinct — guards against accidentally hashing
        // only one component.
        var a = new Point(1, 2);
        var b = new Point(2, 1);

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }
}

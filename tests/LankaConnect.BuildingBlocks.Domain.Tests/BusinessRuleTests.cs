using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class BusinessRuleTests
{
    private sealed class AlwaysBroken : BusinessRule
    {
        public override bool IsBroken() => true;
        public override Error BrokenError { get; } = new("Test.AlwaysBroken", "Always broken");
    }

    private sealed class NeverBroken : BusinessRule
    {
        public override bool IsBroken() => false;
        public override Error BrokenError { get; } = new("Test.NeverBroken", "Never broken");
    }

    [Fact]
    public void Check_NotBroken_ReturnsSuccess()
    {
        var result = BusinessRule.Check(new NeverBroken());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Check_Broken_ReturnsFailureWithRulesError()
    {
        var result = BusinessRule.Check(new AlwaysBroken());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Test.AlwaysBroken");
    }

    [Fact]
    public void Check_NullRule_Throws()
    {
        Action act = () => BusinessRule.Check(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CheckAll_AllPass_ReturnsSuccess()
    {
        var result = BusinessRule.CheckAll(new NeverBroken(), new NeverBroken());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckAll_AnyBroken_ReturnsFirstError()
    {
        var first = new AlwaysBroken();
        var second = new AlwaysBroken();
        // Place a passing rule first to confirm CheckAll iterates in order
        var result = BusinessRule.CheckAll(new NeverBroken(), first, second);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(first.BrokenError);
    }

    [Fact]
    public void CheckAll_EmptyInput_ReturnsSuccess()
    {
        BusinessRule.CheckAll().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckAll_NullArray_Throws()
    {
        Action act = () => BusinessRule.CheckAll(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CheckAll_ArrayContainingNull_Throws()
    {
        BusinessRule? badRule = null;
        Action act = () => BusinessRule.CheckAll(new NeverBroken(), badRule!);

        act.Should().Throw<ArgumentNullException>();
    }
}

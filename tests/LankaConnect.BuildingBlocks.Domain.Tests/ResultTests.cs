using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class ResultTests
{
    private static readonly Error SampleError = new("Test.Failure", "Sample failure");

    // ---------- Non-generic Result ----------

    [Fact]
    public void Success_IsSuccessAndCarriesNoneError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_IsFailureAndCarriesError()
    {
        var result = Result.Failure(SampleError);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Failure_WithNoneError_Throws()
    {
        Action act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-none error*");
    }

    [Fact]
    public void Combine_AllSuccesses_IsSuccess()
    {
        var combined = Result.Combine(Result.Success(), Result.Success(), Result.Success());

        combined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_AnyFailure_ReturnsFirstFailure()
    {
        var first = Result.Failure(new Error("First", "First failure"));
        var second = Result.Failure(new Error("Second", "Second failure"));

        var combined = Result.Combine(Result.Success(), first, second);

        combined.IsFailure.Should().BeTrue();
        combined.Error.Should().Be(first.Error);
    }

    [Fact]
    public void Combine_EmptyInput_IsSuccess()
    {
        Result.Combine().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_Null_ReturnsNullValueError()
    {
        var combined = Result.Combine(null!);

        combined.IsFailure.Should().BeTrue();
        combined.Error.Should().Be(Error.NullValue);
    }

    // ---------- Generic Result<T> ----------

    [Fact]
    public void Generic_Success_CarriesValueAndIsSuccess()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Generic_Failure_AccessingValueThrows()
    {
        var result = Result<int>.Failure(SampleError);

        Action act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot access Value on a failure result*");
    }

    [Fact]
    public void ImplicitConversion_FromValue_BuildsSuccess()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ImplicitConversion_FromError_BuildsFailure()
    {
        Result<string> result = SampleError;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Map_SuccessPath_TransformsValue()
    {
        var doubled = Result<int>.Success(5).Map(x => x * 2);

        doubled.IsSuccess.Should().BeTrue();
        doubled.Value.Should().Be(10);
    }

    [Fact]
    public void Map_FailurePath_PreservesError()
    {
        var failed = Result<int>.Failure(SampleError).Map(x => x * 2);

        failed.IsFailure.Should().BeTrue();
        failed.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Map_NullMapper_Throws()
    {
        Func<int, int>? mapper = null;
        Action act = () => Result<int>.Success(1).Map(mapper!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Bind_SuccessChainsSuccess_ReturnsInnerSuccess()
    {
        var chained = Result<int>.Success(5).Bind(x => Result<string>.Success($"v={x}"));

        chained.IsSuccess.Should().BeTrue();
        chained.Value.Should().Be("v=5");
    }

    [Fact]
    public void Bind_SuccessChainsFailure_ReturnsInnerFailure()
    {
        var chained = Result<int>.Success(5).Bind(x => Result<string>.Failure(SampleError));

        chained.IsFailure.Should().BeTrue();
        chained.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Bind_FailureShortCircuits()
    {
        var bindRan = false;
        var chained = Result<int>.Failure(SampleError)
            .Bind(x => { bindRan = true; return Result<string>.Success("never"); });

        bindRan.Should().BeFalse();
        chained.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Match_SuccessPath_InvokesOnSuccess()
    {
        var matched = Result<int>.Success(7).Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        matched.Should().Be("ok:7");
    }

    [Fact]
    public void Match_FailurePath_InvokesOnFailure()
    {
        var matched = Result<int>.Failure(SampleError).Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        matched.Should().Be("err:Test.Failure");
    }

    [Fact]
    public void Result_SuccessFactory_AcceptsValueArg()
    {
        var r = Result.Success(123);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(123);
    }

    [Fact]
    public void Result_FailureFactory_AcceptsErrorArg()
    {
        var r = Result.Failure<int>(SampleError);

        r.IsFailure.Should().BeTrue();
        r.Error.Should().Be(SampleError);
    }
}

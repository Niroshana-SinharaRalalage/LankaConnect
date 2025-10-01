using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class FluentAssertionsExtensions
{
    public static ResultAssertions<T> Should<T>(this Result<T> result)
    {
        return new ResultAssertions<T>(result);
    }

    public static ResultAssertions Should(this Result result)
    {
        return new ResultAssertions(result);
    }
}

public class ResultAssertions<T> : ReferenceTypeAssertions<Result<T>, ResultAssertions<T>>
{
    public ResultAssertions(Result<T> result) : base(result)
    {
    }

    protected override string Identifier => "result";

    public AndConstraint<ResultAssertions<T>> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.IsSuccess)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected result to be successful{reason}, but it failed with errors: {0}", Subject.Errors);

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    public AndConstraint<ResultAssertions<T>> BeFailure(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.IsFailure)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected result to be a failure{reason}, but it was successful");

        return new AndConstraint<ResultAssertions<T>>(this);
    }
}

public class ResultAssertions : ReferenceTypeAssertions<Result, ResultAssertions>
{
    public ResultAssertions(Result result) : base(result)
    {
    }

    protected override string Identifier => "result";

    public AndConstraint<ResultAssertions> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.IsSuccess)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected result to be successful{reason}, but it failed with errors: {0}", Subject.Errors);

        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeFailure(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject.IsFailure)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected result to be a failure{reason}, but it was successful");

        return new AndConstraint<ResultAssertions>(this);
    }
}
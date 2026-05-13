using FluentValidation;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using MediatR;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class ValidationBehaviorTests
{
    private sealed record SampleRequest(string Payload) : IRequest<string>;

    private sealed class AlwaysPasses : AbstractValidator<SampleRequest>
    {
        public AlwaysPasses() { /* no rules */ }
    }

    private sealed class AlwaysFails : AbstractValidator<SampleRequest>
    {
        public AlwaysFails()
        {
            RuleFor(x => x.Payload).Must(_ => false).WithMessage("forced failure");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_PassesThrough()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        Task<string> Next() => Task.FromResult("ok");

        var result = await behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorsPass_PassesThrough()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { new AlwaysPasses() });
        var called = false;
        Task<string> Next() { called = true; return Task.FromResult("ok"); }

        var result = await behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { new AlwaysFails() });
        var called = false;
        Task<string> Next() { called = true; return Task.FromResult("ok"); }

        Func<Task> act = () => behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*forced failure*");
        called.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        Func<Task> act = () => behavior.Handle(new SampleRequest("x"), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_MultipleValidators_AccumulatesFailures()
    {
        var v1 = new AlwaysFails();
        var v2 = new AlwaysFails();
        var behavior = new ValidationBehavior<SampleRequest, string>(new IValidator<SampleRequest>[] { v1, v2 });
        Task<string> Next() => Task.FromResult("never");

        try
        {
            await behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);
            true.Should().BeFalse("expected ValidationException");
        }
        catch (ValidationException ex)
        {
            // The point: failures from MULTIPLE validators are accumulated, not first-stop.
            // Exact count depends on FluentValidation internals (each validator may yield
            // ≥1 failure per rule), so we assert at least 2.
            ex.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        }
    }
}

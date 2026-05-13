using LankaConnect.BuildingBlocks.Application.Behaviors;
using LankaConnect.BuildingBlocks.Application.Tests.Fakes;
using MediatR;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class LoggingBehaviorTests
{
    private sealed record SampleRequest(string Payload) : IRequest<string>;

    [Fact]
    public async Task Handle_OnSuccess_InvokesNextAndReturnsResponse()
    {
        var behavior = new LoggingBehavior<SampleRequest, string>(NullLog.For<LoggingBehavior<SampleRequest, string>>());
        var called = false;
        Task<string> Next() { called = true; return Task.FromResult("ok"); }

        var result = await behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var behavior = new LoggingBehavior<SampleRequest, string>(NullLog.For<LoggingBehavior<SampleRequest, string>>());

        Func<Task> act = () => behavior.Handle(new SampleRequest("x"), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_OnException_LogsAndRethrows()
    {
        var behavior = new LoggingBehavior<SampleRequest, string>(NullLog.For<LoggingBehavior<SampleRequest, string>>());
        var inner = new InvalidOperationException("boom");
        Task<string> Next() => throw inner;

        Func<Task> act = () => behavior.Handle(new SampleRequest("x"), Next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }
}

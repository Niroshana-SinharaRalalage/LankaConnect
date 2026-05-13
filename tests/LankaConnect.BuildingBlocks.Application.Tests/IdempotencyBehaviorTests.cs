using System.Text.Json;
using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using LankaConnect.BuildingBlocks.Application.Tests.Fakes;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class IdempotencyBehaviorTests
{
    private sealed record SampleCommand(Guid IdempotencyKey, string Payload) : IIdempotentCommand<string>;

    [Fact]
    public async Task Handle_NewKey_ExecutesHandlerAndCachesResponse()
    {
        var store = new FakeIdempotencyStore();
        var behavior = new IdempotencyBehavior<SampleCommand, string>(store, NullLog.For<IdempotencyBehavior<SampleCommand, string>>());
        var key = Guid.NewGuid();
        var executions = 0;

        var result = await behavior.Handle(
            new SampleCommand(key, "hello"),
            () => { executions++; return Task.FromResult("response-1"); },
            CancellationToken.None);

        result.Should().Be("response-1");
        executions.Should().Be(1);
        store.PutCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RepeatedKey_ShortCircuitsWithCachedResponse()
    {
        var store = new FakeIdempotencyStore();
        var behavior = new IdempotencyBehavior<SampleCommand, string>(store, NullLog.For<IdempotencyBehavior<SampleCommand, string>>());
        var key = Guid.NewGuid();
        store.Seed(key, JsonSerializer.Serialize("cached-response"));
        var executions = 0;

        var result = await behavior.Handle(
            new SampleCommand(key, "hello"),
            () => { executions++; return Task.FromResult("never"); },
            CancellationToken.None);

        result.Should().Be("cached-response");
        executions.Should().Be(0);
        store.PutCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CorruptedCacheEntry_FallsThroughToHandler()
    {
        var store = new FakeIdempotencyStore();
        var behavior = new IdempotencyBehavior<SampleCommand, string>(store, NullLog.For<IdempotencyBehavior<SampleCommand, string>>());
        var key = Guid.NewGuid();
        store.Seed(key, "this is not valid JSON for string");
        var executions = 0;

        var result = await behavior.Handle(
            new SampleCommand(key, "hello"),
            () => { executions++; return Task.FromResult("fresh"); },
            CancellationToken.None);

        result.Should().Be("fresh");
        executions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_StorePutFails_StillReturnsHandlerResponse()
    {
        var store = new FakeIdempotencyStore { ThrowOnPut = new InvalidOperationException("storage broken") };
        var behavior = new IdempotencyBehavior<SampleCommand, string>(store, NullLog.For<IdempotencyBehavior<SampleCommand, string>>());

        var result = await behavior.Handle(
            new SampleCommand(Guid.NewGuid(), "hello"),
            () => Task.FromResult("response"),
            CancellationToken.None);

        result.Should().Be("response");
        store.PutCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var store = new FakeIdempotencyStore();
        var behavior = new IdempotencyBehavior<SampleCommand, string>(store, NullLog.For<IdempotencyBehavior<SampleCommand, string>>());

        Func<Task> act = () => behavior.Handle(new SampleCommand(Guid.NewGuid(), "x"), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Application.Tests.Fakes;

/// <summary>Records every UoW call so tests can assert begin/commit/rollback order.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<string> Calls { get; } = new();
    public Exception? ThrowOnBegin { get; set; }
    public Exception? ThrowOnCommit { get; set; }
    public Exception? ThrowOnRollback { get; set; }

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Begin");
        if (ThrowOnBegin is not null) throw ThrowOnBegin;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Commit");
        if (ThrowOnCommit is not null) throw ThrowOnCommit;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Rollback");
        if (ThrowOnRollback is not null) throw ThrowOnRollback;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory idempotency store keyed by GUID.</summary>
public sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<Guid, string> _entries = new();
    public Exception? ThrowOnPut { get; set; }
    public int TryGetCalls { get; private set; }
    public int PutCalls { get; private set; }

    public Task<string?> TryGetAsync(Guid key, CancellationToken cancellationToken = default)
    {
        TryGetCalls++;
        return Task.FromResult(_entries.TryGetValue(key, out var v) ? v : null);
    }

    public Task PutAsync(Guid key, string serializedResponse, CancellationToken cancellationToken = default)
    {
        PutCalls++;
        if (ThrowOnPut is not null) throw ThrowOnPut;
        _entries[key] = serializedResponse;
        return Task.CompletedTask;
    }

    /// <summary>Test helper: directly seed an entry.</summary>
    public void Seed(Guid key, string value) => _entries[key] = value;
}

/// <summary>Captures every outbox enqueue call.</summary>
public sealed class FakeOutbox : IOutbox
{
    public List<object> Enqueued { get; } = new();

    public Task EnqueueAsync(object integrationEvent, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

/// <summary>Holds a fixed list of integration events drained on first call.</summary>
public sealed class FakeIntegrationEventBuffer : IIntegrationEventBuffer
{
    private readonly List<object> _events;
    public int DrainCalls { get; private set; }

    public FakeIntegrationEventBuffer(params object[] events)
    {
        _events = new List<object>(events);
    }

    public IReadOnlyList<object> DrainEvents()
    {
        DrainCalls++;
        var snapshot = _events.ToList();
        _events.Clear();
        return snapshot;
    }
}

/// <summary>Records every audit entry written.</summary>
public sealed class FakeAuditLogger : IAuditLogger
{
    public List<AuditEntry> Entries { get; } = new();
    public Exception? ThrowOnLog { get; set; }

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (ThrowOnLog is not null) throw ThrowOnLog;
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

/// <summary>Returns a fixed actor id (or null for anonymous).</summary>
public sealed class FakeCurrentActor : ICurrentActor
{
    public FakeCurrentActor(string? actorId) { ActorId = actorId; }
    public string? ActorId { get; }
}

/// <summary>No-op logger that satisfies the constructor without an ILoggerFactory.</summary>
public static class NullLog
{
    public static ILogger<T> For<T>() => Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}

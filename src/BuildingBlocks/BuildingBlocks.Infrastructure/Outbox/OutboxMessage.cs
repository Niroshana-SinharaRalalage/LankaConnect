namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Outbox row — captures an integration event that must be published downstream.
/// Written in the SAME transaction as the state change that caused it (per the
/// outbox pattern); polled by <see cref="OutboxProcessor"/> on an interval and
/// dispatched via <see cref="IIntegrationEventDispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// At-least-once delivery: dispatched messages are marked
/// <see cref="ProcessedAt"/>; on dispatch failure the row stays + retry count
/// increments. After <see cref="MaxRetries"/> the row is moved to
/// <see cref="DeadLetterMessage"/> via the processor (the row itself doesn't
/// know about dead-lettering — that's a processor concern).
/// </para>
/// <para>
/// EventType is the AssemblyQualifiedName of the integration-event CLR type so
/// the dispatcher can deserialize via reflection. Modules registering for an
/// event use the same string convention.
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>Convention: retry up to 5 times before dead-lettering.</summary>
    public const int MaxRetries = 5;

    /// <summary>Stable identifier — Guid.NewGuid() at construction.</summary>
    public Guid Id { get; private set; }

    /// <summary>Fully-qualified CLR type name of the integration event (for round-trip deserialization).</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>JSON-serialized integration-event payload.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when the row was inserted (by the originating command's UoW commit).</summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>UTC timestamp when the processor successfully dispatched the event. Null until then.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>How many dispatch attempts have failed (informational; processor uses to gate dead-lettering).</summary>
    public int RetryCount { get; private set; }

    /// <summary>Last error message recorded by a failed dispatch attempt (truncated to 2000 chars).</summary>
    public string? LastError { get; private set; }

    /// <summary>EF Core constructor — DO NOT call from application code.</summary>
    private OutboxMessage() { }

    /// <summary>
    /// Constructs a new outbox row from a serialized integration event.
    /// </summary>
    public static OutboxMessage Create(string eventType, string payload, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredAt = occurredAtUtc,
            ProcessedAt = null,
            RetryCount = 0,
            LastError = null,
        };
    }

    /// <summary>Marks this row as successfully dispatched (sets ProcessedAt).</summary>
    public void MarkProcessed(DateTime processedAtUtc)
    {
        ProcessedAt = processedAtUtc;
        LastError = null;
    }

    /// <summary>
    /// Records a failed dispatch attempt. Truncates <paramref name="errorMessage"/>
    /// to 2000 chars so a long stack trace doesn't blow up the row size.
    /// </summary>
    public void RecordFailure(string errorMessage)
    {
        RetryCount += 1;
        LastError = errorMessage.Length > 2000
            ? errorMessage[..2000]
            : errorMessage;
    }

    /// <summary>True if this row has been retried <see cref="MaxRetries"/> times without success.</summary>
    public bool ShouldDeadLetter => RetryCount >= MaxRetries;
}

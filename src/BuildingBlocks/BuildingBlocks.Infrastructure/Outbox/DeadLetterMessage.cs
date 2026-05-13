namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Dead-letter row — captures an outbox message that failed dispatch
/// <see cref="OutboxMessage.MaxRetries"/> times. Surfaces in admin dashboards
/// for manual investigation + replay (or permanent discard).
/// </summary>
/// <remarks>
/// <para>
/// Dead-letter is intentionally a SEPARATE table from outbox so the outbox
/// stays small + fast to poll; the dead-letter table can grow indefinitely
/// without slowing the hot path. Ops dashboards query dead-letter on an
/// alert when row count grows past a threshold.
/// </para>
/// </remarks>
public sealed class DeadLetterMessage
{
    public Guid Id { get; private set; }
    public Guid OriginalOutboxId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime DeadLetteredAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private DeadLetterMessage() { }

    /// <summary>Constructs a dead-letter row from a failed-out outbox message.</summary>
    public static DeadLetterMessage FromOutboxMessage(OutboxMessage source, DateTime deadLetteredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            OriginalOutboxId = source.Id,
            EventType = source.EventType,
            Payload = source.Payload,
            OccurredAt = source.OccurredAt,
            DeadLetteredAt = deadLetteredAtUtc,
            RetryCount = source.RetryCount,
            LastError = source.LastError,
        };
    }
}

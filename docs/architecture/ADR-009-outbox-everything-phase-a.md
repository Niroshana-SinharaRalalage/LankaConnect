# ADR-009: Outbox-Everything Pattern for Cross-Module Coordination

| | |
|---|---|
| **Status** | Accepted (2026-06-04) |
| **Date** | 2026-06-04 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | Implicit single-`AppDbContext` atomic-transaction pattern across modules |
| **Related** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §2.D5 + §7.8; ADR-006 (5-layer topology) |

## Context

Today, a user registering for an event triggers (atomically in ONE `AppDbContext.SaveChangesAsync`):

1. Insert into `events.registrations`
2. Create `payments.payment_intents`
3. Enqueue `communications.outbox` entry for confirmation email
4. Create `notifications.notifications` row

This is one DB transaction. After modular extraction (Wave 4), this becomes FOUR DbContexts — `EventsDbContext`, `PaymentsDbContext`, `CommunicationsDbContext`, `NotificationsDbContext`. Cross-DbContext atomicity is impossible without distributed transactions (System.Transactions doesn't work reliably with Npgsql; performance penalty unacceptable).

Three patterns considered:

| Option | Verdict |
|---|---|
| `TransactionScope` (distributed transactions) | REJECTED — doesn't work cleanly with Npgsql; requires DTC; performance penalty |
| **Outbox-everything** (originating module writes + publishes; other modules subscribe) | **ACCEPTED** — industry standard for modular monoliths (Vaughn Vernon, Microservices Patterns) |
| Saga pattern (explicit orchestration with compensating actions) | REJECTED for Phase A — over-engineered; revisit for Phase B if Stripe Connect launches |

## Decision

**Every cross-module side-effect flows through the outbox.**

### Pattern

```
[Originating Module (e.g., Events)]
    1. Handler writes to events.registrations AND events.outbox
       in ONE transaction (events DbContext savechanges)
    2. Returns HTTP 200 to user

[OutboxProcessor (background)]
    3. Polls events.outbox
    4. Dispatches IntegrationEventBase via IIntegrationEventDispatcher
    5. Marks outbox row ProcessedAt = now

[Subscribing Modules (Communications, Payments, Notifications)]
    6. Each module's handler runs in its OWN transaction
    7. Failures retry with exponential backoff
    8. Permanent failures dead-letter (90-day retention)
```

### What lives in `BuildingBlocks.Infrastructure` (already shipped W2.5b)

- `OutboxMessage` entity (Id, EventType, Payload jsonb, OccurredAt, ProcessedAt, RetryCount, LastError)
- `DeadLetterMessage` entity (OriginalOutboxId reference + dead-letter audit)
- `OutboxProcessor` background service
- `IIntegrationEventDispatcher` interface + `MediatRIntegrationEventDispatcher` impl

### What every per-Capability DbContext exposes (already pattern in W3.5b for Notifications)

```csharp
public DbSet<OutboxMessage> Outbox { get; set; }
public DbSet<DeadLetterMessage> OutboxDeadLetter { get; set; }
public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
```

### Integration event versioning

Per blueprint §5: all integration events are sealed records named `*IntegrationEventV1`. Schema changes spawn V2 (additive only); consumers handle both during deprecation window.

## Critical Implication — Product / UX

**Eventual consistency between modules.** Cross-module effects are typically delivered in <1 second but are NOT atomic with the originating request.

**UI/UX wording must change**:

| Before | After |
|---|---|
| "Your registration is confirmed and email sent" | "Your registration is confirmed; you'll receive a confirmation email shortly" |
| "Payment processed and receipt emailed" | "Payment processed; receipt will arrive in your inbox" |
| "Photo uploaded and album updated" | "Photo uploaded; gallery is being updated" (or: keep atomic if Media stays in-process for now) |

**Founder accepts** this trade-off as part of ADR-009 approval. Wave 7 frontend work includes wording audit.

## ArchTest Rule Specification

```csharp
[Fact]
public void Every_Capability_DbContext_HasOutbox() {
    var capabilities = GetCapabilityDbContextTypes();
    foreach (var ctx in capabilities) {
        var outboxProp = ctx.GetProperty("Outbox");
        Assert.NotNull(outboxProp);
        Assert.Equal(typeof(DbSet<OutboxMessage>), outboxProp.PropertyType);
    }
}

[Fact]
public void Integration_Events_Are_Versioned() {
    var integrationEvents = Types.InAssemblies(GetAllCapabilityContractsAssemblies())
        .That().ImplementInterface(typeof(IIntegrationEventV1))
        .GetTypes();
    foreach (var t in integrationEvents) {
        Assert.True(t.IsSealed, $"{t.Name} must be sealed");
        Assert.Matches(@"IntegrationEventV\d+$", t.Name);
    }
}

[Fact]
public void Domain_Events_Stay_In_Module() {
    // for every IDomainEvent T, all IRequestHandler<T> impls must live in the same Capability assembly
}
```

## Operational Requirements

- **Dead-letter retention**: 90 days
- **Dead-letter alert SLA**: <15 minutes from dead-letter event (Azure Monitor alert wired in Wave 6)
- **Manual replay tooling**: `POST /api/admin/dead-letter/{id}/replay` (Wave 4 deliverable alongside first real cross-capability outbox use)
- **Trace propagation**: `IntegrationEventBase` carries `traceparent` (W3C); `OutboxProcessor` injects into `Activity.Current` before dispatching to handlers (per blueprint §7.3)
- **OpenTelemetry metrics**: outbox queue depth, dead-letter rate per Capability, processing latency p50/p95/p99 (Wave 6)

## Consequences

### Positive

- Module independence — each module can be deployed, scaled, restarted independently
- Microservice extraction in Phase B requires zero code changes (same outbox pattern works over HTTP/gRPC)
- Failure isolation — Communications outage doesn't block event registration
- At-least-once delivery semantics (consumers must be idempotent — enforced by `IdempotencyKey` table)

### Negative / Trade-offs

- UX wording changes (above)
- Cross-module debugging requires trace-id propagation (otherwise async logs lose correlation)
- Eventual consistency window typically <1s but can stretch under outage (acceptable with SLA monitoring)
- Slight increase in database write volume (originating module writes both business + outbox row)

### Risks

- Risk: consumer not idempotent → mitigated by IdempotencyKey table + per-Capability HasIdempotency ArchTest rule
- Risk: outbox queue depth grows during outage → mitigated by per-Capability depth alert + manual replay tooling
- Risk: integration event schema drift (V1 evolves silently) → mitigated by Integration_Events_Are_Versioned ArchTest rule + Pact contract tests

## Status Update Log

- 2026-06-04: Accepted by founder including the eventual-consistency UX implications. Infrastructure already 80% shipped (W2.5b, W2.7); Wave 4 capability extractions use the pattern for first real cross-capability flows.

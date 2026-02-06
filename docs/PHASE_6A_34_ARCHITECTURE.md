# Phase 6A.34 Architecture Analysis

## System Overview

This document provides architectural analysis of the event registration flow and the Phase 6A.34 fix.

---

## Component Diagram (C4 Level 2)

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Browser                            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Next.js Frontend (Port 3000)                 │  │
│  │                                                           │  │
│  │  Components:                                              │  │
│  │  - EventDetailsPage                                       │  │
│  │  - RegistrationForm                                       │  │
│  │  - AttendeeForm                                          │  │
│  └───────────────────────┬──────────────────────────────────┘  │
│                          │                                      │
│                          │ POST /api/events/{id}/rsvp          │
│                          │ Authorization: Bearer <token>        │
│                          │                                      │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│            Azure Container App (lankaconnect)                   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                   ASP.NET Core API                        │  │
│  │                                                           │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │  EventsController                                    │ │  │
│  │  │  - RsvpToEvent(eventId)                             │ │  │
│  │  │  - Validates JWT token                               │ │  │
│  │  │  - Extracts userId from claims                       │ │  │
│  │  │  - Sends RsvpToEventCommand                         │ │  │
│  │  └────────────────┬────────────────────────────────────┘ │  │
│  │                   │                                       │  │
│  │                   ▼                                       │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │  Application Layer (CQRS + MediatR)                 │ │  │
│  │  │                                                      │ │  │
│  │  │  RsvpToEventCommandHandler.Handle():                │ │  │
│  │  │  1. Load event aggregate                            │ │  │
│  │  │  2. Call event.AddRegistration(userId)              │ │  │
│  │  │  3. Save via repository                             │ │  │
│  │  │  4. Commit transaction (triggers domain events)     │ │  │
│  │  └────────────────┬────────────────────────────────────┘ │  │
│  │                   │                                       │  │
│  │                   ▼                                       │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │  Domain Layer (Event Aggregate)                     │ │  │
│  │  │                                                      │ │  │
│  │  │  Event.AddRegistration(userId):                     │ │  │
│  │  │  1. Validate capacity, status, etc.                 │ │  │
│  │  │  2. Create Registration entity                      │ │  │
│  │  │  3. Add to _registrations collection                │ │  │
│  │  │  4. RaiseDomainEvent(RegistrationConfirmedEvent)    │ │  │
│  │  └────────────────┬────────────────────────────────────┘ │  │
│  │                   │                                       │  │
│  │                   ▼                                       │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │  Infrastructure Layer (EF Core + PostgreSQL)        │ │  │
│  │  │                                                      │ │  │
│  │  │  AppDbContext.CommitAsync():                        │ │  │
│  │  │  ┌──────────────────────────────────────────────┐   │ │  │
│  │  │  │ PHASE 6A.34 FIX                              │   │ │  │
│  │  │  │ ChangeTracker.DetectChanges() ◄── CRITICAL   │   │ │  │
│  │  │  └──────────────────────────────────────────────┘   │ │  │
│  │  │  1. Dispatch domain events (Phase 6A.24)            │ │  │
│  │  │  2. SaveChangesAsync()                              │ │  │
│  │  └────────────────┬────────────────────────────────────┘ │  │
│  │                   │                                       │  │
│  └───────────────────┼───────────────────────────────────────┘  │
│                      │                                          │
│                      ▼                                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Domain Event Handlers                                  │   │
│  │                                                          │   │
│  │  RegistrationConfirmedEventHandler.Handle():            │   │
│  │  1. Create confirmation email content                   │   │
│  │  2. Create OutboxMessage (Status=Pending)               │   │
│  │  3. Save to database                                    │   │
│  └────────────────┬────────────────────────────────────────┘   │
│                   │                                            │
└───────────────────┼────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    PostgreSQL Database                          │
│                                                                 │
│  Tables:                                                        │
│  - Events                                                       │
│  - Registrations (CreatedAt, UpdatedAt, Status, UserId)        │
│  - OutboxMessages (Type, Status, Content, CreatedAt)           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                    │
                    │ Background Job (Hangfire)
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│              EmailQueueProcessor (Recurring Job)                │
│                                                                 │
│  - Runs every 30 seconds                                       │
│  - Queries OutboxMessages WHERE Status=Pending                 │
│  - Sends emails via SendGrid                                   │
│  - Updates OutboxMessage Status=Sent/Failed                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SendGrid (Email Service)                   │
│                                                                 │
│  - Receives email requests via API                             │
│  - Delivers confirmation emails to users                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Sequence Diagram: Registration Flow

```
User          Frontend        API              Application        Domain           Infrastructure    OutboxMessages    EmailProcessor    SendGrid
│                │              │                    │               │                    │                │                │              │
│ Click         │              │                    │               │                    │                │                │              │
│ Register      │              │                    │               │                    │                │                │              │
├──────────────►│              │                    │               │                    │                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │ POST /rsvp  │                    │               │                    │                │                │              │
│                ├─────────────►│                    │               │                    │                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │              │ RsvpToEventCommand │               │                    │                │                │              │
│                │              ├───────────────────►│               │                    │                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │              │                    │ Load Event    │                    │                │                │              │
│                │              │                    ├──────────────►│                    │                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │              │                    │               │ AddRegistration()  │                │                │              │
│                │              │                    │               ├───────────────────►│                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │              │                    │               │  Creates Registration entity       │                │              │
│                │              │                    │               │  Raises RegistrationConfirmedEvent │                │              │
│                │              │                    │               │◄───────────────────┤                │                │              │
│                │              │                    │               │                    │                │                │              │
│                │              │                    │ Save Event    │                    │                │                │              │
│                │              │                    ├───────────────┴───────────────────►│                │                │              │
│                │              │                    │                                    │                │                │              │
│                │              │                    │                   ┌────────────────┤                │                │              │
│                │              │                    │                   │ PHASE 6A.34    │                │                │              │
│                │              │                    │                   │ DetectChanges()│                │                │              │
│                │              │                    │                   └────────────────┤                │                │              │
│                │              │                    │                                    │                │                │              │
│                │              │                    │              Dispatch Domain Events│                │                │              │
│                │              │                    │◄───────────────────────────────────┤                │                │              │
│                │              │                    │                                    │                │                │              │
│                │              │  RegistrationConfirmedEvent                             │                │                │              │
│                │              │  ├──────────────────────────────────────────────────────┤                │                │              │
│                │              │  │                                                      │                │                │              │
│                │              │  │ RegistrationConfirmedEventHandler.Handle()           │                │                │              │
│                │              │  │ - Create email content                               │                │                │              │
│                │              │  │ - Create OutboxMessage                               │                │                │              │
│                │              │  ├─────────────────────────────────────────────────────►│                │                │              │
│                │              │  │                                           INSERT INTO OutboxMessages  │                │              │
│                │              │  │                                           Status=Pending              │                │              │
│                │              │  │                                                      ├───────────────►│                │              │
│                │              │  │                                                      │                │                │              │
│                │              │                                    SaveChangesAsync()   │                │                │              │
│                │              │                                    ├───────────────────►│                │                │              │
│                │              │                                    │                    │                │                │              │
│                │              │                      200 OK        │                    │                │                │              │
│                │              │◄───────────────────────────────────┤                    │                │                │              │
│                │              │                                    │                    │                │                │              │
│                │  200 OK     │                                    │                    │                │                │              │
│                │◄─────────────┤                                    │                    │                │                │              │
│                │              │                                    │                    │                │                │              │
│  Show success │              │                                    │                    │                │                │              │
│◄───────────────┤              │                                    │                    │                │                │              │
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │      30s later │                │              │
│                │              │                                    │                    │                │  Process()     │              │
│                │              │                                    │                    │                │◄───────────────┤              │
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │      SELECT * FROM OutboxMessages│              │
│                │              │                                    │                    │      WHERE Status=Pending        │              │
│                │              │                                    │                    │◄───────────────────────────────┤              │
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │      OutboxMessages              │              │
│                │              │                                    │                    ├─────────────────────────────────►│              │
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │                │  Send Email    │              │
│                │              │                                    │                    │                │                ├─────────────►│
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │                │                │  Email Sent  │
│                │              │                                    │                    │                │                │◄─────────────┤
│                │              │                                    │                    │                │                │              │
│                │              │                                    │                    │      UPDATE OutboxMessages       │              │
│                │              │                                    │                    │      SET Status=Sent             │              │
│                │              │                                    │                    │◄───────────────────────────────┤              │
│                │              │                                    │                    │                │                │              │
│ Receive email ◄───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                │              │                                    │                    │                │                │              │
```

---

## Phase 6A.34 Fix: Before vs After

### BEFORE Phase 6A.34 (BROKEN)

```csharp
public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
{
    // ❌ PROBLEM: No DetectChanges() call
    // EF Core doesn't know about entity changes made in domain aggregate

    // Dispatch domain events
    await _domainEventDispatcher.DispatchEventsAsync(this, cancellationToken);

    // Save changes
    return await SaveChangesAsync(cancellationToken) > 0;
}
```

**Result**:
- Registration entity created but NOT tracked by EF Core
- Domain events dispatched
- Event handler creates OutboxMessage
- SaveChangesAsync() saves ONLY OutboxMessage (not Registration)
- Registration lost!

### AFTER Phase 6A.34 (FIXED)

```csharp
public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
{
    // ✅ FIX: Call DetectChanges() BEFORE dispatching domain events
    // This ensures EF Core tracks ALL entity changes
    ChangeTracker.DetectChanges();

    // Dispatch domain events
    await _domainEventDispatcher.DispatchEventsAsync(this, cancellationToken);

    // Save changes
    return await SaveChangesAsync(cancellationToken) > 0;
}
```

**Result**:
- Registration entity created AND tracked by EF Core
- Domain events dispatched
- Event handler creates OutboxMessage
- SaveChangesAsync() saves BOTH Registration AND OutboxMessage
- Everything persisted correctly!

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         HTTP Request                            │
│  POST /api/events/c1f182a9-c957.../rsvp                        │
│  Authorization: Bearer eyJ...                                   │
│  Content-Type: application/json                                 │
│  Body: {}                                                       │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
                    Extract userId from JWT
                    Extract eventId from URL
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   RsvpToEventCommand                            │
│  {                                                              │
│    EventId: c1f182a9-c957-4a78-a0b2-085917a88900               │
│    UserId: abc123-def456-...                                    │
│  }                                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
              Load Event aggregate from database
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Event Aggregate                            │
│  {                                                              │
│    Id: c1f182a9-c957-4a78-a0b2-085917a88900                    │
│    Title: "Sri Lankan New Year Celebration 2025"               │
│    Status: Published                                            │
│    MaxCapacity: 100                                             │
│    CurrentRegistrations: 45                                     │
│    _registrations: [...]                                        │
│    _domainEvents: []                                            │
│  }                                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
           Call event.AddRegistration(userId)
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                  New Registration Entity                        │
│  {                                                              │
│    Id: def789-ghi012-... (generated)                           │
│    EventId: c1f182a9-c957-4a78-a0b2-085917a88900               │
│    UserId: abc123-def456-...                                    │
│    Status: Confirmed                                            │
│    Quantity: 1                                                  │
│    CreatedAt: 2025-12-18T14:30:15.123Z                         │
│    UpdatedAt: null                                              │
│  }                                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
     Added to event._registrations collection
     Raised RegistrationConfirmedEvent
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              RegistrationConfirmedEvent (Domain Event)          │
│  {                                                              │
│    EventId: c1f182a9-c957-4a78-a0b2-085917a88900               │
│    UserId: abc123-def456-...                                    │
│    RegistrationId: def789-ghi012-...                           │
│    RegisteredAt: 2025-12-18T14:30:15.123Z                      │
│  }                                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
            AppDbContext.CommitAsync()
                           │
            ┌──────────────┴──────────────┐
            │                             │
            ▼                             ▼
    DetectChanges()              Dispatch Domain Events
    (Phase 6A.34)                        │
            │                             │
            ▼                             ▼
    Track Registration Entity    RegistrationConfirmedEventHandler
            │                             │
            │                             ▼
            │                  Create OutboxMessage
            │                             │
            │                             ▼
            │             ┌─────────────────────────────────────────┐
            │             │        OutboxMessage Entity              │
            │             │  {                                      │
            │             │    Id: xyz123-abc456-...                │
            │             │    Type: "RegistrationConfirmedEvent"   │
            │             │    Status: Pending                      │
            │             │    Content: { eventId, userId, ... }    │
            │             │    CreatedAt: 2025-12-18T14:30:15.456Z │
            │             │  }                                      │
            │             └─────────────────┬───────────────────────┘
            │                               │
            └───────────────┬───────────────┘
                            │
                            ▼
                 SaveChangesAsync()
                            │
            ┌───────────────┴───────────────┐
            │                               │
            ▼                               ▼
    INSERT INTO Registrations    INSERT INTO OutboxMessages
            │                               │
            └───────────────┬───────────────┘
                            │
                            ▼
                    COMMIT TRANSACTION
                            │
                            ▼
                    Return 200 OK
```

---

## State Diagram: Registration Status

```
                   ┌─────────────┐
                   │   Pending   │
                   │  (for paid  │
                   │   events)   │
                   └──────┬──────┘
                          │
           Payment        │         Payment
           Success        │         Failed
                ┌─────────┴─────────┐
                │                   │
                ▼                   ▼
         ┌──────────┐        ┌─────────────┐
         │Confirmed │        │  Cancelled  │
         └────┬─────┘        └─────────────┘
              │
              │ Event capacity full
              ▼
        ┌─────────────┐
        │ Waitlisted  │
        └──────┬──────┘
               │
               │ Spot available
               ▼
        ┌──────────┐
        │Confirmed │
        └────┬─────┘
             │
             │ User arrives at event
             ▼
       ┌──────────┐
       │CheckedIn │
       └────┬─────┘
            │
            │ Event ends
            ▼
      ┌──────────┐
      │Completed │
      └──────────┘
```

---

## Technology Stack

### Frontend
- **Framework**: Next.js 14.2.20
- **UI Library**: React 18
- **State Management**: React hooks + Context API
- **HTTP Client**: Fetch API
- **Authentication**: JWT tokens in localStorage

### Backend
- **Framework**: ASP.NET Core 8.0
- **Architecture Pattern**: Clean Architecture + DDD
- **CQRS**: MediatR
- **ORM**: Entity Framework Core 9.0
- **Database**: PostgreSQL (Azure Database for PostgreSQL)
- **Caching**: Redis (Azure Cache for Redis)
- **Background Jobs**: Hangfire
- **Logging**: Serilog

### Infrastructure
- **Hosting**: Azure Container Apps
- **Database**: Azure PostgreSQL Flexible Server
- **Cache**: Azure Cache for Redis
- **Email**: SendGrid
- **Payments**: Stripe
- **Monitoring**: Azure Application Insights (planned)

---

## Quality Attributes

### Performance
- **Response Time**: < 200ms for API requests (p95)
- **Database Queries**: Indexed on EventId, UserId, Status
- **Caching**: Redis cache for frequently accessed data
- **Background Processing**: Asynchronous email sending

### Scalability
- **Horizontal**: Azure Container Apps auto-scaling
- **Database**: Connection pooling, prepared statements
- **Stateless**: API is stateless for easy scaling

### Reliability
- **Domain Events**: Transactional Outbox pattern
- **Email Delivery**: Retry mechanism in EmailQueueProcessor
- **Database**: Azure PostgreSQL with automatic backups
- **Health Checks**: /health endpoint for monitoring

### Security
- **Authentication**: JWT tokens with expiration
- **Authorization**: Role-based access control (RBAC)
- **HTTPS**: Enforced in production
- **CORS**: Configured per environment
- **Input Validation**: FluentValidation

### Maintainability
- **Clean Architecture**: Clear separation of concerns
- **DDD**: Rich domain model with business logic encapsulation
- **SOLID Principles**: Applied throughout codebase
- **Testing**: Unit tests, integration tests (planned)

---

## Architectural Decision Records (ADRs)

### ADR-001: Use Transactional Outbox Pattern for Domain Events

**Context**: Domain events need to trigger side effects (emails, notifications) reliably.

**Decision**: Implement Transactional Outbox pattern with OutboxMessages table.

**Consequences**:
- ✅ Guarantees at-least-once delivery
- ✅ Survives database transaction failures
- ✅ Decouples domain logic from email sending
- ❌ Requires background job processor
- ❌ Slight delay in email delivery (acceptable)

### ADR-002: Call DetectChanges() Before Domain Event Dispatching

**Context**: EF Core wasn't tracking entity changes made in domain aggregates before domain events were dispatched, causing data loss.

**Decision**: Add `ChangeTracker.DetectChanges()` call in `AppDbContext.CommitAsync()` before dispatching domain events (Phase 6A.34).

**Consequences**:
- ✅ Ensures all entity changes are tracked
- ✅ Fixes registration not being saved
- ✅ No performance impact (DetectChanges is called anyway in SaveChanges)
- ✅ More explicit and predictable behavior

### ADR-003: Use CQRS Pattern with MediatR

**Context**: Application logic needs clear separation between queries and commands.

**Decision**: Implement CQRS pattern using MediatR for command/query handling.

**Consequences**:
- ✅ Clear separation of concerns
- ✅ Easy to add pipeline behaviors (validation, logging)
- ✅ Testable handlers
- ❌ More classes/files
- ❌ Learning curve for team

---

## Deployment Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                     Azure Cloud                               │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  Azure Container Apps Environment                        │ │
│  │                                                          │ │
│  │  ┌────────────────────────────────────────────────────┐ │ │
│  │  │  lankaconnect (Container App)                      │ │ │
│  │  │  - Min: 0 replicas                                 │ │ │
│  │  │  - Max: 10 replicas                                │ │ │
│  │  │  - CPU: 0.5 cores                                  │ │ │
│  │  │  - Memory: 1.0 Gi                                  │ │ │
│  │  │  - Revision: 0000313 (100% traffic)                │ │ │
│  │  └────────────────────────────────────────────────────┘ │ │
│  │                                                          │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           │                                   │
│       ┌───────────────────┼───────────────────┐               │
│       │                   │                   │               │
│       ▼                   ▼                   ▼               │
│  ┌──────────┐    ┌────────────────┐    ┌──────────────┐     │
│  │PostgreSQL│    │ Redis Cache    │    │  SendGrid    │     │
│  │Flexible  │    │                │    │  (External)  │     │
│  │Server    │    │ - 250 MB RAM   │    │              │     │
│  │          │    │ - Basic tier   │    └──────────────┘     │
│  │- 2 vCores│    └────────────────┘                          │
│  │- 8 GB RAM│                                                │
│  │- 32 GB   │                                                │
│  │  storage │                                                │
│  └──────────┘                                                │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## Monitoring & Observability

### Current State

**Logging**:
- ✅ Serilog with structured logging
- ✅ Console sink for Azure Container App logs
- ✅ Correlation ID tracking
- ✅ Request logging with enrichment

**Limitations**:
- ❌ Log retention limited to ~300 lines
- ❌ No UTC timestamps in some log entries
- ❌ No centralized log aggregation
- ❌ No Application Insights integration

### Recommended Enhancements

1. **Azure Log Analytics**
   - Longer log retention (30+ days)
   - Advanced querying with KQL
   - Alerting on errors

2. **Application Insights**
   - Distributed tracing
   - Performance monitoring
   - Dependency tracking
   - Custom metrics

3. **Health Checks**
   - Already implemented: `/health` endpoint
   - Add to Azure Container App health probes
   - Monitor database, cache, SendGrid connectivity

4. **Alerts**
   - Failed registrations
   - Email send failures
   - Database connection issues
   - High error rates

---

## Conclusion

Phase 6A.34 implements a critical fix to the event registration flow by ensuring EF Core properly tracks entity changes before domain events are dispatched. This fix, combined with the Transactional Outbox pattern (Phase 6A.24), provides a reliable and maintainable solution for event-driven side effects in the system.

**Key Takeaways**:
1. Always call `DetectChanges()` when using domain events with EF Core
2. Transactional Outbox pattern ensures reliable event processing
3. Structured logging with correlation IDs aids debugging
4. Consider log retention policies for production systems
5. Architecture supports scalability and maintainability

---

## References

- **Phase 6A.24**: Transactional Outbox Pattern implementation
- **Phase 6A.34**: DetectChanges() fix for entity tracking
- **EF Core Documentation**: https://learn.microsoft.com/en-us/ef/core/
- **Domain Events**: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation
- **Transactional Outbox**: https://microservices.io/patterns/data/transactional-outbox.html

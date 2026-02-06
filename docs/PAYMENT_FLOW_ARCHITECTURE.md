# Payment Flow Architecture

## System Context Diagram (C4 Level 1)

```
┌─────────────────────────────────────────────────────────────────────┐
│                          External Systems                            │
│                                                                       │
│  ┌──────────┐              ┌──────────────┐                         │
│  │  User    │   ────────▶  │   Stripe     │                         │
│  │ Browser  │   Checkout   │   Checkout   │                         │
│  └──────────┘              └──────────────┘                         │
│                                    │                                 │
│                                    │ Webhook                         │
│                                    │ (checkout.session.completed)    │
│                                    ▼                                 │
│                         ┌─────────────────────┐                     │
│                         │  LankaConnect API   │                     │
│                         │  (Azure App Service)│                     │
│                         └─────────────────────┘                     │
│                                    │                                 │
│                                    │ Read/Write                      │
│                                    ▼                                 │
│                         ┌─────────────────────┐                     │
│                         │   PostgreSQL DB     │                     │
│                         │   (Azure Database)  │                     │
│                         └─────────────────────┘                     │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Container Diagram (C4 Level 2)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     LankaConnect API (Azure App Service)                  │
│                                                                            │
│  ┌──────────────────────┐        ┌──────────────────────┐               │
│  │ PaymentsController   │        │  AppDbContext        │               │
│  │ - Webhook()          │───────▶│  - CommitAsync()     │               │
│  │ - HandleCheckout...  │        │  - DispatchEvents()  │               │
│  └──────────────────────┘        └──────────────────────┘               │
│           │                                  │                            │
│           │ Calls                            │ Dispatches                 │
│           ▼                                  ▼                            │
│  ┌──────────────────────┐        ┌──────────────────────┐               │
│  │ Registration Entity  │        │     MediatR          │               │
│  │ - CompletePayment()  │        │   IPublisher         │               │
│  │ - RaiseDomainEvent() │        └──────────────────────┘               │
│  └──────────────────────┘                   │                            │
│                                              │ Routes to                  │
│                                              ▼                            │
│                            ┌───────────────────────────────┐             │
│                            │ PaymentCompletedEventHandler  │             │
│                            │ - Handle()                    │             │
│                            │ - GenerateTicket()            │             │
│                            │ - SendEmail()                 │             │
│                            └───────────────────────────────┘             │
│                                     │            │                        │
│                                     │            │                        │
│                       ┌─────────────┘            └──────────────┐        │
│                       ▼                                          ▼        │
│            ┌──────────────────┐                     ┌─────────────────┐  │
│            │  TicketService   │                     │  EmailService   │  │
│            │ - GenerateAsync()│                     │  - SendAsync()  │  │
│            │ - GetPdfAsync()  │                     └─────────────────┘  │
│            └──────────────────┘                              │            │
│                     │                                        │            │
│                     │                                        │            │
└─────────────────────┼────────────────────────────────────────┼───────────┘
                      │                                        │
                      ▼                                        ▼
           ┌───────────────────┐                   ┌──────────────────┐
           │  PostgreSQL DB    │                   │ Azure Email      │
           │  tickets table    │                   │ Communication    │
           └───────────────────┘                   └──────────────────┘
```

## Component Diagram (C4 Level 3) - Webhook Processing

```
                                    Stripe Webhook POST
                                           │
                                           │ /api/payments/webhook
                                           ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                         PaymentsController                                │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ Webhook() - Line 221-284                                        │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 1. Read request body and Stripe-Signature header          │  │    │
│  │ │    Location: Line 227-228                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 2. Verify signature with Stripe library                   │  │    │
│  │ │    EventUtility.ConstructEvent()                          │  │    │
│  │ │    Location: Line 232-237                                 │  │    │
│  │ │    ❌ FAIL → Return 400 Bad Request                       │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 3. Check idempotency                                       │  │    │
│  │ │    _webhookEventRepository.IsEventProcessedAsync()        │  │    │
│  │ │    Location: Line 242-246                                 │  │    │
│  │ │    ✅ Already processed → Return 200 OK (skip)           │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 4. Record event in database                                │  │    │
│  │ │    _webhookEventRepository.RecordEventAsync()             │  │    │
│  │ │    Location: Line 249                                     │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 5. Route by event type                                     │  │    │
│  │ │    switch (stripeEvent.Type)                              │  │    │
│  │ │    Location: Line 252-266                                 │  │    │
│  │ │    case "checkout.session.completed":                     │  │    │
│  │ │      → HandleCheckoutSessionCompletedAsync()              │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 6. Mark as processed                                       │  │    │
│  │ │    _webhookEventRepository.MarkEventAsProcessedAsync()    │  │    │
│  │ │    Location: Line 269                                     │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │                     Return 200 OK                                │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                            │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ HandleCheckoutSessionCompletedAsync() - Line 289-375            │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 1. Extract session data from webhook                      │  │    │
│  │ │    var session = stripeEvent.Data.Object as Session       │  │    │
│  │ │    Location: Line 293-298                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 2. Verify payment_status = "paid"                         │  │    │
│  │ │    if (session.PaymentStatus != "paid") return;           │  │    │
│  │ │    Location: Line 306-310                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 3. Extract metadata                                        │  │    │
│  │ │    registration_id from session.Metadata                  │  │    │
│  │ │    event_id from session.Metadata                         │  │    │
│  │ │    Location: Line 313-326                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 4. Load Event aggregate with registrations                │  │    │
│  │ │    var @event = await _eventRepository.GetByIdAsync()     │  │    │
│  │ │    Location: Line 333-338                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 5. Find registration in aggregate                          │  │    │
│  │ │    var registration = @event.Registrations.FirstOrDefault │  │    │
│  │ │    Location: Line 341-346                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 6. Call domain method                                      │  │    │
│  │ │    registration.CompletePayment(paymentIntentId)          │  │    │
│  │ │    Location: Line 349-359                                 │  │    │
│  │ │    ❌ FAIL → Log error and return                        │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 7. Commit changes (triggers event dispatch)               │  │    │
│  │ │    await _unitOfWork.CommitAsync()                        │  │    │
│  │ │    Location: Line 362                                     │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ UnitOfWork delegates to
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                         AppDbContext                                      │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ CommitAsync() - Line 294-366                                    │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 1. Update timestamps (CreatedAt, UpdatedAt)                │  │    │
│  │ │    Location: Line 297-308                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 2. Collect domain events from tracked entities             │  │    │
│  │ │    ChangeTracker.Entries<BaseEntity>()                    │  │    │
│  │ │      .Where(e => e.Entity.DomainEvents.Any())             │  │    │
│  │ │    Location: Line 310-322                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 3. Persist changes to database                             │  │    │
│  │ │    await SaveChangesAsync(cancellationToken)              │  │    │
│  │ │    Location: Line 325-326                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 4. Dispatch domain events via MediatR                      │  │    │
│  │ │    foreach (var domainEvent in domainEvents)              │  │    │
│  │ │    {                                                       │  │    │
│  │ │      var notification = new DomainEventNotification<>()   │  │    │
│  │ │      await _publisher.Publish(notification)               │  │    │
│  │ │    }                                                       │  │    │
│  │ │    Location: Line 329-359                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 5. Clear domain events from entities                       │  │    │
│  │ │    entry.Entity.ClearDomainEvents()                       │  │    │
│  │ │    Location: Line 353-356                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ MediatR routes to
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                  PaymentCompletedEventHandler                             │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ Handle() - Line 43-242                                          │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 1. Extract event data                                      │  │    │
│  │ │    var domainEvent = notification.DomainEvent             │  │    │
│  │ │    Location: Line 45-49                                   │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 2. Load Event and Registration from database               │  │    │
│  │ │    var @event = await _eventRepository.GetByIdAsync()     │  │    │
│  │ │    var registration = @event.Registrations.FirstOrDefault │  │    │
│  │ │    Location: Line 53-68                                   │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 3. Determine recipient (user or anonymous)                 │  │    │
│  │ │    if (domainEvent.UserId.HasValue)                       │  │    │
│  │ │      get user from repository                             │  │    │
│  │ │    else use attendee info                                 │  │    │
│  │ │    Location: Line 70-96                                   │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 4. Prepare email parameters                                │  │    │
│  │ │    Dictionary with event details, payment info, etc.      │  │    │
│  │ │    Location: Line 113-143                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 5. Generate ticket with QR code                            │  │    │
│  │ │    var ticketResult =                                     │  │    │
│  │ │      await _ticketService.GenerateTicketAsync()           │  │    │
│  │ │    Location: Line 146-176                                 │  │    │
│  │ │    ❌ FAIL → Log warning, continue without ticket        │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 6. Get ticket PDF for attachment                           │  │    │
│  │ │    var pdfResult =                                        │  │    │
│  │ │      await _ticketService.GetTicketPdfAsync()             │  │    │
│  │ │    Location: Line 161-170                                 │  │    │
│  │ │    ❌ FAIL → Log warning, continue without PDF          │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 7. Render email templates                                  │  │    │
│  │ │    Subject: ticket-confirmation-subject                   │  │    │
│  │ │    HTML: ticket-confirmation-html                         │  │    │
│  │ │    Text: ticket-confirmation-text                         │  │    │
│  │ │    Location: Line 179-196                                 │  │    │
│  │ │    ❌ FAIL → Log error and return                        │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 8. Build email message with PDF attachment                 │  │    │
│  │ │    new EmailMessageDto with Attachments list              │  │    │
│  │ │    Location: Line 199-217                                 │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  │                          ▼                                       │    │
│  │ ┌───────────────────────────────────────────────────────────┐  │    │
│  │ │ 9. Send email via email service                            │  │    │
│  │ │    var result = await _emailService.SendEmailAsync()      │  │    │
│  │ │    Location: Line 220-232                                 │  │    │
│  │ │    ❌ FAIL → Log error (fail-silent pattern)            │  │    │
│  │ └───────────────────────────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                            │
│  Note: All exceptions caught at Line 235-241 (fail-silent pattern)       │
└──────────────────────────────────────────────────────────────────────────┘
```

## Sequence Diagram

```
User          Stripe        PaymentsController      Registration      AppDbContext       MediatR        EventHandler      TicketService    EmailService
 │               │                 │                      │                 │                │                 │                  │                │
 │   Checkout    │                 │                      │                 │                │                 │                  │                │
 ├──────────────▶│                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │  POST /webhook  │                      │                 │                │                 │                  │                │
 │               ├────────────────▶│                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │ Verify Signature     │                 │                │                 │                  │                │
 │               │                 ├──────────┐           │                 │                │                 │                  │                │
 │               │                 │          │           │                 │                │                 │                  │                │
 │               │                 │◀─────────┘           │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │ Check Idempotency    │                 │                │                 │                  │                │
 │               │                 ├──────────┐           │                 │                │                 │                  │                │
 │               │                 │          │           │                 │                │                 │                  │                │
 │               │                 │◀─────────┘           │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │ Record Event         │                 │                │                 │                  │                │
 │               │                 ├──────────┐           │                 │                │                 │                  │                │
 │               │                 │          │           │                 │                │                 │                  │                │
 │               │                 │◀─────────┘           │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │ CompletePayment()    │                 │                │                 │                  │                │
 │               │                 ├─────────────────────▶│                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │ Update State    │                │                 │                  │                │
 │               │                 │                      ├────────┐        │                │                 │                  │                │
 │               │                 │                      │        │        │                │                 │                  │                │
 │               │                 │                      │◀───────┘        │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │ RaiseDomainEvent│                │                 │                  │                │
 │               │                 │                      ├────────┐        │                │                 │                  │                │
 │               │                 │                      │        │        │                │                 │                  │                │
 │               │                 │                      │◀───────┘        │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │       Result         │                 │                │                 │                  │                │
 │               │                 │◀─────────────────────┤                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │ CommitAsync()        │                 │                │                 │                  │                │
 │               │                 ├─────────────────────────────────────▶ │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │ Collect Events │                │                 │                  │
 │               │                 │                      │                 ├───────┐        │                 │                  │                │
 │               │                 │                      │                 │       │        │                 │                  │                │
 │               │                 │                      │                 │◀──────┘        │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │ SaveChanges    │                │                 │                  │
 │               │                 │                      │                 ├───────┐        │                 │                  │                │
 │               │                 │                      │                 │       │        │                 │                  │                │
 │               │                 │                      │                 │◀──────┘        │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │ Publish(event) │                │                 │                  │
 │               │                 │                      │                 ├───────────────▶│                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │  Handle(event)  │                  │                │
 │               │                 │                      │                 │                ├────────────────▶│                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │ GenerateTicket() │                │
 │               │                 │                      │                 │                │                 ├─────────────────▶│                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │  Ticket + PDF    │                │
 │               │                 │                      │                 │                │                 │◀─────────────────┤                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │  SendEmailAsync()│                │
 │               │                 │                      │                 │                │                 ├─────────────────────────────────▶│
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │      Result      │                │
 │               │                 │                      │                 │                │                 │◀─────────────────────────────────┤
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │      Done       │                  │                │
 │               │                 │                      │                 │                │◀────────────────┤                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │      Done      │                │                 │                  │
 │               │                 │                      │                 │◀───────────────┤                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │                 │          Done        │                 │                │                 │                  │                │
 │               │                 │◀─────────────────────────────────────┤                 │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
 │               │      200 OK     │                      │                 │                │                 │                  │                │
 │               │◀────────────────┤                      │                 │                │                 │                  │                │
 │               │                 │                      │                 │                │                 │                  │                │
```

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Stripe Webhook Payload                                │
│ {                                                                         │
│   "id": "evt_xxx",                                                       │
│   "type": "checkout.session.completed",                                 │
│   "data": {                                                              │
│     "object": {                                                          │
│       "id": "cs_xxx",                                                    │
│       "payment_status": "paid",                                          │
│       "payment_intent": "pi_xxx",                                        │
│       "metadata": {                                                      │
│         "registration_id": "guid",                                       │
│         "event_id": "guid"                                               │
│       }                                                                  │
│     }                                                                    │
│   }                                                                      │
│ }                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  Database: stripe_webhook_events                         │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ id              | stripe_event_id | event_type              | ...   │ │
│ │─────────────────┼─────────────────┼─────────────────────────┼───────│ │
│ │ 1               | evt_xxx         | checkout.session.compl.. | ...   │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              Database: events.registrations (BEFORE)                     │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ id   | payment_status | status  | stripe_payment_intent_id | ...   │ │
│ │──────┼────────────────┼─────────┼──────────────────────────┼───────│ │
│ │ guid | Pending        | Pending | NULL                     | ...   │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Domain Event: PaymentCompletedEvent                   │
│ {                                                                         │
│   "EventId": "guid",                                                     │
│   "RegistrationId": "guid",                                              │
│   "UserId": "guid",                                                      │
│   "ContactEmail": "user@example.com",                                    │
│   "PaymentIntentId": "pi_xxx",                                           │
│   "AmountPaid": 50.00,                                                   │
│   "AttendeeCount": 2,                                                    │
│   "PaymentCompletedAt": "2025-12-17T10:30:00Z"                          │
│ }                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              Database: events.registrations (AFTER)                      │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ id   | payment_status | status    | stripe_payment_intent_id| ...  │ │
│ │──────┼────────────────┼───────────┼─────────────────────────┼──────│ │
│ │ guid | Completed      | Confirmed | pi_xxx                  | ...  │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      Database: events.tickets                            │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ id   | registration_id | ticket_code | qr_code_data | status | ... │ │
│ │──────┼─────────────────┼─────────────┼──────────────┼────────┼─────│ │
│ │ guid | guid            | TKT-ABC123  | base64...    | Valid  | ... │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                Database: communications.email_messages                   │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ id | to_email         | subject         | status | attachments | ...│ │
│ │────┼──────────────────┼─────────────────┼────────┼─────────────┼───│ │
│ │ 1  | user@example.com | Ticket Confirm..| Sent   | [PDF]       | ...│ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                            User receives email with:
                            - Event confirmation
                            - Payment receipt
                            - PDF ticket with QR code
```

## Technology Stack

- **API Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL (Azure Database for PostgreSQL)
- **ORM**: Entity Framework Core 8.0
- **Domain Events**: MediatR (IPublisher pattern)
- **Payment Processing**: Stripe SDK (Stripe.net)
- **Email Service**: Azure Communication Services
- **QR Code Generation**: QRCoder or similar
- **PDF Generation**: PdfSharpCore or iTextSharp
- **Hosting**: Azure App Service (Linux container)

## Key Design Patterns

1. **Clean Architecture**: Separation of concerns across layers (API → Application → Domain → Infrastructure)
2. **Domain-Driven Design**: Rich domain models with business logic encapsulation
3. **Domain Events**: Decoupled side-effects using event-driven architecture
4. **Repository Pattern**: Abstract data access behind interfaces
5. **Unit of Work**: Transaction management via AppDbContext.CommitAsync()
6. **CQRS Lite**: Commands update state, events trigger side effects
7. **Fail-Silent**: Event handlers catch exceptions to prevent transaction rollback
8. **Idempotency**: Webhook events tracked to prevent duplicate processing

## Error Handling Strategy

### Webhook Controller (PaymentsController)
- Signature verification failure → 400 Bad Request
- Idempotency hit → 200 OK (skip processing)
- Application exception → 500 Internal Server Error
- All errors logged with detailed context

### Domain Method (Registration.CompletePayment)
- Invalid state → Result.Failure (not exception)
- Validates PaymentStatus is Pending
- Returns detailed error message

### Event Handler (PaymentCompletedEventHandler)
- **Fail-Silent Pattern**: Catch all exceptions
- Log errors but don't throw (prevents transaction rollback)
- Partial success allowed (e.g., ticket generated but email fails)
- Each step (ticket, email) independently caught and logged

## Monitoring & Observability

### Key Log Points
1. Webhook received: "Processing webhook event {EventId}"
2. Payment completed: "Successfully completed payment for Event {EventId}"
3. Domain events collected: "Found {Count} domain events to dispatch"
4. Domain event dispatched: "Dispatching domain event: PaymentCompletedEvent"
5. Handler invoked: "PaymentCompletedEventHandler INVOKED"
6. Ticket generated: "Ticket generated successfully: {TicketCode}"
7. Email sent: "Payment confirmation email sent successfully to {Email}"

### Error Log Patterns
- "Stripe webhook signature verification failed"
- "Event {EventId} already processed"
- "Failed to complete payment for Registration {RegistrationId}"
- "Failed to generate ticket: {Error}"
- "Failed to send payment confirmation email: {Errors}"
- "Error handling PaymentCompletedEvent"

### Metrics to Track
- Webhook receipt rate (per minute)
- Webhook processing success rate (%)
- Time from webhook receipt to email sent (ms)
- Ticket generation success rate (%)
- Email delivery success rate (%)
- Registrations stuck in Pending status (count)

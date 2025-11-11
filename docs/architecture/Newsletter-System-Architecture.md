# Newsletter System Architecture

## Overview
Architectural design for production-ready newsletter subscription system following Clean Architecture and DDD patterns.

## 1. Metro Area Storage Strategy

### Decision: Database-Backed with API Caching

**Architecture:**
```
┌─────────────────────────────────────────────────────────────┐
│                    Metro Area System                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Database (metro_areas) → API Endpoint → HTTP Cache         │
│       ↓                        ↓              ↓             │
│  Admin Portal          Frontend Cache    localStorage        │
│  (CRUD metros)         (In-memory)       (Offline fallback)  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Performance Optimization:**
- HTTP Cache-Control: `max-age=86400` (24 hours)
- Frontend in-memory cache with 1-hour TTL
- localStorage persistence for offline scenarios
- Background refresh on cache expiration

**API Endpoint:**
```
GET /api/v1/metro-areas
Response: {
  "metros": [
    {
      "id": "uuid",
      "code": "cleveland-oh",
      "displayName": "Cleveland, OH",
      "stateCode": "OH",
      "timezone": "America/New_York"
    }
  ],
  "cacheMaxAge": 86400
}
```

---

## 2. Database Schema

### Metro Areas Table
```sql
CREATE TABLE metro_areas (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    code VARCHAR(20) NOT NULL UNIQUE,
    display_name NVARCHAR(100) NOT NULL,
    state_code CHAR(2) NOT NULL,
    country_code CHAR(2) NOT NULL DEFAULT 'US',
    timezone VARCHAR(50) NOT NULL,
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    is_active BIT NOT NULL DEFAULT 1,
    display_order INT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### Newsletter Subscribers Table
```sql
CREATE TABLE newsletter_subscribers (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    email NVARCHAR(255) NOT NULL,
    metro_area_id UNIQUEIDENTIFIER NULL,
    receive_all_locations BIT NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    is_confirmed BIT NOT NULL DEFAULT 0,
    confirmation_token VARCHAR(100) NULL,
    confirmation_sent_at DATETIME2 NULL,
    confirmed_at DATETIME2 NULL,
    unsubscribe_token VARCHAR(100) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_newsletter_metro_area
        FOREIGN KEY (metro_area_id) REFERENCES metro_areas(id),
    CONSTRAINT UQ_newsletter_email_metro
        UNIQUE (email, metro_area_id)
);
```

### Newsletter Deliveries Table
```sql
CREATE TABLE newsletter_deliveries (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    subscriber_id UNIQUEIDENTIFIER NOT NULL,
    sent_at DATETIME2 NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
    events_included INT NOT NULL,
    delivery_status VARCHAR(20) NOT NULL,
    opened_at DATETIME2 NULL,
    clicked_at DATETIME2 NULL,

    CONSTRAINT FK_newsletter_delivery_subscriber
        FOREIGN KEY (subscriber_id) REFERENCES newsletter_subscribers(id)
);
```

---

## 3. Domain-Driven Design Pattern

### Domain Layer Structure
```
Domain/
├── Entities/
│   ├── NewsletterSubscriber.cs (Aggregate Root)
│   ├── NewsletterDelivery.cs
│   └── MetroArea.cs (Aggregate Root)
├── ValueObjects/
│   ├── EmailAddress.cs
│   ├── ConfirmationToken.cs
│   └── UnsubscribeToken.cs
├── DomainEvents/
│   ├── NewsletterSubscriptionCreatedEvent.cs
│   ├── NewsletterSubscriptionConfirmedEvent.cs
│   └── NewsletterSubscriptionCancelledEvent.cs
├── Repositories/
│   ├── INewsletterSubscriberRepository.cs
│   └── IMetroAreaRepository.cs
└── Services/
    └── INewsletterDomainService.cs
```

### NewsletterSubscriber Aggregate Root

**Responsibilities:**
- Encapsulate subscription business rules
- Generate confirmation/unsubscribe tokens
- Validate email confirmation workflow
- Raise domain events for side effects

**Key Methods:**
```csharp
public class NewsletterSubscriber : BaseEntity, IAggregateRoot
{
    // Factory method
    public static NewsletterSubscriber Create(
        EmailAddress email,
        MetroAreaId? metroAreaId,
        bool receiveAllLocations);

    // Business logic methods
    public void ConfirmSubscription(string token);
    public void Unsubscribe();
    public void Reactivate();
    public void ChangeMetroArea(MetroAreaId newMetroAreaId);

    // Domain events
    private void RaiseSubscriptionCreatedEvent();
    private void RaiseSubscriptionConfirmedEvent();
}
```

### Domain Events

**NewsletterSubscriptionCreatedEvent:**
- Triggers confirmation email sending
- Integrates with existing IEmailService

**NewsletterSubscriptionConfirmedEvent:**
- Updates analytics/metrics
- Triggers welcome newsletter

**NewsletterSubscriptionCancelledEvent:**
- Triggers unsubscribe confirmation email
- Updates metrics

---

## 4. Application Layer (CQRS Pattern)

### Commands
```
Application/UseCases/Newsletter/Commands/
├── SubscribeToNewsletterCommand.cs
├── ConfirmNewsletterSubscriptionCommand.cs
├── UnsubscribeFromNewsletterCommand.cs
└── ChangeNewsletterMetroAreaCommand.cs
```

### Queries
```
Application/UseCases/Newsletter/Queries/
├── GetActiveSubscribersQuery.cs
├── GetSubscriberByEmailQuery.cs
└── GetNewsletterDeliveryStatsQuery.cs
```

### Command Handler Example
```csharp
public class SubscribeToNewsletterCommandHandler
    : IRequestHandler<SubscribeToNewsletterCommand, Result<Guid>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IMetroAreaRepository _metroAreaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(
        SubscribeToNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate metro area exists
        var metroArea = await _metroAreaRepository
            .GetByIdAsync(request.MetroAreaId);

        if (metroArea == null)
            return Result<Guid>.Failure("Invalid metro area");

        // 2. Check for existing subscription
        var existing = await _repository
            .GetByEmailAndMetroAsync(request.Email, request.MetroAreaId);

        if (existing != null && existing.IsActive)
            return Result<Guid>.Failure("Already subscribed");

        // 3. Create aggregate
        var subscriber = NewsletterSubscriber.Create(
            EmailAddress.Create(request.Email),
            metroArea.Id,
            request.ReceiveAllLocations
        );

        // 4. Persist
        await _repository.AddAsync(subscriber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Domain event handler will send confirmation email

        return Result<Guid>.Success(subscriber.Id);
    }
}
```

---

## 5. Newsletter Sending Infrastructure

### Architecture
```
┌────────────────────────────────────────────────────────────┐
│              Newsletter Sending System                      │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐                                      │
│  │  Hangfire Job    │  (Weekly: Friday 9am local)          │
│  │  Scheduler       │                                      │
│  └────────┬─────────┘                                      │
│           │                                                 │
│           ▼                                                 │
│  ┌──────────────────────────────────┐                      │
│  │  Newsletter Aggregator Service   │                      │
│  │  - Group subscribers by timezone  │                      │
│  │  - Fetch events per metro area   │                      │
│  └────────┬─────────────────────────┘                      │
│           │                                                 │
│           ▼                                                 │
│  ┌──────────────────────────────────┐                      │
│  │  Newsletter Content Builder      │                      │
│  │  - Apply email templates         │                      │
│  │  - Cultural intelligence         │                      │
│  │  - Personalization               │                      │
│  └────────┬─────────────────────────┘                      │
│           │                                                 │
│           ▼                                                 │
│  ┌──────────────────────────────────┐                      │
│  │  Existing Email Infrastructure   │                      │
│  │  - IEmailService                 │                      │
│  │  - Retry logic                   │                      │
│  │  - Delivery tracking             │                      │
│  └──────────────────────────────────┘                      │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

### Scheduled Job Implementation

**Technology:** Hangfire (already in ecosystem for background jobs)

**Schedule:** Weekly Friday 9am (local timezone per metro)

```csharp
public class NewsletterSchedulingService : INewsletterSchedulingService
{
    public void ScheduleWeeklyNewsletters()
    {
        // Group metros by timezone
        var timezones = GetDistinctTimezones();

        foreach (var tz in timezones)
        {
            // Schedule job for 9am Friday in this timezone
            RecurringJob.AddOrUpdate(
                $"newsletter-{tz}",
                () => SendNewsletterForTimezone(tz),
                Cron.Weekly(DayOfWeek.Friday, 9), // 9am Friday
                new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(tz) }
            );
        }
    }

    public async Task SendNewsletterForTimezone(string timezone)
    {
        var metrosInTimezone = await _metroAreaRepository
            .GetByTimezoneAsync(timezone);

        foreach (var metro in metrosInTimezone)
        {
            await SendNewsletterForMetro(metro.Id);
        }
    }

    private async Task SendNewsletterForMetro(Guid metroAreaId)
    {
        // 1. Get confirmed subscribers for this metro
        var subscribers = await _subscriberRepository
            .GetConfirmedByMetroAreaAsync(metroAreaId);

        // 2. Fetch upcoming events for this metro (next 7 days)
        var events = await _eventRepository
            .GetUpcomingByMetroAreaAsync(metroAreaId, DateTime.UtcNow.AddDays(7));

        if (!events.Any())
            return; // Don't send empty newsletters

        // 3. Build newsletter content
        var content = await _contentBuilder.BuildNewsletterAsync(
            metroAreaId,
            events
        );

        // 4. Send to each subscriber
        foreach (var subscriber in subscribers)
        {
            var emailMessage = CreateNewsletterEmail(subscriber, content);

            await _emailService.SendAsync(emailMessage);

            // 5. Track delivery
            await TrackDeliveryAsync(subscriber.Id, content);
        }
    }
}
```

---

## 6. Email Confirmation Workflow

### Decision: **REQUIRED for Production**

**Legal Requirements:**
- CAN-SPAM Act compliance (US law)
- GDPR compliance (if any EU users)
- Double opt-in reduces spam complaints

**Workflow:**
```
User submits form
    ↓
Create subscriber (is_confirmed = false)
    ↓
Generate confirmation_token
    ↓
Send confirmation email (via domain event)
    ↓
User clicks confirmation link
    ↓
Validate token → Set is_confirmed = true
    ↓
Send welcome email
    ↓
Include in next newsletter batch
```

### Confirmation Email Template
```html
Subject: Confirm Your LankaConnect Newsletter Subscription

Hi there,

Please confirm your newsletter subscription for Cleveland, OH events.

[Confirm Subscription Button]
→ https://lankaconnect.com/newsletter/confirm?token={token}

If you didn't request this, simply ignore this email.

Unsubscribe: https://lankaconnect.com/newsletter/unsubscribe?token={unsubscribe_token}
```

### API Endpoint
```
POST /api/v1/newsletter/subscribe
{
  "email": "user@example.com",
  "metroAreaId": "uuid",
  "receiveAllLocations": false
}

Response: 201 Created
{
  "message": "Please check your email to confirm subscription",
  "requiresConfirmation": true
}

GET /api/v1/newsletter/confirm?token={confirmation_token}
Response: 200 OK → Redirect to success page
```

---

## 7. Production Readiness Checklist

### Infrastructure
- [ ] Hangfire configured for scheduled jobs
- [ ] Database migrations created and tested
- [ ] Email service tested with production SMTP
- [ ] Rate limiting on subscription API (prevent spam)
- [ ] CAPTCHA on subscription form (prevent bots)

### Security
- [ ] Email addresses hashed/encrypted at rest
- [ ] Confirmation tokens cryptographically secure (CSPRNG)
- [ ] Unsubscribe tokens unpredictable
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention in email templates

### Compliance
- [ ] CAN-SPAM Act compliance
  - [ ] Unsubscribe link in every email
  - [ ] Physical address in footer
  - [ ] Accurate subject lines
  - [ ] Honor unsubscribe within 10 days
- [ ] GDPR compliance (if applicable)
  - [ ] Clear consent collection
  - [ ] Right to erasure implementation
  - [ ] Data processing agreement

### Monitoring
- [ ] Email delivery tracking (sent/failed/bounced)
- [ ] Open rate tracking (optional, privacy concerns)
- [ ] Subscription metrics dashboard
- [ ] Failed job alerts (Hangfire monitoring)
- [ ] Bounce rate monitoring

### Testing
- [ ] Unit tests for domain logic (90% coverage)
- [ ] Integration tests for email sending
- [ ] E2E tests for subscription flow
- [ ] Load testing for batch email sending
- [ ] Test with real email providers (Gmail, Outlook)

### Performance
- [ ] Database indexes on frequently queried columns
- [ ] Batch processing for email sends (chunks of 100)
- [ ] Connection pooling for database
- [ ] Async/await throughout
- [ ] Pagination for admin subscriber list

---

## 8. Implementation Phases

### Phase 1: Foundation (Week 1)
1. Create database migrations
2. Implement domain entities and value objects
3. Create repositories
4. Unit tests for domain logic

### Phase 2: CQRS Commands/Queries (Week 2)
1. Implement subscription commands
2. Implement confirmation workflow
3. Create API endpoints
4. Integration tests

### Phase 3: Email Integration (Week 3)
1. Domain event handlers for emails
2. Confirmation email template
3. Newsletter email template
4. Welcome email template

### Phase 4: Scheduled Jobs (Week 4)
1. Hangfire configuration
2. Newsletter aggregator service
3. Content builder implementation
4. Delivery tracking

### Phase 5: Production Prep (Week 5)
1. Security hardening
2. Performance optimization
3. Monitoring setup
4. Load testing
5. Documentation

---

## 9. Metrics and Analytics

### Track These Metrics
- **Subscription Rate**: New subscriptions per day/week
- **Confirmation Rate**: % of subscribers who confirm
- **Unsubscribe Rate**: % of subscribers who unsubscribe
- **Delivery Rate**: % of emails successfully delivered
- **Bounce Rate**: % of emails that bounce
- **Engagement**: Open rates (if tracking pixels used)

### Dashboard Queries
```sql
-- Subscription growth over time
SELECT
    CAST(created_at AS DATE) as date,
    COUNT(*) as new_subscriptions
FROM newsletter_subscribers
WHERE is_confirmed = 1
GROUP BY CAST(created_at AS DATE)
ORDER BY date DESC;

-- Metro area distribution
SELECT
    m.display_name,
    COUNT(ns.id) as subscriber_count
FROM newsletter_subscribers ns
JOIN metro_areas m ON ns.metro_area_id = m.id
WHERE ns.is_confirmed = 1 AND ns.is_active = 1
GROUP BY m.display_name
ORDER BY subscriber_count DESC;

-- Delivery success rate
SELECT
    delivery_status,
    COUNT(*) as count,
    (COUNT(*) * 100.0 / SUM(COUNT(*)) OVER()) as percentage
FROM newsletter_deliveries
WHERE sent_at >= DATEADD(day, -30, GETUTCDATE())
GROUP BY delivery_status;
```

---

## 10. Future Enhancements

### Post-MVP Features
1. **Personalized content**: ML-based event recommendations
2. **Multiple newsletters**: Daily digest vs weekly roundup
3. **Category preferences**: Filter by event type (cultural, professional, social)
4. **A/B testing**: Test subject lines and content
5. **SMS notifications**: Optional SMS for urgent events
6. **Mobile app push notifications**: Complement email
7. **Social sharing**: Share events from newsletter
8. **Referral program**: Incentivize sharing

---

## Technology Stack Summary

- **Backend**: .NET 8 / ASP.NET Core
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Background Jobs**: Hangfire
- **Email**: Existing IEmailService with cultural intelligence
- **Patterns**: Clean Architecture, DDD, CQRS
- **Testing**: xUnit, Moq, FluentAssertions

---

## Key Architectural Principles

1. **Domain-Driven Design**: NewsletterSubscriber as aggregate root with business logic encapsulation
2. **CQRS**: Separate read/write models for scalability
3. **Event-Driven**: Domain events for side effects (email sending)
4. **Database-First for Metro Areas**: Dynamic management with caching for performance
5. **Double Opt-In**: Legal compliance and spam prevention
6. **Scheduled Processing**: Weekly batches with timezone awareness
7. **Extensibility**: Foundation for future personalization and analytics

---

**Document Version**: 1.0
**Last Updated**: 2025-11-09
**Author**: System Architecture Designer
**Status**: Approved for Implementation

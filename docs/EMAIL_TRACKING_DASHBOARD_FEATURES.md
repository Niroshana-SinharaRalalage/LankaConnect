# Email Tracking Dashboard - Feature Specification

**Phase**: 6A.87 Week 3
**Priority**: HIGH - Observability for Email System
**Status**: Planning

---

## Dashboard Overview

A comprehensive admin dashboard to monitor all email sending activity, track failures, and measure the typed parameter migration progress.

---

## Feature List

### 1. Summary Dashboard (Home View)

| Metric | Description |
|--------|-------------|
| Total Emails Sent | Today / This Week / This Month |
| Success Rate | Percentage of successfully sent emails |
| Failure Rate | Percentage of failed emails |
| Average Send Duration | How long emails take to send (ms) |
| Migration Progress | % of emails using typed vs dictionary params |

**Visual Components:**
- Success/Failure pie chart
- Emails sent over time (line chart)
- Migration progress bar

---

### 2. By-Template View

For each of the 19+ templates:

| Metric | Description |
|--------|-------------|
| Template Name | e.g., `template-event-reminder` |
| Total Sent | Count of emails sent with this template |
| Success Count | Successfully delivered |
| Failure Count | Failed to send |
| Success Rate | Percentage |
| Avg Duration | Average send time (ms) |
| Last Sent | Timestamp of most recent send |
| Migration Status | Using typed params? Yes/No |

**Sortable by**: Total sent, Failure rate, Last sent

---

### 3. Failed Emails View

List of all failed email sends with:

| Field | Description |
|-------|-------------|
| Timestamp | When the failure occurred |
| Correlation ID | For tracing in logs |
| Template Name | Which template failed |
| Recipient Email | Who was the intended recipient |
| Error Message | What went wrong |
| Handler Name | Which code sent it (e.g., EventReminderJob) |
| Retry Button | Option to retry sending |

**Filters:**
- Date range
- Template name
- Error type (validation, send failure, template not found)

---

### 4. Parameter Validation Failures

Track `{{Parameter}}` issues (the main problem we're solving):

| Field | Description |
|-------|-------------|
| Timestamp | When validation failed |
| Template Name | Which template |
| Missing Parameters | List of parameters that failed validation |
| Handler Name | Which handler had the bug |

**Alert**: Any validation failure should trigger a notification!

---

### 5. Migration Progress View

Track typed parameter adoption:

| Handler | Template | Using Typed? | First Used | Email Count |
|---------|----------|--------------|------------|-------------|
| EventReminderJob | template-event-reminder | ✅ Yes | 2026-01-28 | 150 |
| PaymentCompletedEventHandler | ticket-confirmation | ❌ No | - | 500 |
| ... | ... | ... | ... | ... |

**Metrics:**
- Total handlers: 20
- Migrated handlers: 1
- Migration %: 5%

---

### 6. Real-Time Monitoring

Live feed of email activity:

```
[2026-01-28 10:15:32] ✅ template-event-reminder → john@example.com (45ms)
[2026-01-28 10:15:30] ✅ ticket-confirmation → jane@example.com (120ms)
[2026-01-28 10:15:28] ❌ newsletter → bob@example.com (ERROR: Rate limited)
```

---

### 7. Alerts & Notifications

| Alert Type | Trigger | Action |
|------------|---------|--------|
| Critical | Template not found | Slack/Email notification |
| Warning | Success rate < 95% | Dashboard alert |
| Warning | Validation failure | Dashboard alert |
| Info | New handler migrated | Log entry |

---

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/email-metrics/summary` | GET | Overall dashboard stats |
| `/api/admin/email-metrics/by-template` | GET | Stats grouped by template |
| `/api/admin/email-metrics/by-template/{name}` | GET | Single template stats |
| `/api/admin/email-metrics/failures` | GET | List of failed emails |
| `/api/admin/email-metrics/failures/{id}/retry` | POST | Retry a failed email |
| `/api/admin/email-metrics/validation-failures` | GET | Parameter validation issues |
| `/api/admin/email-metrics/migration-progress` | GET | Typed vs Dictionary usage |
| `/api/admin/email-metrics/live` | WebSocket | Real-time email activity |

---

## Data Storage

### Option A: In-Memory (Current - DefaultEmailMetrics)
- ✅ Simple, no DB changes
- ❌ Lost on restart
- Good for: Development, short-term monitoring

### Option B: Database Table (Recommended for Production)
- ✅ Persistent history
- ✅ Query historical data
- ❌ Requires migration
- Good for: Long-term analytics, audit trail

### Proposed Table: `communications.email_send_logs`

```sql
CREATE TABLE communications.email_send_logs (
    id UUID PRIMARY KEY,
    correlation_id VARCHAR(50) NOT NULL,
    template_name VARCHAR(100) NOT NULL,
    recipient_email VARCHAR(255) NOT NULL,
    handler_name VARCHAR(100) NOT NULL,
    used_typed_parameters BOOLEAN NOT NULL,
    success BOOLEAN NOT NULL,
    error_message TEXT,
    duration_ms INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_email_logs_template ON communications.email_send_logs(template_name);
CREATE INDEX idx_email_logs_created ON communications.email_send_logs(created_at);
CREATE INDEX idx_email_logs_success ON communications.email_send_logs(success);
```

---

## UI Mockup (Admin Dashboard)

```
┌─────────────────────────────────────────────────────────────────┐
│  📧 Email Tracking Dashboard                    [Last 24h ▼]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐   │
│  │   1,234  │  │   98.5%  │  │    45ms  │  │  5% Migrated │   │
│  │  Emails  │  │ Success  │  │ Avg Time │  │  (1/19)      │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Emails Sent Over Time                    [Chart]       │   │
│  │  ▁▂▃▄▅▆▇█▇▆▅▄▃▂▁                                        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Recent Failures (3)                          [View All →]      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ❌ 10:15 newsletter → bob@example.com - Rate limited    │   │
│  │ ❌ 09:30 event-reminder → x@y.com - Invalid email       │   │
│  │ ❌ 09:15 ticket-confirm → test@test - Validation failed │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Priority

| Priority | Feature | Effort |
|----------|---------|--------|
| P0 | Summary API endpoint | 2 hours |
| P0 | By-template API endpoint | 2 hours |
| P0 | Failures API endpoint | 2 hours |
| P1 | Migration progress endpoint | 1 hour |
| P1 | Database persistence | 4 hours |
| P2 | Admin UI page | 8 hours |
| P2 | Real-time WebSocket | 4 hours |
| P3 | Alerts/notifications | 4 hours |

---

## Success Criteria

1. ✅ Can view total emails sent today/week/month
2. ✅ Can see success/failure rate by template
3. ✅ Can list all failed emails with error details
4. ✅ Can track which handlers use typed parameters
5. ✅ Can identify parameter validation failures
6. ✅ Data persists across restarts (database)

---

**Created**: 2026-01-28
**Author**: Development Team

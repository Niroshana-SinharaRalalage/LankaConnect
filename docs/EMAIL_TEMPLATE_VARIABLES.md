# Email Template Variables Reference

This document lists all available variables for email templates in the LankaConnect system.

## Template Types

### 1. Member Email Verification (`member-email-verification`)

**Type**: `MemberEmailVerification`
**Category**: `Authentication`

Used when a new member signs up and needs to verify their email address.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | User's full name | "John Doe" |
| `{{VerificationUrl}}` | string | Complete verification URL with token | "https://lankaconnect.com/verify?token=abc123" |
| `{{ExpirationHours}}` | number | Hours until verification link expires | "6" |

**Event Handler**: `MemberVerificationRequestedEventHandler` (Phase 6A.53)

---

### 2. Signup Commitment Confirmation (`signup-commitment-confirmation`)

**Type**: `SignupCommitmentConfirmation`
**Category**: `Notification`

Sent when a user commits to bringing an item to an event.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | User's full name | "Sarah Smith" |
| `{{EventTitle}}` | string | Event name | "Community Potluck Dinner" |
| `{{ItemDescription}}` | string | Description of committed item | "Vegetarian lasagna" |
| `{{Quantity}}` | number | Number of items/servings | "2 trays" |
| `{{EventDateTime}}` | string | Event date and time (formatted) | "Saturday, January 15, 2025 at 6:00 PM" |
| `{{EventLocation}}` | string | Event location with address | "Community Center, 123 Main St, Colombo" |
| `{{PickupInstructions}}` | string | Instructions for pickup/delivery | "Please bring to the venue by 5:30 PM" |

**Event Handler**: `SignupCommitmentConfirmedEventHandler` (Phase 6A.51)

---

### 3. Registration Cancellation (`registration-cancellation`)

**Type**: `RegistrationCancellationConfirmation`
**Category**: `Notification`

Sent when a user cancels their event registration.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | User's full name | "Michael Johnson" |
| `{{EventTitle}}` | string | Event name | "Live Music Festival" |
| `{{EventDateTime}}` | string | Event date and time (formatted) | "Saturday, February 10, 2025 at 2:00 PM" |
| `{{EventLocation}}` | string | Event location with address | "Colombo Park, Colombo 7" |
| `{{CancellationDateTime}}` | string | When cancellation occurred (formatted) | "January 20, 2025 at 10:30 AM" |
| `{{RefundDetails}}` | string | Refund amount and method | "Full refund of $30.00 will be processed to your credit card within 5-7 business days." |
| `{{CancellationPolicy}}` | string | Cancellation policy text | "Free cancellation up to 7 days before event. 50% refund 3-7 days before. No refund within 3 days." |

**Event Handler**: `RegistrationCancelledEventHandler` (Phase 6A.52)

---

### 4. Organizer Custom Message (`organizer-custom-message`)

**Type**: `OrganizerCustomMessage`
**Category**: `Business`

Custom message from event organizer to selected attendees.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | Recipient's full name | "Emily Chen" |
| `{{OrganizerName}}` | string | Event organizer's name | "David Perera" |
| `{{EventTitle}}` | string | Event name | "Photography Workshop" |
| `{{Subject}}` | string | Custom email subject from organizer | "Important Update About Photography Equipment" |
| `{{MessageContent}}` | string | Custom HTML message content (sanitized) | "<p>Please bring your own DSLR camera...</p>" |
| `{{EventDateTime}}` | string | Event date and time (formatted) | "Sunday, March 5, 2025 at 9:00 AM" |
| `{{EventLocation}}` | string | Event location with address | "Photo Studio, 456 Galle Road, Colombo 3" |

**Command**: `SendOrganizerEventEmailCommand` (Phase 6A.50)

---

### 5. Ticket Confirmation - Paid Events (`ticket-confirmation`)

**Type**: `Transactional`
**Category**: `System`

Sent when a user successfully completes payment for a paid event (with PDF ticket attachment).

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | User's full name | "Niroshana Sinhara" |
| `{{EventTitle}}` | string | Event name | "Live Baila Music Night with DJ Sohan" |
| `{{EventDateTime}}` | string | Event date and time (formatted) | "Tuesday, December 24, 2025 at 8:00 PM" |
| `{{EventLocation}}` | string | Event location with full address | "Grand Ballroom, Hilton Colombo, 2 Sir Chittampalam A Gardiner Mawatha, Colombo 00200" |
| `{{Quantity}}` | number | Number of tickets purchased | "1" |
| `{{AmountPaid}}` | currency | Total amount paid (USD with $ symbol) | "$30.00" |
| `{{PaymentMethod}}` | string | Payment method used | "Credit Card (**** 4242)" |
| `{{TransactionId}}` | string | Stripe payment intent ID | "pi_3SiplNLvfbr023L11fMUahG2" |
| `{{ConfirmationNumber}}` | string | Unique registration confirmation code | "REG-2025-001234" |
| `{{ContactEmail}}` | string | User's email address | "niroshhh@gmail.com" |
| `{{ContactPhone}}` | string | User's phone number | "+94 77 123 4567" |
| `{{Attendees}}` | HTML list | HTML-formatted list of attendees with details | "&lt;ul&gt;&lt;li&gt;John Doe (Adult)&lt;/li&gt;&lt;/ul&gt;" |
| `{{OrganizerName}}` | string | Event organizer's name | "Lanka Events Ltd" |
| `{{OrganizerEmail}}` | string | Event organizer contact email | "events@lankaevents.lk" |
| `{{OrganizerPhone}}` | string | Event organizer contact phone | "+94 11 234 5678" |
| `{{EventDetailsUrl}}` | string | URL to view full event details | "https://lankaconnect.com/events/b9ea09b4-0e9d-4d45-8fe6-50e295da7e0f" |
| `{{ManageBookingUrl}}` | string | URL to manage/cancel booking | "https://lankaconnect.com/bookings/fd07392c-6e4a-4e1a-9f7b-a8018c9c73ce" |
| `{{QRCodeUrl}}` | string | URL to QR code image for ticket scanning | "https://api.lankaconnect.com/qr/REG-2025-001234.png" |

**Event Handler**: `PaymentCompletedEventHandler`
**Attachment**: PDF ticket with QR code

---

### 6. Registration Confirmation - Free Events (`registration-confirmation`)

**Type**: `EventNotification`
**Category**: `System`

Sent when a user registers for a free event.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{UserName}}` | string | User's full name | "Jane Doe" |
| `{{EventTitle}}` | string | Event name | "Community Cleanup Day" |
| `{{EventStartDate}}` | string | Event date (formatted) | "Saturday, January 20, 2025" |
| `{{EventStartTime}}` | string | Event start time | "9:00 AM" |
| `{{EventLocation}}` | string | Event location with address | "Beach Park, Mount Lavinia" |
| `{{Quantity}}` | number | Number of attendees | "2" |
| `{{Attendees}}` | HTML list | HTML-formatted list of registered attendees | "&lt;ul&gt;&lt;li&gt;Jane Doe&lt;/li&gt;&lt;li&gt;John Doe&lt;/li&gt;&lt;/ul&gt;" |
| `{{ContactEmail}}` | string | User's email address | "jane@example.com" |
| `{{ContactPhone}}` | string | User's phone number | "+94 77 987 6543" |

**Event Handler**: `RegistrationConfirmedEventHandler` (existing)

---

### 7. Event Reminder (`event-reminder`)

**Type**: `EventReminder`
**Category**: `Notification`

Sent 7 days, 2 days, and 1 day before event starts to remind attendees.

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{AttendeeName}}` | string | Recipient's name | "Jane Doe" |
| `{{EventTitle}}` | string | Event name | "Community Potluck Dinner" |
| `{{EventStartDate}}` | string | Event date (formatted) | "January 20, 2025" |
| `{{EventStartTime}}` | string | Event start time | "6:00 PM" |
| `{{Location}}` | string | Event location with address | "Community Center, 123 Main St, Colombo" |
| `{{Quantity}}` | number | Number of tickets/attendees | "2" |
| `{{HoursUntilEvent}}` | number | Precise hours until event starts | "168.0" (7 days), "48.0" (2 days), "24.0" (1 day) |
| `{{ReminderTimeframe}}` | string | Human-readable timeframe | "in 1 week", "in 2 days", "tomorrow" |
| `{{ReminderMessage}}` | string | Urgency message based on timeframe | "Your event is coming up next week. Mark your calendar!" |
| `{{EventDetailsUrl}}` | string | URL to event page | "https://lankaconnect.com/events/abc-123" |

**Background Job**: `EventReminderJob` (runs hourly via Hangfire)

**Reminder Schedule**:
- **7-day reminder**: Events starting in 167-169 hours
- **2-day reminder**: Events starting in 47-49 hours
- **1-day reminder**: Events starting in 23-25 hours

**Time Windows**: 2-hour windows prevent duplicate reminders (job runs hourly)

---

## Common Formatting Patterns

### Date/Time Formatting
- **Long format**: "Monday, December 25, 2025 at 7:30 PM"
- **Short date**: "December 25, 2025"
- **Time only**: "7:30 PM"

All dates use **Pacific Time (PT)** for consistency.

### Currency Formatting
- **Culture**: en-US (United States)
- **Symbol**: $ (USD)
- **Format**: "$30.00" (always 2 decimal places)
- **Code**: `.ToString("C", CultureInfo.GetCultureInfo("en-US"))`

### HTML Content
- All HTML content in `{{MessageContent}}` is sanitized using `HtmlSanitizer`
- Allowed tags: `p, br, strong, em, u, ul, ol, li, a, h1-h6`
- Disallowed: `script, iframe, object, embed, style`

### Attendee List Formatting
```html
<ul>
    <li>John Doe (Adult, Ticket #1)</li>
    <li>Jane Smith (Child, Ticket #2)</li>
</ul>
```

---

## Variable Naming Conventions

1. **PascalCase**: All template variables use PascalCase (e.g., `{{UserName}}`, not `{{user_name}}`)
2. **Descriptive**: Variable names clearly indicate their content
3. **Consistent**: Same variable names across all templates where applicable

---

## Template Development Guidelines

### When Creating New Templates

1. **Name**: Use kebab-case for template names (e.g., `member-email-verification`)
2. **Description**: Clear description of when template is used
3. **Type**: Assign appropriate `EmailType` enum value
4. **Category**: Choose from: `Authentication`, `Business`, `Marketing`, `System`, `Notification`
5. **Text Version**: Always provide plain text fallback
6. **HTML Version**: Responsive design, max width 600px
7. **Variables**: Document all variables in this file

### Testing Templates

1. Create test data with all variables populated
2. Verify both text and HTML rendering
3. Test with missing/null variables
4. Verify attachments work (if applicable)
5. Check mobile responsiveness of HTML

### Security Considerations

1. **Never include**: Passwords, API keys, tokens (except verification tokens)
2. **Sanitize HTML**: All user-provided HTML content MUST be sanitized
3. **URL validation**: Verify URLs point to expected domains
4. **Personal data**: Follow GDPR/privacy best practices

---

## Migration History

| Date | Phase | Templates Added | Notes |
|------|-------|-----------------|-------|
| 2025-12-19 | Phase 6A.34 | `registration-confirmation` | Initial free event registration template |
| 2025-12-24 | Phase 6A.45 | `ticket-confirmation` | Paid event ticket with PDF attachment |
| 2025-12-27 | Phase 6A.54 | `member-email-verification`<br/>`signup-commitment-confirmation`<br/>`registration-cancellation`<br/>`organizer-custom-message` | Complete email system templates |
| 2025-12-28 | Phase 6A.57 | `event-reminder` | Professional HTML template, 3 reminder types (7d, 2d, 1d) |

---

## Related Documentation

- [EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md](./EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md) - Full implementation plan
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Development progress
- [Master Requirements Specification.md](./Master%20Requirements%20Specification.md) - Feature requirements
- [PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md](./PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md) - Email system master plan
- [PHASE_6A_ORIGINAL_REQUIREMENTS_GAP_ANALYSIS.md](./PHASE_6A_ORIGINAL_REQUIREMENTS_GAP_ANALYSIS.md) - Requirements gap analysis

---

**Last Updated**: December 28, 2025
**Phase**: 6A.57 - Event Reminder Improvements

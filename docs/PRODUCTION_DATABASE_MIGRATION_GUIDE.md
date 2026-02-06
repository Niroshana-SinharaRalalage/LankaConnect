# Production Database Migration Guide

**Date**: 2026-01-22
**Purpose**: Document database migration strategy for production deployment

---

## Migration Strategy

### ✅ Answer: We Use EXISTING Migrations

We will use the **existing 182 EF Core migrations** that are already tested in staging. These migrations include:
1. Schema creation (tables, indexes, constraints)
2. Reference data seeding (via `migrationBuilder.Sql()` statements)
3. Data fixes and updates

**NO new scripts needed** - the migrations are self-contained and idempotent.

---

## How Migrations Work

### EF Core Migration Process:

1. **Production database is EMPTY** (new Azure PostgreSQL)
2. Run `dotnet ef database update`
3. EF Core applies migrations **in order** by timestamp (YYYYMMDDHHMMSS)
4. Each migration:
   - Creates tables/columns/indexes
   - Seeds reference data
   - Applies data fixes
5. `__EFMigrationsHistory` table tracks which migrations ran

### GitHub Actions Workflow:

When we push to `main` branch, the `deploy-production.yml` workflow:
```yaml
- name: Run EF Migrations
  run: |
    DB_CONNECTION=$(az keyvault secret show --vault-name $KEY_VAULT_NAME --name DATABASE-CONNECTION-STRING --query value -o tsv)
    dotnet ef database update --connection "$DB_CONNECTION"
```

---

## Database Schemas

The database uses **5 schemas** for organization:

| Schema | Purpose |
|--------|---------|
| `public` | Default PostgreSQL schema, used for EF migrations history |
| `events` | Event-related tables (events, registrations, tickets, etc.) |
| `communications` | Email, newsletters, notifications |
| `reference_data` | Lookup tables and reference values |
| `payments` | Stripe payments, subscriptions, transactions |

---

## Tables Created by Migrations

### Schema: `events`

| Table | Description | Reference Data |
|-------|-------------|----------------|
| `events` | Main events table | No |
| `event_images` | Event photos/images | No |
| `event_videos` | Event video links | No |
| `event_templates` | Reusable event templates | No |
| `metro_areas` | US metro area locations | **Yes - 22 metro areas** |
| `user_preferred_metro_areas` | User location preferences | No |
| `event_registrations` | Event registrations | No |
| `event_attendees` | Attendee details | No |
| `tickets` | Event tickets/passes | No |
| `pass_purchases` | Ticket purchase records | No |
| `sign_up_lists` | Event sign-up sheets | No |
| `sign_up_items` | Individual sign-up items | No |
| `sign_up_categories` | Sign-up item categories | No |
| `sign_up_commitments` | User commitments to items | No |
| `event_email_groups` | Email group targeting | No |
| `event_reminders_sent` | Reminder tracking | No |
| `organizer_contact_details` | Organizer contact info | No |

### Schema: `communications`

| Table | Description | Reference Data |
|-------|-------------|----------------|
| `email_messages` | Email queue/history | No |
| `email_templates` | Email template definitions | **Yes - 15+ templates** |
| `newsletters` | Newsletter campaigns | No |
| `newsletter_subscribers` | Subscriber list | No |
| `newsletter_subscriber_metro_areas` | Subscriber locations | No |
| `newsletter_recipients` | Campaign recipients | No |
| `newsletter_send_history` | Send tracking | No |
| `notifications` | In-app notifications | No |
| `event_notification_history` | Notification tracking | No |

### Schema: `reference_data`

| Table | Description | Reference Data |
|-------|-------------|----------------|
| `reference_values` | Unified enum/lookup table | **Yes - 50+ enum values** |
| `state_tax_rates` | US state tax rates | **Yes - 50 states** |

### Schema: `payments`

| Table | Description | Reference Data |
|-------|-------------|----------------|
| `payment_intents` | Stripe payment intents | No |
| `stripe_accounts` | Connected Stripe accounts | No |
| `subscription_plans` | Subscription tier definitions | No |
| `user_subscriptions` | User subscription status | No |

### Schema: `public`

| Table | Description | Reference Data |
|-------|-------------|----------------|
| `users` | User accounts | No |
| `user_role_upgrade_history` | Role change tracking | No |
| `__EFMigrationsHistory` | Migration tracking | Auto-populated |

---

## Reference Data Seeded by Migrations

### 1. Metro Areas (`events.metro_areas`)

**Migration**: `20251112204434_SeedMetroAreasReferenceData.cs`

| ID | Name | State | Radius |
|----|------|-------|--------|
| `01000000-...` | All Alabama | AL | 200 mi |
| `01111111-...` | Birmingham | AL | 30 mi |
| `01111111-...` | Montgomery | AL | 25 mi |
| `01111111-...` | Mobile | AL | 25 mi |
| `02000000-...` | All Alaska | AK | 300 mi |
| `02111111-...` | Anchorage | AK | 30 mi |
| `04000000-...` | All Arizona | AZ | 200 mi |
| `04111111-...` | Phoenix | AZ | 35 mi |
| `04111111-...` | Tucson | AZ | 30 mi |
| `04111111-...` | Mesa | AZ | 25 mi |
| `06000000-...` | All California | CA | 250 mi |
| `06111111-...` | Los Angeles | CA | 40 mi |
| `06111111-...` | San Francisco Bay | CA | 40 mi |
| `06111111-...` | San Diego | CA | 35 mi |
| `17000000-...` | All Illinois | IL | 200 mi |
| `17111111-...` | Chicago | IL | 45 mi |
| `36000000-...` | All New York | NY | 250 mi |
| `36111111-...` | New York City | NY | 40 mi |
| `48000000-...` | All Texas | TX | 300 mi |
| `48111111-...` | Houston | TX | 40 mi |
| `48111111-...` | Dallas-Fort Worth | TX | 40 mi |
| `48111111-...` | Austin | TX | 30 mi |

**Total: 22 metro areas** (state-level + major cities)

---

### 2. Reference Values (`reference_data.reference_values`)

**Migration**: `20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`

This unified table stores all enum values:

#### Event Categories (enum_type = 'EventCategory')
| Code | Name | Int Value |
|------|------|-----------|
| Religious | Religious | 0 |
| Cultural | Cultural | 1 |
| Community | Community | 2 |
| Educational | Educational | 3 |
| Social | Social | 4 |
| Business | Business | 5 |
| Charity | Charity | 6 |
| Entertainment | Entertainment | 7 |

#### Event Statuses (enum_type = 'EventStatus')
| Code | Name | Int Value |
|------|------|-----------|
| Draft | Draft | 0 |
| Published | Published | 1 |
| Active | Active | 2 |
| Postponed | Postponed | 3 |
| Cancelled | Cancelled | 4 |
| Completed | Completed | 5 |
| Archived | Archived | 6 |
| UnderReview | Under Review | 7 |

#### User Roles (enum_type = 'UserRole')
| Code | Name | Int Value |
|------|------|-----------|
| GeneralUser | General User | 1 |
| BusinessOwner | Business Owner | 2 |
| EventOrganizer | Event Organizer | 3 |
| EventOrganizerAndBusinessOwner | Event Organizer & Business Owner | 4 |
| Admin | Admin | 5 |
| AdminManager | Admin Manager | 6 |

#### Age Categories (enum_type = 'AgeCategory')
| Code | Name | Int Value |
|------|------|-----------|
| Adult | Adult | 0 |
| Child | Child | 1 |
| Senior | Senior | 2 |
| Infant | Infant | 3 |
| Teen | Teen | 4 |

#### Genders (enum_type = 'Gender')
| Code | Name | Int Value |
|------|------|-----------|
| Male | Male | 0 |
| Female | Female | 1 |
| Other | Other | 2 |
| PreferNotToSay | Prefer Not To Say | 3 |

**And many more enum types...**

---

### 3. Email Templates (`communications.email_templates`)

**Multiple migrations seed email templates:**

| Template Name | Description | Migration |
|---------------|-------------|-----------|
| `registration-confirmation` | Event registration | 20251219164841 |
| `ticket-confirmation` | Ticket purchase | 20251220155500 |
| `event-published` | Event published notification | 20251221160725 |
| `member-email-verification` | Email verification | 20251228200000 |
| `event-cancelled` | Event cancellation | 20260102052559 |
| `event-update` | Event updates | 20260110000000 |
| `registration-cancellation` | Registration cancelled | 20260110000000 |
| `newsletter` | Newsletter template | 20260110120000 |
| `event-details` | Event details email | 20260113020400 |
| `event-approved` | Event approval notification | 20260120235127 |
| `organizer-role-approved` | Role approval | 20260121041129 |
| `event-reminder` | Event reminders | 20260121173030 |

**Total: 15+ email templates with HTML/Text versions**

---

### 4. State Tax Rates (`reference_data.state_tax_rates`)

**Migration**: `20260117150129_MoveStateTaxRatesToReferenceDataSchema.cs`

| State | Tax Rate |
|-------|----------|
| AL | 4.0% |
| AK | 0.0% |
| AZ | 5.6% |
| ... | ... |
| WY | 4.0% |

**Total: 50 US states + DC**

---

## Migration Execution Plan

### Step 1: Verify Production Database Connection

```bash
# Get connection string from Key Vault
az keyvault secret show \
  --vault-name lankaconnect-prod-kv \
  --name DATABASE-CONNECTION-STRING \
  --query value -o tsv
```

### Step 2: Run Migrations (via GitHub Actions)

```bash
# Triggered automatically when pushing to main branch
# OR manually via:
dotnet ef database update \
  --connection "$DB_CONNECTION" \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.API \
  --context AppDbContext
```

### Step 3: Verify Tables Created

```sql
-- Check all schemas
SELECT schema_name FROM information_schema.schemata
WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast');

-- Check tables per schema
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema IN ('events', 'communications', 'reference_data', 'payments', 'public')
ORDER BY table_schema, table_name;
```

### Step 4: Verify Reference Data

```sql
-- Metro Areas
SELECT COUNT(*) FROM events.metro_areas;
-- Expected: 22+

-- Reference Values
SELECT enum_type, COUNT(*)
FROM reference_data.reference_values
GROUP BY enum_type;

-- Email Templates
SELECT name, type, category
FROM communications.email_templates;
-- Expected: 15+ templates

-- State Tax Rates
SELECT COUNT(*) FROM reference_data.state_tax_rates;
-- Expected: 51 (50 states + DC)
```

---

## Data That Will NOT Be Migrated

The following data is **NOT seeded by migrations** and will be empty in production:

| Table | Reason |
|-------|--------|
| `users` | Users register themselves |
| `events` | Organizers create events |
| `event_registrations` | Users register for events |
| `event_attendees` | Created during registration |
| `tickets` | Created by organizers |
| `pass_purchases` | Created during checkout |
| `newsletters` | Created by admins |
| `newsletter_subscribers` | Users subscribe |
| `email_messages` | Generated by system |
| `payment_intents` | Created by Stripe |
| `stripe_accounts` | Organizers connect |

**These are user-generated data** - production starts fresh.

---

## Rollback Strategy

If migrations fail:

### Option 1: Fix Forward
- Identify the failing migration
- Create a hotfix migration
- Apply and continue

### Option 2: Rollback to Specific Migration
```bash
dotnet ef database update [TargetMigration] --connection "$DB_CONNECTION"
```

### Option 3: Drop and Recreate (Nuclear)
```bash
# Only for production if completely broken
dotnet ef database drop --force
dotnet ef database update
```

---

## Summary

| Question | Answer |
|----------|--------|
| **Use existing migrations or new scripts?** | ✅ **Existing migrations** (182 total) |
| **How is reference data seeded?** | Via `migrationBuilder.Sql()` in migrations |
| **What data is seeded?** | Metro areas, reference values, email templates, tax rates |
| **What tables are created?** | ~40 tables across 5 schemas |
| **Will user data be migrated?** | No - production starts fresh |
| **Is it idempotent?** | Yes - uses `ON CONFLICT DO NOTHING` |
| **How to verify?** | Query tables after migration |

---

## Next Steps

1. ✅ Document migration strategy (this document)
2. ⏳ Push code to main branch
3. ⏳ GitHub Actions runs migrations automatically
4. ⏳ Verify tables and reference data
5. ⏳ Test API endpoints
6. ⏳ Go live!

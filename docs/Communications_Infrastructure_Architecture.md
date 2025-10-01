# Communications Infrastructure Architecture Guide

## Executive Summary

This document provides comprehensive architectural guidance for integrating the Communications domain entities into the LankaConnect application's Entity Framework Core infrastructure. The Communications domain handles email messaging, templates, and user preferences with robust queuing, retry logic, and tracking capabilities.

## Domain Entity Overview

### Core Entities

1. **EmailMessage** - Central entity for email processing and tracking
2. **EmailTemplate** - Reusable email templates with variable substitution
3. **UserEmailPreferences** - User-specific email communication preferences

### Value Objects

1. **EmailSubject** - Validated email subject with length constraints
2. **Email** (from Users domain) - Email address validation
3. **VerificationToken** - Secure token generation for email verification

### Enumerations

1. **EmailStatus** - Email lifecycle states (Pending, Queued, Sending, Sent, Delivered, Failed, Bounced, Rejected)
2. **EmailType** - Email categorization (Welcome, EmailVerification, PasswordReset, BusinessNotification, etc.)
3. **EmailDeliveryStatus** - Legacy compatibility enum

## Entity Framework Configuration

### 1. EmailMessage Configuration

**Key Design Decisions:**

- **Value Object Mapping**: Uses `OwnsOne` pattern for EmailSubject and Email value objects
- **JSON Storage**: Email collections (To, CC, BCC) stored as PostgreSQL JSONB for flexibility
- **String Enum Conversion**: Enums stored as strings for better readability and debugging
- **Comprehensive Indexing**: Optimized for email queue processing and analytics queries

**Table Structure:**
```sql
CREATE TABLE communications.email_messages (
    id uuid PRIMARY KEY,
    from_email varchar(255) NOT NULL,
    to_emails jsonb NOT NULL,
    cc_emails jsonb NOT NULL,
    bcc_emails jsonb NOT NULL,
    subject varchar(200) NOT NULL,
    text_content text NOT NULL,
    html_content text,
    type varchar(50) NOT NULL,
    status varchar(50) NOT NULL,
    sent_at timestamp,
    delivered_at timestamp,
    opened_at timestamp,
    clicked_at timestamp,
    failed_at timestamp,
    next_retry_at timestamp,
    error_message varchar(1000),
    retry_count integer NOT NULL,
    max_retries integer NOT NULL,
    priority integer NOT NULL,
    template_name varchar(100),
    template_data jsonb,
    created_at timestamp NOT NULL DEFAULT NOW(),
    updated_at timestamp
);
```

**Performance Indexes:**
- `IX_EmailMessages_Status` - Email queue processing
- `IX_EmailMessages_Status_NextRetryAt` - Retry scheduling
- `IX_EmailMessages_Type` - Email categorization queries
- `IX_EmailMessages_Priority` - Priority-based processing
- `IX_EmailMessages_RetryCount_Status` - Failed email analysis

### 2. EmailTemplate Configuration

**Key Design Decisions:**

- **Unique Template Names**: Enforced at database level
- **Template Content Storage**: Separate text and HTML templates
- **Active Flag**: Soft deletion pattern for template versioning
- **Type-based Categorization**: Organized by email purpose

**Table Structure:**
```sql
CREATE TABLE communications.email_templates (
    id uuid PRIMARY KEY,
    name varchar(100) NOT NULL UNIQUE,
    description varchar(500) NOT NULL,
    subject_template varchar(200) NOT NULL,
    text_template text NOT NULL,
    html_template text,
    type varchar(50) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    tags varchar(500),
    created_at timestamp NOT NULL DEFAULT NOW(),
    updated_at timestamp
);
```

**Performance Indexes:**
- `IX_EmailTemplates_Name` - Unique template lookup
- `IX_EmailTemplates_Type` - Category-based queries
- `IX_EmailTemplates_Type_IsActive` - Active templates by type

### 3. UserEmailPreferences Configuration

**Key Design Decisions:**

- **One-to-One with Users**: Foreign key relationship with cascade delete
- **Granular Permissions**: Separate flags for different email types
- **Timezone Storage**: Cross-platform compatible string conversion
- **Transactional Override**: Business rule preventing transactional email opt-out

**Table Structure:**
```sql
CREATE TABLE communications.user_email_preferences (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL UNIQUE,
    allow_marketing boolean NOT NULL DEFAULT false,
    allow_notifications boolean NOT NULL DEFAULT true,
    allow_newsletters boolean NOT NULL DEFAULT true,
    allow_transactional boolean NOT NULL DEFAULT true,
    preferred_language varchar(10) DEFAULT 'en-US',
    timezone varchar(100),
    created_at timestamp NOT NULL DEFAULT NOW(),
    updated_at timestamp,
    FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE
);
```

## Relationship Configurations

### Communications to Users Domain

1. **UserEmailPreferences → User**
   - **Type**: One-to-One
   - **Foreign Key**: `user_id` → `users.id`
   - **Delete Behavior**: Cascade (when user deleted, preferences are removed)
   - **Constraint**: Unique constraint ensures one preference record per user

2. **EmailMessage → Users (Implicit)**
   - **Relationship**: No direct FK (email addresses stored as value objects)
   - **Tracking**: Email addresses reference users indirectly through email value matching
   - **Flexibility**: Allows sending emails to external recipients not in the system

## Migration Considerations

### 1. Schema Creation Strategy

```sql
-- Create communications schema
CREATE SCHEMA IF NOT EXISTS communications;

-- Apply migrations in order:
-- 1. EmailMessage (no dependencies)
-- 2. EmailTemplate (no dependencies)  
-- 3. UserEmailPreferences (depends on Users table)
```

### 2. Data Migration Approach

**Phase 1: Structure Setup**
- Create communications schema
- Create tables without data
- Apply indexes and constraints

**Phase 2: Data Population**
- Migrate existing email data if any
- Create default user preferences for existing users
- Seed default email templates

**Phase 3: Validation**
- Verify foreign key relationships
- Test query performance
- Validate business constraints

### 3. Index Creation Strategy

```sql
-- Create indexes concurrently to avoid table locks
CREATE INDEX CONCURRENTLY IX_EmailMessages_Status ON communications.email_messages(status);
CREATE INDEX CONCURRENTLY IX_EmailMessages_Type ON communications.email_messages(type);
CREATE INDEX CONCURRENTLY IX_UserEmailPreferences_UserId ON communications.user_email_preferences(user_id);
```

## Best Practices for Email Message Storage and Querying

### 1. Email Queue Processing

**Efficient Queue Queries:**
```csharp
// Get pending emails for processing
var pendingEmails = await _context.EmailMessages
    .Where(e => e.Status == EmailStatus.Pending || e.Status == EmailStatus.Queued)
    .OrderBy(e => e.Priority)
    .ThenBy(e => e.CreatedAt)
    .Take(100)
    .ToListAsync();

// Get retry candidates
var retryEmails = await _context.EmailMessages
    .Where(e => e.Status == EmailStatus.Failed && 
                e.NextRetryAt <= DateTime.UtcNow &&
                e.RetryCount <= e.MaxRetries)
    .OrderBy(e => e.NextRetryAt)
    .ToListAsync();
```

### 2. Email Analytics and Reporting

**Performance-Optimized Queries:**
```csharp
// Email statistics by type and date range
var emailStats = await _context.EmailMessages
    .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
    .GroupBy(e => new { e.Type, e.Status })
    .Select(g => new EmailStats
    {
        Type = g.Key.Type,
        Status = g.Key.Status,
        Count = g.Count()
    })
    .ToListAsync();
```

### 3. User Preference Enforcement

**Template Selection with Preferences:**
```csharp
// Get templates user allows
var allowedTemplates = await _context.EmailTemplates
    .Where(t => t.IsActive)
    .Where(t => 
        (t.Type == EmailType.Transactional) || // Always allowed
        (t.Type == EmailType.Marketing && userPrefs.AllowMarketing) ||
        (t.Type == EmailType.Newsletter && userPrefs.AllowNewsletters))
    .ToListAsync();
```

## Configuration Updates Applied

### 1. AppDbContext Updates

- ✅ Added Communications entity DbSets
- ✅ Applied entity configurations in OnModelCreating
- ✅ Configured communications schema in ConfigureSchemas
- ✅ Added necessary using statements

### 2. IApplicationDbContext Updates

- ✅ Added Communications DbSets to interface
- ✅ Maintained interface consistency with concrete implementation

### 3. Configuration Classes Created

- ✅ EmailMessageConfiguration.cs (already existed, validated)
- ✅ EmailTemplateConfiguration.cs (newly created)
- ✅ UserEmailPreferencesConfiguration.cs (newly created)

## Next Steps

### 1. Repository Implementation

Create repository interfaces and implementations for Communications domain:

```csharp
public interface IEmailMessageRepository : IRepository<EmailMessage>
{
    Task<List<EmailMessage>> GetPendingEmailsAsync(int batchSize = 100);
    Task<List<EmailMessage>> GetRetryableEmailsAsync();
    Task<EmailMessage?> GetByMessageIdAsync(string messageId);
}

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetByNameAsync(string name);
    Task<List<EmailTemplate>> GetByTypeAsync(EmailType type);
    Task<List<EmailTemplate>> GetActiveTemplatesAsync();
}

public interface IUserEmailPreferencesRepository : IRepository<UserEmailPreferences>
{
    Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId);
    Task<UserEmailPreferences> CreateDefaultForUserAsync(Guid userId);
}
```

### 2. Database Migration Generation

Run EF Core migration commands:

```bash
# Generate migration for Communications domain
dotnet ef migrations add AddCommunicationsDomain --project src/LankaConnect.Infrastructure

# Review and apply migration
dotnet ef database update --project src/LankaConnect.Infrastructure
```

### 3. Service Registration Updates

Update DependencyInjection.cs to register Communications repositories:

```csharp
// Add Communications Repositories
services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
services.AddScoped<IUserEmailPreferencesRepository, UserEmailPreferencesRepository>();
```

## Security Considerations

1. **Email Address Protection**: User email addresses stored as value objects with validation
2. **Template Security**: HTML templates should be sanitized to prevent XSS
3. **Preference Enforcement**: Business rules prevent disabling critical transactional emails
4. **Audit Trail**: All entities inherit BaseEntity for created/updated tracking
5. **Soft Deletion**: EmailTemplate uses IsActive flag instead of hard deletion

## Performance Optimizations

1. **Indexed Queries**: All common query patterns have supporting indexes
2. **Batch Processing**: Email queue designed for batch processing workflows
3. **JSON Storage**: Email recipients stored as JSONB for PostgreSQL performance
4. **Connection Pooling**: DbContext configured with connection pooling in DependencyInjection
5. **Query Optimization**: Specific indexes for retry logic and email analytics

## Monitoring and Observability

1. **Email Metrics**: Track delivery rates, retry counts, and failure reasons
2. **Queue Depth**: Monitor pending email volumes
3. **Template Usage**: Track which templates are most/least used
4. **User Preferences**: Monitor opt-out rates by email type
5. **Performance**: Track query execution times and database connection usage

This architecture provides a robust, scalable foundation for the LankaConnect email messaging system with proper separation of concerns, optimized performance, and comprehensive tracking capabilities.
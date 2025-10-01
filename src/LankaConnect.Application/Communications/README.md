# Email & Notifications Application Layer

This document provides an overview of the Email & Notifications application layer implementation using CQRS with MediatR pattern.

## Architecture Overview

The Email module follows Clean Architecture principles with CQRS pattern:

```
src/LankaConnect.Application/Email/
├── Commands/                           # Write operations
│   ├── SendEmailVerification/         # Send verification email
│   ├── VerifyEmail/                   # Verify email with token
│   ├── SendPasswordReset/             # Send password reset email
│   ├── ResetPassword/                 # Reset password with token
│   ├── SendWelcomeEmail/              # Send welcome emails
│   └── SendBusinessNotification/      # Business notifications
├── Queries/                           # Read operations
│   ├── GetEmailStatus/                # Get email delivery status
│   ├── GetEmailTemplates/             # Get available templates
│   └── GetUserEmailPreferences/       # Get user preferences
└── Common/                            # Shared DTOs and types
    └── EmailDto.cs                    # All email-related DTOs
```

## Commands

### 1. SendEmailVerificationCommand
- **Purpose**: Send email verification to users
- **Features**:
  - Rate limiting (5-minute cooldown)
  - Force resend option
  - Token generation with 24-hour expiry
  - Template-based email rendering

### 2. VerifyEmailCommand
- **Purpose**: Verify user email addresses using tokens
- **Features**:
  - Token validation with expiry check
  - Automatic welcome email sending
  - Domain event triggering
  - Idempotent operation

### 3. SendPasswordResetCommand
- **Purpose**: Send password reset emails
- **Features**:
  - Security-focused (no user enumeration)
  - Rate limiting protection
  - Short token expiry (1 hour)
  - Account lock status checking

### 4. ResetPasswordCommand
- **Purpose**: Reset user passwords using tokens
- **Features**:
  - Token validation
  - Password strength validation
  - Refresh token revocation
  - Confirmation email sending

### 5. SendWelcomeEmailCommand
- **Purpose**: Send welcome emails for various triggers
- **Features**:
  - Multiple trigger types (registration, verification, etc.)
  - Role-based content
  - Custom message support
  - Template selection based on trigger

### 6. SendBusinessNotificationCommand
- **Purpose**: Send business-related notifications
- **Features**:
  - 15 different notification types
  - Priority-based sending
  - Template parameter injection
  - Business context integration

## Queries

### 1. GetEmailStatusQuery
- **Purpose**: Retrieve email delivery status with filtering
- **Features**:
  - Multi-parameter filtering
  - Pagination support
  - Date range filtering
  - Status tracking

### 2. GetEmailTemplatesQuery
- **Purpose**: Get available email templates
- **Features**:
  - Category filtering
  - Search functionality
  - Active status filtering
  - Category count statistics

### 3. GetUserEmailPreferencesQuery
- **Purpose**: Retrieve user email preferences and subscriptions
- **Features**:
  - Comprehensive preference mapping
  - Subscription status tracking
  - Default preference creation
  - Security alert enforcement

## Key Features

### Security
- **No User Enumeration**: Password reset doesn't reveal user existence
- **Token Validation**: Secure token generation and validation
- **Rate Limiting**: Protection against spam and abuse
- **Account Locking**: Integration with user security features

### Performance
- **Async Operations**: All operations are asynchronous
- **Background Processing**: Welcome and confirmation emails sent in background
- **Template Caching**: Email templates are efficiently cached
- **Bulk Operations**: Support for bulk email sending

### Reliability
- **Retry Logic**: Failed emails can be retried automatically
- **Error Handling**: Comprehensive error handling with logging
- **Transaction Support**: Database operations use Unit of Work pattern
- **Idempotent Operations**: Safe to retry commands

### Flexibility
- **Template System**: Configurable email templates
- **Multi-language Support**: Ready for internationalization
- **Custom Parameters**: Dynamic template parameter injection
- **Priority Levels**: Different priority levels for different email types

## Integration Points

### Domain Integration
- **User Aggregate**: Direct integration with User domain model
- **Domain Events**: Triggers domain events for email verification, password changes
- **Business Aggregate**: Integration with business listings and notifications

### Infrastructure Dependencies
- **IEmailService**: Email sending abstraction
- **IEmailTemplateService**: Template rendering service
- **Repository Interfaces**: Data access abstractions
- **IPasswordHashingService**: Password security integration

## Error Handling

All handlers implement comprehensive error handling:

1. **Validation**: FluentValidation rules for all inputs
2. **Domain Validation**: Business rule validation through domain models
3. **Infrastructure Errors**: Graceful handling of email service failures
4. **Logging**: Structured logging for debugging and monitoring
5. **Result Pattern**: Consistent error reporting using Result<T>

## Usage Examples

### Send Email Verification
```csharp
var command = new SendEmailVerificationCommand(userId);
var result = await mediator.Send(command);
```

### Verify Email
```csharp
var command = new VerifyEmailCommand(userId, token);
var result = await mediator.Send(command);
```

### Get Email Status
```csharp
var query = new GetEmailStatusQuery(
    UserId: userId,
    EmailType: EmailType.EmailVerification,
    PageNumber: 1,
    PageSize: 20);
var result = await mediator.Send(query);
```

## Configuration Requirements

The infrastructure layer must provide implementations for:

1. `IEmailService` - Email sending service (SMTP, SendGrid, etc.)
2. `IEmailTemplateService` - Template rendering service
3. `IEmailStatusRepository` - Email status tracking
4. `IEmailTemplateRepository` - Template management
5. `IUserEmailPreferencesRepository` - User preferences

## Testing Strategy

- **Unit Tests**: Each handler has comprehensive unit tests
- **Integration Tests**: End-to-end testing with real email services
- **Mock Services**: Test doubles for all external dependencies
- **Validation Tests**: FluentValidation rule testing
- **Performance Tests**: Load testing for bulk operations

## Future Enhancements

1. **Email Analytics**: Tracking open rates, click rates
2. **A/B Testing**: Template variation testing
3. **Advanced Templates**: Rich HTML templates with images
4. **Webhook Support**: Delivery status webhooks
5. **Email Campaigns**: Marketing campaign management
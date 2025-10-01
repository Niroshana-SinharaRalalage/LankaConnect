# Email & Notifications System - Comprehensive Testing Strategy

## Overview

This document outlines the comprehensive testing strategy for the LankaConnect Email & Notifications system. The strategy follows Clean Architecture principles and achieves 100% test coverage across all layers while maintaining the existing test quality standards.

## Testing Architecture

### Test Pyramid Structure

```
                    /\
                   /E2E\
                  /------\     <- Integration & Performance Tests
                 /  API   \
                /----------\   <- Application Layer Tests  
               /Application \
              /--------------\  <- Infrastructure Layer Tests
             /Infrastructure \
            /------------------\
           /      Domain        \ <- Domain Layer Tests (Foundation)
          /----------------------\
```

## Layer-by-Layer Testing Strategy

### 1. Domain Layer Tests

**Location**: `tests/LankaConnect.Domain.Tests/Email/`

#### Value Objects Tests
- **EmailAddress** (`EmailAddressTests.cs`)
  - Valid email format validation
  - Invalid format rejection
  - Case normalization
  - Display name handling
  - Equality and comparison
  - Implicit string conversion

- **EmailSubject** (`EmailSubjectTests.cs`) 
  - Length validation (max 200 chars)
  - Whitespace trimming
  - Line break prevention
  - Special character handling

- **VerificationToken** (`VerificationTokenTests.cs`)
  - Secure token generation (32+ chars)
  - Expiry time handling
  - Token validation
  - Single-use enforcement
  - Security measures (partial masking)

#### Entity Tests
- **EmailMessage** (`EmailMessageTests.cs`)
  - Entity creation validation
  - State transitions (Pending → Sent → Delivered)
  - Failure handling and retry logic
  - Email tracking (opens, clicks)
  - Audit trail maintenance
  - Business rule enforcement

#### Test Data Builders
- **EmailTestDataBuilder** (`TestHelpers/EmailTestDataBuilder.cs`)
  - Comprehensive email scenario builders
  - Template data generators
  - Status-specific email creators
  - Performance test data generation

### 2. Application Layer Tests

**Location**: `tests/LankaConnect.Application.Tests/Email/`

#### Command Handler Tests
- **SendEmailVerificationCommandHandler** (`Commands/SendEmailVerificationCommandHandlerTests.cs`)
  - Valid email verification flow
  - Non-existent user handling
  - Already verified user prevention
  - Token generation integration
  - Template data preparation
  - Error scenarios and logging

- **VerifyEmailCommandHandler** (`Commands/VerifyEmailCommandHandlerTests.cs`)
  - Token validation workflow
  - User email verification
  - Welcome email trigger
  - Error handling and rollback
  - Concurrent verification prevention

#### Query Handler Tests
- **GetEmailStatusQueryHandler** (`Queries/GetEmailStatusQueryHandlerTests.cs`)
  - Email status retrieval
  - Non-existent email handling
  - Status mapping and DTOs
  - Performance considerations

#### Validation Tests
- FluentValidation rule testing
- Input sanitization
- Business rule validation
- Cross-cutting concern testing

### 3. Infrastructure Layer Tests

**Location**: `tests/LankaConnect.Infrastructure.Tests/Email/`

#### Service Tests
- **EmailService** (`Services/EmailServiceTests.cs`)
  - SMTP integration testing
  - Template rendering integration
  - Queue management
  - Retry logic implementation
  - Configuration handling
  - Development mode behaviors

- **RazorEmailTemplateService** (`Services/RazorEmailTemplateServiceTests.cs`)
  - Template compilation
  - Data binding and rendering
  - Caching mechanisms
  - Error handling
  - Performance optimization
  - Security (HTML encoding)

#### Repository Tests
- **EmailMessageRepository** (`Repositories/EmailMessageRepositoryTests.cs`)
  - CRUD operations
  - Complex queries (status filtering, date ranges)
  - Pagination and sorting
  - Performance with large datasets
  - Concurrency handling

### 4. Integration Tests

**Location**: `tests/LankaConnect.IntegrationTests/Email/`

#### End-to-End Email Flows
- **EmailIntegrationTests** (`EmailIntegrationTests.cs`)
  - Complete email sending workflows
  - Database persistence verification
  - Queue processing validation
  - Template rendering integration
  - Retry mechanism testing
  - Concurrent operation handling

#### MailHog Integration
- **MailHogIntegrationTests** (`MailHogIntegrationTests.cs`)
  - Email delivery verification
  - Content validation (text/HTML)
  - Header inspection
  - Attachment handling
  - Bulk email testing
  - Search and filtering

#### Performance Testing
- **EmailPerformanceTests** (`EmailPerformanceTests.cs`)
  - Single email send timing (<5s)
  - Bulk email queue performance (100 emails <30s)
  - Concurrent sending (20 concurrent <15s)
  - Memory usage monitoring
  - Database operation efficiency
  - Throughput measurement (>1 email/second)

#### API Layer Testing
- **EmailApiTests** (`EmailApiTests.cs`)
  - REST endpoint validation
  - Authentication/authorization
  - Rate limiting verification
  - Content negotiation
  - Error response formatting
  - Webhook handling

## Testing Tools & Frameworks

### Core Testing Stack
- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework
- **AutoFixture**: Test data generation
- **Testcontainers**: PostgreSQL test containers

### Email-Specific Tools
- **MailHog**: SMTP testing server (localhost:1025 SMTP, localhost:8025 Web UI)
- **In-Memory Database**: Entity Framework testing
- **HttpClient**: API testing

### Performance Testing
- **System.Diagnostics.Stopwatch**: Timing measurements
- **Memory profiling**: GC.GetTotalMemory()
- **Concurrent testing**: Task.WhenAll, SemaphoreSlim
- **Load testing**: Bulk operations with metrics

## Test Categories & Scenarios

### Functional Tests
1. **Email Verification Flow**
   - User registration trigger
   - Token generation and storage
   - Email template rendering
   - Delivery confirmation
   - Token validation and expiry

2. **Password Reset Flow**
   - Reset request initiation
   - Secure token generation
   - Email delivery with time limits
   - Token validation and usage

3. **Welcome Email Flow**
   - Post-verification trigger
   - Personalization and templates
   - Delivery tracking

### Non-Functional Tests
1. **Performance Requirements**
   - Single email: <5 seconds
   - Bulk queue (100): <30 seconds  
   - Concurrent (20): <15 seconds
   - Throughput: >1 email/second
   - Memory: <10KB per email

2. **Reliability Tests**
   - SMTP failure recovery
   - Retry mechanisms (exponential backoff)
   - Queue persistence
   - Transaction rollback

3. **Security Tests**
   - Email address validation
   - Template injection prevention
   - Token security (secure generation, single-use)
   - Content sanitization

## Test Data Management

### Builders Pattern
```csharp
// Example usage of test builders
var email = EmailTestDataBuilder
    .CreateEmailVerificationMessage("user@example.com", "secure-token")
    .WithHighPriority()
    .WithCustomTemplate("custom-verification");
```

### Test Scenarios
- **Happy Path**: All operations succeed
- **Edge Cases**: Boundary conditions, null values
- **Error Scenarios**: Network failures, timeouts
- **Concurrent Operations**: Race conditions, deadlocks
- **Performance Stress**: High load, memory constraints

## Coverage Goals & Metrics

### Coverage Targets
- **Domain Layer**: 100% (critical business logic)
- **Application Layer**: 95% (command/query handlers)
- **Infrastructure Layer**: 90% (external integrations)
- **Integration Tests**: Key user journeys
- **Performance Tests**: Critical path operations

### Quality Metrics
- **Test Execution Time**: <2 minutes for full suite
- **Test Reliability**: >99% pass rate
- **Code Coverage**: Maintained at 100%
- **Performance SLAs**: All tests within defined limits

## Continuous Integration

### Test Pipeline Stages
1. **Unit Tests**: Domain + Application layers
2. **Infrastructure Tests**: Database + external services
3. **Integration Tests**: End-to-end workflows
4. **Performance Tests**: Load and stress testing
5. **Coverage Report**: Ensure 100% maintenance

### Environment Requirements
- **PostgreSQL**: Test database
- **MailHog**: Email testing server  
- **Redis**: Caching layer
- **Docker**: Container orchestration

## Best Practices

### Test Organization
1. **Arrange-Act-Assert**: Clear test structure
2. **One Assertion**: Single responsibility per test
3. **Descriptive Names**: Clear test intent
4. **Independent Tests**: No test interdependence
5. **Fast Execution**: Optimize test performance

### Mocking Strategy
- **Mock External Dependencies**: SMTP, databases, APIs
- **Test Real Integrations**: Critical system boundaries  
- **Verify Interactions**: Important method calls
- **Avoid Over-Mocking**: Maintain test value

### Data Management
- **Test Data Builders**: Consistent test data creation
- **Cleanup**: Reset state between tests
- **Isolation**: Each test has clean environment
- **Realistic Data**: Production-like test scenarios

## Maintenance & Evolution

### Regular Activities
1. **Test Review**: Monthly test effectiveness review
2. **Performance Baseline**: Update performance targets
3. **Coverage Analysis**: Identify gaps and improve
4. **Flaky Test Resolution**: Address unreliable tests

### Evolution Strategy
- **New Feature Testing**: Test-first development
- **Regression Prevention**: Comprehensive test coverage
- **Performance Monitoring**: Continuous benchmarking
- **Security Updates**: Regular security test updates

## Troubleshooting Guide

### Common Issues
1. **MailHog Connection**: Ensure Docker container is running
2. **Database Tests**: Check connection strings and migrations
3. **Flaky Tests**: Timing issues, add appropriate waits
4. **Performance Degradation**: Profile and optimize bottlenecks

### Debugging Tips
- Use detailed logging in tests
- Isolate failing components
- Check environment configurations
- Verify test data consistency

## Conclusion

This comprehensive testing strategy ensures the reliability, performance, and maintainability of the LankaConnect email system while maintaining 100% test coverage. The layered approach provides confidence at every level of the architecture, from domain logic to end-user functionality.

The combination of unit tests, integration tests, performance tests, and real email delivery verification through MailHog creates a robust testing foundation that supports continuous delivery and high-quality software releases.
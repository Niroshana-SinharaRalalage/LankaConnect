# ACTION_PLAN.md - Session-by-Session Development Plan
## 3-Month Sprint Breakdown with 4-Hour Session Tasks

**Project:** LankaConnect  
**Timeline:** 12 weeks / 72 sessions  
**Session Duration:** 4 hours each  
**Frequency:** 6 sessions per week

---

## Sprint 0: Infrastructure Setup (Week 0 - 3 Sessions)

### Session 0.1: Azure Infrastructure Setup
**Duration:** 4 hours  
**Prerequisites:** Azure subscription, Azure CLI installed

```yaml
Tasks:
  1. Create Azure Resource Group (30 min)
     - Login to Azure CLI
     - Create resource group in South India region
     - Set up service principal
     
  2. Provision Core Infrastructure (90 min)
     - Create PostgreSQL Flexible Server
     - Set up Redis Cache
     - Create Storage Account
     - Configure Application Insights
     
  3. Configure Networking (60 min)
     - Set up VNet
     - Configure private endpoints
     - Set up firewall rules
     
  4. Create Bicep Templates (60 min)
     - Write main.bicep
     - Create parameters files
     - Test deployment
     
Deliverables:
  - infrastructure/bicep/main.bicep
  - infrastructure/bicep/parameters.dev.json
  - Azure resources running
  - Connection strings documented

Commands:
  ```bash
  az login
  az group create --name rg-lankaconnect-dev --location southindia
  az deployment group create --resource-group rg-lankaconnect-dev --template-file main.bicep
  ```
```

### Session 0.2: Local Development Environment
**Duration:** 4 hours  
**Prerequisites:** Docker Desktop, .NET 8 SDK

```yaml
Tasks:
  1. Create Solution Structure (60 min)
     - Run solution creation script
     - Set up project references
     - Configure Directory.Build.props
     
  2. Docker Configuration (60 min)
     - Create docker-compose.yml
     - Configure local services
     - Test container startup
     
  3. Development Tools Setup (60 min)
     - Install VS Code extensions
     - Configure .editorconfig
     - Set up Git hooks
     
  4. Initial Build & Test (60 min)
     - Restore packages
     - Build solution
     - Run empty test suite
     
Deliverables:
  - Complete solution structure
  - docker-compose.yml configured
  - Development environment ready
  - README.md with setup instructions

Test Points:
  - Docker services start successfully
  - Solution builds without errors
  - Git repository initialized
```

### Session 0.3: CI/CD Pipeline Setup
**Duration:** 4 hours  
**Prerequisites:** GitHub repository created

```yaml
Tasks:
  1. GitHub Actions Workflows (90 min)
     - Create build workflow
     - Set up test workflow
     - Configure code coverage
     
  2. Deployment Pipeline (90 min)
     - Create staging deployment
     - Set up production deployment
     - Configure approvals
     
  3. Security & Quality Gates (60 min)
     - Add security scanning
     - Configure SonarCloud
     - Set up dependency checks
     
Deliverables:
  - .github/workflows/build.yml
  - .github/workflows/deploy.yml
  - GitHub secrets configured
  - First successful pipeline run

Success Criteria:
  - Build completes in < 5 minutes
  - All quality gates passing
  - Deployment to dev successful
```

---

## Sprint 1: Foundation & Domain Models (Weeks 1-2 - 12 Sessions)

### Session 1.1: Base Classes & Common Domain
**Duration:** 4 hours  
**Focus:** Domain foundation

```yaml
Tasks:
  1. Entity Base Class (60 min)
     Tests:
       - Entity_Should_Have_Id
       - Entity_Should_Track_Creation_Date
       - Entity_Should_Raise_Domain_Events
     Implementation:
       - Entity.cs with ID and timestamps
       - Domain event collection
       - Equality members
       
  2. ValueObject Base Class (60 min)
     Tests:
       - ValueObject_Should_Compare_By_Value
       - ValueObject_Should_Be_Immutable
     Implementation:
       - ValueObject.cs abstract class
       - GetEqualityComponents method
       - Equality operators
       
  3. Result Pattern (60 min)
     Tests:
       - Result_Success_Should_Have_Value
       - Result_Failure_Should_Have_Error
     Implementation:
       - Result<T> class
       - Success/Failure factory methods
       - Match method for handling
       
  4. Common Value Objects (60 min)
     Tests:
       - Email_Should_Validate_Format
       - PhoneNumber_Should_Validate_Format
     Implementation:
       - Email value object
       - PhoneNumber value object
       - Money value object

Code Example:
  ```csharp
  public abstract class Entity
  {
      public Guid Id { get; protected set; }
      public DateTime CreatedAt { get; private set; }
      public DateTime? UpdatedAt { get; private set; }
      
      private readonly List<IDomainEvent> _domainEvents = new();
      public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
      
      protected void AddDomainEvent(IDomainEvent domainEvent)
      {
          _domainEvents.Add(domainEvent);
      }
  }
  ```
```

### Session 1.2: Identity Domain Models
**Duration:** 4 hours  
**Focus:** User aggregate and related entities

```yaml
Tasks:
  1. User Aggregate Root (90 min)
     Tests:
       - User_Create_Should_Validate_Email
       - User_Should_Update_Profile
       - User_Should_Change_Status
     Implementation:
       - User.cs aggregate root
       - UserStatus enum
       - Factory and update methods
       
  2. UserProfile Entity (60 min)
     Tests:
       - UserProfile_Should_Validate_Required_Fields
       - UserProfile_Should_Support_Multiple_Languages
     Implementation:
       - UserProfile.cs entity
       - Multi-language support
       - Profile completion tracking
       
  3. PersonName Value Object (45 min)
     Tests:
       - PersonName_Should_Require_FirstName
       - PersonName_Should_Format_Correctly
     Implementation:
       - PersonName value object
       - Formatting logic
       - Validation rules
       
  4. User Domain Events (45 min)
     Tests:
       - Should_Raise_UserRegisteredEvent
       - Should_Raise_ProfileUpdatedEvent
     Implementation:
       - UserRegisteredEvent
       - ProfileUpdatedEvent
       - Event raising logic

Domain Rules:
  - Email must be unique
  - Phone number optional but must be valid format
  - Profile supports Sinhala, Tamil, English
  - User can be Active, Inactive, Suspended
```

### Session 1.3: Event Domain Models (Part 1)
**Duration:** 4 hours  
**Focus:** Event aggregate core

```yaml
Tasks:
  1. Event Aggregate Root (90 min)
     Tests:
       - Event_Create_Should_Validate_Dates
       - Event_Should_Publish_When_Ready
       - Event_Should_Cancel_With_Reason
     Implementation:
       - Event.cs aggregate root
       - EventStatus enum
       - State transition methods
       
  2. Event Value Objects (90 min)
     Tests:
       - Title_Should_Have_Length_Limits
       - Description_Should_Support_Markdown
       - DateTimeRange_Should_Validate_Logic
     Implementation:
       - Title value object (3-200 chars)
       - Description value object
       - DateTimeRange value object
       - Location value object
       
  3. EventCategory Entity (60 min)
     Tests:
       - EventCategory_Should_Have_Unique_Name
       - EventCategory_Should_Support_Hierarchy
     Implementation:
       - EventCategory.cs
       - Parent/child relationships
       - Icon and color properties

Key Validations:
  - End date must be after start date
  - Cannot publish without required fields
  - Cannot modify after event ends
  - Location required for in-person events
```

### Session 1.4: Event Domain Models (Part 2)
**Duration:** 4 hours  
**Focus:** Registration and ticketing

```yaml
Tasks:
  1. Registration Entity (90 min)
     Tests:
       - Registration_Should_Validate_Capacity
       - Registration_Should_Calculate_Total_Price
       - Registration_Should_Support_Cancellation
     Implementation:
       - Registration.cs entity
       - Capacity checking
       - Pricing calculation
       - Cancellation logic
       
  2. TicketType Entity (60 min)
     Tests:
       - TicketType_Should_Have_Price
       - TicketType_Should_Track_Availability
     Implementation:
       - TicketType.cs
       - Pricing tiers
       - Quantity limits
       
  3. Event Methods (90 min)
     Tests:
       - Event_Should_Accept_Registrations
       - Event_Should_Reject_When_Full
       - Event_Should_Calculate_Revenue
     Implementation:
       - Register() method
       - CancelRegistration() method
       - Business rule validations

Business Rules:
  - Cannot register after event starts
  - Early bird pricing until X date
  - Waiting list when at capacity
  - Refund policy enforcement
```

### Session 1.5: Community Domain Models
**Duration:** 4 hours  
**Focus:** Forum and discussion entities

```yaml
Tasks:
  1. Forum Aggregate (60 min)
     Tests:
       - Forum_Should_Have_Categories
       - Forum_Should_Track_Statistics
     Implementation:
       - Forum.cs aggregate
       - ForumCategory.cs
       - Statistics tracking
       
  2. Topic Aggregate (90 min)
     Tests:
       - Topic_Should_Require_Title_And_Content
       - Topic_Should_Support_Tagging
       - Topic_Should_Track_Views
     Implementation:
       - Topic.cs aggregate root
       - Tag support
       - View counting logic
       
  3. Post Entity (90 min)
     Tests:
       - Post_Should_Support_Markdown
       - Post_Should_Allow_Editing
       - Post_Should_Track_Reactions
     Implementation:
       - Post.cs entity
       - Edit history
       - Reaction types

Moderation Rules:
  - New users need approval for first post
  - Spam detection on post creation
  - Report threshold for auto-hiding
```

### Session 1.6: Business Domain Models
**Duration:** 4 hours  
**Focus:** Business directory foundation

```yaml
Tasks:
  1. Business Aggregate (90 min)
     Tests:
       - Business_Should_Validate_Registration
       - Business_Should_Support_Verification
       - Business_Should_Manage_Services
     Implementation:
       - Business.cs aggregate root
       - Verification status
       - Service management
       
  2. Service Entity (60 min)
     Tests:
       - Service_Should_Have_Pricing
       - Service_Should_Define_Duration
     Implementation:
       - Service.cs entity
       - Pricing models
       - Service categories
       
  3. BusinessInfo Value Objects (90 min)
     Tests:
       - ContactInfo_Should_Validate_Formats
       - OpeningHours_Should_Calculate_IsOpen
     Implementation:
       - ContactInfo value object
       - OpeningHours value object
       - BusinessLocation with coordinates

Features:
  - Business verification process
  - Multiple service offerings
  - Flexible pricing models
  - Operating hours per day
```

### Session 1.7: Infrastructure - DbContext Setup
**Duration:** 4 hours  
**Focus:** EF Core configuration

```yaml
Tasks:
  1. AppDbContext Creation (60 min)
     Tests:
       - DbContext_Should_Track_Entities
       - DbContext_Should_Save_Changes
     Implementation:
       - AppDbContext.cs
       - DbSet properties
       - Override SaveChangesAsync
       
  2. Entity Configurations (120 min)
     Tests:
       - User_Configuration_Should_Set_Constraints
       - Event_Configuration_Should_Set_Indexes
     Implementation:
       - UserConfiguration.cs
       - EventConfiguration.cs
       - Other entity configs
       
  3. Value Object Conversions (60 min)
     Tests:
       - Email_Should_Persist_As_String
       - Money_Should_Persist_Correctly
     Implementation:
       - Value converters
       - Owned entity configs
       
  4. Initial Migration (60 min)
     - Create migration
     - Review SQL
     - Update database

Configuration Examples:
  ```csharp
  builder.HasIndex(u => u.Email).IsUnique();
  builder.OwnsOne(u => u.Name, name =>
  {
      name.Property(n => n.FirstName).HasMaxLength(100);
      name.Property(n => n.LastName).HasMaxLength(100);
  });
  ```
```

### Session 1.8: Repository Pattern Implementation
**Duration:** 4 hours  
**Focus:** Generic and specific repositories

```yaml
Tasks:
  1. Generic Repository (90 min)
     Tests:
       - Repository_Should_Add_Entity
       - Repository_Should_Find_By_Id
       - Repository_Should_Update_Entity
     Implementation:
       - IRepository<T> interface
       - Repository<T> base class
       - Unit of Work pattern
       
  2. User Repository (60 min)
     Tests:
       - Should_Find_User_By_Email
       - Should_Check_Email_Exists
     Implementation:
       - IUserRepository interface
       - UserRepository class
       - Custom query methods
       
  3. Event Repository (90 min)
     Tests:
       - Should_Get_Upcoming_Events
       - Should_Search_Events
       - Should_Get_With_Registrations
     Implementation:
       - IEventRepository
       - EventRepository
       - Complex queries

Patterns:
  - Repository returns domain entities
  - Specification pattern for queries
  - Async throughout
  - Include handling for eager loading
```

### Session 1.9: Application Layer - CQRS Setup
**Duration:** 4 hours  
**Focus:** MediatR and base classes

```yaml
Tasks:
  1. MediatR Configuration (60 min)
     Tests:
       - MediatR_Should_Resolve_Handlers
       - Pipeline_Should_Execute
     Implementation:
       - MediatR registration
       - Pipeline behaviors
       - DI configuration
       
  2. Command/Query Base Classes (60 min)
     Tests:
       - Command_Should_Return_Result
       - Query_Should_Be_Readonly
     Implementation:
       - ICommand interface
       - IQuery interface
       - Base handler classes
       
  3. Validation Pipeline (90 min)
     Tests:
       - Should_Validate_Before_Handler
       - Should_Return_Validation_Errors
     Implementation:
       - ValidationBehavior
       - FluentValidation setup
       - Error handling
       
  4. Logging Pipeline (30 min)
     Implementation:
       - LoggingBehavior
       - Performance tracking
       - Error logging

Example:
  ```csharp
  public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      public async Task<TResponse> Handle(TRequest request, ...)
      {
          var failures = _validators
              .Select(v => v.Validate(request))
              .SelectMany(result => result.Errors)
              .Where(f => f != null)
              .ToList();
      }
  }
  ```
```

### Session 1.10: User Registration Flow
**Duration:** 4 hours  
**Focus:** First complete feature

```yaml
Tasks:
  1. RegisterUserCommand (90 min)
     Tests:
       - Should_Create_User_With_Valid_Data
       - Should_Fail_With_Duplicate_Email
       - Should_Send_Verification_Email
     Implementation:
       - RegisterUserCommand
       - RegisterUserCommandHandler
       - RegisterUserCommandValidator
       
  2. Email Service Interface (60 min)
     Tests:
       - Should_Send_Email
       - Should_Handle_Template
     Implementation:
       - IEmailService interface
       - Email templates
       - Mock implementation
       
  3. User Controller (60 min)
     Tests:
       - POST_Register_Should_Return_201
       - Should_Return_400_For_Invalid
     Implementation:
       - UsersController
       - Register endpoint
       - Response DTOs
       
  4. Integration Test (30 min)
     - Full flow test
     - Database verification
     - Email sending verification

Validation Rules:
  - Email required and valid format
  - Password min 8 chars
  - Terms must be accepted
  - Phone optional but validated if provided
```

### Session 1.11: API Base Setup
**Duration:** 4 hours  
**Focus:** API infrastructure

```yaml
Tasks:
  1. Startup Configuration (90 min)
     Implementation:
       - Program.cs setup
       - Service registration
       - Middleware pipeline
       - Swagger configuration
       
  2. Base Controller (60 min)
     Tests:
       - Should_Return_Consistent_Responses
       - Should_Handle_Errors
     Implementation:
       - BaseApiController
       - Standard responses
       - Error handling
       
  3. Exception Middleware (60 min)
     Tests:
       - Should_Catch_Exceptions
       - Should_Log_Errors
       - Should_Return_ProblemDetails
     Implementation:
       - GlobalExceptionMiddleware
       - Error response format
       - Logging integration
       
  4. API Versioning (30 min)
     - Configure versioning
     - Version in URL/header
     - Swagger per version

Middleware Order:
  1. Exception handling
  2. Authentication
  3. Rate limiting
  4. Request logging
  5. Response caching
```

### Session 1.12: Sprint 1 Completion
**Duration:** 4 hours  
**Focus:** Review and documentation

```yaml
Tasks:
  1. Code Review & Refactoring (90 min)
     - Review all code
     - Refactor duplications
     - Improve naming
     - Add missing tests
     
  2. Documentation (60 min)
     - API documentation
     - README updates
     - Architecture decisions
     - Setup instructions
     
  3. Performance Check (60 min)
     - Run load tests
     - Check query performance
     - Optimize if needed
     
  4. Sprint Demo Prep (30 min)
     - Prepare demo script
     - Test all features
     - Note improvements

Sprint 1 Deliverables:
  ✓ Domain models for all contexts
  ✓ Repository pattern implemented
  ✓ CQRS infrastructure ready
  ✓ User registration working
  ✓ API base configured
  ✓ 80%+ test coverage
```

---

## Sprint 2: Identity & Authentication (Weeks 3-4 - 12 Sessions)

### Session 2.1: Azure AD B2C Setup
**Duration:** 4 hours  
**Focus:** Identity provider configuration

```yaml
Tasks:
  1. Azure AD B2C Tenant (90 min)
     - Create B2C tenant
     - Configure applications
     - Set up user flows
     - Custom policies
     
  2. User Flow Configuration (90 min)
     - Sign up/sign in flow
     - Password reset flow
     - Profile edit flow
     - MFA setup (optional)
     
  3. API Integration (60 min)
     Tests:
       - Should_Validate_JWT_Token
       - Should_Extract_Claims
     Implementation:
       - JWT authentication
       - Authorization policies
       - Claims transformation

Configuration:
  - Enable email/phone signup
  - Custom attributes for profile
  - Localization for 3 languages
  - Brand customization
```

### Session 2.2: Authentication Implementation
**Duration:** 4 hours  
**Focus:** JWT and refresh tokens

```yaml
Tasks:
  1. JWT Service (90 min)
     Tests:
       - Should_Generate_Valid_Token
       - Should_Validate_Token
       - Should_Extract_Claims
     Implementation:
       - IJwtService interface
       - Token generation
       - Token validation
       
  2. Refresh Token (90 min)
     Tests:
       - Should_Generate_Refresh_Token
       - Should_Validate_And_Rotate
       - Should_Revoke_Token
     Implementation:
       - RefreshToken entity
       - Token rotation
       - Revocation logic
       
  3. Auth Controller (60 min)
     Tests:
       - Login_Should_Return_Tokens
       - Refresh_Should_Return_New_Tokens
     Implementation:
       - AuthController
       - Login endpoint
       - Refresh endpoint

Security:
  - 15-min access token
  - 7-day refresh token
  - Token rotation on use
  - Device tracking
```

### Session 2.3: Authorization Policies
**Duration:** 4 hours  
**Focus:** Role and policy-based auth

```yaml
Tasks:
  1. Role Management (90 min)
     Tests:
       - Should_Assign_Roles
       - Should_Check_Role_Claims
     Implementation:
       - Role entities
       - Role assignment
       - Default roles
       
  2. Authorization Policies (90 min)
     Tests:
       - Should_Require_Authenticated_User
       - Should_Check_Resource_Owner
     Implementation:
       - Policy definitions
       - Custom handlers
       - Resource-based auth
       
  3. Authorize Attributes (60 min)
     Implementation:
       - Custom attributes
       - Policy application
       - Controller decoration

Policies:
  - RequireAdmin
  - RequireModerator  
  - RequireBusinessOwner
  - RequireEmailVerified
  - ResourceOwner
```

### Session 2.4: User Profile Management
**Duration:** 4 hours  
**Focus:** Profile CRUD operations

```yaml
Tasks:
  1. Get Profile Query (60 min)
     Tests:
       - Should_Return_Own_Profile
       - Should_Return_Public_Profile
     Implementation:
       - GetProfileQuery
       - Privacy levels
       - DTO mapping
       
  2. Update Profile Command (90 min)
     Tests:
       - Should_Update_Basic_Info
       - Should_Validate_Changes
       - Should_Track_Completion
     Implementation:
       - UpdateProfileCommand
       - Validation rules
       - Completion percentage
       
  3. Profile Preferences (90 min)
     Tests:
       - Should_Save_Language_Preference
       - Should_Update_Notifications
     Implementation:
       - Preferences entity
       - Settings management
       - Default values

Features:
  - Multi-language profiles
  - Avatar upload
  - Privacy settings
  - Notification preferences
```

### Session 2.5: Social Authentication
**Duration:** 4 hours  
**Focus:** OAuth providers

```yaml
Tasks:
  1. Google Authentication (90 min)
     Tests:
       - Should_Validate_Google_Token
       - Should_Create_User_From_Google
     Implementation:
       - Google OAuth setup
       - Token validation
       - User creation/linking
       
  2. Facebook Authentication (90 min)
     Tests:
       - Should_Validate_FB_Token
       - Should_Link_Existing_User
     Implementation:
       - Facebook OAuth
       - Account linking
       - Profile import
       
  3. External Login Flow (60 min)
     Tests:
       - Should_Handle_New_User
       - Should_Link_Existing
     Implementation:
       - External login command
       - Linking logic
       - Error handling

Rules:
  - Email must match for linking
  - Import name/photo from provider
  - Still require terms acceptance
```

### Session 2.6: Security Features
**Duration:** 4 hours  
**Focus:** Security hardening

```yaml
Tasks:
  1. Account Security (90 min)
     Tests:
       - Should_Track_Login_Attempts
       - Should_Lock_After_Failures
     Implementation:
       - Login tracking
       - Account lockout
       - Suspicious activity
       
  2. Password Management (90 min)
     Tests:
       - Should_Validate_Password_Strength
       - Should_Hash_Securely
       - Should_Support_Reset
     Implementation:
       - Password policies
       - Secure hashing
       - Reset flow
       
  3. Audit Logging (60 min)
     Tests:
       - Should_Log_Security_Events
       - Should_Track_Changes
     Implementation:
       - Audit log entity
       - Security events
       - Change tracking

Security Measures:
  - Argon2 password hashing
  - Account lockout after 5 attempts
  - Email verification required
  - Security event notifications
```

---

## Sprint 3: Event Management (Weeks 5-6 - 12 Sessions)

### Session 3.1: Event Creation Flow
**Duration:** 4 hours  
**Focus:** Create event feature

```yaml
Tasks:
  1. CreateEventCommand (90 min)
     Tests:
       - Should_Create_Valid_Event
       - Should_Validate_Dates
       - Should_Set_Organizer
     Implementation:
       - Command and handler
       - Business validation
       - Event creation
       
  2. Event DTOs (60 min)
     Tests:
       - Should_Map_To_DTO
       - Should_Include_Relations
     Implementation:
       - EventDto
       - EventDetailDto
       - AutoMapper profiles
       
  3. Events Controller (90 min)
     Tests:
       - POST_Should_Create_Event
       - Should_Return_Created_Event
     Implementation:
       - EventsController
       - Create endpoint
       - Response formatting

Validations:
  - Title 3-200 characters
  - Future date required
  - Category must exist
  - Location required
```

### Session 3.2: Event Listing & Search
**Duration:** 4 hours  
**Focus:** Query operations

```yaml
Tasks:
  1. GetEventsQuery (90 min)
     Tests:
       - Should_Return_Paged_Results
       - Should_Filter_By_Category
       - Should_Sort_By_Date
     Implementation:
       - Query and handler
       - Pagination
       - Filtering logic
       
  2. Search Implementation (90 min)
     Tests:
       - Should_Search_Title_Description
       - Should_Filter_By_Location
       - Should_Apply_Date_Range
     Implementation:
       - Search query
       - Full-text search
       - Complex filters
       
  3. Performance Optimization (60 min)
     - Query optimization
     - Index creation
     - Response caching

Features:
  - Pagination with metadata
  - Multiple sort options
  - Location-based search
  - Category filtering
```

### Session 3.3: Event Details & Updates
**Duration:** 4 hours  
**Focus:** Event management

```yaml
Tasks:
  1. GetEventByIdQuery (60 min)
     Tests:
       - Should_Return_Full_Details
       - Should_Include_Stats
     Implementation:
       - Detailed query
       - Include relations
       - View tracking
       
  2. UpdateEventCommand (90 min)
     Tests:
       - Should_Update_If_Owner
       - Should_Validate_Changes
       - Should_Prevent_Past_Changes
     Implementation:
       - Update command
       - Authorization check
       - Validation rules
       
  3. Event Status Management (90 min)
     Tests:
       - Should_Publish_Event
       - Should_Cancel_With_Reason
     Implementation:
       - Status transitions
       - Publish command
       - Cancel command

Rules:
  - Only organizer can edit
  - Cannot change past events
  - Cancellation needs reason
  - Notify registrants on changes
```

### Session 3.4: Event Registration System
**Duration:** 4 hours  
**Focus:** Registration feature

```yaml
Tasks:
  1. RegisterForEventCommand (90 min)
     Tests:
       - Should_Register_If_Space
       - Should_Reject_If_Full
       - Should_Calculate_Price
     Implementation:
       - Registration command
       - Capacity checking
       - Pricing logic
       
  2. Registration Management (90 min)
     Tests:
       - Should_List_Registrations
       - Should_Cancel_Registration
       - Should_Transfer_Ticket
     Implementation:
       - List registrations
       - Cancellation logic
       - Transfer feature
       
  3. Waiting List (60 min)
     Tests:
       - Should_Add_To_Waitlist
       - Should_Promote_From_Waitlist
     Implementation:
       - Waitlist logic
       - Auto-promotion
       - Notifications

Features:
  - Multiple ticket types
  - Early bird pricing
  - Group registrations
  - QR code generation
```

### Session 3.5: Calendar & Scheduling
**Duration:** 4 hours  
**Focus:** Calendar features

```yaml
Tasks:
  1. Calendar View Query (90 min)
     Tests:
       - Should_Return_Month_View
       - Should_Group_By_Date
     Implementation:
       - Calendar query
       - Date grouping
       - Event formatting
       
  2. ICS Export (90 min)
     Tests:
       - Should_Generate_Valid_ICS
       - Should_Include_Details
     Implementation:
       - ICS generation
       - Calendar standards
       - Download endpoint
       
  3. Recurring Events (60 min)
     Tests:
       - Should_Create_Series
       - Should_Handle_Exceptions
     Implementation:
       - Recurrence rules
       - Series management
       - Exception dates

Standards:
  - iCalendar RFC 5545
  - Timezone handling
  - Reminder support
```

### Session 3.6: Event Analytics
**Duration:** 4 hours  
**Focus:** Analytics and reporting

```yaml
Tasks:
  1. Event Statistics (90 min)
     Tests:
       - Should_Track_Views
       - Should_Calculate_Conversion
     Implementation:
       - View tracking
       - Registration stats
       - Revenue calculation
       
  2. Analytics Queries (90 min)
     Tests:
       - Should_Return_Demographics
       - Should_Show_Trends
     Implementation:
       - Analytics queries
       - Demographic breakdown
       - Time-based trends
       
  3. Organizer Dashboard (60 min)
     Implementation:
       - Dashboard DTOs
       - Summary statistics
       - Chart data

Metrics:
  - View count
  - Registration rate
  - Revenue tracking
  - Geographic distribution
```

---

## Sprint 4: Community Features (Weeks 7-8 - 12 Sessions)

### Session 4.1: Forum Structure
**Duration:** 4 hours  
**Focus:** Forum foundation

```yaml
Tasks:
  1. Forum Setup Commands (90 min)
     Tests:
       - Should_Create_Categories
       - Should_Set_Permissions
     Implementation:
       - Forum categories
       - Permission system
       - Moderation settings
       
  2. Forum Queries (90 min)
     Tests:
       - Should_List_Categories
       - Should_Show_Statistics
     Implementation:
       - Category listing
       - Post counts
       - Active topics
       
  3. Forum Controller (60 min)
     Implementation:
       - ForumController
       - Category endpoints
       - Permission checks

Structure:
  - Hierarchical categories
  - Permission per category
  - Pinned topics
  - Announcement support
```

### Session 4.2: Topic Management
**Duration:** 4 hours  
**Focus:** Discussion topics

```yaml
Tasks:
  1. CreateTopicCommand (90 min)
     Tests:
       - Should_Create_In_Category
       - Should_Validate_Content
       - Should_Check_Permissions
     Implementation:
       - Topic creation
       - Content validation
       - Tag support
       
  2. Topic Queries (90 min)
     Tests:
       - Should_List_By_Category
       - Should_Sort_By_Activity
       - Should_Search_Topics
     Implementation:
       - Topic listing
       - Search functionality
       - Sorting options
       
  3. Topic Interactions (60 min)
     Tests:
       - Should_Track_Views
       - Should_Pin_Topic
       - Should_Lock_Topic
     Implementation:
       - View counting
       - Moderation actions
       - Topic states

Features:
  - Rich text editor
  - Image attachments
  - Topic tagging
  - Subscribe to topic
```

### Session 4.3: Post & Reply System
**Duration:** 4 hours  
**Focus:** Discussion posts

```yaml
Tasks:
  1. CreatePostCommand (90 min)
     Tests:
       - Should_Add_To_Topic
       - Should_Support_Markdown
       - Should_Notify_Subscribers
     Implementation:
       - Post creation
       - Markdown parsing
       - Notifications
       
  2. Post Interactions (90 min)
     Tests:
       - Should_Edit_Own_Post
       - Should_Add_Reactions
       - Should_Quote_Reply
     Implementation:
       - Edit functionality
       - Reaction system
       - Quote support
       
  3. Threading Support (60 min)
     Tests:
       - Should_Support_Nested_Replies
       - Should_Maintain_Order
     Implementation:
       - Reply threading
       - Nested display
       - Pagination

Rules:
  - Edit window: 15 minutes
  - Markdown with sanitization
  - Max 3 levels of nesting
```

### Session 4.4: Real-time Features
**Duration:** 4 hours  
**Focus:** SignalR implementation

```yaml
Tasks:
  1. SignalR Hub Setup (90 min)
     Tests:
       - Should_Connect_Authenticated
       - Should_Join_Groups
     Implementation:
       - ForumHub setup
       - Authentication
       - Group management
       
  2. Live Notifications (90 min)
     Tests:
       - Should_Notify_New_Post
       - Should_Show_Typing
       - Should_Update_Online_Users
     Implementation:
       - New post notifications
       - Typing indicators
       - Presence tracking
       
  3. Real-time Updates (60 min)
     Implementation:
       - Post reactions live
       - Edit notifications
       - View count updates

Features:
  - Typing indicators
  - Online user list
  - Live post updates
  - Notification badges
```

### Session 4.5: Moderation System
**Duration:** 4 hours  
**Focus:** Content moderation

```yaml
Tasks:
  1. Reporting System (90 min)
     Tests:
       - Should_Report_Content
       - Should_Track_Reports
       - Should_Auto_Hide
     Implementation:
       - Report command
       - Report tracking
       - Threshold actions
       
  2. Moderation Queue (90 min)
     Tests:
       - Should_List_Reports
       - Should_Take_Action
       - Should_Track_History
     Implementation:
       - Moderation queries
       - Action commands
       - Audit trail
       
  3. Auto-moderation (60 min)
     Tests:
       - Should_Detect_Spam
       - Should_Filter_Words
     Implementation:
       - Spam detection
       - Word filtering
       - New user limits

Actions:
  - Hide post
  - Delete post
  - Warn user
  - Suspend user
```

### Session 4.6: Community Analytics
**Duration:** 4 hours  
**Focus:** Forum analytics

```yaml
Tasks:
  1. Engagement Metrics (90 min)
     Tests:
       - Should_Track_Active_Users
       - Should_Calculate_Engagement
     Implementation:
       - User activity tracking
       - Engagement scores
       - Trend analysis
       
  2. Content Analytics (90 min)
     Tests:
       - Should_Track_Popular_Topics
       - Should_Analyze_Tags
     Implementation:
       - Popular content
       - Tag analytics
       - Category stats
       
  3. Leaderboards (60 min)
     Implementation:
       - Top contributors
       - Most helpful
       - Point system

Metrics:
  - Daily active users
  - Posts per user
  - Response times
  - Popular topics
```

---

## Sprint 5: Business Directory (Weeks 9-10 - 12 Sessions)

### Session 5.1: Business Registration
**Duration:** 4 hours  
**Focus:** Business onboarding

```yaml
Tasks:
  1. RegisterBusinessCommand (90 min)
     Tests:
       - Should_Create_Business
       - Should_Validate_Details
       - Should_Assign_Owner
     Implementation:
       - Business registration
       - Validation rules
       - Owner assignment
       
  2. Business Verification (90 min)
     Tests:
       - Should_Submit_Documents
       - Should_Track_Status
       - Should_Notify_Decision
     Implementation:
       - Document upload
       - Verification flow
       - Status tracking
       
  3. Business Controller (60 min)
     Implementation:
       - BusinessController
       - Registration endpoint
       - Verification endpoints

Requirements:
  - Business registration number
  - Contact verification
  - Document upload
  - Admin approval
```

### Session 5.2: Service Management
**Duration:** 4 hours  
**Focus:** Service catalog

```yaml
Tasks:
  1. Service Commands (90 min)
     Tests:
       - Should_Add_Service
       - Should_Set_Pricing
       - Should_Define_Duration
     Implementation:
       - Add service command
       - Update service
       - Pricing models
       
  2. Service Queries (90 min)
     Tests:
       - Should_List_By_Business
       - Should_Filter_By_Category
     Implementation:
       - Service listing
       - Category filtering
       - Price ranges
       
  3. Availability Management (60 min)
     Tests:
       - Should_Set_Schedule
       - Should_Block_Dates
     Implementation:
       - Schedule setting
       - Availability rules
       - Holiday handling

Pricing Models:
  - Fixed price
  - Hourly rate
  - Custom quote
  - Package deals
```

### Session 5.3: Business Search
**Duration:** 4 hours  
**Focus:** Discovery features

```yaml
Tasks:
  1. Search Implementation (90 min)
     Tests:
       - Should_Search_By_Name
       - Should_Filter_Location
       - Should_Sort_By_Rating
     Implementation:
       - Search query
       - Location filtering
       - Multi-criteria sort
       
  2. Advanced Filters (90 min)
     Tests:
       - Should_Filter_Price_Range
       - Should_Filter_Availability
       - Should_Filter_Features
     Implementation:
       - Price filtering
       - Availability check
       - Feature tags
       
  3. Map Integration (60 min)
     Implementation:
       - Location search
       - Distance calculation
       - Map view data

Features:
  - Radius search
  - Category filters
  - Price ranges
  - Open now
```

### Session 5.4: Booking System
**Duration:** 4 hours  
**Focus:** Service bookings

```yaml
Tasks:
  1. CreateBookingCommand (90 min)
     Tests:
       - Should_Check_Availability
       - Should_Calculate_Price
       - Should_Send_Confirmation
     Implementation:
       - Booking creation
       - Availability check
       - Price calculation
       
  2. Booking Management (90 min)
     Tests:
       - Should_List_Bookings
       - Should_Cancel_Booking
       - Should_Reschedule
     Implementation:
       - Booking queries
       - Cancellation logic
       - Rescheduling
       
  3. Calendar Integration (60 min)
     Implementation:
       - Booking calendar
       - Time slot management
       - Conflict detection

Rules:
  - Advance booking required
  - Cancellation policy
  - No double booking
  - Buffer time between
```

### Session 5.5: Review System
**Duration:** 4 hours  
**Focus:** Ratings and reviews

```yaml
Tasks:
  1. SubmitReviewCommand (90 min)
     Tests:
       - Should_Require_Booking
       - Should_Validate_Rating
       - Should_Update_Average
     Implementation:
       - Review submission
       - Rating validation
       - Average calculation
       
  2. Review Display (90 min)
     Tests:
       - Should_List_Reviews
       - Should_Show_Statistics
       - Should_Filter_Verified
     Implementation:
       - Review listing
       - Statistics calc
       - Verification badge
       
  3. Response System (60 min)
     Tests:
       - Should_Allow_Owner_Response
       - Should_Notify_Reviewer
     Implementation:
       - Owner responses
       - Notification system
       - Display logic

Features:
  - 1-5 star rating
  - Photo reviews
  - Verified badge
  - Owner responses
```

### Session 5.6: Business Analytics
**Duration:** 4 hours  
**Focus:** Business insights

```yaml
Tasks:
  1. Performance Metrics (90 min)
     Tests:
       - Should_Track_Views
       - Should_Calculate_Conversion
     Implementation:
       - View tracking
       - Inquiry tracking
       - Booking conversion
       
  2. Revenue Analytics (90 min)
     Tests:
       - Should_Calculate_Revenue
       - Should_Show_Trends
     Implementation:
       - Revenue calculation
       - Trend analysis
       - Forecasting
       
  3. Dashboard Data (60 min)
     Implementation:
       - Dashboard queries
       - Chart data
       - Export functionality

Metrics:
  - Profile views
  - Booking rate
  - Revenue trends
  - Customer retention
```

---

## Sprint 6: Integration & Polish (Weeks 11-12 - 12 Sessions)

### Session 6.1: Payment Integration
**Duration:** 4 hours  
**Focus:** Payment processing

```yaml
Tasks:
  1. Payment Gateway Setup (90 min)
     - Stripe/PayPal config
     - Webhook endpoints
     - Test credentials
     
  2. Payment Processing (90 min)
     Tests:
       - Should_Process_Payment
       - Should_Handle_Webhooks
     Implementation:
       - Payment service
       - Webhook handling
       - Status updates
       
  3. Refund System (60 min)
     Tests:
       - Should_Process_Refunds
       - Should_Apply_Policy
     Implementation:
       - Refund logic
       - Policy enforcement
       - Accounting

Features:
  - Card payments
  - Bank transfers
  - Refund automation
  - Invoice generation
```

### Session 6.2: Email System
**Duration:** 4 hours  
**Focus:** Transactional emails

```yaml
Tasks:
  1. Email Service (90 min)
     Tests:
       - Should_Send_Emails
       - Should_Use_Templates
     Implementation:
       - SendGrid integration
       - Template system
       - Queue processing
       
  2. Email Templates (90 min)
     Implementation:
       - Registration welcome
       - Booking confirmation
       - Event reminders
       - Review requests
       
  3. Localization (60 min)
     Tests:
       - Should_Send_In_User_Language
     Implementation:
       - Multi-language templates
       - Language detection
       - Fallback logic

Templates:
  - Welcome email
  - Verification
  - Password reset
  - Booking confirmation
```

### Session 6.3: Performance Optimization
**Duration:** 4 hours  
**Focus:** Speed improvements

```yaml
Tasks:
  1. Caching Implementation (90 min)
     Tests:
       - Should_Cache_Responses
       - Should_Invalidate_On_Change
     Implementation:
       - Redis caching
       - Cache strategies
       - Invalidation logic
       
  2. Query Optimization (90 min)
     - Analyze slow queries
     - Add indexes
     - Optimize N+1
     - Query caching
     
  3. Response Compression (60 min)
     Implementation:
       - Gzip compression
       - Static file caching
       - CDN integration

Targets:
  - < 200ms API response
  - < 50ms DB queries
  - 80% cache hit rate
```

### Session 6.4: Security Hardening
**Duration:** 4 hours  
**Focus:** Security improvements

```yaml
Tasks:
  1. Security Audit (90 min)
     - OWASP checklist
     - Vulnerability scan
     - Code analysis
     - Fix issues
     
  2. Rate Limiting (90 min)
     Tests:
       - Should_Limit_Requests
       - Should_Track_By_User
     Implementation:
       - Rate limiter
       - Per-endpoint limits
       - User tracking
       
  3. Security Headers (60 min)
     Implementation:
       - CORS policy
       - CSP headers
       - HSTS setup
       - XSS protection

Measures:
  - Input sanitization
  - SQL injection prevention
  - XSS protection
  - CSRF tokens
```

### Session 6.5: Monitoring Setup
**Duration:** 4 hours  
**Focus:** Observability

```yaml
Tasks:
  1. Application Insights (90 min)
     - Configure AI
     - Custom metrics
     - Alerts setup
     - Dashboard creation
     
  2. Health Checks (90 min)
     Tests:
       - Should_Check_Dependencies
       - Should_Report_Status
     Implementation:
       - Health endpoints
       - Dependency checks
       - Status page
       
  3. Error Tracking (60 min)
     Implementation:
       - Error logging
       - Alert rules
       - Incident response

Monitoring:
  - Performance metrics
  - Error rates
  - User analytics
  - Business KPIs
```

### Session 6.6: Final Testing
**Duration:** 4 hours  
**Focus:** Quality assurance

```yaml
Tasks:
  1. End-to-End Testing (90 min)
     - User registration flow
     - Event creation/booking
     - Forum participation
     - Business listing
     
  2. Load Testing (90 min)
     - Setup k6/JMeter
     - Run load tests
     - Analyze results
     - Fix bottlenecks
     
  3. UAT Preparation (60 min)
     - Test data setup
     - User guides
     - Known issues
     - Feedback forms

Test Coverage:
  - Unit tests > 80%
  - Integration tests
  - E2E critical paths
  - Performance benchmarks
```

### Session 6.7: Documentation
**Duration:** 4 hours  
**Focus:** Technical documentation

```yaml
Tasks:
  1. API Documentation (90 min)
     - Swagger/OpenAPI
     - Example requests
     - Error codes
     - Authentication guide
     
  2. Developer Guide (90 min)
     - Architecture overview
     - Setup instructions
     - Coding standards
     - Contribution guide
     
  3. Operations Manual (60 min)
     - Deployment process
     - Monitoring guide
     - Troubleshooting
     - Backup procedures

Deliverables:
  - API reference
  - Developer wiki
  - Operations runbook
  - Architecture diagrams
```

### Session 6.8: Deployment Preparation
**Duration:** 4 hours  
**Focus:** Production readiness

```yaml
Tasks:
  1. Environment Setup (90 min)
     - Production resources
     - Configuration management
     - Secrets setup
     - SSL certificates
     
  2. Database Migration (90 min)
     - Migration scripts
     - Seed data
     - Backup plan
     - Rollback procedure
     
  3. Deployment Pipeline (60 min)
     - Final CI/CD check
     - Smoke tests
     - Rollback plan
     - Go-live checklist

Checklist:
  - All tests passing
  - Performance validated
  - Security verified
  - Monitoring active
```

### Session 6.9: Production Deployment
**Duration:** 4 hours  
**Focus:** Go-live

```yaml
Tasks:
  1. Pre-deployment (60 min)
     - Final backup
     - Team briefing
     - Support ready
     - Rollback prepared
     
  2. Deployment (120 min)
     - Database migration
     - Application deployment
     - Configuration verify
     - Smoke tests
     
  3. Post-deployment (60 min)
     - Monitor metrics
     - Check errors
     - User feedback
     - Quick fixes

Success Criteria:
  - All services running
  - No critical errors
  - Performance normal
  - Users can access
```

---

## Phase 1 Complete - Maintenance Mode

### Ongoing Tasks (2-4 hours/week)
```yaml
Weekly Maintenance:
  - Bug fixes
  - Performance monitoring
  - User feedback review
  - Security updates
  
Bi-weekly:
  - Dependency updates
  - Performance review
  - Feature requests triage
  - Team retrospective
  
Monthly:
  - Security audit
  - Backup verification
  - Metrics review
  - Roadmap planning
```

---

## Success Metrics

### Technical Metrics
- Code Coverage: > 80%
- API Response Time: < 200ms (p95)
- Zero Critical Bugs
- All Tests Passing
- Documentation Complete

### Business Metrics
- Core Features Working
- User Registration Active
- Events Being Created
- Forum Activity Started
- Businesses Listed

### Project Metrics
- On Time: 12 weeks
- On Budget: Azure costs controlled
- Quality: Meeting standards
- Team: Knowledge transferred

---

## Notes for Claude Agents

1. **Always Start With Tests** - TDD is mandatory
2. **Respect Architecture** - Clean Architecture boundaries
3. **Document Decisions** - ADRs for major choices
4. **Performance Matters** - Consider scale from day 1
5. **Security First** - Never compromise on security

This action plan provides a clear path from empty repository to production-ready Phase 1 of LankaConnect.
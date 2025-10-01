# Implementation Roadmap: Infrastructure Dependencies Resolution

## Executive Summary

This roadmap provides a detailed implementation plan for resolving Infrastructure layer dependencies that are preventing EF Core migrations. The plan follows Test-Driven Development principles and maintains Clean Architecture compliance throughout the process.

## Timeline Overview

**Total Estimated Duration**: 3 days (24 hours)  
**Success Criteria**: EF Core migrations run successfully with all dependencies resolved  
**Risk Mitigation**: Incremental approach with validation at each milestone  

## Phase 1: Critical Dependencies (Day 1)

### Morning Session (4 hours): Migration Blockers

#### Milestone 1.1: IMemoryCache Registration (1 hour)
**Time**: 09:00 - 10:00  
**Priority**: CRITICAL  
**Owner**: Infrastructure Team  

**Tasks**:
1. **Add IMemoryCache to Infrastructure DI** (30 min)
   ```csharp
   services.AddMemoryCache(options =>
   {
       options.SizeLimit = 100;
       options.CompactionPercentage = 0.25;
       options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
   });
   ```

2. **Configure cache settings in appsettings.json** (15 min)
   ```json
   "MemoryCache": {
     "SizeLimit": 100,
     "CompactionPercentage": 0.25,
     "ExpirationScanFrequencyMinutes": 5
   }
   ```

3. **Test RazorEmailTemplateService resolution** (15 min)
   ```bash
   dotnet run --project src/LankaConnect.API
   # Verify: No IMemoryCache resolution errors
   ```

**Success Metrics**:
- [ ] RazorEmailTemplateService instantiates successfully
- [ ] Memory cache configuration loads from settings
- [ ] Template caching works in integration tests

**Risk**: Low - Simple service registration  
**Rollback**: Remove AddMemoryCache() if issues occur

#### Milestone 1.2: BusinessLocation EF Core Configuration (3 hours)  
**Time**: 10:00 - 13:00  
**Priority**: CRITICAL  
**Owner**: Domain/Infrastructure Team

**Tasks**:

1. **Create BusinessConfiguration class** (45 min)
   ```csharp
   public class BusinessConfiguration : IEntityTypeConfiguration<Business>
   {
       public void Configure(EntityTypeBuilder<Business> builder)
       {
           builder.OwnsOne(b => b.Location, location =>
           {
               // Configure Address as owned type
               location.OwnsOne(l => l.Address, address => {
                   address.Property(a => a.Street).HasColumnName("Location_Address_Street");
                   // ... other address properties
               });
               
               // Configure Coordinates as owned type  
               location.OwnsOne(l => l.Coordinates, coords => {
                   coords.Property(c => c.Latitude).HasColumnName("Location_Coordinates_Latitude");
                   coords.Property(c => c.Longitude).HasColumnName("Location_Coordinates_Longitude");
               });
           });
       }
   }
   ```

2. **Write configuration unit tests** (60 min)
   ```csharp
   [Test]
   public void BusinessLocationConfiguration_CanSaveAndRetrieve_ValueObjectProperties()
   {
       // Test owned entity mapping works correctly
   }
   ```

3. **Generate and test migration** (75 min)
   ```bash
   dotnet ef migrations add "ConfigureBusinessLocationValueObject" --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
   dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
   ```

**Success Metrics**:
- [ ] Migration generates without constructor binding errors
- [ ] BusinessLocation saves and retrieves correctly  
- [ ] Value object equality works as expected
- [ ] Database schema matches expected structure

**Risk**: Medium - Complex value object mapping  
**Fallback Strategy**: Simplify to property-based mapping if owned entities fail

### Afternoon Session (4 hours): Begin Core Services

#### Milestone 1.3: IEmailService Implementation Start (4 hours)
**Time**: 14:00 - 18:00  
**Priority**: HIGH  
**Owner**: Application/Infrastructure Team

**Tasks**:

1. **TDD: Write failing EmailService tests** (90 min)
   ```csharp
   [Test]
   public async Task SendEmailAsync_WithValidMessage_ReturnsSuccessResult()
   {
       // Arrange - Mock dependencies
       var mockSimpleEmailService = new Mock<ISimpleEmailService>();
       var mockTemplateService = new Mock<IEmailTemplateService>();
       var mockRepository = new Mock<IEmailMessageRepository>();
       
       // Setup mocks for success scenario
       mockSimpleEmailService
           .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(Result.Success());
           
       var emailService = new EmailService(
           mockSimpleEmailService.Object,
           mockTemplateService.Object, 
           mockRepository.Object,
           Mock.Of<ILogger<EmailService>>());
           
       var emailMessage = new EmailMessageDto
       {
           ToEmail = "test@example.com",
           Subject = "Test",
           HtmlBody = "Test body"
       };
       
       // Act
       var result = await emailService.SendEmailAsync(emailMessage, CancellationToken.None);
       
       // Assert
       result.IsSuccess.Should().BeTrue();
       mockSimpleEmailService.Verify(x => x.SendEmailAsync(
           "test@example.com", 
           It.IsAny<string>(), 
           "Test", 
           "Test body"), Times.Once);
   }

   [Test]
   public async Task SendEmailAsync_WithInvalidEmail_ReturnsFailureResult()
   {
       // Test validation logic
   }

   [Test]
   public async Task SendTemplatedEmailAsync_WithValidTemplate_RendersAndSends()
   {
       // Test template integration
   }
   ```

2. **Create minimal EmailService implementation** (90 min)
   ```csharp
   public class EmailService : IEmailService
   {
       private readonly ISimpleEmailService _simpleEmailService;
       private readonly IEmailTemplateService _templateService;
       private readonly IEmailMessageRepository _repository;
       private readonly ILogger<EmailService> _logger;

       public EmailService(
           ISimpleEmailService simpleEmailService,
           IEmailTemplateService templateService,  
           IEmailMessageRepository repository,
           ILogger<EmailService> logger)
       {
           _simpleEmailService = simpleEmailService ?? throw new ArgumentNullException(nameof(simpleEmailService));
           _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
           _repository = repository ?? throw new ArgumentNullException(nameof(repository));
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
       }

       public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
       {
           // Basic validation
           if (string.IsNullOrEmpty(emailMessage?.ToEmail))
               return Result.Failure("Recipient email is required");
               
           if (string.IsNullOrEmpty(emailMessage.Subject))
               return Result.Failure("Email subject is required");

           try
           {
               using (LogContext.PushProperty("Operation", "SendEmail"))
               using (LogContext.PushProperty("RecipientEmail", emailMessage.ToEmail))
               {
                   _logger.Information("Sending email to {RecipientEmail}", emailMessage.ToEmail);
                   
                   // Delegate to existing simple email service
                   var result = await _simpleEmailService.SendEmailAsync(
                       emailMessage.ToEmail,
                       emailMessage.FromEmail ?? "noreply@lankaconnect.com",
                       emailMessage.Subject,
                       emailMessage.HtmlBody);
                       
                   if (result.IsSuccess)
                   {
                       _logger.Information("Email sent successfully to {RecipientEmail}", emailMessage.ToEmail);
                   }
                   else
                   {
                       _logger.Warning("Email sending failed to {RecipientEmail}: {Error}", 
                           emailMessage.ToEmail, result.Error);
                   }
                   
                   return result;
               }
           }
           catch (Exception ex)
           {
               _logger.Error(ex, "Unexpected error sending email to {RecipientEmail}", emailMessage.ToEmail);
               return Result.Failure($"Failed to send email: {ex.Message}");
           }
       }

       // Minimal implementations for other interface methods
       public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail, 
           Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
       {
           try
           {
               // Render template
               var templateResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);
               if (!templateResult.IsSuccess)
                   return Result.Failure($"Template rendering failed: {templateResult.Error}");
                   
               // Send rendered email
               var emailMessage = new EmailMessageDto
               {
                   ToEmail = recipientEmail,
                   Subject = templateResult.Value.Subject,
                   HtmlBody = templateResult.Value.HtmlContent
               };
               
               return await SendEmailAsync(emailMessage, cancellationToken);
           }
           catch (Exception ex)
           {
               _logger.Error(ex, "Failed to send templated email");
               return Result.Failure($"Failed to send templated email: {ex.Message}");
           }
       }

       // TODO: Implement other interface methods in next phase
       public Task<Result<BulkEmailResult>> SendBulkEmailAsync(IEnumerable<EmailMessageDto> emailMessages, CancellationToken cancellationToken = default)
       {
           throw new NotImplementedException("Will implement in Phase 2");
       }

       public Task<Result> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
       {
           return _templateService.ValidateTemplateAsync(templateName, cancellationToken);
       }
   }
   ```

3. **Register EmailService in DI** (15 min)
   ```csharp
   // Add to Infrastructure.DependencyInjection.cs
   services.AddScoped<IEmailService, EmailService>();
   ```

4. **Test service resolution** (15 min)
   ```bash
   dotnet run --project src/LankaConnect.API
   # Verify: No IEmailService resolution errors for basic methods
   ```

**Success Metrics**:
- [ ] Basic EmailService tests pass
- [ ] Service resolves in DI container
- [ ] SendEmailAsync and SendTemplatedEmailAsync work
- [ ] Email command handlers no longer throw resolution errors

**Risk**: Medium - Multiple service dependencies  
**Mitigation**: Focus on core functionality first, defer advanced features

## Phase 2: Repository Implementation (Day 2)

### Morning Session (4 hours): UserEmailPreferencesRepository

#### Milestone 2.1: Repository TDD Implementation (4 hours)
**Time**: 09:00 - 13:00  
**Priority**: HIGH  
**Owner**: Domain/Infrastructure Team

**Tasks**:

1. **Create UserEmailPreferences entity and migration** (60 min)
   ```csharp
   public class UserEmailPreferences : BaseEntity
   {
       public Guid UserId { get; private set; }
       public bool EmailEnabled { get; private set; }
       public bool MarketingEnabled { get; private set; }
       public bool NotificationEnabled { get; private set; }
       
       private UserEmailPreferences(Guid userId, bool emailEnabled = true, 
           bool marketingEnabled = false, bool notificationEnabled = true)
       {
           UserId = userId;
           EmailEnabled = emailEnabled;
           MarketingEnabled = marketingEnabled;
           NotificationEnabled = notificationEnabled;
       }
       
       public static Result<UserEmailPreferences> Create(Guid userId, 
           bool emailEnabled = true, bool marketingEnabled = false, bool notificationEnabled = true)
       {
           if (userId == Guid.Empty)
               return Result<UserEmailPreferences>.Failure("User ID is required");
               
           return Result<UserEmailPreferences>.Success(
               new UserEmailPreferences(userId, emailEnabled, marketingEnabled, notificationEnabled));
       }
       
       public Result EnableMarketing()
       {
           MarketingEnabled = true;
           return Result.Success();
       }
       
       public Result DisableMarketing()
       {
           MarketingEnabled = false;
           return Result.Success();
       }
   }
   ```

2. **Write comprehensive repository tests** (90 min)
   ```csharp
   [TestFixture]
   public class UserEmailPreferencesRepositoryTests
   {
       private AppDbContext _context;
       private UserEmailPreferencesRepository _repository;

       [SetUp]
       public void SetUp()
       {
           _context = TestDbContextFactory.CreateInMemoryContext();
           _repository = new UserEmailPreferencesRepository(_context);
       }

       [Test]
       public async Task GetByUserIdAsync_WithExistingUser_ReturnsPreferences()
       {
           // Arrange
           var userId = Guid.NewGuid();
           var preferences = UserEmailPreferences.Create(userId, true, false, true).Value;
           await _context.UserEmailPreferences.AddAsync(preferences);
           await _context.SaveChangesAsync();
           
           // Act
           var result = await _repository.GetByUserIdAsync(userId);
           
           // Assert
           result.Should().NotBeNull();
           result.UserId.Should().Be(userId);
           result.EmailEnabled.Should().BeTrue();
           result.MarketingEnabled.Should().BeFalse();
       }

       [Test]
       public async Task GetByUserIdAsync_WithNonExistentUser_ReturnsNull()
       {
           // Act
           var result = await _repository.GetByUserIdAsync(Guid.NewGuid());
           
           // Assert
           result.Should().BeNull();
       }

       [Test]
       public async Task AddAsync_WithValidPreferences_ReturnsSuccess()
       {
           // Arrange
           var preferences = UserEmailPreferences.Create(Guid.NewGuid()).Value;
           
           // Act
           var result = await _repository.AddAsync(preferences);
           
           // Assert
           result.IsSuccess.Should().BeTrue();
           _context.UserEmailPreferences.Should().Contain(p => p.Id == preferences.Id);
       }

       [Test]
       public async Task AddAsync_WithDuplicateUserId_ReturnsFailure()
       {
           // Test unique constraint handling
       }

       [Test] 
       public async Task UpdateAsync_WithValidPreferences_ReturnsSuccess()
       {
           // Test update operations
       }

       [Test]
       public async Task GetUsersByPreferenceAsync_WithMarketingEnabled_ReturnsFilteredUsers()
       {
           // Test preference filtering
       }
   }
   ```

3. **Implement UserEmailPreferencesRepository** (90 min)
   ```csharp
   public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
   {
       public UserEmailPreferencesRepository(AppDbContext context) : base(context)
       {
       }

       public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
       {
           using (LogContext.PushProperty("Operation", "GetByUserId"))
           using (LogContext.PushProperty("UserId", userId))
           {
               _logger.Debug("Getting user email preferences for user {UserId}", userId);
               
               var result = await _dbSet
                   .AsNoTracking()
                   .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
                   
               _logger.Debug("Retrieved preferences for user {UserId}: {Found}", userId, result != null);
               return result;
           }
       }

       public async Task<UserEmailPreferences?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
       {
           using (LogContext.PushProperty("Operation", "GetByEmail"))
           using (LogContext.PushProperty("Email", email))
           {
               // Join with Users table to find by email
               var result = await _context.UserEmailPreferences
                   .AsNoTracking()
                   .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => new { Preferences = p, User = u })
                   .Where(x => x.User.Email == email)
                   .Select(x => x.Preferences)
                   .FirstOrDefaultAsync(cancellationToken);
                   
               _logger.Debug("Retrieved preferences for email {Email}: {Found}", email, result != null);
               return result;
           }
       }

       public async Task<Result> AddAsync(UserEmailPreferences preferences, CancellationToken cancellationToken = default)
       {
           try
           {
               using (LogContext.PushProperty("Operation", "AddPreferences"))
               using (LogContext.PushProperty("UserId", preferences.UserId))
               {
                   _logger.Information("Adding email preferences for user {UserId}", preferences.UserId);
                   
                   await base.AddAsync(preferences, cancellationToken);
                   await _context.SaveChangesAsync(cancellationToken);
                   
                   _logger.Information("Email preferences added successfully for user {UserId}", preferences.UserId);
                   return Result.Success();
               }
           }
           catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
           {
               _logger.Warning("Duplicate email preferences for user {UserId}", preferences.UserId);
               return Result.Failure("User email preferences already exist");
           }
           catch (Exception ex)
           {
               _logger.Error(ex, "Failed to add email preferences for user {UserId}", preferences.UserId);
               return Result.Failure($"Failed to add email preferences: {ex.Message}");
           }
       }

       // Implement other interface methods following same pattern...
   }
   ```

**Success Metrics**:
- [ ] All repository unit tests pass
- [ ] Repository integrates with EF Core correctly
- [ ] GetUserEmailPreferencesQueryHandler resolves and works
- [ ] Performance meets requirements (<100ms for single operations)

### Afternoon Session (4 hours): EmailService Completion

#### Milestone 2.2: Complete EmailService Implementation (4 hours)
**Time**: 14:00 - 18:00  
**Priority**: HIGH  
**Owner**: Infrastructure Team

**Tasks**:

1. **Implement bulk email functionality** (120 min)
2. **Add comprehensive error handling** (60 min)
3. **Write integration tests** (90 min)
4. **Performance optimization** (30 min)

**Success Metrics**:
- [ ] All EmailService interface methods implemented
- [ ] Bulk email operations work correctly
- [ ] Integration tests pass
- [ ] All email command handlers work end-to-end

## Phase 3: Integration and Validation (Day 3)

### Morning Session (4 hours): EmailStatusRepository Resolution

#### Milestone 3.1: Consolidate Email Repositories (4 hours)
**Time**: 09:00 - 13:00  
**Priority**: MEDIUM  
**Owner**: Infrastructure Team

**Decision**: Merge IEmailStatusRepository functionality into EmailMessageRepository

**Tasks**:

1. **Analysis and design** (60 min)
   - Compare IEmailStatusRepository and IEmailMessageRepository interfaces
   - Design consolidated repository that implements both
   - Plan backward compatibility

2. **Update EmailMessageRepository** (90 min)
   ```csharp
   public class EmailMessageRepository : Repository<EmailMessage>, 
       IEmailMessageRepository, IEmailStatusRepository
   {
       // Existing IEmailMessageRepository methods...
       
       // New IEmailStatusRepository methods
       public async Task<List<EmailMessage>> GetEmailStatusAsync(
           Guid? userId = null,
           string? emailAddress = null,
           EmailType? emailType = null,
           EmailStatus? status = null,
           DateTime? fromDate = null,
           DateTime? toDate = null,
           int pageNumber = 1,
           int pageSize = 20,
           CancellationToken cancellationToken = default)
       {
           var query = _dbSet.AsNoTracking();
           
           if (userId.HasValue)
               query = query.Where(e => e.UserId == userId.Value);
               
           if (!string.IsNullOrEmpty(emailAddress))
               query = query.Where(e => e.ToEmail == emailAddress);
               
           // Apply other filters...
           
           return await query
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .OrderByDescending(e => e.CreatedAt)
               .ToListAsync(cancellationToken);
       }
   }
   ```

3. **Update dependency injection** (30 min)
   ```csharp
   services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
   services.AddScoped<IEmailStatusRepository>(provider => 
       provider.GetRequiredService<IEmailMessageRepository>());
   ```

4. **Test consolidated repository** (90 min)

**Success Metrics**:
- [ ] GetEmailStatusQueryHandler resolves successfully
- [ ] All email status queries work correctly
- [ ] No functional regression in email operations  
- [ ] Performance maintained or improved

### Afternoon Session (4 hours): Final Integration and Validation

#### Milestone 3.2: End-to-End Validation (4 hours)
**Time**: 14:00 - 18:00  
**Priority**: CRITICAL  
**Owner**: Full Team

**Tasks**:

1. **Migration validation** (60 min)
   ```bash
   # Clean migration test
   dotnet ef database drop --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --force
   dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
   
   # Verify: No dependency errors, all migrations apply successfully
   ```

2. **Application startup validation** (30 min)
   ```bash
   dotnet run --project src/LankaConnect.API
   # Verify: No DI resolution errors, all services start correctly
   ```

3. **End-to-end integration tests** (90 min)
   ```csharp
   [TestFixture]
   public class EmailIntegrationTests
   {
       [Test]
       public async Task SendWelcomeEmail_EndToEnd_EmailSentSuccessfully()
       {
           // Test complete email flow from command to actual sending
       }

       [Test]
       public async Task UserEmailPreferences_CRUD_Operations_WorkCorrectly()
       {
           // Test complete user preferences workflow
       }

       [Test]
       public async Task EmailStatusQuery_WithFilters_ReturnsCorrectResults()
       {
           // Test consolidated email status repository
       }
   }
   ```

4. **Performance validation** (60 min)
   - Load test email operations
   - Validate database query performance
   - Memory usage validation
   - Response time measurements

5. **Final documentation and cleanup** (60 min)
   - Update architectural documentation
   - Code cleanup and final refactoring
   - Deployment preparation

**Success Metrics**:
- [ ] All migrations run successfully
- [ ] Application starts without DI errors
- [ ] All MediatR handlers resolve correctly
- [ ] End-to-end email functionality works
- [ ] Performance benchmarks meet requirements
- [ ] Test coverage maintains 90%+

## Risk Management

### High-Risk Items and Mitigation

#### BusinessLocation EF Configuration
**Risk**: Complex value object mapping may fail  
**Mitigation**: 
- Create comprehensive test cases first
- Have property-based mapping fallback ready
- Test migration rollback scenarios

#### Service Integration Complexity
**Risk**: Multiple service dependencies may cause integration issues  
**Mitigation**:
- Use TDD with mocked dependencies
- Implement services incrementally
- Maintain clear interface contracts

#### Performance Impact
**Risk**: Result pattern and extensive logging may affect performance  
**Mitigation**:
- Establish performance baselines
- Use appropriate logging levels
- Profile critical code paths

### Rollback Strategies

#### Day 1 Issues
- Remove IMemoryCache registration
- Revert BusinessLocation configuration to simple properties
- Use basic email service stub

#### Day 2 Issues  
- Remove repository implementations
- Use stub implementations for testing
- Defer advanced email features

#### Day 3 Issues
- Keep separate repositories if consolidation fails
- Use existing simple email functionality
- Focus on core migration success

## Success Validation

### Functional Validation
```bash
# 1. Migration Success
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
# Should show all migrations including new ones

# 2. Application Startup
dotnet run --project src/LankaConnect.API
# Should start without DI errors

# 3. Health Check
curl http://localhost:5000/health
# Should return healthy status

# 4. Email Functionality
# Test email sending through API endpoints
curl -X POST http://localhost:5000/api/auth/send-verification-email \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'
# Should return success response
```

### Quality Validation
- [ ] Test coverage ≥ 90% for all new implementations
- [ ] No code analysis warnings
- [ ] All architectural decisions documented
- [ ] Performance benchmarks meet requirements
- [ ] Security scan passes without critical issues

### Documentation Deliverables
- [ ] Updated ADR with final decisions
- [ ] Implementation summary with lessons learned
- [ ] Performance benchmark results
- [ ] Troubleshooting guide for common issues
- [ ] Maintenance procedures for ongoing operations

## Conclusion

This roadmap provides a systematic approach to resolving Infrastructure dependencies while maintaining high code quality and architectural integrity. The phased approach allows for incremental validation and risk mitigation, ensuring successful project completion within the 3-day timeline.
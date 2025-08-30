# STREAMLINED ACTION PLAN - LankaConnect
## Local Development → Production (Target: Before Thanksgiving)

**Philosophy:** Build locally, iterate fast, ship to Azure when ready  
**Approach:** Complete each item fully before moving to next  
**Priority:** Phase 1 MVP to production ASAP

---

## 🏗️ FOUNDATION SETUP (Local Development)

### Local Infrastructure Setup
```yaml
Local Development Stack:
  - PostgreSQL: Docker container (postgres:15-alpine)
  - Redis: Docker container (redis:7-alpine) 
  - Email: MailHog container (mailhog/mailhog)
  - Storage: Azurite container (Azure Storage emulator)
  - Logging: Seq container (datalust/seq)
  - Auth: Local JWT implementation (skip Azure AD B2C initially)

Task List:
  ✓ Install Docker Desktop
  ✓ Create docker-compose.yml with all services
  ✓ Configure local database with schemas
  ✓ Set up Redis for caching
  ✓ Configure MailHog for email testing
  ✓ Set up Azurite for file storage
  ✓ Configure Seq for structured logging
  ✓ Verify all containers start and communicate
```

### Solution Structure Creation
```yaml
.NET 8 Solution Setup:
  ✓ Create Clean Architecture solution structure
  ✓ Configure project references correctly
  ✓ Set up Directory.Build.props with standards
  ✓ Configure Directory.Packages.props for central package management
  ✓ Create .editorconfig and .gitignore
  ✓ Set up initial Git repository
  ✓ Configure VS Code workspace settings
  ✓ Install and configure required NuGet packages
```

### Build Pipeline Setup
```yaml
CI/CD Foundation:
  ✓ Create GitHub repository
  ✓ Set up GitHub Actions for build
  ✓ Configure automated testing pipeline
  ✓ Set up code coverage reporting
  ✓ Configure Docker build for API
  ✓ Set up staging environment workflow (for later Azure deploy)
```

---

## 📋 PHASE 1: CORE MVP FEATURES

### 1. Domain Foundation
```yaml
Core Domain Models:
  ✓ Entity and ValueObject base classes
  ✓ Result pattern implementation
  ✓ Domain events infrastructure
  ✓ Common value objects (Email, PhoneNumber, Money)
  ✓ User aggregate with profile
  ✓ Event aggregate with registration
  ✓ Business aggregate with services
  ✓ Forum aggregate with topics/posts
  ✓ All domain models with comprehensive unit tests
```

### 2. Data Access Layer
```yaml
EF Core Configuration:
  ✓ AppDbContext with all entities
  ✓ Entity configurations for all domain models
  ✓ Value object converters
  ✓ Database schema with proper indexes
  ✓ Initial migration creation
  ✓ Repository pattern implementation
  ✓ Unit of Work pattern
  ✓ Integration tests for data access
```

### 3. Application Layer (CQRS)
```yaml
MediatR Setup:
  ✓ Configure MediatR with DI
  ✓ Create command and query base classes
  ✓ Implement validation pipeline behavior
  ✓ Set up logging pipeline behavior
  ✓ Create first commands and queries
  ✓ FluentValidation integration
  ✓ AutoMapper configuration
  ✓ Error handling infrastructure
```

### 4. Identity & Authentication (Local)
```yaml
Local JWT Authentication:
  ✓ User registration command/handler
  ✓ User login command/handler
  ✓ JWT token service implementation
  ✓ Password hashing with BCrypt
  ✓ Refresh token implementation
  ✓ Email verification flow (using MailHog)
  ✓ Password reset flow
  ✓ Role-based authorization
  ✓ Policy-based authorization
  ✓ User profile management
```

### 5. Event Management System
```yaml
Complete Event Features:
  ✓ Create event command and validation
  ✓ Update event command (organizer only)
  ✓ Delete event command (with rules)
  ✓ Publish event command
  ✓ Cancel event command
  ✓ Get events query with filtering
  ✓ Get event by ID query
  ✓ Search events query
  ✓ Event registration system
  ✓ Registration cancellation
  ✓ Waiting list functionality
  ✓ Event analytics (views, registrations)
  ✓ Calendar integration (ICS export)
  ✓ Event categories management
```

### 6. Community Forums
```yaml
Forum System:
  ✓ Forum categories setup
  ✓ Create topic command
  ✓ Create post/reply command
  ✓ Edit post functionality
  ✓ Topic and post reactions (likes)
  ✓ Forum moderation (basic)
  ✓ Topic subscription/notifications
  ✓ Search topics and posts
  ✓ Forum statistics
  ✓ User reputation system (basic)
```

### 7. Business Directory
```yaml
Business Listing:
  ✓ Business registration command
  ✓ Business verification system
  ✓ Service management (CRUD)
  ✓ Business search and filtering
  ✓ Business categories
  ✓ Contact information management
  ✓ Operating hours setup
  ✓ Business images/gallery
  ✓ Basic booking system
  ✓ Review and rating system
  ✓ Business analytics dashboard
```

### 8. API Infrastructure
```yaml
REST API Setup:
  ✓ Configure ASP.NET Core Web API
  ✓ Swagger/OpenAPI documentation
  ✓ Global exception handling middleware
  ✓ Request/response logging
  ✓ API versioning
  ✓ CORS configuration
  ✓ Rate limiting
  ✓ Response caching
  ✓ Health checks
  ✓ Base controller with standard responses
```

### 9. Email & Notifications
```yaml
Communication System:
  ✓ Email service interface
  ✓ Local SMTP implementation (MailHog)
  ✓ Email templates (HTML/text)
  ✓ Transactional emails:
    - Welcome email
    - Email verification
    - Password reset
    - Event registration confirmation
    - Event reminders
    - Forum notifications
    - Business booking confirmations
  ✓ Email queue processing
  ✓ Notification preferences
```

### 10. File Storage
```yaml
Media Management:
  ✓ File upload service
  ✓ Local file storage (Azurite)
  ✓ Image resizing/optimization
  ✓ File type validation
  ✓ User avatar uploads
  ✓ Event banner images
  ✓ Business gallery images
  ✓ Forum post attachments
  ✓ File cleanup jobs
```

### 11. Caching & Performance
```yaml
Performance Optimization:
  ✓ Redis caching implementation
  ✓ Cache-aside pattern
  ✓ Query result caching
  ✓ Distributed caching for sessions
  ✓ API response caching
  ✓ Database query optimization
  ✓ Proper indexing strategy
  ✓ Lazy loading configuration
  ✓ Response compression
```

### 12. Security Implementation
```yaml
Security Features:
  ✓ Input validation and sanitization
  ✓ XSS protection
  ✓ CSRF protection
  ✓ SQL injection prevention
  ✓ Rate limiting per endpoint
  ✓ Account lockout after failed attempts
  ✓ Password strength requirements
  ✓ Secure headers middleware
  ✓ Audit logging
  ✓ Data encryption at rest
```

### 13. Testing Suite
```yaml
Comprehensive Testing:
  ✓ Unit tests for all domain models (80%+ coverage)
  ✓ Unit tests for all handlers
  ✓ Integration tests for API endpoints
  ✓ Integration tests for database operations
  ✓ End-to-end tests for critical flows:
    - User registration and login
    - Event creation and registration
    - Forum topic and post creation
    - Business registration and booking
  ✓ Performance tests for key endpoints
  ✓ Security tests (basic)
```

### 14. Local Deployment Ready
```yaml
Production Readiness:
  ✓ Environment-specific configurations
  ✓ Connection string management
  ✓ Secret management (local)
  ✓ Logging configuration
  ✓ Health check endpoints
  ✓ Docker containers for all services
  ✓ Docker Compose for full stack
  ✓ Database migration scripts
  ✓ Seed data for initial setup
  ✓ Admin user creation
  ✓ Documentation for local setup
```

---

## 🚀 AZURE MIGRATION (When Ready)

### Azure Infrastructure Setup
```yaml
Cloud Migration:
  ✓ Create Azure subscription
  ✓ Set up resource groups
  ✓ Deploy Azure Container Apps environment
  ✓ Provision Azure Database for PostgreSQL
  ✓ Set up Azure Cache for Redis
  ✓ Configure Azure Storage Account
  ✓ Set up Azure AD B2C (replace local JWT)
  ✓ Configure Application Insights
  ✓ Set up custom domain and SSL
  ✓ Configure backup and disaster recovery
```

### Azure Integration
```yaml
Cloud Services Integration:
  ✓ Migrate local JWT to Azure AD B2C
  ✓ Replace Azurite with Azure Storage
  ✓ Configure SendGrid for email
  ✓ Set up Azure Key Vault
  ✓ Configure monitoring and alerting
  ✓ Set up CI/CD to Azure
  ✓ Database migration to cloud
  ✓ Performance testing in cloud
  ✓ Security review in cloud environment
```

---

## 📈 PHASE 2: ADVANCED FEATURES (Post-Launch)

### Real-time Features
```yaml
SignalR Implementation:
  - Real-time forum discussions
  - Live event updates
  - Instant notifications
  - Chat system
  - Live user presence
  - Real-time analytics
```

### Payment Integration
```yaml
E-commerce Features:
  - Stripe payment gateway
  - Subscription management
  - Event ticket payments
  - Business service payments
  - Refund processing
  - Invoice generation
  - Payment analytics
```

### Advanced Analytics
```yaml
Business Intelligence:
  - User behavior analytics
  - Event performance metrics
  - Business directory analytics
  - Revenue tracking
  - Custom dashboards
  - Export capabilities
  - Machine learning insights
```

### Multi-language Support
```yaml
Internationalization:
  - Sinhala language support
  - Tamil language support
  - Multi-language content
  - RTL support
  - Cultural calendar integration
  - Localized date/time formats
```

### Mobile Application
```yaml
React Native App:
  - iOS and Android apps
  - Push notifications
  - Offline capabilities
  - Native integrations
  - App store deployment
```

### Education Platform
```yaml
Learning Management:
  - Course creation and management
  - Educational content delivery
  - Student progress tracking
  - Certification system
  - Virtual classroom integration
```

---

## 🎯 LOCAL DEVELOPMENT ENVIRONMENT SETUP

### Docker Services Configuration
```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: lankaconnect
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data

  mailhog:
    image: mailhog/mailhog
    ports:
      - "1025:1025"  # SMTP
      - "8025:8025"  # Web UI

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"
      - "10001:10001" 
      - "10002:10002"

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:80"

volumes:
  postgres_data:
  redis_data:
```

### Local Configuration
```yaml
# appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lankaconnect;Username=postgres;Password=postgres123",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-for-development",
    "Issuer": "LankaConnect",
    "Audience": "LankaConnect-Users",
    "ExpiryInMinutes": 15,
    "RefreshExpiryInDays": 7
  },
  "EmailSettings": {
    "SmtpServer": "localhost",
    "SmtpPort": 1025,
    "FromEmail": "noreply@lankaconnect.local"
  },
  "StorageSettings": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ]
  }
}
```

---

## 🎪 GETTING STARTED CHECKLIST

### Prerequisites Verification
```yaml
✓ Docker Desktop installed and running
✓ .NET 8 SDK installed
✓ Visual Studio Code with extensions
✓ Git configured
✓ Node.js (for any frontend tooling)
✓ PostgreSQL client (pgAdmin or similar)
```

### First Steps
```yaml
1. ✓ Clone/create repository
2. ✓ Run `docker-compose up -d` 
3. ✓ Create solution structure
4. ✓ Set up first domain model
5. ✓ Create first migration
6. ✓ Build and run API
7. ✓ Verify Swagger UI works
8. ✓ Create first endpoint
9. ✓ Write first test
10. ✓ Commit initial code
```

---

## 🏆 SUCCESS CRITERIA

### Phase 1 MVP Definition
```yaml
✓ Users can register and login locally
✓ Users can create and manage events
✓ Users can register for events
✓ Users can participate in forums
✓ Businesses can register and list services
✓ Users can book services
✓ Users can leave reviews
✓ Email notifications work
✓ All core APIs documented
✓ 80%+ test coverage
✓ Ready for Azure deployment
```

### Technical Readiness
```yaml
✓ All containers start successfully
✓ Database migrations run cleanly  
✓ All tests pass
✓ No security vulnerabilities
✓ Performance benchmarks met
✓ Documentation complete
✓ Deployment process documented
```

---

## 📝 NOTES

### Development Approach
- **Build one feature completely** before moving to next
- **Test extensively** at each step
- **Refactor continuously** to maintain quality
- **Document decisions** as you go
- **Commit frequently** with clear messages

### Local Development Benefits
- **Fast iteration** - no cloud deployment delays
- **Cost effective** - no Azure costs during development
- **Full control** - configure everything as needed
- **Easy debugging** - everything local
- **Offline capability** - work anywhere

### Migration to Azure
- **Keep local environment** for development
- **Use Azure for staging/production** only
- **Maintain feature parity** between local and cloud
- **Test thoroughly** before cloud migration
- **Plan for zero-downtime** deployment

This streamlined plan focuses on **getting to a working MVP fast** while maintaining the quality and architecture standards you've established. You can work through each item at your own pace without worrying about artificial time constraints.

Ready to start with the foundation setup?
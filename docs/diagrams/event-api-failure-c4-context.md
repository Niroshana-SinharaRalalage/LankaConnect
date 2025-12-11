# C4 Architecture Diagrams: Event API Failure Analysis

## C4 Level 1: System Context Diagram

```mermaid
graph TB
    User["User<br/>(Developer/Product Owner)"]
    Browser["Browser<br/>(Swagger UI)"]
    GitHub["GitHub Actions<br/>(CI/CD Pipeline)"]
    Azure["Azure Container Apps<br/>(Staging Environment)"]
    Database["Azure PostgreSQL<br/>(Staging Database)"]
    ACR["Azure Container Registry<br/>(Docker Images)"]

    User -->|"Visits /swagger"| Browser
    Browser -->|"HTTPS GET /"| Azure
    Azure -.->|"Returns 0 Event endpoints<br/>(BROKEN)"| Browser
    GitHub -->|"Triggers deployment"| Azure
    GitHub -->|"Pushes image"| ACR
    Azure -->|"Pulls image"| ACR
    Azure -->|"Runs migrations<br/>(FAILS)"| Database
    Azure -.->|"Crashes on startup<br/>(PROBLEM)"| Azure

    style Azure fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style Database fill:#ffd43b,stroke:#fab005,stroke-width:3px
    style Browser fill:#ff6b6b,stroke:#c92a2a,stroke-width:2px
```

**Key Problem**: Container crashes during migration execution, preventing EventsController from loading.

---

## C4 Level 2: Container Diagram

```mermaid
graph TB
    subgraph "Azure Container Apps"
        Container["API Container<br/>(ASP.NET Core 8.0)"]
        subgraph "Application Startup Sequence"
            Step1["1. Configure Services"]
            Step2["2. Build App"]
            Step3["3. Apply Migrations<br/>(FAILS HERE)"]
            Step4["4. Map Controllers<br/>(NEVER REACHED)"]
            Step5["5. Start Kestrel<br/>(NEVER REACHED)"]

            Step1 --> Step2
            Step2 --> Step3
            Step3 -.->|"Exception thrown"| Step4
            Step4 --> Step5
        end
    end

    subgraph "Azure PostgreSQL"
        DB["lankaconnect database"]
        MigrationHistory["__EFMigrationsHistory<br/>(Contains deleted migration)"]
        EventsTable["events.events<br/>(Missing Status column)"]

        DB --> MigrationHistory
        DB --> EventsTable
    end

    Step3 -->|"context.Database.MigrateAsync()"| MigrationHistory
    MigrationHistory -->|"Execute pending migrations"| EventsTable
    EventsTable -.->|"ERROR: column status does not exist"| Step3

    style Step3 fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style Step4 fill:#868e96,stroke:#495057,stroke-width:2px,stroke-dasharray: 5 5
    style Step5 fill:#868e96,stroke:#495057,stroke-width:2px,stroke-dasharray: 5 5
    style EventsTable fill:#ffd43b,stroke:#fab005,stroke-width:3px
    style MigrationHistory fill:#ffd43b,stroke:#fab005,stroke-width:3px
```

**Critical Issue**: Migrations fail at Step 3, preventing application startup.

---

## C4 Level 3: Component Diagram - Migration Execution

```mermaid
graph TB
    subgraph "Program.cs (Lines 150-169)"
        Startup["Application Startup"]
        CreateScope["Create Service Scope"]
        GetContext["Get AppDbContext"]
        MigrateAsync["context.Database.MigrateAsync()"]
        Success["Log Success"]
        CatchBlock["Catch Exception<br/>Log Error<br/>Re-throw"]

        Startup --> CreateScope
        CreateScope --> GetContext
        GetContext --> MigrateAsync
        MigrateAsync -->|"Success"| Success
        MigrateAsync -.->|"Exception"| CatchBlock
        CatchBlock -.->|"Crash container"| Startup
    end

    subgraph "EF Core Migration Engine"
        GetPending["Get Pending Migrations"]
        CheckHistory["Check __EFMigrationsHistory"]
        ExecuteMigration["Execute Migration SQL"]
        UpdateHistory["Update __EFMigrationsHistory"]

        GetPending --> CheckHistory
        CheckHistory -->|"20251102061243 pending"| ExecuteMigration
        ExecuteMigration --> UpdateHistory
    end

    subgraph "AddEventLocationWithPostGIS (Line 118)"
        SQL["CREATE INDEX ix_events_status_city_startdate<br/>ON events.events (status, address_city, start_date)"]
        PostgreSQL["PostgreSQL Query Executor"]
        Error["ERROR: column 'status' does not exist"]

        SQL --> PostgreSQL
        PostgreSQL -.->|"Case-sensitive lookup"| Error
    end

    MigrateAsync --> GetPending
    ExecuteMigration --> SQL
    Error -.-> CatchBlock

    style MigrateAsync fill:#ffd43b,stroke:#fab005,stroke-width:3px
    style CatchBlock fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style SQL fill:#ffd43b,stroke:#fab005,stroke-width:3px
    style Error fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
```

**Root Cause**: Raw SQL uses lowercase `status` but table has PascalCase `Status`.

---

## C4 Level 4: Code Diagram - Migration Failure Sequence

```mermaid
sequenceDiagram
    participant Container as Container Startup
    participant Program as Program.cs
    participant EFCore as EF Core
    participant Postgres as PostgreSQL
    participant History as __EFMigrationsHistory

    Container->>Program: Start Application
    Program->>Program: CreateScope()
    Program->>EFCore: GetRequiredService<AppDbContext>()
    Program->>EFCore: context.Database.MigrateAsync()

    EFCore->>History: SELECT MigrationId FROM __EFMigrationsHistory
    History-->>EFCore: [Applied migrations list]

    Note over EFCore: Compare applied vs code migrations<br/>Find pending: 20251102061243_AddEventLocationWithPostGIS

    EFCore->>EFCore: Generate migration SQL
    Note over EFCore: Line 118: CREATE INDEX ... (status, ...)

    EFCore->>Postgres: Execute SQL
    Note over Postgres: Column lookup: "status" (lowercase)

    Postgres->>Postgres: Search events.events for column "status"
    Note over Postgres: Found: "Status" (PascalCase)<br/>Case mismatch!

    Postgres-->>EFCore: ERROR: column "status" does not exist
    EFCore-->>Program: Npgsql.PostgresException
    Program->>Program: Log error
    Program->>Container: Re-throw exception
    Container->>Container: Crash and restart

    Note over Container: Crash loop begins<br/>EventsController never loads<br/>Swagger shows 0 endpoints
```

**Timeline**: Container crashes within 5 seconds of startup every time.

---

## Data Flow Diagram - Expected vs Actual

### Expected Flow (Before Deletion)

```mermaid
graph LR
    subgraph "Migration 1: InitialCreate"
        M1["CREATE TABLE events.events<br/>(Status varchar(20))"]
    end

    subgraph "Migration 2: AddEventLocation"
        M2["ALTER TABLE events.events<br/>ADD COLUMN address_city"]
        M3["CREATE INDEX ON events.events (Status, address_city)"]
    end

    M1 -->|"Status column exists"| M2
    M2 -->|"Both columns exist"| M3
    M3 -->|"Success"| Result["Events API available"]

    style M1 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style M2 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style M3 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style Result fill:#51cf66,stroke:#2f9e44,stroke-width:2px
```

### Actual Flow (After Deletion)

```mermaid
graph LR
    subgraph "Migration 1: InitialCreate"
        M1A["CREATE TABLE events.events<br/>(Status varchar(20))"]
    end

    subgraph "Migration 2: DELETED CreateEvents (Applied to DB)"
        M2A["DROP TABLE events.events"]
        M2B["CREATE TABLE events.events<br/>(NO Status column)"]
    end

    subgraph "Migration 3: AddEventLocation (Fails)"
        M3A["ALTER TABLE events.events<br/>ADD COLUMN address_city"]
        M3B["CREATE INDEX ON events.events (status, address_city)"]
        M3C["ERROR: column 'status' does not exist"]
    end

    M1A -->|"Status exists"| M2A
    M2A -->|"Table dropped"| M2B
    M2B -.->|"Status MISSING"| M3A
    M3A -.->|"Tries to reference status"| M3B
    M3B -.->|"Failure"| M3C
    M3C -.->|"Crash"| Result["Events API UNAVAILABLE"]

    style M2A fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style M2B fill:#ffd43b,stroke:#fab005,stroke-width:3px
    style M3B fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style M3C fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style Result fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
```

---

## System State Diagram

```mermaid
stateDiagram-v2
    [*] --> ContainerStarting: Docker run

    ContainerStarting --> LoadingServices: Configure DI
    LoadingServices --> ApplyingMigrations: Build app

    ApplyingMigrations --> CheckingHistory: Read __EFMigrationsHistory
    CheckingHistory --> FoundPending: 20251102061243 not applied

    FoundPending --> ExecutingSQL: Generate SQL
    ExecutingSQL --> PostgreSQLError: CREATE INDEX ... (status, ...)

    PostgreSQLError --> LoggingError: column "status" does not exist
    LoggingError --> ThrowingException: Re-throw

    ThrowingException --> ContainerCrashed: Exit code 1
    ContainerCrashed --> ContainerStarting: Kubernetes restart

    state ApplyingMigrations {
        [*] --> InitialCreate
        InitialCreate --> AddEventLocation: Next migration
        AddEventLocation --> [*]: Success
    }

    note right of PostgreSQLError
        Root Cause:
        - Migration uses lowercase "status"
        - Table has PascalCase "Status"
        - PostgreSQL case-sensitive
    end note

    note right of ContainerCrashed
        Impact:
        - EventsController never loads
        - Swagger shows 0 Event endpoints
        - Health check fails
        - Crash loop every 5 seconds
    end note
```

---

## Architecture Decision Flow - How This Happened

```mermaid
graph TB
    Decision1["Developer: Create Events feature"]
    Decision2["InitialCreate migration:<br/>CREATE TABLE events (Status varchar(20))"]
    Decision3["Developer: 'Events table missing?'"]
    Decision4["Create redundant migration:<br/>20251102000000_CreateEventsAndRegistrationsTables"]
    Decision5["Deploy to staging:<br/>Redundant migration applied"]
    Decision6["Developer: 'Oh, it's redundant'"]
    Decision7["Delete migration from code:<br/>git rm 20251102000000_*.cs"]
    Decision8["Create location migration:<br/>AddEventLocationWithPostGIS"]
    Decision9["Write raw SQL:<br/>CREATE INDEX (status, ...) - WRONG CASE"]
    Decision10["Deploy to staging:<br/>Migration fails"]

    Decision1 --> Decision2
    Decision2 --> Decision3
    Decision3 --> Decision4
    Decision4 --> Decision5
    Decision5 --> Decision6
    Decision6 --> Decision7
    Decision7 --> Decision8
    Decision8 --> Decision9
    Decision9 --> Decision10

    Decision10 -.->|"PROBLEM"| Problem["Events API unavailable"]

    style Decision4 fill:#ffd43b,stroke:#fab005,stroke-width:2px
    style Decision7 fill:#ff8787,stroke:#c92a2a,stroke-width:2px
    style Decision9 fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style Decision10 fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
    style Problem fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
```

**Key Mistakes**:
1. Created redundant migration instead of verifying existing schema
2. Deleted migration from code without reverting database
3. Used lowercase column name in raw SQL (case mismatch)
4. No migration validation in CI/CD pipeline

---

## Solution Architecture

```mermaid
graph TB
    subgraph "Phase 1: Emergency Fix (2 hours)"
        Fix1["DROP SCHEMA events CASCADE"]
        Fix2["DELETE FROM __EFMigrationsHistory<br/>WHERE MigrationId LIKE '%Event%'"]
        Fix3["Redeploy application"]
        Fix4["Migrations recreate schema from scratch"]
        Fix5["EventsController loads successfully"]

        Fix1 --> Fix2
        Fix2 --> Fix3
        Fix3 --> Fix4
        Fix4 --> Fix5
    end

    subgraph "Phase 2: Code Fix (4 hours)"
        Code1["Fix column name case in AddEventLocationWithPostGIS"]
        Code2["Standardize naming convention (snake_case)"]
        Code3["Add migration validation to CI/CD"]
        Code4["Implement schema health check"]

        Code1 --> Code2
        Code2 --> Code3
        Code3 --> Code4
    end

    subgraph "Phase 3: Prevention (16 hours)"
        Prevent1["Pre-deployment schema backup"]
        Prevent2["Migration dry-run in CI/CD"]
        Prevent3["Database rollback procedure"]
        Prevent4["Team training on migration best practices"]

        Prevent1 --> Prevent2
        Prevent2 --> Prevent3
        Prevent3 --> Prevent4
    end

    Fix5 --> Code1
    Code4 --> Prevent1

    style Fix1 fill:#ff6b6b,stroke:#c92a2a,stroke-width:2px
    style Fix5 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style Code4 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style Prevent4 fill:#51cf66,stroke:#2f9e44,stroke-width:2px
```

---

## Risk Matrix

```mermaid
quadrantChart
    title Migration Risk Assessment
    x-axis Low Impact --> High Impact
    y-axis Low Probability --> High Probability
    quadrant-1 Critical
    quadrant-2 High
    quadrant-3 Medium
    quadrant-4 Low

    Column name case mismatch: [0.8, 0.9]
    Deleted migration in DB: [0.7, 0.6]
    No CI/CD validation: [0.9, 0.8]
    No schema health check: [0.8, 0.7]
    Manual migration errors: [0.5, 0.6]
    Network timeout during migration: [0.3, 0.2]
    Disk space exhaustion: [0.2, 0.1]
    PostgreSQL version incompatibility: [0.1, 0.1]
```

**Critical Risks** (top-right quadrant):
- Column name case mismatch
- No CI/CD validation
- No schema health check

---

## Component Interaction - Healthy State

```mermaid
graph TB
    subgraph "Container Startup (Healthy)"
        Start["Container Start"]
        Migrate["Apply Migrations"]
        MapControllers["Map Controllers"]
        StartKestrel["Start Kestrel"]

        Start --> Migrate
        Migrate --> MapControllers
        MapControllers --> StartKestrel
    end

    subgraph "Swagger Generation"
        ScanControllers["Scan Controllers Assembly"]
        FindEventsController["Find EventsController.cs"]
        Extract20Endpoints["Extract 20 Event endpoints"]
        GenerateSwaggerJSON["Generate /swagger/v1/swagger.json"]

        ScanControllers --> FindEventsController
        FindEventsController --> Extract20Endpoints
        Extract20Endpoints --> GenerateSwaggerJSON
    end

    subgraph "User Request"
        UserVisit["User visits /swagger"]
        LoadSwaggerUI["Load Swagger UI"]
        FetchSwaggerJSON["Fetch /swagger/v1/swagger.json"]
        Display20Endpoints["Display 20 Event endpoints"]

        UserVisit --> LoadSwaggerUI
        LoadSwaggerUI --> FetchSwaggerJSON
        FetchSwaggerJSON --> Display20Endpoints
    end

    StartKestrel --> ScanControllers
    GenerateSwaggerJSON --> FetchSwaggerJSON

    style Migrate fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style MapControllers fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style StartKestrel fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style Display20Endpoints fill:#51cf66,stroke:#2f9e44,stroke-width:2px
```

**Expected Behavior**: All steps complete successfully, Swagger shows all endpoints.

---

## Deployment Pipeline - Current vs Improved

### Current Pipeline (Problematic)

```mermaid
graph LR
    Build["dotnet build"]
    Test["dotnet test"]
    Publish["dotnet publish"]
    DockerBuild["docker build"]
    DockerPush["docker push"]
    ContainerUpdate["az containerapp update"]
    ContainerStart["Container starts"]
    MigrationFail["Migrations fail"]

    Build --> Test
    Test --> Publish
    Publish --> DockerBuild
    DockerBuild --> DockerPush
    DockerPush --> ContainerUpdate
    ContainerUpdate --> ContainerStart
    ContainerStart -.->|"No validation"| MigrationFail

    style MigrationFail fill:#ff6b6b,stroke:#c92a2a,stroke-width:3px
```

### Improved Pipeline (With Safeguards)

```mermaid
graph LR
    Build["dotnet build"]
    Test["dotnet test"]
    ValidateMigrations["Validate Migrations<br/>(NEW)"]
    GenerateScript["Generate SQL Script<br/>(NEW)"]
    DryRun["Dry-run on Test DB<br/>(NEW)"]
    Publish["dotnet publish"]
    DockerBuild["docker build"]
    DockerPush["docker push"]
    BackupSchema["Backup DB Schema<br/>(NEW)"]
    ContainerUpdate["az containerapp update"]
    ContainerStart["Container starts"]
    HealthCheck["Schema Health Check<br/>(NEW)"]
    Success["Deployment Success"]

    Build --> Test
    Test --> ValidateMigrations
    ValidateMigrations --> GenerateScript
    GenerateScript --> DryRun
    DryRun --> Publish
    Publish --> DockerBuild
    DockerBuild --> DockerPush
    DockerPush --> BackupSchema
    BackupSchema --> ContainerUpdate
    ContainerUpdate --> ContainerStart
    ContainerStart --> HealthCheck
    HealthCheck --> Success

    style ValidateMigrations fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style DryRun fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style BackupSchema fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style HealthCheck fill:#51cf66,stroke:#2f9e44,stroke-width:2px
    style Success fill:#51cf66,stroke:#2f9e44,stroke-width:3px
```

---

## Summary

These C4 diagrams illustrate:

1. **System Context**: User cannot access Event APIs due to container crash
2. **Container**: Application fails at migration step, never reaches controller mapping
3. **Component**: EF Core migration engine executes SQL with wrong column case
4. **Code**: Detailed sequence showing PostgreSQL case-sensitive column lookup failure

**Key Takeaways**:
- Root cause: Column name case mismatch in raw SQL
- Impact: Complete Event API unavailability
- Solution: Drop/recreate schema + fix migration bugs + add safeguards
- Prevention: Multi-layer validation in CI/CD pipeline

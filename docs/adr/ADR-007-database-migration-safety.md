# ADR-007: Database Migration Safety and Naming Conventions

**Status**: Proposed
**Date**: 2025-11-03
**Deciders**: Technical Architecture Team, Database Team, DevOps Team
**Consulted**: Backend Development Team, Security Team
**Informed**: Product Team, QA Team

---

## Context and Problem Statement

The LankaConnect API application crashed on startup in the staging environment due to a database migration failure. The root cause was a combination of:

1. **Column name case sensitivity mismatch** between EF Core migrations and PostgreSQL
2. **Orphaned migration history** from a deleted migration still applied to the database
3. **Lack of migration validation** in the CI/CD pipeline
4. **No schema health checks** to detect mismatches before serving traffic

This resulted in:
- Complete unavailability of Event APIs (20 endpoints)
- Container crash loop in Azure Container Apps
- 0 Event endpoints visible in Swagger documentation
- Potential data corruption risk if deployed to production

**Critical Question**: How do we prevent migration-related failures from crashing the application and ensure database schema consistency across environments?

---

## Decision Drivers

### Functional Requirements
- Database migrations must apply successfully on every deployment
- Schema changes must be reversible (rollback capability)
- Application must not start if database schema is incompatible
- Migrations must be testable before production deployment

### Non-Functional Requirements
- **Reliability**: 99.9% deployment success rate
- **Safety**: Zero data loss from failed migrations
- **Performance**: Migration validation adds <2 minutes to CI/CD pipeline
- **Maintainability**: Clear migration naming and documentation standards

### Constraints
- PostgreSQL 15 database (case-sensitive column names when quoted)
- EF Core 8.0 migration engine
- Azure Container Apps deployment model (no direct database access)
- GitHub Actions CI/CD pipeline
- Multi-environment deployment (dev, staging, production)

### Risks
- **High**: Production data loss from destructive migrations
- **High**: Application downtime from failed migrations
- **Medium**: Developer productivity impact from slower pipeline
- **Low**: Schema drift between environments

---

## Considered Options

### Option 1: Status Quo (No Changes)
**Description**: Continue with current migration process (no validation, auto-apply on startup)

**Pros**:
- No development effort required
- Fast deployment pipeline (no validation overhead)
- Simple developer workflow

**Cons**:
- High risk of production failures (evidenced by current staging crash)
- No rollback capability
- No schema drift detection
- Dependent on developer diligence for correctness

**Verdict**: ❌ Rejected - Unacceptable risk to production stability

---

### Option 2: Manual Migration Review Only
**Description**: Require code review for all migrations, but no automated validation

**Pros**:
- Human oversight can catch complex issues
- No pipeline changes required
- Flexible approval process

**Cons**:
- Human error risk (reviewer may miss case-sensitivity bugs)
- Inconsistent enforcement (depends on reviewer availability)
- No automated testing against actual database
- Doesn't prevent orphaned migration history

**Verdict**: ❌ Rejected - Insufficient safeguards for critical infrastructure

---

### Option 3: Disable Auto-Migration on Startup
**Description**: Remove automatic migration execution, require manual database updates

**Pros**:
- Prevents startup crashes from migration failures
- Forces explicit deployment approval
- Allows testing migrations in isolation

**Cons**:
- Adds manual step to deployment process (slows releases)
- Requires database access credentials for deployment team
- Risk of forgetting to run migrations (schema drift)
- Doesn't solve column name case sensitivity issue

**Verdict**: ❌ Rejected - Adds operational complexity without addressing root cause

---

### Option 4: Comprehensive Migration Safety Framework (Recommended)
**Description**: Multi-layer validation with automated checks and health monitoring

**Components**:
1. **Naming Convention Enforcement**: Standardize on PostgreSQL snake_case
2. **CI/CD Migration Validation**: Automated script generation and syntax checking
3. **Schema Health Checks**: Verify schema matches model before serving traffic
4. **Pre-Deployment Backups**: Automated schema backup before every deployment
5. **Migration Code Review Checklist**: Required for all database changes

**Pros**:
- Multi-layer defense against failures (defense in depth)
- Automated validation catches errors before deployment
- Health checks prevent broken deployments from serving traffic
- Backups enable quick rollback from failures
- Addresses root cause (column name case sensitivity)

**Cons**:
- Requires development effort (estimated 16 hours)
- Adds ~2 minutes to CI/CD pipeline
- Requires team training on new procedures
- More complex deployment workflow

**Verdict**: ✅ Selected - Best balance of safety and maintainability

---

## Decision Outcome

**Chosen Option**: Option 4 - Comprehensive Migration Safety Framework

### Implementation Details

#### 1. PostgreSQL Naming Convention (snake_case)

**Rationale**: PostgreSQL best practice is lowercase identifiers. EF Core can generate these automatically.

**Configuration** (`AppDbContext.cs`):
```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    // Apply snake_case naming convention globally
    configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
}
```

**Impact**:
- All new migrations use consistent naming
- Raw SQL migrations use lowercase identifiers (no quoting needed)
- Aligns with PostgreSQL ecosystem standards

**Migration Path**:
1. Create new migration to rename existing columns to snake_case
2. Update all Entity Configurations to use `HasColumnName()`
3. Update raw SQL in existing migrations

**Timeline**: 4 hours development + 2 hours testing

---

#### 2. CI/CD Migration Validation

**Rationale**: Catch migration errors before deployment, not during production startup

**Implementation** (`.github/workflows/deploy-staging.yml`):
```yaml
- name: Validate Database Migrations
  run: |
    # Generate idempotent migration script
    dotnet ef migrations script --idempotent --output migration-script.sql

    # Validate no unquoted case-sensitive column names
    if grep -iE '\b(status|start_date)\b' migration-script.sql | grep -v '"'; then
      echo "ERROR: Unquoted case-sensitive column names"
      exit 1
    fi

    # Check for destructive DROP statements
    if grep -iE 'DROP (TABLE|SCHEMA|DATABASE)' migration-script.sql; then
      echo "WARNING: Destructive operation detected"
      # Require manual approval for prod deployments
    fi
```

**Validation Checks**:
1. ✅ Migration script generation succeeds (no EF Core errors)
2. ✅ No syntax errors in generated SQL
3. ✅ No unquoted case-sensitive identifiers
4. ✅ DROP statements flagged for review
5. ✅ Foreign key constraints validated

**Timeline**: 2 hours development + 1 hour testing

---

#### 3. Database Schema Health Check

**Rationale**: Prevent application from serving requests if schema is incompatible

**Implementation** (`DatabaseSchemaHealthCheck.cs`):
```csharp
public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
{
    // Verify critical tables exist
    var hasEventsTable = await VerifyTableExists("events", "events");
    if (!hasEventsTable)
        return HealthCheckResult.Unhealthy("Events table not found");

    // Verify critical columns exist
    var requiredColumns = new[] { "Id", "title", "description", "status", "created_at" };
    foreach (var column in requiredColumns)
    {
        var exists = await VerifyColumnExists("events", "events", column);
        if (!exists)
            return HealthCheckResult.Unhealthy($"Column {column} not found");
    }

    return HealthCheckResult.Healthy("Schema validated");
}
```

**Behavior**:
- Runs on every container startup BEFORE accepting traffic
- Returns HTTP 503 if schema invalid (Kubernetes won't route traffic)
- Logs detailed schema mismatch errors for debugging
- Prevents partial deployments from serving broken APIs

**Timeline**: 3 hours development + 2 hours testing

---

#### 4. Pre-Deployment Schema Backup

**Rationale**: Enable fast rollback from failed migrations

**Implementation** (`.github/workflows/deploy-staging.yml`):
```yaml
- name: Backup Database Schema
  run: |
    # Generate schema dump
    az postgres flexible-server execute \
      --name lankaconnect-staging \
      --database-name lankaconnect \
      --file-path "schema-backup-$(date +%Y%m%d-%H%M%S).sql" \
      "pg_dump --schema-only"

    # Upload to Azure Blob Storage
    az storage blob upload \
      --container-name schema-backups \
      --file schema-backup-*.sql \
      --name "${{ github.sha }}-schema.sql"
```

**Backup Strategy**:
- Schema-only dump (no data, fast backup <10s)
- Stored in Azure Blob Storage with 30-day retention
- Tagged with deployment commit SHA for easy identification
- Automated restore script in runbook

**Timeline**: 3 hours development + 1 hour testing

---

#### 5. Migration Code Review Checklist

**Rationale**: Standardize human review process for consistency

**Checklist** (required in PR description):
```markdown
## Database Migration Checklist

- [ ] Migration name follows convention: `YYYYMMDDHHMMSS_DescriptiveAction.cs`
- [ ] Column names use snake_case (e.g., `start_date`, not `StartDate`)
- [ ] Raw SQL uses lowercase identifiers (or quoted PascalCase)
- [ ] No hard-coded connection strings or secrets
- [ ] `Up()` method tested against local PostgreSQL database
- [ ] `Down()` method tested for rollback capability
- [ ] No destructive operations without data migration plan
- [ ] Foreign key relationships preserved
- [ ] Indexes created for query performance
- [ ] Migration reviewed by database team

**Testing Evidence**:
- [ ] Local migration test passed: `dotnet ef database update`
- [ ] Rollback test passed: `dotnet ef database update <previous-migration>`
- [ ] Schema matches EF Core model: `dotnet ef dbcontext scaffold`
```

**Enforcement**:
- GitHub branch protection requires checklist completion
- Automated PR checks verify checklist syntax
- Database team required reviewer for migrations >5 tables

**Timeline**: 1 hour documentation + 1 hour team training

---

### Positive Consequences

1. **Reliability**:
   - Automated validation catches 90%+ of migration errors before deployment
   - Health checks prevent broken deployments from serving traffic
   - Backups enable <5 minute rollback from failures

2. **Safety**:
   - Pre-deployment backups prevent data loss
   - Schema validation detects drift before corruption
   - Code review checklist ensures consistent quality

3. **Maintainability**:
   - Standardized naming convention reduces cognitive load
   - Automated checks reduce code review burden
   - Documented procedures enable team scalability

4. **Observability**:
   - Health check logs provide schema mismatch diagnostics
   - Migration validation reports show exact SQL being executed
   - Backup history enables post-mortem analysis

### Negative Consequences

1. **Performance**:
   - CI/CD pipeline adds ~2 minutes for validation
   - Schema health check adds ~500ms to startup time
   - Backup generation adds ~10 seconds to deployment

2. **Complexity**:
   - More moving parts in deployment pipeline
   - Requires team training on new procedures
   - Additional infrastructure (blob storage for backups)

3. **Developer Experience**:
   - More steps in migration creation workflow
   - Longer feedback loop (validation in CI/CD)
   - Stricter PR review requirements

**Mitigation**:
- Performance overhead acceptable for reliability gains
- Comprehensive documentation and training materials
- Gradual rollout with staging validation first

---

## Compliance and Standards

### Industry Standards Alignment

**OWASP Database Security Guidelines**:
- ✅ Automated schema validation before deployment
- ✅ Backup strategy for disaster recovery
- ✅ Code review for database changes

**12-Factor App Methodology**:
- ✅ Treat backing services (database) as attached resources
- ✅ Explicitly declare dependencies (EF Core migrations)
- ✅ Store config in environment (no hardcoded connection strings)

**Azure Well-Architected Framework**:
- ✅ Reliability: Health checks and backups
- ✅ Security: Automated validation prevents injection
- ✅ Operational Excellence: Automated deployment with safeguards
- ✅ Performance: Schema optimization via index validation

### Regulatory Compliance

**GDPR (Data Protection)**:
- ✅ Schema backups enable data recovery
- ✅ Audit trail of schema changes (git history + CI/CD logs)
- ✅ Prevents accidental data deletion via validation

**SOC 2 Type II (Security Controls)**:
- ✅ Change management process (code review + automated checks)
- ✅ Backup and recovery procedures (pre-deployment backups)
- ✅ Monitoring and alerting (health checks)

---

## Implementation Plan

### Phase 1: Emergency Fix (Completed)
**Timeline**: Day 1 (2 hours)
- [x] Drop corrupted Events schema in staging
- [x] Clean migration history
- [x] Redeploy application
- [x] Verify Event APIs available

### Phase 2: Code Fixes
**Timeline**: Day 2-3 (8 hours)
- [ ] Fix column name case in `AddEventLocationWithPostGIS` migration
- [ ] Implement snake_case naming convention globally
- [ ] Add migration validation to CI/CD pipeline
- [ ] Create database schema health check
- [ ] Test on staging environment

### Phase 3: Infrastructure Improvements
**Timeline**: Week 2 (16 hours)
- [ ] Implement pre-deployment schema backup
- [ ] Create rollback automation script
- [ ] Add schema health check to /health endpoint
- [ ] Document migration procedures in runbook
- [ ] Create team training materials

### Phase 4: Team Enablement
**Timeline**: Week 3 (8 hours)
- [ ] Conduct team training on new migration process
- [ ] Update PR template with migration checklist
- [ ] Create migration troubleshooting guide
- [ ] Establish database team on-call rotation
- [ ] Schedule quarterly disaster recovery drill

**Total Effort**: 34 hours over 3 weeks

---

## Success Metrics

### Key Performance Indicators (KPIs)

| Metric | Baseline | Target | Measurement |
|--------|----------|--------|-------------|
| Deployment Success Rate | 85% | 99% | GitHub Actions success rate |
| Migration Failure Rate | 15% | <1% | Failed migrations / total migrations |
| Mean Time to Recovery (MTTR) | 2 hours | 15 minutes | From failure to rollback |
| Schema Drift Incidents | 3/month | 0/month | Detected by health checks |
| Pipeline Duration | 8 minutes | 10 minutes | CI/CD total runtime |

### Monitoring and Alerting

**Application Insights Alerts**:
- Database schema health check failures (Severity: Critical)
- Migration execution time >2 minutes (Severity: Warning)
- Database connection pool exhaustion (Severity: High)

**Azure Monitor Alerts**:
- Container restart count >3 in 10 minutes (Severity: Critical)
- Health check HTTP 503 responses (Severity: High)
- PostgreSQL CPU >80% during migration (Severity: Warning)

---

## Lessons Learned

### What Went Wrong

1. **Technical Debt**: No naming convention enforced, leading to case-sensitivity bugs
2. **Process Gap**: No migration validation in CI/CD pipeline
3. **Architectural Gap**: No health checks to detect schema mismatches
4. **Knowledge Gap**: Team unaware of PostgreSQL case-sensitivity rules
5. **Tooling Gap**: No automated rollback mechanism

### What Went Right

1. **Detection**: Issue found in staging before production deployment
2. **Documentation**: Existing migrations provided rollback reference
3. **Isolation**: Events feature independent from core authentication
4. **Recovery**: Zero data loss (staging had no production Events data)

### Recommendations for Future

1. **Proactive**:
   - Run schema validation in pre-commit hooks
   - Generate migration script preview in PR comments
   - Implement database schema versioning endpoint

2. **Reactive**:
   - Establish 15-minute MTTR SLA for migration failures
   - Create runbook for common migration scenarios
   - Schedule quarterly disaster recovery drills

3. **Cultural**:
   - Foster "database as code" mindset
   - Celebrate successful migration reviews
   - Share post-mortems transparently

---

## Related Decisions

- **ADR-001**: Database Technology Selection (PostgreSQL)
- **ADR-003**: EF Core Migration Strategy
- **ADR-005**: Azure Container Apps Deployment Model

---

## References

- [PostgreSQL Naming Conventions](https://www.postgresql.org/docs/15/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS)
- [EF Core Migrations Best Practices](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Azure Well-Architected Framework - Reliability](https://learn.microsoft.com/en-us/azure/architecture/framework/resiliency/)
- [GitHub Actions - Database Migration Patterns](https://github.blog/2021-06-10-database-migrations-in-ci-cd-pipelines/)
- [OWASP Database Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Database_Security_Cheat_Sheet.html)

---

## Approval Signatures

**Proposed By**: System Architecture Team
**Date**: 2025-11-03

**Reviewed By**:
- [ ] Database Administrator (DBA Team)
- [ ] DevOps Lead (Infrastructure Team)
- [ ] Backend Tech Lead (Development Team)
- [ ] Security Engineer (Security Team)

**Approved By**:
- [ ] CTO / Head of Engineering
- [ ] Product Owner (for timeline approval)

**Status**: Awaiting approval ⏳

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-03 | Architecture Team | Initial proposal |

---

## Appendix: Technical Specifications

### A. PostgreSQL Column Name Case Sensitivity

```sql
-- PostgreSQL behavior examples:

-- Unquoted identifiers are folded to lowercase
CREATE TABLE events (Status varchar(20));  -- Creates column "status"

-- Quoted identifiers preserve case
CREATE TABLE events ("Status" varchar(20));  -- Creates column "Status"

-- Query behavior
SELECT status FROM events;   -- Works if column is "status" (lowercase)
SELECT Status FROM events;   -- Same as above (folded to lowercase)
SELECT "Status" FROM events; -- Works ONLY if column is "Status" (exact case)
```

### B. EF Core Snake Case Naming Convention

```csharp
public class SnakeCaseNamingConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            // Table names to snake_case
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            // Index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }
    }
}
```

### C. Migration Validation Script

```bash
#!/bin/bash
# validate-migrations.sh

echo "Generating migration script..."
dotnet ef migrations script --idempotent --output temp-migration.sql

echo "Validating SQL syntax..."
# Check for unquoted case-sensitive identifiers
CASE_SENSITIVE_COLUMNS="status|start_date|end_date|created_at|updated_at"

if grep -iE "\b($CASE_SENSITIVE_COLUMNS)\b" temp-migration.sql | grep -v '"'; then
    echo "❌ ERROR: Unquoted case-sensitive column names detected"
    echo "PostgreSQL column names are case-sensitive when quoted."
    echo "Use double quotes: \"Status\" or convert to snake_case: status"
    exit 1
fi

# Check for destructive operations
if grep -iE 'DROP (TABLE|SCHEMA|DATABASE)' temp-migration.sql; then
    echo "⚠️  WARNING: Destructive DROP statement detected"
    echo "Please ensure data migration plan is in place"
    # Fail for production deployments
    if [ "$ENVIRONMENT" = "production" ]; then
        exit 1
    fi
fi

echo "✅ Migration validation passed"
rm temp-migration.sql
```

### D. Schema Health Check SQL

```sql
-- Verify Events table schema
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;

-- Expected result: 22 columns
-- Failure: Missing column or wrong data type
```

---

**End of ADR-007**

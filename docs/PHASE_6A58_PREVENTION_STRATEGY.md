# Phase 6A.58: Prevention Strategy - Database Naming Consistency

**Date**: 2025-12-30
**Purpose**: Prevent mixed database naming conventions
**Applies To**: All future EF Core configurations and migrations

---

## Problem Statement

Phase 6A.58 revealed that the database schema has mixed naming conventions:
- **PascalCase**: `Status`, `Category`, `StartDate` (EF Core defaults)
- **snake_case**: `title`, `description`, `search_vector` (explicit configuration)

This causes:
1. Developer confusion about correct column names
2. Raw SQL query failures in PostgreSQL
3. Maintenance overhead (must remember which columns use which case)
4. Increased risk of bugs when writing database queries

**Root Cause**: Inconsistent use of `.HasColumnName()` in EF Core configurations.

---

## Prevention Rules

### Rule 1: ALWAYS Use .HasColumnName()

**REQUIRED**: Every property in EF Core configuration MUST have explicit `.HasColumnName()`.

**Enforcement**: Code review checklist + automated tests.

**Example**:

```csharp
// ❌ WRONG: Relies on EF Core default (PascalCase)
builder.Property(e => e.Status)
    .HasConversion<string>()
    .IsRequired();

// ✅ CORRECT: Explicitly specifies column name
builder.Property(e => e.Status)
    .HasColumnName("status")
    .HasConversion<string>()
    .IsRequired();
```

### Rule 2: Use snake_case for ALL Columns

**REQUIRED**: All database columns MUST use snake_case (PostgreSQL convention).

**Rationale**:
- PostgreSQL standard convention
- Case-insensitive by default (no quotes needed)
- Consistent with most PostgreSQL tools and ORMs
- Easier to read in raw SQL queries

**Example**:

```csharp
// ✅ CORRECT: All properties use snake_case
builder.Property(e => e.StartDate)
    .HasColumnName("start_date")
    .HasColumnType("timestamp with time zone");

builder.Property(e => e.OrganizerId)
    .HasColumnName("organizer_id")
    .IsRequired();

builder.Property(e => e.Status)
    .HasColumnName("status")
    .HasConversion<string>();

builder.Property(e => e.Category)
    .HasColumnName("category")
    .HasConversion<string>();
```

### Rule 3: Enums Always Use String Conversion

**REQUIRED**: All enum properties MUST use `.HasConversion<string>()`.

**Rationale**:
- Readable in database
- Self-documenting
- Safe for enum refactoring (adding/removing values)
- Compatible with raw SQL queries

**Example**:

```csharp
// ✅ CORRECT: Enum stored as string
builder.Property(e => e.Status)
    .HasColumnName("status")
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired();

// ❌ WRONG: Enum stored as integer (fragile)
builder.Property(e => e.Status)
    .HasColumnName("status")
    .IsRequired();
```

### Rule 4: Value Objects Use Nested snake_case

**REQUIRED**: Nested properties in value objects MUST also use snake_case.

**Example**:

```csharp
// ✅ CORRECT: Value object with explicit column names
builder.OwnsOne(e => e.Location, location =>
{
    location.OwnsOne(l => l.Address, address =>
    {
        address.Property(a => a.Street)
            .HasColumnName("address_street")  // Prefixed snake_case
            .IsRequired();

        address.Property(a => a.City)
            .HasColumnName("address_city")
            .IsRequired();

        address.Property(a => a.ZipCode)
            .HasColumnName("address_zip_code")
            .IsRequired();
    });

    location.OwnsOne(l => l.Coordinates, coords =>
    {
        coords.Property(c => c.Latitude)
            .HasColumnName("coordinates_latitude")  // Prefixed snake_case
            .HasPrecision(10, 7);

        coords.Property(c => c.Longitude)
            .HasColumnName("coordinates_longitude")
            .HasPrecision(10, 7);
    });
});
```

---

## Code Review Checklist

### For EF Core Configuration Files

**Before approving PR**:

- [ ] All properties have `.HasColumnName("snake_case")`
- [ ] No property relies on EF Core default naming
- [ ] All enums use `.HasConversion<string>()`
- [ ] Value objects have explicit nested column names
- [ ] Column names follow `parent_property_name` pattern for nested objects
- [ ] No mixed case in column names (all lowercase with underscores)

### For Migration Files

**Before applying migration**:

- [ ] Review generated SQL for column names
- [ ] Verify all columns use snake_case
- [ ] Check for unintended PascalCase columns
- [ ] Ensure indexes use snake_case column names
- [ ] Validate foreign key column names

### For Raw SQL Queries

**Before deploying**:

- [ ] All column names match database schema
- [ ] Enum comparisons use string values (`.ToString()`)
- [ ] Schema prefix used: `schema_name.table_name`
- [ ] No quoted identifiers unless absolutely necessary
- [ ] JSONB property access uses correct case: `column->>'PropertyName'`

---

## Automated Testing

### Test 1: Schema Consistency Test

**Purpose**: Verify all properties have explicit column names.

**File**: `tests/LankaConnect.UnitTests/Infrastructure/ConfigurationTests/SchemaConsistencyTests.cs`

```csharp
using LankaConnect.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LankaConnect.UnitTests.Infrastructure.ConfigurationTests;

public class SchemaConsistencyTests
{
    [Theory]
    [InlineData(typeof(EventConfiguration))]
    [InlineData(typeof(RegistrationConfiguration))]
    [InlineData(typeof(UserConfiguration))]
    // Add all configuration types
    public void Configuration_AllPropertiesHaveExplicitColumnNames(Type configurationType)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var configuration = (IEntityTypeConfiguration<object>)Activator.CreateInstance(configurationType)!;
        var entityType = configurationType.BaseType!.GetGenericArguments()[0];

        // Act
        configuration.Configure(modelBuilder.Entity(entityType));
        var entityTypeConfig = modelBuilder.Model.FindEntityType(entityType)!;

        // Assert
        foreach (var property in entityTypeConfig.GetProperties())
        {
            var columnName = property.GetColumnName();
            Assert.NotNull(columnName);
            Assert.NotEmpty(columnName);

            // Verify snake_case (lowercase with underscores)
            Assert.Equal(columnName, columnName.ToLowerInvariant());
            Assert.DoesNotContain(columnName, c => char.IsUpper(c));
        }
    }

    [Theory]
    [InlineData(typeof(EventConfiguration))]
    [InlineData(typeof(RegistrationConfiguration))]
    // Add all configuration types
    public void Configuration_EnumsUseStringConversion(Type configurationType)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var configuration = (IEntityTypeConfiguration<object>)Activator.CreateInstance(configurationType)!;
        var entityType = configurationType.BaseType!.GetGenericArguments()[0];

        // Act
        configuration.Configure(modelBuilder.Entity(entityType));
        var entityTypeConfig = modelBuilder.Model.FindEntityType(entityType)!;

        // Assert
        foreach (var property in entityTypeConfig.GetProperties())
        {
            var clrType = property.ClrType;
            if (clrType.IsEnum)
            {
                var converter = property.GetValueConverter();
                Assert.NotNull(converter);

                // Verify enum is converted to string, not int
                Assert.Equal(typeof(string), converter.ProviderClrType);
            }
        }
    }
}
```

### Test 2: Migration Naming Test

**Purpose**: Verify migrations use snake_case column names.

**File**: `tests/LankaConnect.IntegrationTests/MigrationNamingTests.cs`

```csharp
using LankaConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace LankaConnect.IntegrationTests;

public class MigrationNamingTests
{
    [Fact]
    public void Migrations_AllColumnsUseSnakeCase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Server=localhost;Database=test;")
            .Options;

        using var context = new ApplicationDbContext(options);
        var migrator = context.GetService<IMigrator>();

        // Act
        var migrations = context.Database.GetMigrations();

        // Assert
        foreach (var migration in migrations)
        {
            var sql = migrator.GenerateScript(migration, migration);

            // Check for PascalCase column names in SQL
            var lines = sql.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("ADD COLUMN", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("ALTER COLUMN", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract column name
                    var match = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"(?:ADD|ALTER)\s+COLUMN\s+""?([a-zA-Z_]+)""?",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );

                    if (match.Success)
                    {
                        var columnName = match.Groups[1].Value;

                        // Verify snake_case
                        Assert.Equal(columnName, columnName.ToLowerInvariant());
                        Assert.DoesNotContain(columnName, c => char.IsUpper(c));
                    }
                }
            }
        }
    }
}
```

### Test 3: Raw SQL Query Test

**Purpose**: Verify raw SQL queries use correct column names.

**File**: `tests/LankaConnect.IntegrationTests/RawSqlQueryTests.cs`

```csharp
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LankaConnect.IntegrationTests;

public class RawSqlQueryTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public RawSqlQueryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task EventRepository_SearchAsync_UsesCorrectColumnNames()
    {
        // Arrange
        var repository = new EventRepository(_context);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await repository.SearchAsync(
                "test",
                limit: 10,
                offset: 0
            )
        );

        Assert.Null(exception); // No "column does not exist" error
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## Migration Strategy: Fix Existing Schema

### Phase 1: Create Migration to Rename Columns

**Purpose**: Standardize all PascalCase columns to snake_case.

**Timeline**: Next sprint (non-critical)

**Steps**:

1. **Create migration**:
```bash
dotnet ef migrations add StandardizeDatabaseNamingConvention
```

2. **Edit migration** to rename columns:
```csharp
public partial class StandardizeDatabaseNamingConvention : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Events table
        migrationBuilder.RenameColumn(
            name: "Status",
            table: "events",
            schema: "events",
            newName: "status");

        migrationBuilder.RenameColumn(
            name: "Category",
            table: "events",
            schema: "events",
            newName: "category");

        migrationBuilder.RenameColumn(
            name: "StartDate",
            table: "events",
            schema: "events",
            newName: "start_date");

        migrationBuilder.RenameColumn(
            name: "EndDate",
            table: "events",
            schema: "events",
            newName: "end_date");

        migrationBuilder.RenameColumn(
            name: "OrganizerId",
            table: "events",
            schema: "events",
            newName: "organizer_id");

        migrationBuilder.RenameColumn(
            name: "Capacity",
            table: "events",
            schema: "events",
            newName: "capacity");

        migrationBuilder.RenameColumn(
            name: "CreatedAt",
            table: "events",
            schema: "events",
            newName: "created_at");

        migrationBuilder.RenameColumn(
            name: "UpdatedAt",
            table: "events",
            schema: "events",
            newName: "updated_at");

        migrationBuilder.RenameColumn(
            name: "PublishedAt",
            table: "events",
            schema: "events",
            newName: "published_at");

        migrationBuilder.RenameColumn(
            name: "CancellationReason",
            table: "events",
            schema: "events",
            newName: "cancellation_reason");

        // Rename indexes that reference renamed columns
        migrationBuilder.RenameIndex(
            name: "ix_events_status",
            table: "events",
            schema: "events",
            newName: "ix_events_status");

        migrationBuilder.RenameIndex(
            name: "ix_events_organizer_id",
            table: "events",
            schema: "events",
            newName: "ix_events_organizer_id");

        migrationBuilder.RenameIndex(
            name: "ix_events_start_date",
            table: "events",
            schema: "events",
            newName: "ix_events_start_date");

        // Repeat for other tables...
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse the renames
        // ...
    }
}
```

### Phase 2: Update EF Core Configurations

**Update all configuration files** to use `.HasColumnName("snake_case")`:

```csharp
// EventConfiguration.cs
builder.Property(e => e.Status)
    .HasColumnName("status")  // ADDED
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired();

builder.Property(e => e.Category)
    .HasColumnName("category")  // ADDED
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired();

builder.Property(e => e.StartDate)
    .HasColumnName("start_date")  // ADDED
    .HasColumnType("timestamp with time zone");

builder.Property(e => e.EndDate)
    .HasColumnName("end_date")  // ADDED
    .HasColumnType("timestamp with time zone");

// ... all other properties
```

### Phase 3: Update Raw SQL Queries

**Find all raw SQL queries**:
```bash
grep -r "FromSqlRaw\|SqlQueryRaw" src/
```

**Update column references** to remove quotes:
```csharp
// BEFORE:
@"e.""Status"" = {0}"
@"e.""Category"" = {0}"
@"e.""StartDate"" >= {0}"

// AFTER:
"e.status = {0}"
"e.category = {0}"
"e.start_date >= {0}"
```

### Phase 4: Test and Deploy

1. Run all tests
2. Deploy to staging
3. Verify all functionality works
4. Deploy to production

---

## Developer Guidelines

### When Adding New Entities

**Template for entity configuration**:

```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        // Table name (snake_case, singular)
        builder.ToTable("my_entity");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Simple properties (ALWAYS use HasColumnName)
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Enums (ALWAYS use string conversion)
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Foreign keys
        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        // Value objects (nested properties)
        builder.OwnsOne(e => e.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("address_street")
                .HasMaxLength(200);

            address.Property(a => a.City)
                .HasColumnName("address_city")
                .HasMaxLength(100);

            // ... all nested properties
        });

        // Relationships
        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey("my_entity_id")  // snake_case FK
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes (snake_case names)
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_my_entity_status");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_my_entity_created_at");
    }
}
```

### When Writing Raw SQL Queries

**Template for repository methods**:

```csharp
public async Task<IReadOnlyList<MyEntity>> SearchAsync(
    string searchTerm,
    CancellationToken cancellationToken = default)
{
    // Use snake_case column names (unquoted)
    // Use schema.table_name format
    // Use string values for enum comparisons
    var sql = @"
        SELECT e.*
        FROM my_schema.my_entity e
        WHERE e.name ILIKE {0}
          AND e.status = {1}
        ORDER BY e.created_at DESC
        LIMIT 100";

    var parameters = new object[]
    {
        $"%{searchTerm}%",
        MyEntityStatus.Active.ToString()  // String, not integer
    };

    return await _dbSet
        .FromSqlRaw(sql, parameters)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
```

---

## Training and Onboarding

### For New Developers

**Required reading**:
1. This document (PHASE_6A58_PREVENTION_STRATEGY.md)
2. Root Cause Analysis (PHASE_6A58_ROOT_CAUSE_ANALYSIS.md)
3. EF Core naming conventions documentation

**Required tasks**:
1. Review existing EF Core configurations
2. Complete naming convention quiz
3. Write sample entity configuration following guidelines
4. Review code review checklist

### For Existing Developers

**Action items**:
1. Review Phase 6A.58 root cause analysis
2. Update any in-progress feature branches
3. Apply rules to new entity configurations
4. Participate in migration planning (Phase 1-4)

---

## Enforcement

### Pre-Commit Hooks (Optional)

**File**: `.git/hooks/pre-commit`

```bash
#!/bin/bash

# Check for PascalCase in new migrations
git diff --cached --name-only | grep "Migrations" | while read file; do
    if grep -q "ADD COLUMN [A-Z]" "$file"; then
        echo "ERROR: Migration contains PascalCase column names in $file"
        echo "Please use snake_case for all column names"
        exit 1
    fi
done
```

### GitHub Actions Check

**File**: `.github/workflows/schema-validation.yml`

```yaml
name: Schema Validation

on:
  pull_request:
    paths:
      - 'src/**.cs'
      - 'src/**/Configurations/**.cs'
      - 'src/**/Migrations/**.cs'

jobs:
  validate-schema:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run schema consistency tests
        run: dotnet test --filter "FullyQualifiedName~SchemaConsistencyTests"
```

---

## Summary

**Key Takeaways**:
1. ALWAYS use `.HasColumnName("snake_case")` for every property
2. ALWAYS use `.HasConversion<string>()` for enums
3. ALWAYS review migrations for naming consistency
4. NEVER rely on EF Core default naming (PascalCase)

**Next Actions**:
1. Implement automated tests (SchemaConsistencyTests)
2. Update code review checklist
3. Plan migration to standardize existing schema
4. Train team on new guidelines

---

**Document Version**: 1.0
**Created**: 2025-12-30
**Last Updated**: 2025-12-30
**Applies To**: All EF Core entity configurations

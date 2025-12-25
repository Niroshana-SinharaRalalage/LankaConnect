# Architecture Decision Record: Email Template Value Object Hydration

**Status**: Accepted
**Date**: 2025-12-24
**Context**: Phase 6A.41 - Email Template Loading Failure
**Decision Makers**: System Architecture Team

---

## Context

Entity Framework Core was unable to materialize `EmailTemplate` entities from the database, throwing the error:
```
Cannot access value of a failed result at lambda_method4016
```

This occurred when loading email templates for sending event publication and registration confirmation emails.

### Problem Statement

The `EmailTemplate` entity contains a value object `EmailSubject` (type `EmailSubject`):

```csharp
public class EmailTemplate : BaseEntity
{
    public EmailSubject SubjectTemplate { get; private set; }
    // ... other properties
}
```

The `EmailSubject` value object uses the Result pattern for creation:

```csharp
public class EmailSubject : ValueObject
{
    public static Result<EmailSubject> Create(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result<EmailSubject>.Failure("Email subject is required");
        // ... validation
        return Result<EmailSubject>.Success(new EmailSubject(trimmed));
    }
}
```

### The EF Core Materialization Problem

**Original Configuration** (Phase 6A.34):
```csharp
builder.OwnsOne(e => e.SubjectTemplate, subject =>
{
    subject.Property(s => s.Value)
        .HasColumnName("subject_template")
        .HasMaxLength(200)
        .IsRequired();
});
```

**What happened during query execution**:

1. EF Core reads `subject_template` column from database (value: NULL or valid string)
2. EF Core attempts to create `EmailSubject` instance by calling `EmailSubject.Create(dbValue)`
3. If `dbValue` is NULL/empty, `Create()` returns `Result<EmailSubject>.Failure(...)`
4. EF Core tries to access `.Value` on the failed Result
5. **Exception thrown**: "Cannot access value of a failed result"

### Why This Happened

The combination of:
- `OwnsOne` configuration (complex type mapping)
- Result pattern validation (fails for NULL/empty values)
- EF Core's materialization logic (expects success for existing data)

Created a conflict: EF Core expected to always successfully hydrate entities from the database, but the value object's validation could fail.

---

## Decision

### Change EF Core Configuration from OwnsOne to HasConversion

**New Configuration** (Phase 6A.41):
```csharp
builder.Property(e => e.SubjectTemplate)
    .HasColumnName("subject_template")
    .HasMaxLength(200)
    .IsRequired()
    .HasConversion(
        // Convert EmailSubject to string for database
        subject => subject.Value,
        // Convert string from database to EmailSubject
        // Use FromDatabase() to bypass validation during hydration
        value => EmailSubject.FromDatabase(value));
```

### Add Internal Factory Method for Database Hydration

**New Method in EmailSubject**:
```csharp
/// <summary>
/// Phase 6A.41: Internal constructor for EF Core hydration.
/// Bypasses validation to allow loading potentially invalid data from database.
/// Should only be used by infrastructure layer during entity materialization.
/// </summary>
internal static EmailSubject FromDatabase(string value)
{
    // For EF Core hydration, create instance even with empty/null value
    // This prevents "Cannot access value of a failed result" error during query materialization
    return new EmailSubject(value ?? string.Empty);
}
```

### Keep Public Factory for Domain Logic

**Unchanged** (still validates):
```csharp
public static Result<EmailSubject> Create(string subject)
{
    if (string.IsNullOrWhiteSpace(subject))
        return Result<EmailSubject>.Failure("Email subject is required");
    // ... validation continues
}
```

---

## Rationale

### Why HasConversion Instead of OwnsOne?

| Aspect | OwnsOne | HasConversion |
|--------|---------|---------------|
| **Complexity** | Complex type with nested properties | Simple value conversion |
| **EF Core Behavior** | Calls factory/constructor during materialization | Calls custom converter function |
| **Result Pattern** | Incompatible (expects success) | Compatible (we control conversion) |
| **Performance** | Slightly slower (complex mapping) | Faster (direct conversion) |
| **Control** | EF Core controls instantiation | We control instantiation |

### Why Bypass Validation During Hydration?

**Principle**: Data already in the database is assumed valid.

**Reasoning**:
1. **Validation occurs at write time**: When creating new `EmailTemplate` entities, `EmailSubject.Create()` validates the subject
2. **Database is source of truth**: If data exists in database, it was validated when written
3. **Corruption should be handled separately**: If database has corrupted data, hydration shouldn't fail - handle via health checks
4. **EF Core pattern**: Many EF Core scenarios bypass validation during hydration (private setters, internal constructors)

### Separation of Concerns

- **Domain Layer** (`EmailSubject.Create()`): Enforces business rules for NEW data
- **Infrastructure Layer** (`EmailSubject.FromDatabase()`): Handles database persistence concerns
- **Validation**: Happens at boundaries (API, commands), not during data access

---

## Consequences

### Positive

✅ **No More Materialization Errors**: EF Core can successfully load entities even with NULL/empty subjects
✅ **Validation Still Enforced**: New templates created via `EmailTemplate.Create()` still validate subjects
✅ **Better Performance**: `HasConversion` is simpler and faster than `OwnsOne`
✅ **Cleaner Code**: Single property mapping instead of owned entity configuration
✅ **Testability**: Easier to test value object creation separately from persistence

### Negative

⚠️ **Potential for Invalid Data**: Database could contain templates with empty subjects
⚠️ **Silent Failures**: Invalid templates loaded successfully but fail later during rendering
⚠️ **Two Creation Paths**: Developers must know when to use `Create()` vs `FromDatabase()`

### Mitigation

1. **Database Constraints**: Ensure `subject_template` column is NOT NULL
   ```sql
   ALTER TABLE communications.email_templates
   ALTER COLUMN subject_template SET NOT NULL;
   ```

2. **Health Checks**: Add validation to detect invalid templates
   ```csharp
   public async Task<HealthCheckResult> ValidateEmailTemplates()
   {
       var invalidTemplates = await _dbContext.EmailTemplates
           .Where(t => string.IsNullOrEmpty(t.SubjectTemplate.Value))
           .ToListAsync();

       return invalidTemplates.Any()
           ? HealthCheckResult.Unhealthy($"Invalid templates: {string.Join(", ", invalidTemplates.Select(t => t.Name))}")
           : HealthCheckResult.Healthy();
   }
   ```

3. **Migration Verification**: Add checks in migrations to ensure seeded data is valid
   ```sql
   DO $$
   BEGIN
       IF EXISTS (SELECT 1 FROM communications.email_templates WHERE subject_template IS NULL) THEN
           RAISE EXCEPTION 'Migration failed: Templates with NULL subjects detected';
       END IF;
   END $$;
   ```

4. **Documentation**: Clear comments on when to use each factory method
   - `Create()`: For domain logic (new templates, updates)
   - `FromDatabase()`: Only for EF Core (marked `internal`)

---

## Alternatives Considered

### Alternative 1: Keep OwnsOne and Fix Result Pattern

**Approach**: Modify `EmailSubject.Create()` to return success for NULL/empty values

**Rejected because**:
- Violates single responsibility (mixing validation and hydration)
- Weakens domain validation
- Confusing behavior (when does validation actually happen?)

### Alternative 2: Use Shadow Property

**Approach**: Store subject as shadow property, expose as value object

**Code**:
```csharp
builder.Property<string>("_subject")
    .HasColumnName("subject_template");

builder.Ignore(e => e.SubjectTemplate);
```

**Rejected because**:
- Requires custom materialization logic
- More complex than HasConversion
- Harder to test and maintain

### Alternative 3: Make EmailSubject Nullable

**Approach**: Allow `EmailSubject?` on entity

**Rejected because**:
- Subject is required business rule
- Pushes null checks to all consumers
- Violates domain model integrity

---

## Implementation Notes

### Files Changed (Phase 6A.41)

1. **EmailTemplateConfiguration.cs**
   - Changed from `OwnsOne` to `HasConversion`
   - Added comment explaining the change

2. **EmailSubject.cs**
   - Added `FromDatabase()` internal static method
   - Documented with XML comments

3. **No Changes Required**:
   - Domain logic (`EmailTemplate.Create()`)
   - Repository layer (uses EF Core transparently)
   - Application layer (uses domain logic)

### Migration Path

**Existing Databases**: No migration required - configuration change only

**New Databases**: Works immediately

**Data Validation**: Add post-deployment health check to verify no NULL subjects

---

## Future Improvements

1. **Consider for Other Value Objects**: Apply same pattern to:
   - `EventTitle`
   - `EventDescription`
   - Other value objects that use Result pattern

2. **Add Base Class**: Create `ValueObjectWithDatabaseHydration<T>` base class

3. **Code Generator**: Auto-generate `FromDatabase()` methods for value objects

4. **Analyzer**: Create Roslyn analyzer to detect OwnsOne + Result pattern anti-pattern

---

## References

- **EF Core Value Conversions**: https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions
- **Result Pattern**: Railway-Oriented Programming (Scott Wlaschin)
- **Domain-Driven Design**: Value Objects (Eric Evans)
- **Related Issue**: Phase 6A.41 Email Template Loading Failure

---

## Decision Log

| Date | Decision | Reason |
|------|----------|--------|
| 2025-12-24 | Use HasConversion instead of OwnsOne | Fixes materialization error, simpler code |
| 2025-12-24 | Add FromDatabase() internal method | Separates domain validation from infrastructure hydration |
| 2025-12-24 | Keep Create() with validation | Maintains domain integrity for new entities |

---

**Status**: ✅ Accepted and Implemented
**Impact**: Medium - Affects all value objects with Result pattern
**Review Date**: 2025-03-24 (3 months) - Evaluate if pattern should be applied to other value objects

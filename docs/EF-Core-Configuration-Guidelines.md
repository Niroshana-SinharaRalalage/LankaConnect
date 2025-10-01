# EF Core Configuration Guidelines for Value Objects and Complex Types

## Overview

This document provides comprehensive guidelines for configuring EF Core entities in the LankaConnect application, with particular focus on value objects, owned entity types, and complex domain models that follow Clean Architecture and Domain-Driven Design principles.

## Core Configuration Principles

### 1. Domain Purity
- **Domain entities remain pure** - No EF Core attributes or dependencies
- **Configuration is external** - All mapping in separate configuration classes
- **Value objects maintain immutability** - Private constructors and readonly properties

### 2. Owned Entity Types Strategy
- **Use owned entities for value objects** - Address, GeoCoordinate, etc.
- **Proper table splitting** - Multiple owned types can share parent table
- **Navigation configuration** - Prevent unwanted foreign key relationships

### 3. Constructor Binding Resolution
- **Private constructors supported** - EF Core 5.0+ can bind to private constructors
- **Parameter name matching** - Constructor parameters must match property names (case-insensitive)
- **Backing field configuration** - Use backing fields for complex scenarios

## BusinessLocation Configuration Solution

### Current Issue Analysis
```
Error: Cannot bind 'address', 'coordinates' in 'BusinessLocation(Address address, GeoCoordinate coordinates)'
```

**Root Cause**: EF Core cannot automatically bind constructor parameters for nested value objects without explicit configuration.

### Solution: Owned Entity Configuration

```csharp
public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");

        // Primary key and basic properties
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        
        // Configure BusinessLocation as owned entity
        builder.OwnsOne(b => b.Location, location =>
        {
            // Configure Address as nested owned entity
            location.OwnsOne(l => l.Address, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("Location_Address_Street")
                    .HasMaxLength(200)
                    .IsRequired();

                address.Property(a => a.City)
                    .HasColumnName("Location_Address_City")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.State)
                    .HasColumnName("Location_Address_State")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.ZipCode)
                    .HasColumnName("Location_Address_ZipCode")
                    .HasMaxLength(20)
                    .IsRequired();

                address.Property(a => a.Country)
                    .HasColumnName("Location_Address_Country")
                    .HasMaxLength(100)
                    .IsRequired();

                // Ignore methods and computed properties
                address.Ignore(a => a.FullAddress);
            });

            // Configure GeoCoordinate as nested owned entity (nullable)
            location.OwnsOne(l => l.Coordinates, coords =>
            {
                coords.Property(c => c.Latitude)
                    .HasColumnName("Location_Coordinates_Latitude")
                    .HasPrecision(18, 15); // High precision for GPS coordinates

                coords.Property(c => c.Longitude)
                    .HasColumnName("Location_Coordinates_Longitude")
                    .HasPrecision(18, 15);

                // Ignore computed methods
                coords.Ignore(c => c.IsValid);
            });

            // Handle nullable coordinates properly
            location.Navigation(l => l.Coordinates).IsRequired(false);

            // Ignore computed methods on BusinessLocation
            location.Ignore(l => l.DistanceTo);
        });

        // Other Business entity configurations...
    }
}
```

### Alternative Solution: Backing Field Configuration

For complex scenarios where owned entities don't work:

```csharp
public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        // Use backing field approach for complex value objects
        builder.Property<string>("_locationJson")
            .HasColumnName("LocationData")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<BusinessLocationData>(v, (JsonSerializerOptions)null));

        builder.Ignore(b => b.Location);
        
        // Custom property accessor
        builder.Property(b => b.Location)
            .HasConversion(
                location => SerializeLocation(location),
                json => DeserializeLocation(json))
            .HasColumnName("LocationData");
    }
}
```

## General Value Object Configuration Patterns

### 1. Simple Value Object (Single Property)
```csharp
// For value objects like Email, PhoneNumber
builder.OwnsOne(e => e.Email, email =>
{
    email.Property(e => e.Value)
        .HasColumnName("Email")
        .HasMaxLength(320)
        .IsRequired();
});
```

### 2. Complex Value Object (Multiple Properties)
```csharp
// For value objects like Money, DateRange
builder.OwnsOne(e => e.Price, price =>
{
    price.Property(p => p.Amount)
        .HasColumnName("Price_Amount")
        .HasPrecision(18, 2)
        .IsRequired();

    price.Property(p => p.Currency)
        .HasColumnName("Price_Currency")
        .HasMaxLength(3)
        .IsRequired();
});
```

### 3. Collection of Value Objects
```csharp
// For collections like List<Tag>, List<Category>
builder.OwnsMany(e => e.Tags, tags =>
{
    tags.ToTable("BusinessTags");
    tags.WithOwner().HasForeignKey("BusinessId");
    tags.Property<int>("Id").ValueGeneratedOnAdd();
    tags.HasKey("Id");
    
    tags.Property(t => t.Name)
        .HasColumnName("TagName")
        .HasMaxLength(50)
        .IsRequired();
});
```

## Entity Configuration Best Practices

### 1. Configuration Class Organization
```csharp
// Separate configuration file per aggregate root
public class BusinessConfiguration : IEntityTypeConfiguration<Business>
public class UserConfiguration : IEntityTypeConfiguration<User>
public class EventConfiguration : IEntityTypeConfiguration<Event>

// Register all configurations in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

### 2. Naming Conventions
```csharp
// Use consistent column naming
- Single level: "PropertyName" 
- Owned entity: "OwnedType_PropertyName"
- Nested owned: "OwnedType_NestedType_PropertyName"

// Examples:
address.Property(a => a.Street).HasColumnName("Address_Street");
coords.Property(c => c.Latitude).HasColumnName("Coordinates_Latitude");
location.OwnsOne(...).Property(...).HasColumnName("Location_Address_City");
```

### 3. Constraint Configuration
```csharp
// Always specify constraints for value objects
builder.Property(x => x.Email)
    .HasMaxLength(320)           // Practical email length limit
    .IsRequired();               // Business rule enforcement

builder.Property(x => x.Latitude)
    .HasPrecision(18, 15);       // GPS precision requirements

// Add check constraints for business rules
builder.ToTable(t => t.HasCheckConstraint("CK_Business_PricePositive", "Price_Amount > 0"));
```

### 4. Performance Optimizations
```csharp
// Index configuration for owned entities
builder.OwnsOne(e => e.Address, address =>
{
    address.Property(a => a.City).HasColumnName("Address_City");
    
    // Add index on frequently queried owned properties
    builder.HasIndex("Address_City").HasDatabaseName("IX_Business_Address_City");
    builder.HasIndex(new[] { "Address_City", "Address_State" })
           .HasDatabaseName("IX_Business_Address_Location");
});
```

## Migration Generation Guidelines

### 1. Migration Naming
```bash
# Use descriptive names for value object migrations
dotnet ef migrations add "ConfigureBusinessLocationValueObject"
dotnet ef migrations add "AddUserEmailPreferencesEntity" 
```

### 2. Data Migration Scripts
```csharp
// Include data transformation scripts for existing data
public partial class ConfigureBusinessLocationValueObject : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Schema changes
        migrationBuilder.AddColumn<string>("Location_Address_Street", "Businesses");
        
        // Data migration script
        migrationBuilder.Sql(@"
            UPDATE Businesses 
            SET Location_Address_Street = OldStreetColumn,
                Location_Address_City = OldCityColumn
            WHERE OldStreetColumn IS NOT NULL
        ");
        
        // Drop old columns
        migrationBuilder.DropColumn("OldStreetColumn", "Businesses");
    }
}
```

### 3. Rollback Safety
```csharp
// Always ensure migrations can be rolled back
protected override void Down(MigrationBuilder migrationBuilder)
{
    // Add old columns back first
    migrationBuilder.AddColumn<string>("OldStreetColumn", "Businesses");
    
    // Copy data back
    migrationBuilder.Sql(@"
        UPDATE Businesses 
        SET OldStreetColumn = Location_Address_Street
        WHERE Location_Address_Street IS NOT NULL
    ");
    
    // Then drop new columns
    migrationBuilder.DropColumn("Location_Address_Street", "Businesses");
}
```

## Testing Value Object Configurations

### 1. Configuration Unit Tests
```csharp
[Test]
public void BusinessLocationConfiguration_CanSaveAndRetrieve_ValueObjectProperties()
{
    // Arrange
    using var context = CreateTestContext();
    var business = CreateTestBusiness();
    
    // Act
    context.Businesses.Add(business);
    context.SaveChanges();
    
    context.ChangeTracker.Clear();
    var retrieved = context.Businesses.First(b => b.Id == business.Id);
    
    // Assert
    retrieved.Location.Address.Street.Should().Be(business.Location.Address.Street);
    retrieved.Location.Coordinates?.Latitude.Should().Be(business.Location.Coordinates?.Latitude);
}
```

### 2. Migration Tests
```csharp
[Test]
public void BusinessLocationMigration_GeneratesExpectedSchema()
{
    // Test that migration creates correct columns
    var migration = new ConfigureBusinessLocationValueObject();
    var sql = migration.GenerateUpScript();
    
    sql.Should().Contain("Location_Address_Street");
    sql.Should().Contain("Location_Coordinates_Latitude");
}
```

### 3. Performance Tests
```csharp
[Test]
public void BusinessLocationQueries_MeetPerformanceRequirements()
{
    // Test query performance with value objects
    using var context = CreateTestContextWithData(1000);
    
    var stopwatch = Stopwatch.StartNew();
    var results = context.Businesses
        .Where(b => b.Location.Address.City == "Colombo")
        .ToList();
    stopwatch.Stop();
    
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
}
```

## Common Issues and Solutions

### Issue 1: Constructor Binding Failures
**Problem**: EF Core cannot bind constructor parameters  
**Solution**: Ensure parameter names match property names (case-insensitive)

### Issue 2: Nullable Value Objects
**Problem**: Optional value objects cause EF errors  
**Solution**: Use `Navigation(x => x.Property).IsRequired(false)`

### Issue 3: Circular References
**Problem**: Value objects reference each other  
**Solution**: Use `Ignore()` for computed properties and methods

### Issue 4: Performance Issues
**Problem**: Complex value objects slow queries  
**Solution**: Add strategic indexes and use `AsNoTracking()` for read-only operations

## Validation Checklist

- [ ] All value objects configured as owned entities
- [ ] Constructor parameters match property names
- [ ] Nullable value objects properly configured  
- [ ] Column names follow consistent conventions
- [ ] Appropriate constraints and indexes added
- [ ] Migration scripts handle existing data
- [ ] Unit tests verify configuration correctness
- [ ] Performance tests validate query speed

This configuration approach ensures that domain value objects remain pure while providing EF Core with the information needed for proper persistence and querying.
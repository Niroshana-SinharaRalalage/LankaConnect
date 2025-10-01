# ADR: Business Domain TDD Strategy - Critical Dependencies Analysis

## Status
**ACCEPTED** - 2025-09-12

## Context
**Critical Discovery**: Analysis reveals **UserId** as the **highest priority missing type** blocking Business domain progress.

### Current Error Analysis
- **15+ UserId compilation errors** across multiple Payment and Business interfaces
- **Multiple access modifier violations** requiring systematic resolution
- **StronglyTypedId<>** pattern missing - foundational infrastructure gap
- **Business aggregate dependencies** require User domain foundation

### Business Domain Dependencies Chain
```
UserId (CRITICAL) 
  ↓
ContactInformation → Business Entity
  ↓
Service, Review entities → Business Aggregate
  ↓
Payment Services → Revenue Generation
```

## Decision

### Priority 1: User Domain Foundation (IMMEDIATE)
**STRATEGIC SHIFT**: Focus TDD efforts on **User Domain Core Types** first

#### **Critical Missing Types** (Priority Order):
1. **UserId** - Strongly typed identifier (15+ compilation errors)
2. **StronglyTypedId<>** - Base infrastructure pattern
3. **User** entity - Core aggregate root
4. **ContactInformation** - Value object (required by Business)
5. **UserProfile** - Value object
6. **UserStatus** - Enum
7. **UserRole** - Enum/Entity

### Priority 2: Business Domain Integration (SECONDARY)
After User foundation is stable:
1. **Service** entity
2. **Review** entity and **ReviewStatus** enum
3. **BusinessImage** value object (if missing)
4. Business aggregate completion

## Implementation Strategy

### Phase 1: User Domain TDD (Next Batch - 15-20 types)
**Target**: Eliminate UserId compilation errors through systematic User domain creation

#### TDD RED-GREEN-REFACTOR Approach:
```csharp
// RED: Write failing test
[Test]
public void UserId_Should_Create_With_Valid_Guid()
{
    // Arrange
    var guid = Guid.NewGuid();
    
    // Act & Assert
    var userId = new UserId(guid);
    Assert.That(userId.Value, Is.EqualTo(guid));
}

// GREEN: Implement minimum code
public readonly record struct UserId(Guid Value) : IStronglyTypedId<Guid>;

// REFACTOR: Add validation, equality, etc.
```

#### Systematic Implementation Order:
1. **StronglyTypedId<> Infrastructure**
2. **UserId** with comprehensive tests
3. **UserStatus, UserRole** enums
4. **UserProfile** value object
5. **ContactInformation** value object
6. **User** aggregate root
7. **IUserRepository** interface

### Phase 2: Business Domain Integration
After User domain stabilizes Business dependencies:
1. Update **Business.cs** to properly reference User types
2. Implement **Service** and **Review** entities
3. Complete Business aggregate functionality

## Architecture Benefits

### Clean Architecture Compliance
- **Domain Layer**: User and Business entities properly separated
- **Application Layer**: Interfaces reference domain types correctly
- **Infrastructure Layer**: Repository implementations support domain patterns

### Performance Optimization
- **Strongly Typed IDs**: Compile-time safety, zero allocation
- **Value Objects**: Immutable, thread-safe patterns
- **Domain Events**: Proper aggregate boundary enforcement

### Revenue Impact
- **User Foundation**: Enables authentication, authorization, billing
- **Business Integration**: Supports listings, services, reviews
- **Payment Services**: Requires UserId for Stripe integration

## Success Metrics

### Error Reduction Targets
- **Current**: 885 compilation errors
- **Phase 1 Target**: 765 errors (120+ UserId-related fixes)
- **Phase 2 Target**: 645 errors (120+ Business domain completion)

### Quality Gates
- **100% TDD Coverage**: RED-GREEN-REFACTOR for every type
- **Zero Tolerance**: No architectural violations
- **Performance**: <50ms for User operations, <200ms for Business operations

## Risk Mitigation

### Dependency Risk
- **Challenge**: User domain may have its own missing dependencies
- **Mitigation**: Focus on minimal viable User types first
- **Fallback**: Implement placeholder types if needed for compilation

### Complexity Risk
- **Challenge**: User domain authentication/authorization complexity
- **Mitigation**: Start with basic User identity, expand incrementally
- **Strategy**: Separate identity from authentication concerns

### Performance Risk
- **Challenge**: StronglyTypedId performance implications
- **Mitigation**: Use struct-based implementations for zero allocation
- **Validation**: Benchmark critical path operations

## Next Actions

### Immediate (Current Session)
1. **Analyze User domain structure** - identify existing User files
2. **Create StronglyTypedId<> base infrastructure**
3. **Implement UserId with comprehensive TDD**
4. **Validate 15+ UserId compilation error resolution**

### Next Session (Phase 1 Continuation)
1. **Complete User domain core types**
2. **Implement ContactInformation value object**
3. **Create User aggregate root**
4. **Target 120+ error reduction**

### Future (Phase 2)
1. **Business domain integration**
2. **Service/Review entity implementation**
3. **Payment service completion**

## Decision Rationale
1. **Error Impact**: 15+ UserId errors block multiple domains
2. **Dependency Chain**: User foundation required for Business success
3. **Revenue Enablement**: Payment services require User identity
4. **Architecture First**: Proper domain separation from start
5. **TDD Discipline**: Maintains proven RED-GREEN-REFACTOR approach

---
**Strategic Recommendation**: Execute immediate pivot to User domain foundation using proven TDD methodology for maximum error reduction and architectural soundness.
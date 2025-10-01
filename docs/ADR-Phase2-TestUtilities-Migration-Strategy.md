# ADR: Phase 2 TestUtilities Migration Strategy

## Status
**PROPOSED** - Systematic resolution of 246 compilation errors

## Context
Following Phase 1 completion (namespace imports fixed, 290→246 errors), we need a systematic approach to resolve remaining architectural misalignments in our test suite.

## Decision

### **Phase 2A: Test Framework Standardization (Priority 1)**
**Problem**: Mixed NUnit/xUnit attributes causing compilation failures
**Architectural Impact**: Test execution infrastructure

**Resolution**:
1. **Standardize on xUnit** (aligns with .NET Core best practices)
2. **Replace all NUnit attributes**:
   - `[Test]` → `[Fact]`
   - `[TestCase]` → `[Theory]` + `[InlineData]`
   - `[SetUp]` → Constructor or `IAsyncLifetime.InitializeAsync`
   - `Assert.That` → `Assert.Equal/True/False` (xUnit style)

### **Phase 2B: Domain Namespace Correction (Priority 2)**
**Problem**: References to non-existent `LankaConnect.Domain.Email` namespace
**Architectural Impact**: Domain layer consistency

**Resolution**:
1. **Global namespace replacement**: `Email` → `Communications`
2. **Verify domain model alignment** with current Communications aggregate
3. **Update using statements** systematically

### **Phase 2C: Repository Interface Completion (Priority 3)**
**Problem**: Missing repository methods (`CountAsync`, `UpdateAsync`, `AddAsync`)
**Architectural Impact**: Infrastructure layer contracts

**Resolution**:
1. **Add missing methods to base repository interface**
2. **Implement in concrete repositories**
3. **Follow Repository Pattern consistently**

### **Phase 2D: Entity Construction Modernization (Priority 4)**
**Problem**: Tests using old entity creation patterns
**Architectural Impact**: Domain model usage

**Resolution**:
1. **Update entity construction** to use current aggregate patterns
2. **Align with TestUtilities builders**
3. **Ensure domain invariants are respected**

## Implementation Order

### **Sequential Execution** (maintains architectural integrity):

1. **2A: Framework Standardization** - Foundation for all tests
2. **2B: Namespace Correction** - Domain layer alignment  
3. **2C: Repository Completion** - Infrastructure contracts
4. **2D: Entity Modernization** - Domain usage patterns

## Rationale

This order follows **Clean Architecture dependency flow**:
- **Test Infrastructure** (outermost) → **Domain** → **Application** → **Infrastructure**
- Each phase enables the next without circular dependencies
- Maintains compilation at each step

## Consequences

### Positive
- **Systematic error reduction** following architectural principles
- **Consistent test framework** across solution
- **Aligned domain model usage**
- **Complete repository contracts**

### Negative  
- **Sequential approach** may take longer than parallel fixes
- **Temporary compilation failures** during each phase

## Success Metrics
- **Compilation errors**: 246 → 0
- **Test execution**: All tests runnable
- **Architecture compliance**: Clean Architecture principles maintained
- **Framework consistency**: Single test framework (xUnit)

## Implementation Notes

### Phase 2A Commands:
```bash
# Replace NUnit with xUnit attributes globally
find tests/ -name "*.cs" -exec sed -i 's/\[Test\]/[Fact]/g' {} \;
find tests/ -name "*.cs" -exec sed -i 's/\[TestCase(/[InlineData(/g' {} \;
```

### Phase 2B Commands:  
```bash
# Fix namespace references
find tests/ -name "*.cs" -exec sed -i 's/LankaConnect\.Domain\.Email/LankaConnect.Domain.Communications/g' {} \;
```

### Validation Strategy:
- **Compile after each phase** to verify progress
- **Run subset of tests** to validate framework changes
- **Architecture compliance** checks at each step

---

**Decision Record**: Phase 2 follows architectural hierarchy for systematic error resolution
**Next Review**: After Phase 2A completion for framework standardization validation
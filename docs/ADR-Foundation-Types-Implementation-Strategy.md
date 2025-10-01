# ADR: Foundation Types Implementation Strategy

## Status: DRAFT - Architecture Analysis

## Context
Current compilation status: 568 Application errors after implementing Domain Result<T> pattern.
Key error patterns identified:

1. **ValidationResult Ambiguity** (High Impact)
   - Error: CS0104 between Domain.Common.ValidationResult and Domain.Common.Database.ValidationResult
   - Files affected: Multiple interface files
   - Impact: Namespace collision blocking compilation

2. **Missing Application Result Types** (High Impact)
   - PrivilegedAccessResult, AccessValidationResult, JITAccessResult
   - Error: CS0246 missing types
   - Files affected: 3+ files with database security interfaces

3. **FluentValidation Alias Issues** (Medium Impact)
   - Error: CS0307 FVValidator cannot be used with type arguments
   - Files affected: ValidationPipelineBehavior.cs

4. **CulturalEvent Ambiguity** (Medium Impact)
   - Between Application.Common.Models.Performance.CulturalEvent and Domain.Common.Database.CulturalEvent

## Decision Framework Analysis

### Option A: Application-Specific Result Types Extending Domain Result<T>
**Impact Assessment**: HIGH
- Resolves 3+ missing result type errors immediately
- Maintains clean architecture boundaries
- Leverages existing Domain Result<T> foundation
- Follows DDD patterns with Application layer extensions

### Option B: Resolve Ambiguities with Using Aliases
**Impact Assessment**: MEDIUM
- Quick fix for ValidationResult and CulturalEvent ambiguities
- Risk of creating coupling issues
- May mask deeper architectural decisions needed

### Option C: Focus on Specific Missing Types
**Impact Assessment**: HIGH
- Direct error resolution for immediate compilation success
- Builds on successful Domain Result<T> pattern
- Enables TDD RED-GREEN-REFACTOR workflow

## Recommended Strategy: Hybrid Approach with TDD

### Phase 1: Application Result Types (Immediate High Impact)
1. **PrivilegedAccessResult** - TDD implementation
2. **AccessValidationResult** - TDD implementation  
3. **JITAccessResult** - TDD implementation

### Phase 2: Ambiguity Resolution (Quick Wins)
1. Fix FluentValidation alias in ValidationPipelineBehavior
2. Resolve ValidationResult namespace conflicts
3. Address CulturalEvent type ambiguity

### Phase 3: Foundation Security Types
1. Create missing domain entities (PrivilegedUser, CulturalPrivilegePolicy, etc.)
2. Implement supporting value objects and aggregates

## Architecture Principles

1. **Domain Result<T> as Foundation**: All Application result types extend Domain.Common.Result<T>
2. **Clean Architecture Boundaries**: Application types don't leak into Domain
3. **TDD RED-GREEN-REFACTOR**: Each new type follows test-first approach
4. **Namespace Clarity**: Resolve ambiguities with explicit using statements, not aliases

## Expected Impact
- **Immediate**: 15-25 error reduction from Application result types
- **Phase 2**: 10-15 error reduction from ambiguity fixes
- **Phase 3**: Foundation for remaining missing type errors

## Risk Mitigation
- Incremental implementation with compilation validation at each step
- Test coverage for each new result type
- Documentation of namespace resolution strategy
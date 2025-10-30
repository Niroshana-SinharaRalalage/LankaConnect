# LankaConnect Architecture Documentation

This directory contains Architecture Decision Records (ADRs) and detailed design documents for the LankaConnect application.

---

## Latest ADR: Microsoft Entra External ID Integration

**Status:** ✅ Design Complete - Ready for Implementation
**Date:** 2025-10-28

### Complete Documentation Set (200KB+ of Architecture Documentation)

1. **[ADR-002: Entra External ID Integration](./ADR-002-Entra-External-ID-Integration.md)** (26KB)
   - Architecture Decision Record with comprehensive rationale
   - Decision drivers, alternatives considered, consequences
   - 5-week implementation phases
   - Risk assessment and mitigation strategies

2. **[Domain Model Design](./Entra-External-ID-Domain-Model-Design.md)** (29KB)
   - User aggregate refactoring with identity provider abstraction
   - Business rules and invariants
   - Domain events for external providers
   - 25+ TDD test specifications

3. **[Database Migration Strategy](./Entra-External-ID-Database-Migration-Strategy.md)** (28KB)
   - Complete schema changes with SQL scripts
   - EF Core migration code
   - Rollback procedures
   - Data validation queries

4. **[Component Architecture](./Entra-External-ID-Component-Architecture.md)** (44KB)
   - Layer interaction diagrams
   - Authentication flow sequences
   - Security considerations
   - Performance optimization strategies

5. **[Implementation Roadmap](./Entra-External-ID-Implementation-Roadmap.md)** (25KB)
   - Step-by-step TDD implementation plan
   - 5 phases with detailed checkpoints
   - Daily checklist templates
   - Success metrics and completion criteria

6. **[Architecture Summary](./Entra-External-ID-Architecture-Summary.md)** (23KB)
   - Executive overview
   - Key decisions at a glance
   - Configuration examples
   - Testing pyramid

7. **[Quick Reference Guide](./Entra-External-ID-Quick-Reference.md)** (25KB)
   - Fast lookup for common questions
   - Code snippets for all layers
   - Troubleshooting guide
   - Testing commands

---

## Document Structure

### Primary Decision Document
```
ADR-002-Entra-External-ID-Integration.md
├── Context and Problem Statement
├── Decision Drivers
├── Key Decisions (8 sections)
│   ├── Identity Provider Abstraction
│   ├── Authentication Service Architecture
│   ├── User Entity Refactoring
│   ├── Application Layer Commands
│   ├── Database Schema Changes
│   ├── Dual Authentication Mode
│   ├── Domain Events Strategy
│   └── Testing Strategy
├── Implementation Roadmap (5 weeks)
├── Alternatives Considered
└── Consequences and Risks
```

### Design Documents
```
Entra-External-ID-Domain-Model-Design.md
├── User Aggregate Enhancements
├── Factory Methods
├── Business Rules
├── Domain Events
└── TDD Test Cases

Entra-External-ID-Database-Migration-Strategy.md
├── Current Schema Analysis
├── Migration Phases
├── EF Core Migration Code
├── Validation Procedures
└── Rollback Strategy

Entra-External-ID-Component-Architecture.md
├── Architecture Overview Diagram
├── Authentication Flow Diagrams
├── Component Interaction
├── Security Considerations
└── Performance Optimization

Entra-External-ID-Implementation-Roadmap.md
├── Phase 1: Domain Layer (Week 1)
├── Phase 2: Infrastructure Layer (Week 2)
├── Phase 3: Application Layer (Week 3)
├── Phase 4: Presentation Layer (Week 4)
└── Phase 5: Integration & Deployment (Week 5)
```

---

## Quick Start

### For Architects
Start here: [ADR-002](./ADR-002-Entra-External-ID-Integration.md) → [Architecture Summary](./Entra-External-ID-Architecture-Summary.md)

### For Developers
Start here: [Implementation Roadmap](./Entra-External-ID-Implementation-Roadmap.md) → [Quick Reference](./Entra-External-ID-Quick-Reference.md)

### For DBAs
Start here: [Database Migration Strategy](./Entra-External-ID-Database-Migration-Strategy.md)

### For QA Engineers
Start here: [Testing Strategy (in ADR)](./ADR-002-Entra-External-ID-Integration.md#8-testing-strategy) → [Roadmap Testing Sections](./Entra-External-ID-Implementation-Roadmap.md#testing-quick-reference)

---

## Key Architectural Decisions

### 1. Identity Provider Abstraction
- User entity supports multiple authentication providers via `IdentityProvider` enum
- No separate user tables - single User aggregate with provider-specific rules

### 2. Dual Authentication Mode
- Existing local JWT authentication preserved
- New Entra External ID authentication added
- Both can coexist during migration period

### 3. Auto-Provisioning
- Entra users automatically created on first login
- User profile data synced from Entra claims
- Email pre-verified by Entra

### 4. Token Strategy
- Entra token validated once at login
- LankaConnect JWT issued for API access
- Reduces dependency on Entra for each request

---

## Architecture Principles Followed

### Clean Architecture
- **Domain Layer:** Pure business logic, no external dependencies
- **Application Layer:** Use case orchestration via CQRS
- **Infrastructure Layer:** External services (Entra, database)
- **Presentation Layer:** HTTP API controllers

### Domain-Driven Design
- **Aggregates:** User entity with consistency boundaries
- **Value Objects:** Email, IdentityProvider enum
- **Domain Events:** UserCreatedFromExternalProviderEvent
- **Business Rules:** Enforced at domain level

### Test-Driven Development
- **Red-Green-Refactor:** Every change follows TDD cycle
- **Zero Tolerance:** No compilation errors at any checkpoint
- **High Coverage:** 90%+ across all layers
- **Test First:** Tests written before implementation

---

## Implementation Status

### ✅ Completed
- [x] Architecture design and decision documents
- [x] Domain model design with business rules
- [x] Database migration strategy
- [x] Component architecture diagrams
- [x] Implementation roadmap (50-70 hours)
- [x] Testing strategy
- [x] Quick reference guide

### ⏳ In Progress
- [ ] Phase 1: Domain Layer implementation
- [ ] Phase 2: Infrastructure Layer implementation
- [ ] Phase 3: Application Layer implementation
- [ ] Phase 4: Presentation Layer implementation
- [ ] Phase 5: Integration and Deployment

---

## Related Documents

### Current Architecture
- [ADR-001: Email Verification Automation](./ADR-001-EMAIL-VERIFICATION-AUTOMATION.md)
- [ADR-001: Integration Test Strategy](./ADR-001-Integration-Test-Strategy.md)

### Supporting Documents
- [Email Verification MVP Implementation](./EMAIL-VERIFICATION-MVP-IMPLEMENTATION.md)
- [Email Verification Options Comparison](./EMAIL-VERIFICATION-OPTIONS-COMPARISON.md)
- [Integration Test Migration Plan](./Integration-Test-Migration-Plan.md)

---

## Document Conventions

### ADR Numbering
- ADR-001: Email Verification features
- ADR-002: Entra External ID Integration
- ADR-00X: Future decisions

### Status Labels
- ✅ **Approved:** Decision accepted and in use
- 🔄 **Proposed:** Under review
- ⏳ **Draft:** Work in progress
- ❌ **Rejected:** Decision not accepted
- 🔒 **Superseded:** Replaced by newer ADR

### Document Sections
All ADRs follow this structure:
1. Status and metadata
2. Context and problem statement
3. Decision drivers
4. Proposed solution
5. Alternatives considered
6. Consequences
7. References

---

## Review Process

### Architecture Reviews
1. **Draft Phase:** Author creates ADR and supporting documents
2. **Team Review:** Developers, DBAs, QA review for 3-5 days
3. **Architecture Review Board:** Senior architects approve/reject
4. **Security Review:** Security team assesses risks
5. **Approval:** ADR status changed to "Approved"

### Required Reviewers
- Domain Expert
- Security Team
- DevOps Team
- Lead Developer
- QA Lead (for testing strategy)

---

## Contact

**For Architecture Questions:**
- System Architecture Designer
- Lead Developer

**For Implementation Questions:**
- Lead Developer (Domain/Application layers)
- Infrastructure Team (Database/Entra)
- DevOps Team (Deployment)

---

## Change Log

### 2025-10-28
- ✅ Added ADR-002: Entra External ID Integration (26KB)
- ✅ Added Domain Model Design (29KB)
- ✅ Added Database Migration Strategy (28KB)
- ✅ Added Component Architecture (44KB)
- ✅ Added Implementation Roadmap (25KB)
- ✅ Added Architecture Summary (23KB)
- ✅ Added Quick Reference Guide (25KB)
- **Total:** 200KB+ of comprehensive architecture documentation

### 2025-10-XX (Previous)
- Added ADR-001: Email Verification Automation
- Added Integration Test Strategy
- Added Email Verification MVP Implementation

---

## External References

### Microsoft Documentation
- [Microsoft Entra External ID](https://learn.microsoft.com/en-us/entra/external-id/)
- [OAuth 2.0 and OpenID Connect](https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-protocols)

### Architecture Resources
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Test-Driven Development by Kent Beck](https://en.wikipedia.org/wiki/Test-driven_development)

---

**Last Updated:** 2025-10-28
**Document Maintainer:** System Architecture Designer
**Review Status:** ✅ Ready for Team Review

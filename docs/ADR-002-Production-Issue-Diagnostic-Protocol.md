# ADR-002: Production Issue Diagnostic Protocol

**Status**: Proposed
**Date**: 2025-12-17
**Context**: Badge 500 error incident - Hours spent on assumption-driven debugging
**Decision**: Mandatory diagnostic protocol before production fixes

---

## Context

During Badge API 500 error incident (2025-12-17), we deployed 5 migrations over several hours based on unvalidated hypothesis about NULL values. This resulted in:

1. **Wasted Time**: Hours of effort without progress
2. **System Risk**: Multiple deployments without understanding root cause
3. **Technical Debt**: Migrations that may not have been necessary
4. **User Frustration**: Lack of visible progress despite significant effort

**Root Cause of Incident Response Failure**: Skipped evidence collection phase and proceeded directly to fix implementation based on assumptions.

---

## Decision

We will adopt a **mandatory three-phase diagnostic protocol** for all production issues:

### Phase 1: Evidence Collection (REQUIRED)
No code changes permitted until ALL evidence collected:

1. **Exception Capture**
   - Full exception type, message, and stack trace
   - Inner exceptions
   - Request/response context
   - Timestamp and frequency

2. **System State Verification**
   - Database state (for data issues)
   - Schema state (for migration issues)
   - Configuration state (for config issues)
   - Deployment state (code version running)

3. **Reproduction**
   - Local reproduction if possible
   - Staging reproduction with detailed logging
   - Minimal test case isolation

4. **Recent Changes Review**
   - Git log review (last 10 commits)
   - Deployment history
   - Migration history
   - Configuration changes

**Exit Criteria**: All evidence documented in `logs/[issue]-diagnostic-results.txt`

---

### Phase 2: Root Cause Analysis (REQUIRED)
No fix implementation permitted until root cause confirmed:

1. **Hypothesis Formation**
   - Based on collected evidence (not assumptions)
   - Multiple hypotheses if evidence unclear
   - Ranked by likelihood based on evidence

2. **Hypothesis Validation**
   - Test hypothesis with evidence
   - Eliminate alternative explanations
   - Document why other hypotheses rejected

3. **Root Cause Confirmation**
   - Single confirmed root cause
   - Supporting evidence documented
   - Reviewed by second person (architect or senior dev)

**Exit Criteria**: Root cause documented in `docs/RCA-[issue].md` and approved

---

### Phase 3: Fix Implementation (ONLY AFTER PHASES 1 & 2)
With confirmed root cause, implement minimal fix:

1. **Fix Design**
   - Minimal scope addressing confirmed root cause
   - No "while we're here" additions
   - Rollback plan prepared

2. **Local Validation**
   - Test fix locally
   - Verify fix addresses root cause
   - Check for regressions

3. **Staged Deployment**
   - Deploy to staging first
   - Verify fix in staging
   - Monitor for 30 minutes minimum

4. **Production Deployment**
   - Deploy to production
   - Monitor for 24 hours
   - Document resolution

**Exit Criteria**: Issue resolved, monitoring confirms stability

---

## Rationale

### Why This Protocol is Necessary

#### 1. Prevents Assumption-Driven Debugging
**Problem**: Operating on assumptions wastes time and creates risk

**Example from Incident**:
- Assumption: "NULL values must be causing 500 error"
- Reality: Never checked if NULL values actually existed
- Result: 5 migrations that may not have been needed

**Solution**: Evidence collection forces validation of assumptions

---

#### 2. Reduces Mean Time to Resolution (MTTR)
**Counterintuitive**: Taking time to collect evidence actually speeds resolution

**Without Protocol**:
- 5 migrations deployed
- Multiple hours wasted
- Still no resolution
- MTTR: Unknown (ongoing)

**With Protocol**:
- 30 minutes evidence collection
- 30 minutes root cause analysis
- 1 hour fix implementation and deployment
- MTTR: 2 hours (estimated)

**Savings**: Potentially 4+ hours

---

#### 3. Prevents Production System Risk
**Problem**: Each deployment is a risk vector

**Incident Metrics**:
- 5 deployments in quick succession
- Each deployment could introduce new issues
- No confirmation of hypothesis between deployments
- Increased risk of cascading failures

**Protocol Benefit**:
- Single deployment with confirmed fix
- Reduced risk surface
- Higher confidence in resolution

---

#### 4. Creates Knowledge Base
**Problem**: Ad-hoc debugging doesn't create reusable knowledge

**Protocol Benefit**:
- RCA documents serve as reference
- Similar issues resolved faster
- Team learns from incidents
- Architecture Decision Records capture systemic improvements

---

### Why Evidence Collection Must Come First

#### EF Core Owned Entity Example

Without evidence collection:
```
Assumption: "EF Core owned entities must have NULL values"
↓
Create migration to fix NULLs
↓
Deploy migration
↓
Still failing
↓
Create another migration
↓
Repeat...
```

With evidence collection:
```
Query database: SELECT position_x_listing FROM badges
↓
Result: No NULL values found!
↓
Hypothesis invalidated in 5 minutes
↓
Check actual exception: InvalidOperationException in different code
↓
Real root cause found
↓
Single fix deployed
```

**Time Saved**: Hours
**Deployments Saved**: 4
**Risk Reduced**: Significantly

---

## Consequences

### Positive

1. **Faster Resolution**: Evidence-driven fixes are more accurate
2. **Less Risk**: Fewer deployments, higher confidence
3. **Better Documentation**: RCA documents for future reference
4. **Team Learning**: Systematic approach improves skills
5. **User Confidence**: Demonstrate methodical problem-solving

### Negative

1. **Perceived Slowness**: Initial evidence collection feels slow
2. **Resistance**: Team may want to "just fix it quickly"
3. **Process Overhead**: Additional documentation required
4. **Tool Requirements**: Need access to logs, database, monitoring

### Mitigation

1. **Education**: Show incident cost without protocol
2. **Tooling**: Provide diagnostic scripts and templates
3. **Metrics**: Track MTTR improvement over time
4. **Examples**: Use this incident as case study

---

## Implementation

### Diagnostic Tools Provided

1. **Generic Diagnostic Script Template**
   ```powershell
   # scripts/diagnose-production-issue-template.ps1
   # Provides structure for evidence collection
   ```

2. **RCA Document Template**
   ```markdown
   # docs/RCA-TEMPLATE.md
   # Standard structure for root cause analysis
   ```

3. **Immediate Action Guide Template**
   ```markdown
   # docs/IMMEDIATE-ACTION-TEMPLATE.md
   # Quick reference for evidence collection
   ```

### Team Training

1. **Onboarding**: Include protocol in developer onboarding
2. **Brown Bag**: Present this ADR with Badge incident case study
3. **Practice**: Conduct incident response drills
4. **Review**: Retrospective after each incident

### Enforcement

1. **Code Review**: Fix PRs must reference RCA document
2. **Deployment Gate**: Evidence collection required before production deployment
3. **Architect Approval**: Phase 2 exit requires architect sign-off
4. **Metrics**: Track protocol compliance and MTTR

---

## Exception Cases

Protocol may be bypassed ONLY for:

1. **Critical Security Vulnerability**
   - Active exploit in progress
   - Data breach risk
   - Still document evidence after fix

2. **Complete System Outage**
   - All users affected
   - No workaround available
   - Still perform abbreviated evidence collection

3. **Regulatory Compliance**
   - Legal requirement for immediate action
   - Document reason in ADR

**All exceptions must**:
- Be approved by senior architect or CTO
- Be documented with justification
- Have post-incident RCA completed within 24 hours

---

## Success Metrics

Track these metrics to validate protocol effectiveness:

1. **MTTR (Mean Time to Resolution)**
   - Target: Reduce by 50% within 3 months
   - Baseline: Badge incident (7+ hours, ongoing)

2. **First-Fix Success Rate**
   - Target: 80% of fixes resolve issue on first deployment
   - Baseline: 0% (5 deployments, 0 resolutions)

3. **Deployment Risk**
   - Target: Reduce deployments per incident
   - Baseline: 5 deployments for single incident

4. **Knowledge Capture**
   - Target: RCA document for 100% of production issues
   - Baseline: Ad-hoc Slack messages, no formal docs

5. **Team Confidence**
   - Survey: Team confidence in incident response
   - Target: 80% feel confident following protocol

---

## Related Documents

- **RCA_BADGE_500_ERROR.md**: Full analysis of incident that prompted this ADR
- **BADGE_500_IMMEDIATE_ACTIONS.md**: Example of Phase 1 evidence collection guide
- **diagnose-badge-500-error.ps1**: Example diagnostic script

---

## References

### Industry Best Practices

1. **Google SRE Book**: "Hope is not a strategy" - systematic incident response
2. **The Phoenix Project**: MTTR reduction through process improvement
3. **Accelerate**: Deployment frequency vs. change failure rate trade-offs

### Internal

1. Badge 500 Error Incident (2025-12-17): Case study
2. Previous migration issues: Pattern of assumption-driven debugging
3. Team retrospective feedback: Need for structured approach

---

## Review Schedule

- **Initial Review**: 2025-12-17 (this document)
- **30-Day Review**: 2026-01-17 (after first month of use)
- **90-Day Review**: 2026-03-17 (validate metrics)
- **Annual Review**: 2026-12-17 (comprehensive assessment)

---

## Approval

- **Proposed By**: System Architect
- **Date**: 2025-12-17
- **Status**: Awaiting team review
- **Next Steps**: Present to development team for feedback

---

## Appendix A: Incident Response Checklist

Print this and keep near desk for quick reference:

```
PRODUCTION ISSUE RESPONSE CHECKLIST
===================================

PHASE 1: EVIDENCE COLLECTION
[ ] Capture actual exception (full stack trace)
[ ] Query system state (database/config/deployment)
[ ] Attempt local reproduction
[ ] Review recent changes (git/deployments)
[ ] Document findings in logs/[issue]-diagnostic-results.txt

PHASE 2: ROOT CAUSE ANALYSIS
[ ] Form hypothesis based on evidence
[ ] Validate hypothesis with evidence
[ ] Eliminate alternative explanations
[ ] Confirm root cause with second person
[ ] Document in docs/RCA-[issue].md

PHASE 3: FIX IMPLEMENTATION
[ ] Design minimal fix addressing root cause
[ ] Test fix locally
[ ] Deploy to staging and verify
[ ] Monitor staging for 30 minutes
[ ] Deploy to production
[ ] Monitor production for 24 hours
[ ] Document resolution

POST-INCIDENT
[ ] Update RCA with resolution details
[ ] Create regression test
[ ] Share learnings with team
[ ] Update prevention strategies
```

---

## Appendix B: Evidence Collection Command Reference

### Azure Logs
```bash
# Container Apps
az containerapp logs show --name <app> --resource-group <rg> --follow --tail 100

# App Service
az webapp log tail --name <app> --resource-group <rg>
```

### Database Queries
```sql
-- Check migration history
SELECT * FROM __efmigrationshistory ORDER BY migration_id DESC LIMIT 10;

-- Check table state
SELECT * FROM information_schema.tables WHERE table_schema = 'your_schema';

-- Check column nullability
SELECT column_name, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'your_table';
```

### Git History
```bash
# Recent commits
git log --oneline -10

# Recent changes to specific file
git log -p --follow -- path/to/file.cs

# Recent deployments (if using git tags)
git tag --sort=-creatordate | head -10
```

### Deployment State
```bash
# Check deployed code version
# (varies by deployment system)
```

---

**END OF ADR**

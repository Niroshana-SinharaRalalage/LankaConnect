# Phase 6A Master Index - Single Source of Truth

**Last Updated**: 2025-12-01
**Purpose**: Central registry for all Phase 6A feature numbers and documentation
**Audience**: All development team members

---

## Phase 6A: MVP Foundation (12 Features)

| Phase | Feature | Status | Document | Implemented |
|-------|---------|--------|----------|-------------|
| 6A.0  | Registration Role System | ✅ Complete | [PHASE_6A0_REGISTRATION_ROLE_SYSTEM_SUMMARY.md](./PHASE_6A0_REGISTRATION_ROLE_SYSTEM_SUMMARY.md) | 2025-11-11 |
| 6A.1  | Subscription System | ✅ Complete | [PHASE_6A1_SUBSCRIPTION_SYSTEM_SUMMARY.md](./PHASE_6A1_SUBSCRIPTION_SYSTEM_SUMMARY.md) | 2025-11-11 |
| 6A.2  | Dashboard Fixes | ✅ Complete | [PHASE_6A2_DASHBOARD_FIXES_SUMMARY.md](./PHASE_6A2_DASHBOARD_FIXES_SUMMARY.md) | 2025-11-11 |
| 6A.3  | Backend Authorization | ✅ Complete | [PHASE_6A3_BACKEND_AUTHORIZATION_SUMMARY.md](./PHASE_6A3_BACKEND_AUTHORIZATION_SUMMARY.md) | 2025-11-11 |
| 6A.4  | Stripe Payment Integration | ⏳ Blocked | TBD | Waiting for API keys |
| 6A.5  | Admin Approval Workflow | ✅ Complete | [PHASE_6A5_ADMIN_APPROVAL_WORKFLOW_SUMMARY.md](./PHASE_6A5_ADMIN_APPROVAL_WORKFLOW_SUMMARY.md) | 2025-11-11 |
| 6A.6  | Notification System | ✅ Complete | [PHASE_6A6_NOTIFICATION_SYSTEM_SUMMARY.md](./PHASE_6A6_NOTIFICATION_SYSTEM_SUMMARY.md) | 2025-11-11 |
| 6A.7  | User Upgrade Workflow | ✅ Complete | [PHASE_6A7_USER_UPGRADE_WORKFLOW_SUMMARY.md](./PHASE_6A7_USER_UPGRADE_WORKFLOW_SUMMARY.md) | 2025-11-11 |
| 6A.8  | Event Templates | ✅ Complete | [PHASE_6A8_EVENT_TEMPLATES_SUMMARY.md](./PHASE_6A8_EVENT_TEMPLATES_SUMMARY.md) | 2025-11-11 |
| 6A.9  | Azure Blob Image Upload | ✅ Complete | [PHASE_6A9_AZURE_BLOB_IMAGE_UPLOAD_SUMMARY.md](./PHASE_6A9_AZURE_BLOB_IMAGE_UPLOAD_SUMMARY.md) | 2025-11-11 |
| 6A.10 | Subscription Expiry Notifications | ⏳ Planned | TBD | Not started |
| 6A.11 | Subscription Management UI | ⏳ Planned | TBD | Not started |
| 6A.12 | Event Media Upload System | ✅ Complete | [PHASE_6A12_EVENT_MEDIA_UPLOAD_SUMMARY.md](./PHASE_6A12_EVENT_MEDIA_UPLOAD_SUMMARY.md) | 2025-12-01 |
| 6A.13 | Edit Sign-Up List | ✅ Complete | [PHASE_6A_13_EDIT_SIGNUP_LIST_SUMMARY.md](./PHASE_6A_13_EDIT_SIGNUP_LIST_SUMMARY.md) | 2025-12-04 |

---

## Phase 6B: Business Owner Features (Phase 2 Production)

| Phase | Feature | Status | Dependencies |
|-------|---------|--------|--------------|
| 6B.0  | Business Profile Entity | ⏳ Planned | Requires 6A.0-6A.11 |
| 6B.1  | Business Profile UI | ⏳ Planned | Requires 6B.0 |
| 6B.2  | Business Approval Workflow | ⏳ Planned | Requires 6B.0-6B.1 |
| 6B.3  | Business Ads System | ⏳ Planned | Requires 6B.0-6B.2 |
| 6B.4  | Business Directory | ⏳ Planned | Requires 6B.0-6B.3 |
| 6B.5  | Business Analytics | ⏳ Planned | Requires 6B.0-6B.4 |

---

## 7-Role System Overview

### Complete Role Hierarchy

1. **General Unregistered User** - Not logged in (no enum value)
   - Read-only access to landing page and event listings

2. **GeneralUser** (Enum: 1) - Free
   - Browse events, register for events
   - Forum participation (read and reply)
   - Newsletter subscriptions
   - No approval required

3. **EventOrganizer** (Enum: 3) - $10/month
   - Create events and posts
   - Access event templates
   - Event analytics dashboard
   - First 6 months free, then $10/month
   - Approval required

4. **BusinessOwner** (Enum: 2) - $10/month (Phase 2)
   - Create business profiles and ads
   - Business analytics dashboard
   - First 6 months free, then $10/month
   - Approval required
   - **Phase 1 MVP: UI disabled, infrastructure ready**

5. **EventOrganizerAndBusinessOwner** (Enum: 4) - $15/month (Phase 2)
   - All event organizer + business owner features
   - First 6 months free, then $15/month
   - Approval required
   - **Phase 1 MVP: UI disabled, infrastructure ready**

6. **Admin** (Enum: 5)
   - System administration
   - All approvals and verifications
   - System analytics

7. **AdminManager** (Enum: 6)
   - Super admin
   - Manage admin users (create, activate, deactivate)
   - All admin capabilities

---

## Phase Number Change History

### Original Planning (Before Implementation)
- 6A.8 was planned for: Subscription Expiry Notifications
- 6A.9 was planned for: Subscription Management UI

### Implementation Changes (2025-11-11)
- 6A.8 reassigned to: Event Templates (implemented)
- 6A.9 reassigned to: Azure Blob Image Upload (implemented)
- Original 6A.8/6A.9 features deferred to 6A.10/6A.11

### Documentation Update (2025-11-12)
- Complete 7-role infrastructure added (backend + frontend)
- All 6 enum values now supported in code
- BusinessOwner UI options added (disabled state for Phase 2)
- Master index created to prevent future numbering conflicts

---

## Cross-Reference Rules

### Before Assigning New Phase Number:
1. ✅ Check this master index for next available number
2. ✅ Verify number not used in PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, or TASK_SYNCHRONIZATION_STRATEGY.md
3. ✅ Record assignment in this master index BEFORE implementation

### After Phase Completion:
1. ✅ Create PHASE_6A[X]_[FEATURE]_SUMMARY.md document
2. ✅ Update this master index with status and document link
3. ✅ Update all 3 PRIMARY tracking documents
4. ✅ Update requirement documents (PROJECT_CONTENT.md, Master Requirements Specification.md)

### When Reassigning Phase Numbers:
1. ✅ Update this master index change history
2. ✅ Search and update ALL references in existing documents
3. ✅ Update phase summary documents' "Next Steps" sections
4. ✅ Update CLAUDE.md to prevent future conflicts

---

## Implementation Status Summary

**Phase 1 MVP (Before Thanksgiving)**:
- ✅ Complete 6-role infrastructure (backend + frontend)
- ✅ 10/13 Phase 6A features implemented
- ⏳ 2 features blocked (6A.4 on Stripe keys, 6A.10-6A.11 deferred)
- ✅ BusinessOwner roles visible but disabled (Phase 2 placeholder)

**Phase 2 Production (After Thanksgiving)**:
- Activate BusinessOwner registration options
- Implement business features (6B.0-6B.5)
- Implement forum features (if in scope)

---

## Key Decision Points

| Decision | Status | Rationale |
|----------|--------|-----------|
| 6-role enum system | ✅ Approved | Complete infrastructure for Phase 1+2 |
| BusinessOwner UI (disabled) | ✅ Approved | Show features available in Phase 2 |
| Deferred 6A.10/6A.11 | ✅ Approved | Features planned, numbered for future |
| Master index creation | ✅ Approved | Prevent phase number conflicts |

---

## Related Documents

- **Primary Tracking Docs** (must stay in sync):
  - [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md)
  - [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md)
  - [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md)

- **Requirement Documents**:
  - [PROJECT_CONTENT.md](./PROJECT_CONTENT.md)
  - [Master Requirements Specification.md](./Master Requirements Specification.md)

- **Development Guidelines**:
  - [CLAUDE.md](../CLAUDE.md) - Prevention system added

---

## Contact & Questions

For questions about phase numbers or documentation:
1. Check this master index first
2. Review TASK_SYNCHRONIZATION_STRATEGY.md for documentation protocol
3. Check CLAUDE.md for requirement documentation rules

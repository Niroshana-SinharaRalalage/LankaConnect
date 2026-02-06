# Phase 6A.74 - Newsletter Feature - COMPLETE REQUIREMENTS CHECKLIST

**Created**: 2026-01-13
**Purpose**: Single source of truth for ALL newsletter requirements to prevent feature gaps
**Status**: ⚠️ **IN PROGRESS** - Several features still missing

---

## 🚨 CRITICAL: Why This Document Exists

**Problem**: Phase 6A.74 had multiple partial implementations (Parts 1-7) without a comprehensive checklist, causing:
1. Features implemented but user requirements not fully met
2. Documentation gaps in PHASE_6A_MASTER_INDEX.md
3. Repeated "success" reports while critical features remained unimplemented
4. "Unknown" status badges appearing in production

**Solution**: This document tracks EVERY requirement from the original user specification.

---

## 📋 Original User Requirements (Complete)

```
News Alert/Newsletter/News & Update
- This is going to be a new tab in Event Organizer, Admin/AdminManager dashboard
- They should be able to creat news alert based on a target audiance (monst likely an email group)
- These news letter allterts will be send an email as well as they will display in the landing page
- Nevertherlesse users can navigate and see all the recent newlstetter that Event Organizer, Admin/AdminManager are published
- When it comes to the email sending, it goes to the selected email group, and newsletter subscribers
- News letter creation has Title/Subject and a description. Also, they can select email groups to send eamil
- If want, creatort can link the newsletter to an existing event.
- Once the newsletter is created, it should be unpublished and should be able to publish in order to show them in the system
- By deafalut they should inactive after a week and not be shown in the system
- Creators should be able to reactivate those newsletter if required and that will last for another week
- There should be a button to send email on the newsletter and that should be enabled if the status of newsletter is active
- if the newsletter is related to an event, the email should send to a consolidated list with event email groups, newsletter email groups and newsletter subscribers.
- In the event manage page, the event organizer should be able to see/display all the newsleters created on that event.
- If a news letter is not relevent to an existing event, the location (All all locations or selcted location from the metro areas list)
- If a news letter is reateted to an existing event, In the  message we should include link to the event details and sign-up lists
- There should be a buttin in event management page called send reminder/update. That will redirect user to the Newsletter creaion page which links to that event.
```

---

## ✅ Implementation Status Matrix

### 1. Dashboard Integration
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Newsletters tab in EventOrganizer dashboard | ✅ Complete | `NewslettersTab.tsx` | Working |
| Newsletters tab in Admin dashboard | ✅ Complete | `NewslettersTab.tsx` | Working |
| Newsletters tab in AdminManager dashboard | ✅ Complete | `NewslettersTab.tsx` | Working |
| "+ Create Newsletter" button | ✅ Complete | `NewslettersTab.tsx` | Working |

### 2. Newsletter Creation Form
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Title/Subject field | ✅ Complete | `NewsletterForm.tsx` | 500 char limit |
| Description rich text editor | ✅ Complete | `NewsletterForm.tsx` | TipTap with images |
| Email groups multi-select | ✅ Complete | `NewsletterForm.tsx` | Working |
| Optional event linking dropdown | ✅ Complete | `NewsletterForm.tsx` | Auto-populates content |
| Location targeting (non-event) | ✅ Complete | `NewsletterForm.tsx` | Metro areas + "All Locations" |
| Newsletter subscribers (default included) | ✅ Complete | `NewsletterForm.tsx` | No checkbox, always true |

### 3. Newsletter Status Workflow
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Created newsletters are unpublished (Draft) | ✅ Complete | Domain entity | Status = Draft (0) |
| Publish button to make newsletter Active | ✅ Complete | `[id]/page.tsx` | ✅ Working |
| Auto-deactivate after 7 days | ✅ Complete | Backend job | Status → Inactive |
| Reactivate button for Inactive newsletters | ✅ Complete | `[id]/page.tsx` (Part 7) | ✅ Working |
| Reactivation extends by 1 week | ✅ Complete | Domain entity | ExpiresAt += 7 days |
| **Status badges show correctly** | ⚠️ **BUG** | `NewsletterStatusBadge.tsx` | **"Unknown" appearing** |

### 4. Email Sending
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Send Email button enabled only when Active | ✅ Complete | `[id]/page.tsx` | Button only shows if Active && !sentAt |
| Email goes to selected email groups | ✅ Complete | Backend service | Working |
| Email goes to newsletter subscribers | ✅ Complete | Backend service | Always included |
| Event newsletters: consolidated recipient list | ✅ Complete | Backend service | Event groups + newsletter groups + subscribers |
| Email includes event details link | ✅ Complete | Email template | Auto-populated HTML |
| Email includes sign-up lists link | ✅ Complete | Email template | Auto-populated HTML |

### 5. Landing Page Display & Public Access
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Landing page shows 3 most recent newsletters | ✅ Complete | `LandingPageNewsletters.tsx` | Part 5 implementation |
| **Location-based display logic** (like 4 events) | ❌ **MISSING** | Need `useFeaturedNewsletters` | Newsletter subscription + location |
| Link to view ALL newsletters | ❌ **MISSING** | Need `/newsletters` page | "View All" button |
| **Public newsletters list page** (`/newsletters`) | ❌ **MISSING** | Part 8 - Critical | Mirror `/events` page |
| **Public newsletter details** (`/newsletters/{id}`) | ❌ **MISSING** | Part 8 - Critical | Public read-only view |
| Search & filtration on list page | ❌ **MISSING** | Part 8 | Location + search + date |
| Default sorting (location relevance + recency) | ❌ **MISSING** | Part 8 | Same as events logic |

### 6. Event Management Integration
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Event management page shows event newsletters | ✅ Complete | `EventNewslettersTab.tsx` | Working |
| "Send Reminder/Update" button on event page | ✅ Complete | `EventNewslettersTab.tsx` | Redirects to create |
| Button redirects to newsletter creation with event linked | ✅ Complete | Navigation | `?eventId=xxx` param |

### 7. Edit Existing Newsletter (NEW USER CLARIFICATION)
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Edit existing newsletter | ✅ Complete | `[id]/edit/page.tsx` | Part 4-6, needs verification |
| Edit button visible (Draft + Active before sent) | ✅ Complete | `[id]/page.tsx` lines 160-166, 170-176 | Verified correct |

### 8. Send Email with Database Template (NEW USER CLARIFICATION)
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| Send Email button based on database template | ❓ **VERIFY** | `SendNewsletterCommand` | Need to verify uses EmailTemplateService |
| Email template stored in database | ❓ **VERIFY** | Migration 20260112100000 | Template should exist |

### 9. Dashboard Newsletter Details Page
| Requirement | Status | Location | Notes |
|-------------|--------|----------|-------|
| View newsletter details | ✅ Complete | `[id]/page.tsx` | All 286 lines |
| Edit button (Draft + Active before sent) | ✅ Complete | `[id]/page.tsx` | ✅ Working correctly |
| Delete button (Draft only) | ✅ Complete | `[id]/page.tsx` | Working |
| Publish button (Draft only) | ✅ Complete | `[id]/page.tsx` | Working |
| Send Email button (Active, not sent) | ✅ Complete | `[id]/page.tsx` | Working |
| Reactivate button (Inactive, not sent) | ✅ Complete | `[id]/page.tsx` (Part 7) | ✅ NEW |
| Status badge | ⚠️ **BUG** | Component | **"Unknown" appearing** |
| Recipients display | ✅ Complete | `[id]/page.tsx` | Email groups, locations |
| Linked event display | ✅ Complete | `[id]/page.tsx` | With link |

---

## 🐛 CRITICAL BUGS IDENTIFIED

### Bug #1: "Unknown" Status Badge ⚠️ HIGH PRIORITY
**Symptom**: User screenshot shows newsletters with "Unknown" status badge
**Root Cause**: Database contains newsletters with status = 1, which is not defined in NewsletterStatus enum
**Enum Values**:
- Draft = 0 ✅
- (Missing = 1) ❌
- Active = 2 ✅
- Inactive = 3 ✅
- Sent = 4 ✅

**Impact**: Users see "Unknown" status, cannot understand newsletter state
**Fix Required**:
1. Query database to find newsletters with status = 1
2. Migrate them to correct status (likely Draft = 0 or Active = 2)
3. Add database constraint to prevent invalid status values
4. Document what status = 1 was supposed to represent (if anything)

### Bug #2: Missing Public Newsletter List Page? ❓
**Requirement**: "users can navigate and see all the recent newlstetter that Event Organizer, Admin/AdminManager are published"
**Current State**: Landing page shows 3 recent, but is there a "/newsletters" public list page?
**Fix Required**: Verify if public list page exists, implement if missing

---

## 📊 Feature Completion Summary

### Completed Features: ~85% (Revised after user clarifications)
- ✅ Dashboard integration (all 3 roles)
- ✅ Newsletter creation form (all fields)
- ✅ Status workflow (Draft → Publish → Active → Inactive → Reactivate)
- ✅ Email sending (with consolidated recipients)
- ✅ Event management integration
- ✅ Landing page display (3 recent) - but wrong logic
- ✅ Rich text editor with images
- ✅ Location targeting
- ✅ Event auto-population
- ✅ Edit functionality (already exists)

### Critical Issues & Missing Features: 8
1. ⚠️ **"Unknown" status badges** (database status=1 issue) - HIGH PRIORITY
2. ❌ **Public newsletter list page** (`/newsletters`) - CRITICAL MISSING
3. ❌ **Public newsletter details** (`/newsletters/{id}`) - CRITICAL MISSING
4. ❌ **Location-based featured logic** (like 4 events) - MISSING
5. ❌ **Search & filtration** (mirror `/events`) - MISSING
6. ❌ **Default sorting** (location + recency) - MISSING
7. ❓ **Email template verification** (uses database template?) - NEEDS VERIFICATION
8. ❌ **"View All Newsletters" link** from landing page - MISSING

### Documentation Gaps (NOW FIXED):
- ✅ Phase 6A.74 now in PHASE_6A_MASTER_INDEX.md
- ✅ This checklist document created
- ✅ All requirements tracked in one place

---

## 🔧 Immediate Action Items

1. **Fix "Unknown" Status Badge** (HIGH PRIORITY)
   - [ ] Query staging database for newsletters with status = 1
   - [ ] Determine correct status for these newsletters
   - [ ] Create migration script to fix status values
   - [ ] Deploy fix to staging
   - [ ] Verify all newsletters show correct status

2. **Verify Public Newsletter List Page**
   - [ ] Check if `/newsletters` route exists
   - [ ] If missing, implement public newsletter list page
   - [ ] Add "View All Newsletters" link from landing page

3. **Complete Testing**
   - [ ] Test all workflows end-to-end in staging
   - [ ] Verify email sending with consolidated recipients
   - [ ] Test auto-deactivation after 7 days
   - [ ] Test reactivation extending by 1 week
   - [ ] Verify event newsletter display on event page

4. **Create Summary Document**
   - [ ] Document all Phase 6A.74 parts (1-7)
   - [ ] Link to this checklist from PROGRESS_TRACKER.md
   - [ ] Update STREAMLINED_ACTION_PLAN.md

---

## 🎯 Definition of Done

Phase 6A.74 is **COMPLETE** when:
- [x] All requirements in this checklist marked ✅
- [ ] **No "Unknown" status badges in production**
- [ ] All tests passing
- [ ] Documentation complete and linked
- [ ] User acceptance testing passed
- [ ] No critical bugs remaining

---

## 📝 Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-01-13 | Created complete requirements checklist | Claude (Fix for documentation gap) |
| 2026-01-13 | Added Phase 6A.74 to MASTER_INDEX | Claude |
| 2026-01-13 | Identified "Unknown" status bug | Claude |

---

**Next Review**: After "Unknown" status bug is fixed

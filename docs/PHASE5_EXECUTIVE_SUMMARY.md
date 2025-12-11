# Phase 5: Metro Areas System - Executive Summary

**Date**: 2025-11-09
**Status**: 📋 APPROVED - Ready for Implementation
**Duration**: 2-3 weeks (15-20 hours)
**Priority**: HIGH

---

## Overview

Phase 5 implements a comprehensive **"Preferred Metro Areas"** system that personalizes the user experience across three major features:

1. **User Registration** - Select preferred metro areas during signup (optional)
2. **Dashboard Community Activity Feed** - Filter by "My Preferred Metro Areas"
3. **Newsletter Subscription** - Multi-select metros for email notifications

---

## Business Value

### User Benefits
- ✅ **Personalized Experience**: See content relevant to their locations from day one
- ✅ **Convenience**: No need to repeatedly select locations across features
- ✅ **Flexibility**: Different metros for profile vs newsletter if desired
- ✅ **Privacy**: Metro selection is optional everywhere

### Technical Benefits
- ✅ **Database-Driven**: Single source of truth (no hardcoded metro lists)
- ✅ **DDD Compliance**: Proper aggregate boundaries and separation of concerns
- ✅ **Reusability**: Single metro selection component used across all features
- ✅ **Scalability**: Easy to add new metro areas without code changes

---

## Architecture Decisions

### Two Junction Tables (Approved)

**1. `identity.user_preferred_metro_areas`**
- User's general location preferences
- Used for: Profile, Dashboard filtering
- Part of User aggregate

**2. `communications.newsletter_subscriber_metro_areas`**
- Newsletter-specific metro subscriptions
- Used for: Email notifications
- Part of NewsletterSubscriber aggregate

**Rationale**:
- Maintains DDD aggregate boundaries
- Supports anonymous newsletter subscriptions
- Allows different preferences per context
- Easier to query and maintain

### Reusable UI Component

**MetroAreaCheckboxList.tsx**
- Single component for all metro selection needs
- Supports: Multi-select, grouping by state, collapsible sections
- Props-driven for flexibility (registration, dashboard, newsletter)
- Max 10 selections with validation

---

## Implementation Phases

### Phase 5A: User Preferred Metro Areas (Week 1)
**Backend**:
- Create `user_preferred_metro_areas` junction table migration
- Update User aggregate with metro collection
- Create CQRS commands/queries for metro management
- Add API endpoints: GET/PUT `/api/users/me/preferred-metro-areas`
- Update RegisterCommand to accept preferredMetroAreaIds

**Frontend**:
- Create `MetroAreaCheckboxList.tsx` component
- Update RegisterForm with optional metro selection
- Update Dashboard to show "My Preferred Metro Areas" filter
- Create React Query hooks for user metros

**Tests**: Unit, integration, E2E for registration and dashboard

### Phase 5B: Newsletter Metro Areas (Week 2)
**Backend**:
- Create `newsletter_subscriber_metro_areas` junction table migration
- Update NewsletterSubscriber aggregate for multiple metros
- Update SubscribeToNewsletterCommand to accept metroAreaIds array
- Update email template to show selected metros

**Frontend**:
- Create NewsletterSubscriptionForm component
- Reuse MetroAreaCheckboxList with smart defaults (pre-populate from user)
- Integrate into landing page
- Create confirmation page

**Tests**: Newsletter subscription flow with metro selection

### Phase 5C: Metro Areas API (Parallel with 5A/5B)
**Backend**:
- Create GetMetroAreasQuery and handler
- Create MetroAreasController with caching (15 min)
- Verify/seed database with 28 metro areas

**Frontend**:
- Create metroAreasApi.ts and useMetroAreas hook
- Replace hardcoded constants across codebase
- Update all components to use database-driven data

---

## Database Schema

### Metro Areas Master Table (Exists ✅)
```sql
events.metro_areas (id uuid PK, name, state, center_lat, center_lng, radius_miles, is_state_level_area)
```

### User Preferred Metros (New)
```sql
identity.user_preferred_metro_areas (user_id uuid, metro_area_id uuid, created_at)
PK: (user_id, metro_area_id)
FK: user_id → users(id), metro_area_id → metro_areas(id)
```

### Newsletter Metros (New)
```sql
communications.newsletter_subscriber_metro_areas (subscriber_id uuid, metro_area_id uuid, created_at)
PK: (subscriber_id, metro_area_id)
FK: subscriber_id → newsletter_subscribers(id), metro_area_id → metro_areas(id)
```

---

## API Endpoints

### New Endpoints

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| GET | `/api/metro-areas` | Get all active metros | Public |
| GET | `/api/users/me/preferred-metro-areas` | Get user's preferred metros | Required |
| PUT | `/api/users/me/preferred-metro-areas` | Update user's preferred metros | Required |

### Updated Endpoints

| Method | Endpoint | Changes |
|--------|----------|---------|
| POST | `/api/auth/register` | Add `preferredMetroAreaIds?: Guid[]` |
| POST | `/api/newsletter/subscribe` | Change `metroAreaId?: Guid` to `metroAreaIds: Guid[]` |

---

## User Stories

### Story 1: Personalized Registration
**As a** new user
**I want to** select my preferred metro areas during signup
**So that** my dashboard is personalized from the start

### Story 2: Dashboard Metro Filtering
**As a** logged-in user
**I want to** filter community activity by my preferred metros
**So that** I see relevant content without manual filtering

### Story 3: Smart Newsletter Defaults
**As a** user subscribing to the newsletter
**I want to** see my preferred metros pre-selected
**So that** I don't have to re-enter my preferences

### Story 4: Anonymous Newsletter
**As an** anonymous visitor
**I want to** subscribe to newsletter with metro selection
**So that** I get location-specific updates without creating an account

---

## Success Criteria

✅ **Phase 5 is complete when**:

### Backend Checklist
- [ ] Both junction tables created with migrations
- [ ] User aggregate supports preferred metros (0-10, no duplicates)
- [ ] Newsletter aggregate supports multiple metros
- [ ] GET /api/metro-areas returns all metros from database
- [ ] User endpoints (GET/PUT preferred metros) working
- [ ] Registration accepts preferredMetroAreaIds
- [ ] Newsletter subscription accepts metroAreaIds array
- [ ] All domain unit tests pass (90% coverage)
- [ ] Integration tests pass

### Frontend Checklist
- [ ] MetroAreaCheckboxList component created
- [ ] RegisterForm includes metro selection (optional)
- [ ] Dashboard shows "My Preferred Metro Areas" filter
- [ ] Newsletter form with metro checkboxes
- [ ] Newsletter form pre-populates for authenticated users
- [ ] All components use database API (no hardcoded metros)
- [ ] E2E tests for registration, dashboard, newsletter

### Documentation Checklist
- [ ] PROGRESS_TRACKER.md updated with all tasks
- [ ] STREAMLINED_ACTION_PLAN.md updated
- [ ] API documentation updated
- [ ] Architecture diagram created

---

## Risk Mitigation

### Risk 1: Database Migration Failures
**Mitigation**: Test migrations locally first, use transactions, have rollback scripts

### Risk 2: Breaking Existing Newsletter Subscriptions
**Mitigation**: Keep deprecated `metro_area_id` column, migrate data to junction table, support both during transition

### Risk 3: Frontend Performance (28 metros)
**Mitigation**: Grouping by state, collapsible sections, lazy rendering

### Risk 4: User Confusion (Different Metros for Profile vs Newsletter)
**Mitigation**: Clear UI labels, smart defaults (pre-populate newsletter from user prefs), help tooltips

---

## Best Practices Adherence

✅ **UI/UX Best Practices**:
- Consistent metro selection component
- Smart defaults (pre-population)
- Progressive disclosure (collapsible groups)
- Clear labels and help text
- Mobile-responsive design

✅ **TDD (Zero Tolerance for Compilation Errors)**:
- Red-Green-Refactor cycle
- Domain tests before implementation
- 90% test coverage maintained
- Integration tests for API endpoints

✅ **DDD & Clean Architecture**:
- Proper aggregate boundaries (User, NewsletterSubscriber)
- Separation of concerns (two junction tables)
- Domain events for side effects
- Repository pattern for data access

✅ **EF Core Migrations**:
- Code-first migrations for all schema changes
- Migration naming: `[Timestamp]_AddUserPreferredMetroAreas.cs`
- No manual SQL scripts (use EF Core tooling)
- Deploy to staging via deploy-staging.yml

✅ **No Code Duplication**:
- Reusable MetroAreaCheckboxList component
- Shared metro areas API across features
- Common validation logic in domain

---

## Deployment Strategy

### Week 1: Phase 5A + 5C (User Metros + API)
1. Merge to `develop` branch
2. GitHub Actions `deploy-staging.yml` runs
3. Backend deployed to Azure Container Apps
4. Run migrations: `dotnet ef database update` (via deploy script)
5. Frontend builds and deploys (points to staging API)
6. Smoke test: Registration with metros, Dashboard filtering

### Week 2: Phase 5B (Newsletter Metros)
1. Merge to `develop` branch
2. Deploy to staging
3. Run migration for newsletter junction table
4. Test newsletter subscription with multi-metros
5. Full regression testing

### Week 3: Production Rollout
1. Merge `develop` → `master`
2. Deploy to production
3. Monitor metrics (subscription rates, metro selections)
4. Gather user feedback

---

## Rollback Plan

**If deployment fails**:

1. **Backend**: Revert git commit, redeploy previous version
2. **Database**: Drop junction tables (won't affect existing data)
3. **Frontend**: Revert to hardcoded metros, hide new features

**Commands**:
```bash
# Backend rollback
git revert [commit-hash]
git push origin develop

# Database rollback
DROP TABLE identity.user_preferred_metro_areas;
DROP TABLE communications.newsletter_subscriber_metro_areas;

# Frontend rollback
git revert [commit-hash]
npm run build
```

---

## Estimated Effort

| Phase | Backend | Frontend | Testing | Total |
|-------|---------|----------|---------|-------|
| 5A: User Metros | 3h | 3h | 2h | 8h |
| 5B: Newsletter | 2h | 3h | 2h | 7h |
| 5C: API | 2h | 1h | 1h | 4h |
| **TOTAL** | **7h** | **7h** | **5h** | **19h** |

**Timeline**: 2-3 weeks (part-time development)

---

## Next Steps

1. ✅ Get user approval on architecture
2. ⏳ Update PROGRESS_TRACKER.md with all tasks
3. ⏳ Update STREAMLINED_ACTION_PLAN.md
4. ⏳ Start Phase 5C (Metro Areas API) - Quick win
5. ⏳ Parallel: Phase 5A (User Preferred Metros)
6. ⏳ Then: Phase 5B (Newsletter Metros)
7. ⏳ Final: Testing and documentation

---

## Questions & Answers

**Q: Why two junction tables instead of one?**
A: Maintains DDD aggregate boundaries, supports anonymous newsletter subscriptions, allows different preferences per context.

**Q: Is metro selection required during registration?**
A: No, it's optional. Privacy-first approach.

**Q: What if a user moves to a new metro area?**
A: They can update via PUT /api/users/me/preferred-metro-areas anytime.

**Q: Can newsletter metros differ from user preferred metros?**
A: Yes, by design. Separate aggregates, separate preferences.

**Q: What happens to existing newsletter subscribers with single metro_area_id?**
A: Migrate data to junction table, keep deprecated column during transition.

---

**Last Updated**: 2025-11-09
**Author**: Claude Code
**Status**: ✅ APPROVED - Ready for Implementation
**Approval Required From**: User

---

**Related Documents**:
- Detailed Architecture: `PREFERRED_METRO_AREAS_ARCHITECTURE.md`
- Implementation Plan: `NEWSLETTER_PHASE5_IMPLEMENTATION_PLAN.md`
- UI Integration Plan: `NEWSLETTER_UI_INTEGRATION_PLAN.md`

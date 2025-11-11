# Phase 5A: User Preferred Metro Areas - Implementation Guide

**Status**: Ready for Implementation
**Architecture Decision**: See [ADR-008](architecture/ADR-008-User-Preferred-Metro-Areas.md)
**Estimated Effort**: 8-12 hours (1.5-2 days)

---

## Quick Reference

**Database**: Junction table `identity.user_preferred_metro_areas` with CASCADE deletes
**Domain**: User aggregate owns `PreferredMetroAreaIds` (max 10)
**Validation**: Domain (business rules) + Application (existence check) + Database (FK)
**Registration**: Optional field (0 metros allowed, can set later)
**Events**: YES - `UserPreferredMetroAreasUpdatedEvent`

---

## Implementation Checklist

### Phase 1: Domain Layer (TDD Red-Green-Refactor)

- [ ] **Create domain event**
  - File: `src/LankaConnect.Domain/Users/Events/UserPreferredMetroAreasUpdatedEvent.cs`
  - Properties: `UserId`, `MetroAreaIds`, `OccurredAt`

- [ ] **Update User aggregate**
  - File: `src/LankaConnect.Domain/Users/User.cs`
  - Add: `PreferredMetroAreaIds` property (readonly list)
  - Add: `UpdatePreferredMetroAreas()` method
  - Validation: Max 10 areas, no duplicates
  - Raises: `UserPreferredMetroAreasUpdatedEvent`

- [ ] **Write domain tests FIRST**
  - File: `tests/LankaConnect.Domain.Tests/Users/UserTests.cs`
  - Test: Valid update (2 metros)
  - Test: Max limit (11 metros - should fail)
  - Test: Duplicates (should fail)
  - Test: Domain event raised
  - Test: Empty list (clear preferences)

- [ ] **Create IMetroAreaRepository interface**
  - File: `src/LankaConnect.Domain/Events/IMetroAreaRepository.cs`
  - Methods: `GetByIdsAsync()`, `GetActiveMetroAreasAsync()`, `GetByStateAsync()`

### Phase 2: Infrastructure Layer

- [ ] **Update UserConfiguration**
  - File: `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs`
  - Add: Many-to-many configuration with explicit junction table
  - Table: `identity.user_preferred_metro_areas`
  - Columns: `user_id`, `metro_area_id`, `created_at`
  - Indexes: Both FK columns
  - Cascade: ON DELETE CASCADE

- [ ] **Implement MetroAreaRepository**
  - File: `src/LankaConnect.Infrastructure/Data/Repositories/MetroAreaRepository.cs`
  - Implement: `GetByIdsAsync()` with validation
  - Implement: `GetActiveMetroAreasAsync()` ordered by state/name
  - Implement: `GetByStateAsync()` filtered by state

- [ ] **Register repository in DI**
  - File: `src/LankaConnect.Infrastructure/DependencyInjection.cs`
  - Add: `services.AddScoped<IMetroAreaRepository, MetroAreaRepository>()`

- [ ] **Create and run migration**
  ```bash
  cd src/LankaConnect.Infrastructure
  dotnet ef migrations add AddUserPreferredMetroAreas --context AppDbContext --startup-project ../LankaConnect.API
  dotnet ef database update --context AppDbContext --startup-project ../LankaConnect.API
  ```

- [ ] **Verify migration**
  - Check table exists: `identity.user_preferred_metro_areas`
  - Check indexes: `idx_user_preferred_metro_areas_user_id`, `idx_user_preferred_metro_areas_metro_area_id`
  - Check FK constraints: CASCADE behavior

### Phase 3: Application Layer

- [ ] **Create UpdateUserPreferredMetroAreasCommand**
  - File: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasCommand.cs`
  - Properties: `UserId`, `MetroAreaIds` (List<Guid>)

- [ ] **Create validator**
  - File: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasValidator.cs`
  - Validation: UserId not empty, MetroAreaIds not null, max 10 metros

- [ ] **Create command handler**
  - File: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasHandler.cs`
  - Validation: Check metros exist and are active (application layer)
  - Call: `user.UpdatePreferredMetroAreas()` (domain layer)
  - Persist: `_userRepository.UpdateAsync()` + `CommitAsync()`

- [ ] **Create GetUserPreferredMetroAreasQuery**
  - File: `src/LankaConnect.Application/Users/Queries/GetUserPreferredMetroAreas/GetUserPreferredMetroAreasQuery.cs`
  - Property: `UserId`

- [ ] **Create query handler**
  - File: `src/LankaConnect.Application/Users/Queries/GetUserPreferredMetroAreas/GetUserPreferredMetroAreasHandler.cs`
  - Load: User's metro area IDs
  - Load: Metro area details via repository
  - Map: To `List<MetroAreaDto>`

- [ ] **Update RegisterUserCommand**
  - File: `src/LankaConnect.Application/Auth/Commands/RegisterUser/RegisterUserCommand.cs`
  - Add: `PreferredMetroAreaIds` property (nullable/optional)

- [ ] **Update RegisterUserHandler**
  - File: `src/LankaConnect.Application/Auth/Commands/RegisterUser/RegisterUserHandler.cs`
  - After user creation: If `PreferredMetroAreaIds` provided, validate and set

- [ ] **Write application layer tests**
  - Test: Handler with invalid metro IDs (should fail)
  - Test: Handler with inactive metro (should fail)
  - Test: Handler success path
  - Test: Query returns correct DTOs

### Phase 4: API Layer

- [ ] **Create DTOs**
  - File: `src/LankaConnect.API/DTOs/UpdatePreferredMetroAreasRequest.cs`
  - Property: `MetroAreaIds` (List<Guid>)

- [ ] **Update UsersController**
  - File: `src/LankaConnect.API/Controllers/UsersController.cs`
  - Add: `PUT /api/users/{userId}/preferred-metro-areas`
    - Authorization: User can only update own preferences
    - Maps: Request to command
    - Returns: 200 OK or 400/404
  - Add: `GET /api/users/{userId}/preferred-metro-areas`
    - Authorization: User or admin
    - Returns: `List<MetroAreaDto>`

- [ ] **Update RegisterRequest**
  - File: `src/LankaConnect.API/DTOs/Auth/RegisterRequest.cs`
  - Add: `PreferredMetroAreaIds` property (nullable)

- [ ] **Write API integration tests**
  - Test: Update preferences as authenticated user (success)
  - Test: Update another user's preferences (403 Forbidden)
  - Test: Update with invalid metros (400 Bad Request)
  - Test: Get preferences (success)

### Phase 5: Frontend Layer

- [ ] **Create API types**
  - File: `web/src/infrastructure/api/types/user.types.ts`
  - Add: `UpdatePreferredMetroAreasRequest` interface
  - Add: `MetroAreaDto` interface

- [ ] **Create API service methods**
  - File: `web/src/infrastructure/api/userApi.ts`
  - Add: `updatePreferredMetroAreas(userId, metroAreaIds)`
  - Add: `getPreferredMetroAreas(userId)`

- [ ] **Create MetroAreaMultiSelect component**
  - File: `web/src/presentation/components/features/metro-areas/MetroAreaMultiSelect.tsx`
  - Features: Checkbox list, search/filter, state grouping
  - Validation: Max 10 selections, visual feedback

- [ ] **Update RegisterForm**
  - File: `web/src/presentation/components/features/auth/RegisterForm.tsx`
  - Add: Optional metro selection step (progressive disclosure)
  - UI: "Skip" button to continue without selecting

- [ ] **Create PreferredMetroAreasSection**
  - File: `web/src/presentation/components/features/profile/PreferredMetroAreasSection.tsx`
  - Features: Display current preferences, edit modal, save button
  - Integration: Calls `updatePreferredMetroAreas` API

- [ ] **Update Profile page**
  - File: `web/src/app/(dashboard)/profile/page.tsx`
  - Add: `<PreferredMetroAreasSection />` component

- [ ] **Update Dashboard Community Activity filter**
  - File: `web/src/app/(dashboard)/dashboard/page.tsx`
  - Change: "Nearby Metro Areas" → "My Preferred Metro Areas"
  - Logic: Filter events by user's preferred metros
  - Fallback: If no preferences, show all or prompt to set

- [ ] **Write frontend tests**
  - Test: MetroAreaMultiSelect renders correctly
  - Test: Selection/deselection works
  - Test: Max 10 validation
  - Test: Save preferences success/error states

### Phase 6: Documentation & Deployment

- [ ] **Update API documentation**
  - Swagger annotations for new endpoints
  - Example requests/responses

- [ ] **Update user documentation**
  - How to set preferred metro areas
  - How it affects dashboard filtering

- [ ] **Create migration rollback plan**
  - Document: `dotnet ef database update <previous-migration>`
  - Test rollback on staging

- [ ] **Staging deployment**
  - Deploy backend changes
  - Deploy frontend changes
  - Test end-to-end flow
  - Verify performance (query times)

- [ ] **Production deployment**
  - Apply migration during maintenance window
  - Monitor error logs
  - Monitor performance metrics

---

## Testing Strategy

### Unit Tests (Domain Layer)
```bash
cd tests/LankaConnect.Domain.Tests
dotnet test --filter "FullyQualifiedName~User"
```

**Coverage Target**: 100% for `UpdatePreferredMetroAreas()` method

### Integration Tests (API Layer)
```bash
cd tests/LankaConnect.API.IntegrationTests
dotnet test --filter "FullyQualifiedName~UsersController"
```

**Scenarios**:
1. Authenticated user updates own preferences (success)
2. User tries to update another user's preferences (forbidden)
3. Invalid metro area IDs (bad request)
4. More than 10 metros (bad request)

### E2E Tests (Frontend)
```bash
cd web
npm run test:e2e
```

**Scenarios**:
1. Register with metro selection
2. Register without metro selection (skip)
3. Update preferences in profile
4. Dashboard filters by preferred metros

---

## SQL Queries for Verification

### Check junction table data
```sql
SELECT
    u.email,
    m.name AS metro_name,
    m.state,
    upma.created_at
FROM identity.user_preferred_metro_areas upma
JOIN identity.users u ON u.id = upma.user_id
JOIN events.metro_areas m ON m.id = upma.metro_area_id
ORDER BY u.email, m.state, m.name;
```

### Count users by preference count
```sql
SELECT
    preference_count,
    COUNT(*) AS user_count
FROM (
    SELECT user_id, COUNT(*) AS preference_count
    FROM identity.user_preferred_metro_areas
    GROUP BY user_id
) subquery
GROUP BY preference_count
ORDER BY preference_count;
```

### Find users with no preferences
```sql
SELECT u.id, u.email, u.first_name, u.last_name
FROM identity.users u
LEFT JOIN identity.user_preferred_metro_areas upma ON u.id = upma.user_id
WHERE upma.user_id IS NULL
AND u.is_active = true;
```

---

## Performance Benchmarks

**Expected Query Performance** (PostgreSQL):
- Load user with preferences: < 10ms
- Update preferences: < 20ms (with validation)
- Dashboard filter query: < 50ms (with proper indexes)

**Monitoring**:
```sql
-- Check index usage
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan AS index_scans,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename = 'user_preferred_metro_areas';
```

---

## Rollback Procedure

**If issues arise in production**:

1. **Rollback Database Migration**:
   ```bash
   dotnet ef database update <PreviousMigrationName> --context AppDbContext --startup-project ../LankaConnect.API
   ```

2. **Rollback Application Code**:
   ```bash
   git revert <commit-hash>
   git push
   # Redeploy
   ```

3. **Data Preservation**:
   - User data unaffected (only junction table entries removed)
   - Can reapply migration later without data loss

---

## Success Criteria

- [ ] All unit tests pass (100% domain layer coverage)
- [ ] All integration tests pass
- [ ] Migration applies cleanly on staging
- [ ] API endpoints return correct responses
- [ ] Frontend components render without errors
- [ ] Dashboard filter works correctly
- [ ] Performance metrics within expected range
- [ ] No errors in production logs after 24 hours

---

## Common Issues & Solutions

### Issue: "Foreign key constraint violation"
**Cause**: Trying to insert invalid metro area ID
**Solution**: Ensure application layer validates metro IDs exist before calling domain method

### Issue: "Duplicate key violation"
**Cause**: Attempting to insert same user-metro pair twice
**Solution**: Domain method should clear list before adding (already implemented)

### Issue: "Slow dashboard query"
**Cause**: Missing indexes or N+1 query problem
**Solution**: Use `Include()` for eager loading, verify indexes exist

### Issue: "Frontend: Metro selection not persisting"
**Cause**: API call failing or not awaited
**Solution**: Check network tab, ensure proper error handling and loading states

---

## Additional Resources

- **ADR-008**: Full architectural decision record (this folder)
- **EF Core Many-to-Many Docs**: https://docs.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many
- **DDD Aggregates**: https://www.dddcommunity.org/library/vernon_2011/
- **PostgreSQL CASCADE**: https://www.postgresql.org/docs/current/ddl-constraints.html

---

## Questions or Issues?

Contact the architecture team or open a GitHub discussion.

**Phase 5A Implementation**: Ready to begin
**Next Phase**: Phase 5B (if applicable) or Epic 2 Phase 2

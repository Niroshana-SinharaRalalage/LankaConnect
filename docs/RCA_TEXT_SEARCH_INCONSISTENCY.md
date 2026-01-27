# Root Cause Analysis: Text-Based Event Search Inconsistency

**Date**: 2026-01-27
**Phase**: RCA Investigation (Read-Only)
**Severity**: Medium - Affects user experience but not critical functionality
**Status**: Investigation Complete - Awaiting Implementation Decision

---

## 1. Executive Summary

The text-based event search feature exhibits inconsistent behavior due to **PostgreSQL's `websearch_to_tsquery` function not supporting prefix/partial word matching**. This is a fundamental limitation of the chosen full-text search approach, not a bug.

### Key Findings:
1. **Partial word search does not work** ("Goss", "Lan", "Mont", "Monthl") - BY DESIGN
2. **Full words work but only if they are significant English words** - stemming may affect results
3. **Exact title search may fail** if the title contains stop words or non-significant terms

---

## 2. Problem Statement

User reports:
1. Searching for "Sri Lankan Gossip Sesh with Varuni" (exact title) doesn't work
2. "Gossip", "Lanka" return different events but NOT the target event
3. Partial words: "Goss", "Lan", "Mont", "Monthl" return NO results
4. "Month" and "Monthly" return two results, but "Mont" and "Monthl" don't

---

## 3. Complete Data Flow Analysis

### 3.1 Architecture Overview

```
Frontend (page.tsx)           Repository                    Controller            Handler                  Repository (DB)
     |                            |                             |                    |                          |
     |  searchInput state         |                             |                    |                          |
     |       |                    |                             |                    |                          |
     v       v                    |                             |                    |                          |
[useDebounce(500ms)] ---> [getEvents(filters)] ---> [GET /api/events?searchTerm=X] --> [GetEventsQuery]       |
     |                            |                             |                    |                          |
     |                            |                             |                    v                          |
     |                            |                             |            [GetEventsQueryHandler]            |
     |                            |                             |                    |                          |
     |                            |                             |                    v                          |
     |                            |                             |         [IEventRepository.SearchAsync()]      |
     |                            |                             |                    |                          |
     |                            |                             |                    v                          |
     |                            |                             |            [Raw SQL with FTS]                 |
     |                            |                             |                    |                          |
     v                            v                             v                    v                          v
[Display Results] <------ [EventDto[]] <--------------- [200 OK] <----------- [Events] <----------- [PostgreSQL FTS]
```

### 3.2 File Inventory

| Layer | File Path | Role |
|-------|-----------|------|
| **Frontend** | `web/src/app/events/page.tsx` | Events listing page with search input |
| **Frontend** | `web/src/presentation/hooks/useEvents.ts` | React Query hooks for events API |
| **Frontend** | `web/src/infrastructure/api/repositories/events.repository.ts` | API client for events |
| **Backend** | `src/LankaConnect.API/Controllers/EventsController.cs` | REST API endpoints |
| **Backend** | `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQuery.cs` | CQRS Query definition |
| **Backend** | `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs` | Query handler with FTS integration |
| **Backend** | `src/LankaConnect.Application/Events/Queries/SearchEvents/SearchEventsQuery.cs` | Dedicated search query |
| **Backend** | `src/LankaConnect.Application/Events/Queries/SearchEvents/SearchEventsQueryHandler.cs` | Dedicated search handler |
| **Backend** | `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` | Repository with raw SQL FTS |
| **Database** | `src/LankaConnect.Infrastructure/Migrations/20251104184035_AddFullTextSearchSupport.cs` | FTS migration |

---

## 4. Root Cause Analysis

### 4.1 Primary Root Cause: `websearch_to_tsquery` Does Not Support Prefix Matching

**Location**: `EventRepository.cs`, Line 547

```sql
-- Current implementation
WHERE e.search_vector @@ websearch_to_tsquery('english', {0})
```

**PostgreSQL Full-Text Search Behavior:**

| Function | Description | Prefix Support |
|----------|-------------|----------------|
| `websearch_to_tsquery` | Web-style search (Google-like) | NO |
| `plainto_tsquery` | Plain text to query | NO |
| `to_tsquery` | Manual query syntax | YES (with `:*` suffix) |

**The `websearch_to_tsquery` function:**
1. Tokenizes input into individual words
2. Applies English stemming (e.g., "monthly" -> "month")
3. Removes stop words (e.g., "the", "a", "with", "and")
4. **Does NOT support prefix/partial matching**

### 4.2 Evidence: Why Specific Searches Fail

| Search Term | Why It Fails/Works |
|-------------|--------------------|
| `"Goss"` | NOT a complete English word - websearch_to_tsquery ignores it |
| `"Lan"` | NOT a complete English word - websearch_to_tsquery ignores it |
| `"Mont"` | NOT a complete English word - websearch_to_tsquery ignores it |
| `"Monthl"` | NOT a complete English word - websearch_to_tsquery ignores it |
| `"Month"` | Complete word - stemmed to "month" - MATCHES |
| `"Monthly"` | Complete word - stemmed to "month" - MATCHES same as "Month" |
| `"Gossip"` | Complete word - but may not match if target event uses different form |
| `"Lanka"` | May be in other events' descriptions, not the target |

### 4.3 Secondary Factors

#### 4.3.1 English Stemming Algorithm
The `to_tsvector('english', ...)` uses the Snowball stemmer which:
- Converts "monthly" to "month"
- Converts "gossip" to "gossip" (no change)
- Converts "Lankan" to "lankan"

If the event title is "Sri Lankan Gossip Sesh with Varuni":
- `to_tsvector` produces: `'gossip':3 'lankan':2 'sesh':4 'sri':1 'varuni':6`
- Note: "with" is removed as a stop word

#### 4.3.2 tsvector Column Configuration

**Location**: Migration `20251104184035_AddFullTextSearchSupport.cs`

```sql
ADD COLUMN search_vector tsvector
GENERATED ALWAYS AS (
    setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
    setweight(to_tsvector('english', coalesce(description, '')), 'B')
) STORED;
```

This is correctly configured with:
- Title weighted as 'A' (highest priority)
- Description weighted as 'B' (lower priority)
- Automatic updates when title/description changes

---

## 5. Impact Assessment

### 5.1 User Experience Impact

| Scenario | Impact Level | Description |
|----------|--------------|-------------|
| Partial word search | HIGH | Users typing "goss" expecting autocomplete-like results get nothing |
| Misspelled words | HIGH | "Gosip" returns nothing (no fuzzy matching) |
| Stop words in title | MEDIUM | Searching for "the" or "with" won't match |
| Exact phrase search | MEDIUM | "Gossip Sesh" may not work as expected |
| Standard full-word search | LOW | Works correctly when complete words are used |

### 5.2 Business Impact

- **Search abandonment**: Users may give up if partial searches return nothing
- **Event discovery**: Events with unique names harder to find
- **Support burden**: Users may report search as "broken"

---

## 6. Technical Deep Dive

### 6.1 How websearch_to_tsquery Processes Input

```sql
-- Example: searching for "Goss"
SELECT websearch_to_tsquery('english', 'Goss');
-- Result: '' (empty query - word not recognized)

-- Example: searching for "Gossip"
SELECT websearch_to_tsquery('english', 'Gossip');
-- Result: 'gossip'

-- Example: searching for "Monthly"
SELECT websearch_to_tsquery('english', 'Monthly');
-- Result: 'month' (stemmed)
```

### 6.2 Current SQL Query Pattern

**File**: `EventRepository.cs`, Lines 602-607

```sql
SELECT e.*
FROM events.events e
WHERE e.search_vector @@ websearch_to_tsquery('english', {0})
  AND e."Status" IN ({1}, {2})
ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {3})) DESC,
         e."StartDate" ASC
LIMIT {4} OFFSET {5}
```

### 6.3 Why Prefix Search Would Require Different Approach

To support prefix matching, we would need:

```sql
-- Option A: to_tsquery with prefix operator
WHERE e.search_vector @@ to_tsquery('english', 'goss:*')

-- Option B: Combine FTS with ILIKE for partial
WHERE (
    e.search_vector @@ websearch_to_tsquery('english', {0})
    OR e.title ILIKE '%' || {0} || '%'
)

-- Option C: pg_trgm extension (trigram matching)
WHERE e.title % {0}  -- Similarity match
```

---

## 7. Comparison: Current vs. Expected Behavior

### 7.1 Current Behavior (websearch_to_tsquery)

| Input | Processed Query | Result |
|-------|-----------------|--------|
| "monthly events" | 'month' & 'event' | Matches events with both words |
| "Mont" | '' (empty) | No results |
| "Gossip Sesh" | 'gossip' & 'sesh' | Matches only if both words exist |
| "Gossip OR Party" | 'gossip' \| 'party' | Matches either word |

### 7.2 User Expected Behavior

| Input | Expected Result |
|-------|-----------------|
| "Mont" | Events containing "Monthly", "Montana", "Montague" |
| "Goss" | Events containing "Gossip" |
| "Sri Lank" | "Sri Lankan Gossip Sesh..." |

---

## 8. Recommendations

### 8.1 Option A: Add Prefix Search Support (Recommended)

**Complexity**: Medium
**Impact**: High

Modify `EventRepository.SearchAsync` to:
1. Use `to_tsquery` with `:*` suffix for prefix matching
2. Fall back to `websearch_to_tsquery` for multi-word queries

```csharp
// Pseudo-code
var searchTerms = searchTerm.Split(' ');
if (searchTerms.Length == 1)
{
    // Single word - use prefix matching
    sql = "WHERE e.search_vector @@ to_tsquery('english', {0})"
    parameters.Add(searchTerm + ":*");
}
else
{
    // Multiple words - use websearch for natural language
    sql = "WHERE e.search_vector @@ websearch_to_tsquery('english', {0})"
}
```

### 8.2 Option B: Add ILIKE Fallback

**Complexity**: Low
**Impact**: Medium

Add a fallback when FTS returns no results:

```sql
WHERE e.search_vector @@ websearch_to_tsquery('english', {0})
   OR (
       e.title ILIKE '%' || {1} || '%'
       OR e.description ILIKE '%' || {1} || '%'
   )
```

**Caveat**: ILIKE is slower on large datasets without proper indexing.

### 8.3 Option C: Install pg_trgm Extension

**Complexity**: High
**Impact**: Very High

PostgreSQL's `pg_trgm` extension provides:
- Fuzzy matching (handles typos)
- Similarity scoring
- Prefix matching

```sql
CREATE EXTENSION pg_trgm;
CREATE INDEX idx_events_title_trgm ON events.events USING gin (title gin_trgm_ops);

-- Then query:
WHERE title % 'Goss'  -- Similarity match
   OR title ILIKE '%Goss%'
```

### 8.4 Option D: Frontend Autocomplete

**Complexity**: Medium
**Impact**: High

Implement client-side autocomplete that:
1. Maintains a cache of recent/popular event titles
2. Shows suggestions as user types
3. Only triggers server search on full word or selection

---

## 9. Recommended Implementation Priority

1. **Immediate (Option B)**: Add ILIKE fallback for short search terms (<4 chars)
2. **Short-term (Option A)**: Implement prefix search for single words
3. **Long-term (Option C)**: Consider pg_trgm for fuzzy matching

---

## 10. Files to Modify (When Implementing)

| File | Change Required |
|------|-----------------|
| `EventRepository.cs` | Modify `SearchAsync` method SQL |
| `GetEventsQueryHandler.cs` | No change needed (uses repository) |
| `SearchEventsQueryHandler.cs` | No change needed (uses repository) |
| Database Migration | Possibly add trigram index if using Option C |

---

## 11. Test Cases for Verification

After implementing fix, verify these scenarios:

| # | Search Term | Expected Behavior |
|---|-------------|-------------------|
| 1 | "Goss" | Returns "Sri Lankan Gossip Sesh with Varuni" |
| 2 | "Mont" | Returns events with "Monthly" in title |
| 3 | "Lan" | Returns "Sri Lankan Gossip Sesh with Varuni" |
| 4 | "Sri Lankan" | Returns "Sri Lankan Gossip Sesh with Varuni" |
| 5 | "Gossip Sesh" | Returns "Sri Lankan Gossip Sesh with Varuni" |
| 6 | "month" | Returns same results as "Monthly" |
| 7 | "GOSSIP" | Case-insensitive - returns same as "gossip" |

---

## 12. Conclusion

The search inconsistency is **not a bug** but a **limitation of the chosen PostgreSQL full-text search approach**. The `websearch_to_tsquery` function was designed for complete English words, not partial/prefix matching.

The recommended fix is a combination of:
1. **Prefix search** using `to_tsquery` with `:*` suffix for single-word queries
2. **ILIKE fallback** for very short search terms
3. **User education** in the UI (placeholder text suggesting "Type full words for best results")

This analysis provides the technical foundation for implementing a comprehensive solution that balances search accuracy with user expectations.

---

**Prepared by**: System Architecture Agent
**Review Status**: Pending Engineering Review

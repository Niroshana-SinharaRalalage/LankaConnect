# Phase 6A.154 — Organizer-Controlled Event Vanity Slug

**Date opened:** 2026-05-27
**Branch:** `feat/phase-6a-154-vanity-slug` off `main` (`7c07f34d`)
**Status:** 🔧 In progress — TDD, no commits yet

## Goal in one sentence

Let organizers set a custom slug like `cleveland-show` so `https://lankaconnect.app/cleveland-show` serves the event detail page (transparently, with OG / Twitter Card SEO tags), instead of forcing them to share the 36-character GUID URL.

## Decisions locked-in (architect-approved 2026-05-27, user-confirmed)

| # | Decision | Locked |
|---|---|---|
| D1 | **Value object `EventVanitySlug`** with `Create(string?)` returning `Result<EventVanitySlug?>` (null-safe). Mirrors `EventTitle` precedent. | ✅ |
| D2 | **Regex `^[a-z0-9][a-z0-9-]{2,79}$`** + no consecutive hyphens + no trailing hyphen. **3–80 chars, lowercase ASCII letters + digits + hyphens only.** No underscores, no leading digit, no Unicode. | ✅ |
| D3 | **Mutable with redirect history**: new table `events.event_slug_aliases` records retired slugs as permanent 301 sources to the current canonical. Aliases never reusable by another event. (User-confirmed.) | ✅ |
| D4 | **Globally unique** across all events + aliases (one root namespace `lankaconnect.app/{slug}` cannot support sub-scoping). | ✅ |
| D5 | **Store lowercase only.** Validation rejects mixed case (surfaces canonical form rather than silently normalizing). Route lookup is exact-match on stored lowercase. | ✅ |
| D6 | **Editable in `Draft`, `Planning`, `Published`. Locked in `Active`, `Cancelled`, `Completed`, `Archived`** — same lock-list as 6A.153. (User-confirmed.) | ✅ |
| D7 | **Default null (organizer opt-in).** No auto-generate from title. | ✅ |
| D8 | **Transparent render** (not redirect). `/cleveland-show` renders the same content as `/events/{guid}` — user said "instead of", not "and also". | ✅ |
| D9 | Route at `web/src/app/[slug]/page.tsx` (root catch-all). Next.js routes static > dynamic > catch-all, so explicit routes win automatically. Reserved-words validator is defense-in-depth. | ✅ |
| D10 | **SSR with `generateMetadata`** for the new `[slug]` route (OG/Twitter tags). Existing `/events/[id]` stays client-only (backport deferred). | ✅ |
| D11 | Unknown slug → `notFound()` 404 with `Cache-Control: no-store`. | ✅ |
| D12 | **Debounced `GET /api/events/check-slug?slug=` (300ms FE)**, anonymous + rate-limited 30/min/IP. Returns `{ available, reason? }`. | ✅ |
| D13 | **Extend existing `POST /api/events` + `PATCH /api/events/{id}`**. No new write endpoint. Matches 6A.153 pattern. | ✅ |
| D14 | Reserved-words list lives at `LankaConnect.Domain.Events.ValueObjects.EventVanitySlug.ReservedSlugs` (immutable `IReadOnlySet<string>`). FE pulls via `GET /api/events/slug-config` (24h cache). | ✅ |
| D15 | Migration: one nullable `varchar(80)` column on `events.events`; partial unique index `WHERE vanity_slug IS NOT NULL`; new `events.event_slug_aliases` table. No backfill. | ✅ |
| D16 | Organizer can clear slug → old value automatically rolled into `event_slug_aliases` as permanent 301 source. | ✅ |
| D17 | **Canonical URL is `/{slug}`** when slug set; `/events/{guid}` page emits `<link rel="canonical" href="…/{slug}">`. Aliases canonical → current active slug. | ✅ |
| D18 | `generateMetadata` on the new `[slug]` route only. **Do NOT** backport SSR to `/events/[id]` (separate phase candidate). | ✅ |

## Reserved-words list (Domain const, ~65 entries)

Lives at `LankaConnect.Domain.Events.ValueObjects.EventVanitySlug.ReservedSlugs`:

- **Top-level app routes:** about, animation-preview, api, blog, business, contact, dashboard, events, forums, guidelines, help, lanka-events, lanka-events-logos, marketplace, newsletter, newsletters, safety, search, templates
- **Auth routes:** forgot-password, login, register, reset-password, verify-email
- **Dashboard routes:** notifications, profile
- **Static / well-known:** robots.txt, sitemap.xml, favicon.ico, manifest.json, .well-known, _next, static, assets, public
- **Future namespace:** admin, support, terms, privacy, legal, settings, account, billing, payments, checkout, cart, organizer, ticket, tickets, refund, refunds, refund-request, refund-requests, my, me, user, users, group, groups, community, communities, post, posts, comment, comments, like, likes, share, follow, followers, following, message, messages, chat, notification, embed, oembed, og, share-card, qr, app, mobile, web, ios, android, desktop, download, downloads, status, health, healthz, metrics, ping, version
- **Trademark/brand blocks:** lankaconnect, lc, sri-lanka, srilanka, ceylon

## Scope of changes

### Domain (`LankaConnect.Domain`)
- `Events/ValueObjects/EventVanitySlug.cs` — VO with `Create()`, `ReservedSlugs`, regex.
- `Events/Event.cs` — add `VanitySlug: EventVanitySlug?` private-set property.
- `Events/Event.VanitySlug.cs` — partial: `SetVanitySlug(slug)` mutator + status lockout + alias-emit event.
- `Events/Entities/EventSlugAlias.cs` — new entity (`Id`, `EventId`, `Alias`, `CreatedAt`, `RetiredAt`).
- `Events/DomainEvents/EventSlugRetiredDomainEvent.cs` — emitted when slug changes/cleared so the application layer can persist an alias row.

### Infrastructure (`LankaConnect.Infrastructure`)
- `Data/Configurations/EventConfiguration.cs` — property + partial unique index.
- `Data/Configurations/EventSlugAliasConfiguration.cs` — table + indexes.
- `Data/Repositories/EventRepository.cs` — `GetByVanitySlugAsync`, `GetByAliasAsync`.
- Migration `Phase6A154_AddEventVanitySlug` — column + partial unique index + alias table.

### Application (`LankaConnect.Application`)
- `Events/Commands/CreateEvent/CreateEventCommand.cs` + handler — accept `VanitySlug`.
- `Events/Commands/UpdateEvent/UpdateEventCommand.cs` + handler — accept `VanitySlug` (tri-state).
- `Events/Queries/CheckSlugAvailability/` — new query + handler.
- `Events/Queries/GetSlugConfig/` — new query returning regex + reserved-list.
- `Events/Queries/GetEventBySlug/` — new query returning EventDto.
- `Events/Common/EventDto.cs` + mapper — surface `VanitySlug`, `CanonicalUrl`.

### API
- `EventsController.GetSlugConfig` (anonymous, 24h cache).
- `EventsController.CheckSlugAvailability` (anonymous, rate-limited 30/min/IP).
- `EventsController.GetEventBySlug` (anonymous, used by SSR).
- Existing `Post` + `Patch` extended (no new endpoints for writes).

### Frontend (`web/`)
- `web/src/app/[slug]/page.tsx` — new SSR route with `generateMetadata` for OG/Twitter.
- `web/src/presentation/components/features/events/VanitySlugField.tsx` — debounced availability-check input.
- `web/src/presentation/lib/validators/event.schemas.ts` — `vanitySlug` field on `createEventSchema` + `baseEditEventSchema`.
- `EventCreationForm.tsx` + `EventEditForm.tsx` — mount the field; update `FIELD_TO_SECTION`.
- `web/src/app/events/[id]/page.tsx` — emit `<link rel="canonical">` when event has slug.
- `web/src/infrastructure/api/types/events.types.ts` — add `vanitySlug?` and `canonicalUrl?`.

### Tests
- **Domain (12 + 4 + 5):** `EventVanitySlug_Create_Tests` (slug shape) + `EventVanitySlug_ReservedSlugs_Tests` + `Event_SetVanitySlug_Tests` (mutator + status lockout).
- **Infra (3):** `EventRepository_VanitySlug_Tests` (unique constraint, partial index, alias lookup).
- **App/API (3):** `CheckSlugAvailabilityQueryHandlerTests`, `UpdateEventCommandHandlerTests.UpdateVanitySlug_*`, integration test for `PATCH` 409 conflict.
- **Web (2):** RTL test for `VanitySlugField` debounce behavior + route-precedence assertion (visit `/about` shouldn't hit catch-all).
- **Build-time CI (1):** Vitest test enumerating top-level `web/src/app/*` dirs and asserting each appears in `ReservedSlugs`.

29-case matrix total.

## Risks (mostly low)
- **Slug squatting** on brand terms — mitigated by reserved-words list including lankaconnect, sri-lanka, etc.
- **Catch-all swallows future static route** — mitigated by CI build-time test (Test #29).
- **TOCTOU between availability-check and submit** — backend re-validates uniqueness inside the same transaction.
- **Slug leaks "private" event title** — slug route returns 404 unless event status = Published.
- **Sinhala/Tamil slugs (i18n)** — explicitly rejected (ASCII-only); Punycode/IDN deferred.

## Phase reservation (4-source check, 2026-05-27)
- Master index: highest is 6A.153; **6A.154 absent** ✅
- `git log --all`: no 6A.154 commits ✅
- `git branch -a`: no 6A-154 branches ✅
- `docs/MASTER_TODO_PHASE_6A_154*.md`: this file is the first ✅

## Deploy plan
1. Domain → Infrastructure → Application → API → Web (TDD per substream)
2. Backend EF migration verified locally (`[Migration("...")]` attribute on Designer.cs)
3. Push branch → trigger `deploy-staging.yml` + `deploy-ui-staging.yml` together
4. API smoke against all 5 endpoints
5. Browser smoke on staging: organizer sets a slug, anonymous visitor hits `lankaconnect.app/{slug}`, page renders + OG tag inspectable
6. Update `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md`
7. PR to main

## Branch base note
Branched off `main` at `7c07f34d` (post-6A.152 sync). 6A.153 PR #130 still in operator UAT — independent code paths (different columns, different concerns), so no conflicts expected at merge time. Whichever PR merges first, the other rebases cleanly.

# LankaSeyla Landing Entry — Design

**Date:** 2026-08-04
**Status:** Approved
**Scope:** `web/` only. No backend, no database, no API changes.

## Problem

The LankaConnect umbrella landing page (`web/src/app/page.tsx`) exposes exactly one
sub-brand entry point: a LankaEvents card in the middle band of the 100vh hero.
LankaSeyla — the premium Sri Lankan clothing store at
`https://lankaseyla.lankaconnect.app/`, branded "by LankaConnect" — has no entry
point at all.

Add a second, peer-level entry card for LankaSeyla.

## Key constraint discovered during design

LankaSeyla's supplied logo (`LankaSeyla.jpeg`, 1600×887) is a **4.03:1 wordmark**
once its black padding is trimmed (1268×315). Letterboxing that whole shape into
the 110×110 square the card format expects yields a text band 27px tall inside the
square, which renders **14px tall** at the card's actual 55×55 display size — with
the "by LankaConnect" sub-line illegible and ~70% of the square empty.

**Resolution:** build the square asset as a stacked lockup — "Lanka" (white) over
"Seyla" (gold) — cut from the real letterforms. This fills the square and raises
per-word text height to ~17px at display size while preserving brand typography.

Brand gold sampled from the artwork: **`#D7A959`** dominant, `#E3B565` on highlights.

## Architecture

### 1. Extract a shared `EntryCard`

The existing LankaEvents card is ~100 lines of inline JSX in `page.tsx` with four
hand-written mouse handlers (`enter`/`leave`/`down`/`up`), each rebuilding a
five-layer `boxShadow` string from the brand colour. Duplicating that for a second
card would put ~200 lines of near-identical style arithmetic in the page and
guarantee the two cards drift apart.

New component: `web/src/presentation/components/features/landing/EntryCard.tsx`
(alongside the existing `WorldMapAnimation`).

```
Props:
  brandColor   string    drives gradient + border + all four shadow states
  logoSrc      string    110×110 asset, rendered at 55×55
  logoAlt      string
  name         string
  badge        string    the pill ("Event Planner")
  tagline      string
  href         string
  external?    boolean   -> target="_blank" rel="noopener noreferrer"
  live?        boolean   -> green pulsing "LIVE" dot
```

The shadow arithmetic collapses into one `shadowFor(state, color)` helper so the
four handlers become one-liners.

**LankaEvents must render pixel-identically after extraction.** This is a pure
refactor of working UI, which CLAUDE.md Section 3 treats as high-risk; the
mitigation is the test ordering below.

### 2. Card contents

| Prop | LankaEvents (unchanged) | LankaSeyla (new) |
|---|---|---|
| `brandColor` | `#FF7900` | `#D7A959` |
| `logoSrc` | `/lanka-events.png` | `/lanka-seyla.png` |
| `name` | LankaEvents | LankaSeyla |
| `badge` | Event Planner | Clothing Store |
| `tagline` | Plan Your Event with Ease | Tradition Woven with Elegance |
| `href` | `/lanka-events` | `https://lankaseyla.lankaconnect.app/` |
| `external` | false | true |
| `live` | true | false |

The green **LIVE** dot is deliberately omitted from the LankaSeyla card. It
signals live in-app data; on a card that leaves the site it would assert something
we do not know. The `ArrowUpRight` carries the affordance alone.

### 3. Layout

The middle band becomes a flex container: `flex-col` below `md`, `flex-row` at `md`
and up, `items-stretch` so both cards match height when a tagline wraps. Each card
keeps its 520px cap — desktop ~1060px total, mobile two full-width cards stacked
(unchanged from today's single-card behaviour).

## Accessibility

- External link carries `rel="noopener noreferrer"`; `noopener` denies the opened
  page a `window.opener` handle back.
- **Existing gap being fixed:** the current card is a `Link` wrapping a `div` with
  no focus style — keyboard-reachable but invisible when focused. `EntryCard` adds
  a `focus-visible` ring, which fixes LankaEvents at the same time.
- Both logos carry non-empty `alt`.

## Testing

`web/src/__tests__/pages/landing-entry-cards.test.tsx`, following the placement of
the existing `landing-page-metro-filtering.test.tsx`.

1. **Written first, before the extraction** — pin the current LankaEvents card's
   rendered output so the refactor proves itself non-breaking rather than
   asserting it.
2. Both cards render.
3. LankaSeyla: correct href, `target="_blank"`, `rel` contains `noopener`.
4. LankaEvents: internal `/lanka-events`, **no** `target` (regression guard).
5. Only LankaEvents shows LIVE.
6. Both logos have non-empty `alt`.

## Asset generation

`web/public/lanka-seyla.png` (110×110) is generated from
`C:\Niroshan\LankaConnect Marketplace\Logo Files\LankaSeyla.jpeg` via `sharp`:
trim padding → split the wordmark into "Lanka" / "Seyla" → stack, pad, square,
downscale. The generation script is committed to `scripts/` so the asset is
reproducible rather than a mystery binary.

## Deployment

`web/`-only change, so it ships via `deploy-ui-staging.yml`, not the backend
workflow. Note that workflow triggers on push to `develop`; deploying any other
branch to staging requires a `workflow_dispatch` against that ref.

## Out of scope

- Any LankaSeyla content inside LankaConnect (it is an external storefront).
- Changes to `/lanka-events` or any other route.
- A third sub-brand — though adding one is now a five-line `EntryCard` call.

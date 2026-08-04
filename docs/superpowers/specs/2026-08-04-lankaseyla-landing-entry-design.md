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

## Logo treatment

LankaSeyla's supplied logo (`LankaSeyla.jpeg`, 1600×887) is a **4.03:1 wordmark**
once its black padding is trimmed (1268×315).

**The wordmark is used as supplied** — trimmed and scaled to a fixed 110px height,
its natural width riding along (443×110), rendered in the card at half that
(221×55). The card does not force it into the square-icon slot the LankaEvents
button uses.

An earlier revision cropped the wordmark into a stacked 110×110 "Lanka"/"Seyla"
lockup to keep the two cards structurally identical. That was rejected on operator
review: matching LankaEvents is not a requirement, and altering a brand's wordmark
to satisfy a layout is the wrong trade. The two cards may differ.

Because the wordmark spells the brand out, the card omits the separate name text —
printing "LankaSeyla" beside a logo that already reads "LankaSeyla" says it twice.
The name still reaches assistive technology through the image's `alt`.

**Background is transparent.** A transparent-background master is stored alongside
the supplied artwork as
`C:\Niroshan\LankaConnect Marketplace\Logo Files\LankaSeyla-transparent.png`
(1600×887 — same pixel dimensions as the JPEG; the only change is the alpha
channel), and the web asset is derived from it. The card's gold gradient now shows
through behind the letterforms instead of a black rectangle sitting on it.

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
  logoSrc      string    logo asset, authored at 2x
  logoAlt      string
  logoWidth?   number    default 55; wordmarks pass their natural aspect
  logoHeight?  number    default 55
  name         string
  showName?    boolean   false when the logo is a wordmark that already says it
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
| logo size | 55×55 (square icon) | 221×55 (wordmark, natural aspect) |
| `showName` | true | false — wordmark already reads "LankaSeyla" |
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

`scripts/generate-lanka-seyla-logo.js` produces two artifacts from
`C:\Niroshan\LankaConnect Marketplace\Logo Files\LankaSeyla.jpeg` via `sharp`:

1. **`LankaSeyla-transparent.png`** (1600×887), written back beside the source in
   the brand Logo Files folder. Same dimensions as the JPEG; the only change is
   that the black field becomes transparent.
2. **`web/public/lanka-seyla.png`** (443×110) — that master trimmed of its now
   transparent padding and scaled to 110px height, natural width following.

No cropping, splitting, recolouring or reflowing of the wordmark at either step.

**How the alpha is derived:** the artwork is light text on a flat black field, so
a pixel's max channel measures ink coverage directly. Measured on the source, the
background occupies 0–15 and the solid letterforms plateau at ≥200, with only
antialiased edges between; alpha ramps across that gap, leaving glyphs fully
opaque and the field fully clear. RGB is copied through untouched rather than
un-premultiplied, so the brand gold stays exactly `#D7A959` instead of being
brightened toward `#FFC869`. Verified by compositing over magenta: no dark fringe.

The script is committed so the assets are reproducible rather than mystery
binaries.

## Deployment

`web/`-only change, so it ships via `deploy-ui-staging.yml`, not the backend
workflow. Note that workflow triggers on push to `develop`; deploying any other
branch to staging requires a `workflow_dispatch` against that ref.

## Out of scope

- Any LankaSeyla content inside LankaConnect (it is an external storefront).
- Changes to `/lanka-events` or any other route.
- A third sub-brand — though adding one is now a five-line `EntryCard` call.

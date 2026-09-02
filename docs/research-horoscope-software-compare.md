# Horoscope software comparison — UI/UX & feature reference

**Purpose:** what to borrow (and what to avoid) from existing Vedic-astrology products when
building `ikiastrro`'s web UI (`src/Ikiastrro.Web`) and its later feature surface.
A working log — updated as more sources are reviewed.

**Inputs behind this doc**
- `Downloads/vedic_astrology_software_competitive_intelligence.md` — market/feature/naming landscape.
- `Downloads/vedic_astrology_open_source_windows11_research.md` — OSS repo shortlist.
- `_research/README.md` — the repos actually cloned into this project (git-ignored) + the method
  used for this comparison.
- Live-site screenshots: `_research/_ui_refs/_screenshots/<source>/` (git-ignored — regenerable
  via the method in `_research/README.md`).

**Design constraint every "adopt" below is filtered through** (`src/Ikiastrro.Web/Components/DESIGN.md`):
one dark-only parchment/serif theme, tokens in `wwwroot/css/tokens.css`, per-component
`.razor.css` isolation, **no CSS framework**. Borrow layout / geometry / interaction ideas;
re-skin with our tokens. The three OSS repos below are MIT, so their chart-rendering **code**
may also be ported with attribution.

| Source | Type | License | Reviewed |
|---|---|---|---|
| VedAstro (vedastro.org) | Live Blazor app — same framework as ours | MIT | 15 screens, full flow |
| AstroSage Kundli | Live — birth-data form only | proprietary | 3 screens (results blocked — see TODO) |
| Cosmic Insights | Live — app landing only, no web app | proprietary | 1 screen (palette) |
| jyotish-dashboard (`_research/_ui_refs/jyotish-dashboard/`) | Cloned code — closest structural analogue | **MIT** | code read; not yet run |
| almamesh (`_research/_ui_refs/almamesh/`) | Cloned code — local-first PWA (React/TS) | **MIT** | UI layer read (`frontend/apps/web/src`); not run |

---

## 1. VedAstro — `_screenshots/vedastro/`

The hosted site has diverged from the cloned `VedAstro/Website/` source (live site is now
AI-chat-first; `/Calculator` 404s). Source still matches for chart rendering
(`Website/wwwroot/js/VedAstro.js`, `Interop.js`) and the Horoscope page
(`Website/Pages/Calculator/Horoscope.razor`).

### 01–02 Home / AI-chat landing
- **Adopt:** "How can I help you?" + a row of example-question cards as the entry point — matches
  the competitive-intel doc's "ask a question, not navigate tables" thesis. Good later direction
  for a landing above `ChartDetail`.
- **Reject:** the questions shown (sex / love-loss / soulmate) — off-brand for our "calm,
  scholarly, non-sensational" positioning (`methods_prodmag.md` brand section).

### 03–09 Birth-data entry — **stepped modal wizard**
- Flow: Time (h/m/AM-PM dropdowns) → Location (Country select + city autocomplete + "Detect my
  location") → Gender → Name → spinner → "person" saved to a reusable profile list.
- **Adopt:** (a) one field-group per step keeps each screen trivial; (b) **saved-profile list** so
  a chart is recalculated by picking a person, not re-typing — we already store birth details in
  `tbl_BirthDetails`, so `Add.razor` + a picker on `Charts.razor` gets the same payoff; (c) city
  autocomplete with a "Country" pre-filter (we geocode via Nominatim already).
- **Reject:** 4 modal hops is heavy for a keyboard-first desktop tool. Prefer **one `Add.razor`
  page, sectioned** (Identity / Birth time / Birth place), with the same saved-profile outcome.
- **Maps to:** `Components/Pages/Add.razor`, `Components/Pages/Charts.razor` (add person picker).

### 10–11 Results top + key-details card + Strength
- **Adopt:** a compact **key-details card** (LMT, Ayanamsa value, Yoni, Maraka, "Show all
  (20 more)" expander) as the first thing under the header — progressive disclosure done simply.
  We compute most of this already (`ChartKeyDetailsRepository`).
- **Adopt:** **Shadbala as two bar charts** (planet strength + house strength) side by side. Use
  the dataviz skill; colour planets from a new `--planet-*` token set, houses from a sequential
  ramp. (Shadbala itself is still un-scoped for us — this is the view for when it lands.)
- **Reject:** their default chart.js light styling — re-skin to `--paper` / `--ink`.
- **Maps to:** `Components/Pages/ChartDetail.razor`, new `Components/Charts/StrengthBars.razor`.

### 12 D1 + D9 charts + chart-style switcher
- Renders **Rasi D1** and **Navamsha D9** as square grids: each cell = house-number badge +
  planet names + tiny constellation line-art + sign name; a gear menu switches chart style.
- **Adopt:** D1 and D9 **side by side** (we currently show them separately); the per-cell
  "badge + planets + sign" content model matches our `SouthIndianGrid.razor`.
- **Adopt:** a **style switcher** (South ↔ North Indian) as a small menu on the charts section.
- **Reject:** the constellation line-art per cell — decorative noise at chart size; our glyph
  approach (`GridPlanetGlyph.cs`) is cleaner.
- **Maps to:** `Components/Charts/SouthIndianGrid.razor`, `ChartView.razor`; new `NorthIndianGrid.razor`.

### 13–14 Life-area results — Visual Overview + score cards
- 12 life areas as a green/red "good/low" bar chart, then 12 cards (`AREA n`, title, big coloured
  score, "Good/Low Result"), then Top-3-strong / Top-3-weak callouts.
- **Adopt:** the **house → life-area framing** ("Career & Fame", "Marriage & Partnerships"…) as
  the human-readable skin over house analysis — directly relevant to
  `docs/reference-house-lagna-significations.md` and `docs/scope-bhava-coverage.md`. The Top-3 / Bottom-3 summary
  is a good `ChartDetail` header.
- **Reject:** reducing a house to a single red/green number — too lossy; keep it as an entry
  point that expands to the factors.
- **Maps to:** `ChartDetail.razor`; feeds the Bhava-analysis backlog items in `methods_prodmag.md`.

### 15 Prediction list + Planet Data table
- Classical yoga predictions as a scrollable list of expandable rows; planetary data as a
  user-configurable table (column picker, delete).
- **Adopt:** expandable prediction rows (claim → expand → the chart factors behind it) = the
  "explainability" pattern. Configurable planet-data table matches our existing tables view.
- **Maps to:** `ChartDetail.razor`; future Yoga-detection design pass.

---

## 2. AstroSage Kundli — `_screenshots/astrosage/`

Only the birth-data form was reachable; submitting routes to `ascloud.astrosage.com`, which the
browser extension has no permission for (see TODO).

### 01–03 Birth-data form — **single dense form** (contrast to VedAstro's wizard)
- One screen: Name · Gender toggle · Day/Month/Year · Hrs/Min/Sec · Place (autocomplete) ·
  `[+] SETTINGS` (ayanamsa / house-system advanced) · "Current location" / "Now" shortcuts ·
  **DONE** vs **DONE AND SAVE**.
- **Adopt:** the better model for our desktop, keyboard-first tool than VedAstro's 4 modals —
  everything visible, tab through it. Take: separate H/M/S fields (we keep a nullable *corrected*
  time — see `ikiastrro.md` input fields); an **advanced `[+] SETTINGS` disclosure**
  for ayanamsa / house system even though v1 hard-codes Lahiri + Whole Sign (room to expose later
  without a redesign); the **"Done" vs "Done and save"** split (calculate once vs persist to
  `tbl_BirthDetails`).
- **Reject:** the ad rails, the 12-sign top nav, the buy-Kundli upsell.
- **Maps to:** `Components/Pages/Add.razor`.

---

## 3. Cosmic Insights — `_screenshots/cosmicinsights/`

`cosmicinsights.net` is an app-store landing page; the product is mobile-only, no web UI.

- **Only takeaway:** the palette — deep indigo/purple ground + gold serif (Cinzel-style) wordmark.
  Close to our own `--paper #14132a` / `--accent #e2ad4f`. Confirms the dark-indigo + gold
  direction reads as "serious Jyotish", not "fortune-telling app".
- Nothing to adopt structurally.

---

## 4. jyotish-dashboard — cloned code, MIT — **closest structural analogue**

Flask + Jinja + Bootstrap, but the *shape* is our project: "Sidereal / Lahiri Ayanamsa",
**D1 Rashi + D9 Navamsa**, charts drawn on `<canvas>`, dark-gold "vedic" theme, `saveChart()`
persistence. Read `app/templates/kundli.html`, `app/static/js/chart.js`, `app/static/css/style.css`.

### `app/static/js/chart.js` — North Indian kundli renderer (Canvas 2D)
- Self-contained: DPR-aware 440px canvas, `HOUSE_CELLS` 4×4 grid map, `PLANET_COLORS`,
  `PLANET_SYMBOLS`, dignity colouring, decorative centre box.
- **Adopt (port, MIT + attribution):** the `HOUSE_CELLS` layout map and the diamond-square
  geometry are exactly what a `NorthIndianGrid.razor` needs. Port the drawing routine to an SVG
  Razor component (preferred — matches `SouthIndianGrid.razor`, themeable via `var(--*)`) or a
  canvas + small JS interop.
- **Reject:** hard-coded hex (`#0d0820`, `#FFD700`) — swap for `--paper`, `--accent`, `--dignity-*`.

### `app/static/css/style.css` — token system
- `:root` with `--gold #c9940a`, `--saffron`, `--moon`, `--rahu-color #7c3aed`, `--ketu-color
  #db2777`, `--radius`, `--sidebar-w`; dignity classes `.dignity-exalted / -debilitated /
  -own_sign / -neutral`; `.btn-vedic` gradient; `.card-vedic`; `.native-name { font-family:
  'Cinzel', serif }`.
- **Adopt:** the token *structure* and the **planet + dignity colour assignments** map straight
  onto our `tokens.css`. Theirs is a light "warm cream" theme; we have the dark values — compare
  their dignity hues against our `--dignity-*` and reconcile.
- **Adopt:** per-planet colour tokens — we only have `--dasha-*` (dasha lords); a `--planet-*`
  set for chart glyphs / strength bars is missing and this is a ready reference.
- **Maps to:** `wwwroot/css/tokens.css`, `Components/Charts/*`.

### `app/templates/kundli.html` — layout
- Two-column: left = D1 card + D9 card stacked; right = detail panels. Card-header pattern
  (`card-vedic-header` with icon + "D1 — Rashi Chart (Lagna: …)").
- **Adopt:** the "D1/D9 stacked on the left, analysis on the right" layout for `ChartDetail.razor`;
  the Lagna-in-the-card-header label.

---

## 5. almamesh — cloned code, MIT — UI layer read (`hseshadr/almamesh`)

Local-first PWA, React/TS Bun monorepo. `frontend/apps/web/src`. LICENSE = MIT (Harish Seshadri,
2026) — **geometry and algorithm may be ported with attribution**; still re-skin colours to our
tokens, don't copy CSS/i18n files. The chart-rendering code is the highest-value part: two clean,
well-commented, **pure-renderer** SVG components (`components/chart/{NorthIndianChartSVG,
SouthIndianChartSVG}.tsx`) that consume a pre-shaped `ChartGeometry` and compute no astrology —
exactly our `D1ChartViewModel` → grid split. Where it's *behind* us: its dashboard shows **D1
only** (`ChartVisualization.tsx`: "the engine emits no D9 / varga, so there is no Navamsa panel
here") — so almamesh gives us renderer geometry and a "name the chart via props" pattern, not a
multi-varga layout to copy.

### `components/chart/SouthIndianChartSVG.tsx` — sign-fixed 4×4 SVG

- `GRID_LAYOUT` = a `(number|null)[][]` 4×4 array, **Pisces top-left running clockwise**, centre
  2×2 = `null` (empty, holds the chart label). Per cell: house number top-right (faint), `La`
  lagna marker top-left (brass), 3-letter sign abbrev, planets **one per row sorted by
  longitude**.
- **Adopt — `centerTitle` / `centerCode` props.** The empty centre 2×2 renders the chart's own
  name (`Rāśi`/`D1`, `Navāṁśa`/`D9`, …). One component renders *any* varga by passing its
  name/code — the cleanest answer to our D1/D2/D6/D9/D10/D11 need (six charts, one grid
  component), and it stops a D9 plate mislabelling itself "Rāśi".
- **Adopt — `precision: 'degree' | 'sign'` → `showDegrees`.** A varga is a sign-placement chart:
  it renders the **bare glyph** (`Ma`), never a fabricated `Ma 0°00'`, and its `aria-label`
  switches to "{planet} in {sign}". Direct fit — our D2/D6/D9/D10/D11 `KeyDetails` rows carry
  `DegreesInSignDisplay = NULL`; the grid must key off that, not print zeros.
- **Adopt — combust = `opacity 0.55`; retrograde = ASCII `(R)`** *not* the U+211E ℞ glyph (their
  comment: the print font has no ℞ → tofu box). Order always `label · (R) · degree`.
- **Adopt — cross-component selection.** `selectedPlanet` / `onSelectPlanet` threaded from a
  store: clicking a planet in one chart highlights it in every chart and the positions table.
- **Reject:** their Tailwind utility classes and hard-coded per-`variant` object — we use
  `.razor.css` isolation + `var(--*)`. Take the geometry (`GRID_LAYOUT`, cell math), not the CSS.
- **Maps to:** `Components/Charts/SouthIndianGrid.razor` (compare our current cell orientation
  against Pisces-TL-clockwise), `Components/Charts/ChartView.razor`, `D1ChartViewModel`.

### `components/chart/NorthIndianChartSVG.tsx` — house-fixed diamond SVG

- **Adopt — the canonical geometry, ready to port.** Unit-square space (0,0→1,1); 13 named
  points: 4 corners, 4 edge-midpoints, centre `C`, and 4 "quarter" points
  `QTL/QTR/QBR/QBL = {0.25|0.75, 0.25|0.75}` (where the inner side-midpoint diamond crosses the
  diagonals). `HOUSE_CELLS[12]` = a polygon per house, **counter-clockwise from H1 = top-centre
  diamond** (`[T,QTL,C,QTR]`), alternating diamond / triangle. Houses fixed, **sign rotates by
  lagna** (`house.signIndex + 1` shown faint) — matches our whole-sign model exactly. Six
  internal lines (2 diagonals + the 4 diamond edges) drawn *on top* of the cell fills.
- This is a **better source than jyotish-dashboard's `chart.js`** for the planned
  `NorthIndianGrid.razor` (§4, cross-cutting #3): it's already SVG, already unit-normalised,
  already MIT, and shares the exact `showDegrees` / selection / theming model as its South
  sibling. Recommend porting from here instead.
- Same `PlanetGlyph` rules as the South chart (bare glyph for vargas, `(R)`, combust dim,
  keyboard-activatable `role="button"` with `aria-label="{planet} in {sign}, house {n}"`).
- **Maps to:** new `Components/Charts/NorthIndianGrid.razor` (SVG sibling of `SouthIndianGrid`).

### `components/chart/chartTheme.ts` — resolved token object

- 9 named chart tokens (`background`, `cellFill`, `gridStroke`, `lagnaFill`, `accent`,
  `selectedStroke`, `ink`, `signText`, `mutedText`), selected **once** per render by
  `chartTheme(variant)` and threaded down — no per-element `variant` branching.
- The `screen` palette (`#0B0E17` bg, `#11151F` cell, `#262B38` grid, `#C9A24B` brass accent,
  `#F4F1E8` ivory ink, `#8A8576` muted) is essentially **our own family** — independent
  corroboration of the dark-obsidian + brass-gold direction (with Cosmic Insights, §3).
- **Adopt:** the *token set*. Our `tokens.css` has `--paper` / `--ink` / `--accent` / `--dignity-*`
  but no distinct `cellFill` vs `lagnaFill` vs `gridStroke` vs `signText` vs `mutedText` — add
  those so the grid components stop reaching for one-off values.
- **Adopt (reinforces §4 / cross-cutting #4):** `planetInk()` shows they carry a **per-planet
  colour** (`ChartPlanet.color`); we still only have `--dasha-*`. A `--planet-*` set is needed.
- **Reject:** the `paper` (light print) variant and the `planetInk` luminance-blend — we are
  dark-only; a planet colour is used verbatim.

### `components/ui/{Disclosure,ContentModeToggle,DualModeContent}.tsx` — progressive disclosure

- **`Disclosure`** — accessible inline expand/collapse. Real `<button aria-expanded aria-controls>`;
  the panel animates its **real measured height via `grid-template-rows: 0fr → 1fr`** (no JS
  measurement, no `max-h-[5000px]` hack); content stays mounted across toggles; collapsed → the
  panel is `inert` + `aria-hidden`. The grid-rows technique ports straight into a `.razor.css`
  component. This is the "show all (N more)" / "read full reading" primitive §1 and cross-cutting
  #2 call for.
- **`ContentModeToggle` + `DualModeContent`** — one **global** segmented toggle, "For You"
  (layman) vs "For Astrologer" (technical), that flips *every* interpretation block at once;
  `DualModeContent` falls back gracefully when only one mode's text exists. This is the
  beginner→expert tier (competitive-intel §20) done as a single global switch, not per-section
  state — the model to copy for `ChartDetail.razor` and the later Yoga / prediction surfaces.
- **Maps to:** new `Components/Shared/Disclosure.razor` + `.razor.css`; a content-mode toggle in
  `MainLayout.razor` / `ChartDetail.razor` for when interpretation text lands.

### `components/features/dashboard/IdentityStrip.tsx` — key-details band

- "Quiet and typographic": the person's **name as the page heading**, then four facts an
  astrologer reaches for first — **Lagna, Moon sign + nakshatra, and the full running daśā stack
  (mahā / antar / pratyantar) with the declared year convention** — then one hairline rule.
  "No boxes-in-boxes." `Fact` = `text-[11px] uppercase tracking-[0.18em]` label + value in a
  `<dl>/<dt>/<dd>`.
- **Adopt — the near-cusp "Birth-time sensitivity" callout as a first-class element** (not a
  footnote): when the Ascendant sits within a few arc-degrees of a sign boundary, name the
  alternative rising sign, state that *every house shifts with it*, and link out. We already
  compute the Ascendant's exact within-sign degree, so this is cheap to surface on
  `ChartDetail.razor` (the link target — rectification — doesn't exist for us yet; the callout
  still stands as an honesty signal).
- **Reject:** the `timeConfidence`-gated "refine from life events" nudge — depends on a
  rectification flow we don't have.
- **Maps to:** `Components/Pages/ChartDetail.razor` header; a new `Components/Charts/IdentityStrip.razor`.

### `components/features/dashboard/{LifeAtlas,ChartVisualization}.tsx` + `layout/AppLayout.tsx`

- **`ChartVisualization`** — the chart panel is a `Card` with `title` + an **`actions` slot in the
  header holding `<ChartStyleToggle />`** (a segmented North/South control bound to one store
  field, `displayStyle`, "so any chart consumer can switch styles in one place"). One `geometry`,
  `displayStyle === 'north' ? <North…> : <South…>`. Loading = shimmer skeleton, error = inline
  banner (`status-debilitated` colour), empty = icon + title + body. `print-no-break` /
  `print-container` classes gate PDF layout.
  - **Adopt:** Card-with-header-actions as the chart-panel frame; the single global style toggle
    (cross-cutting #3); the three explicit non-ready states.
- **`LifeAtlas`** — the **house → life-area framing** as a responsive card grid (1 / 2 / 3 / 4
  cols) → `/life/:domain` detail pages. Card face = domain name + strength-band **badge** +
  current-emphasis one-liner + **next timed window** (mono date + label). Renders a designed
  *pending* face immediately; the ~30 s compute runs in a **background Worker in parallel** with
  interpretation and the gate is **informational, never a manual button** ("never a dead-end for
  a returning visitor"). An 8th dashed "About" cell explains the section and links to the full
  panel.
  - **Adopt:** this whole IA for our Bhava-analysis surface — it's the concrete shape for
    `docs/reference-house-lagna-significations.md` + `docs/scope-bhava-coverage.md` output. Also the principle:
    slow analytics render a pending state immediately and compute off the paint path.
  - **Maps to:** a future `Components/Pages/LifeDomain.razor` + a `LifeAtlas.razor` section on
    `ChartDetail.razor`.
- **`AppLayout`** — sticky, `backdrop-blur` header (wordmark → `/welcome` splash · profile
  switcher · live status badge → settings); `main` carries a faint starfield + astrolabe-ring
  texture; `max-w-7xl` gutter; `min-h-dvh` (mobile URL-bar safe).
  - **Adopt:** the header triad (identity · profile picker · status/settings) and the atmospheric
    but low-contrast main background. **Reject:** the AI-status badge (no default AI for us).
  - **Maps to:** `Components/Layout/MainLayout.razor`.

### `components/shared/LocationSearch.tsx` — birth-place typeahead

- **Online-primary** (Open-Meteo geocoder) **+ transparent offline fallback to a bundled
  city-list + manual lat/long as the last resort.** 250 ms `useDebounce`; an `AbortController`
  cancels the in-flight request on each new keystroke ("an aborted search is expected, not a
  failure"); every emitted result carries a valid IANA `timezone`.
- **Adopt:** the **bundled offline city-list fallback** — we're online-only via Nominatim today
  with a manual prompt when it fails; a small embedded city list keeps `Add.razor` working with
  no network. Also the debounce-then-abort pattern.
- **Reject:** nothing structural — the shape matches what `Add.razor` needs.
- **Maps to:** `Components/Pages/Add.razor` place field; a `Components/Shared/LocationSearch.razor`.

### `pages/Onboarding.tsx` — a 5-step wizard (reinforces the *rejected* option)

- name → birth-date → birth-location → birth-time → life-events → generating, with a
  `progressPercent` bar, `isStepValid` gating, `nextStep` / `prevStep`, per-step save. So almamesh
  and VedAstro both use a **wizard**; AstroSage uses the dense single form — and cross-cutting #1
  still stands (one sectioned `Add.razor` for a keyboard-first desktop tool). What's reusable
  regardless of wizard-vs-page: the **per-field components** — a birth-date picker that "buffers
  in-progress edits internally and only emits complete values" (no half-typed dates reaching the
  handler), and `LocationSearch` above.

---

## Cross-cutting decisions taken from this pass

1. **`Add.razor` = one sectioned page**, not a wizard (AstroSage model), but keep VedAstro's
   **saved-profile → pick a person** outcome.
2. **`ChartDetail.razor` layout:** key-details card (with "show all" expander) → D1 + D9 side by
   side (jyotish-dashboard's left-column stack is the fallback) → strength bars → life-area
   summary (Top-3 / Bottom-3) → expandable prediction rows. Progressive disclosure throughout.
3. **New `NorthIndianGrid.razor`** as an SVG sibling of `SouthIndianGrid.razor`. **Port the
   geometry from almamesh `NorthIndianChartSVG.tsx`** (§5) — it is already SVG, unit-normalised
   (0→1 square, `HOUSE_CELLS[12]` CCW from H1), MIT, and shares the `showDegrees` / selection /
   theming model. (jyotish-dashboard's `chart.js` is the canvas fallback.) Add **one global
   North/South style switcher** bound to a single store field, in the chart panel's header
   actions.
4. **`tokens.css`:** add a `--planet-*` set (glyph + strength-bar colours; both jyotish-dashboard
   and almamesh carry per-planet colour), and add the missing chart tokens almamesh's `chartTheme`
   names — `cellFill` / `lagnaFill` / `gridStroke` / `signText` / `mutedText` distinct from
   `--paper` / `--ink`. Reconcile `--dignity-*` against jyotish-dashboard's dignity hues.
5. Add an **advanced `[+] Settings` disclosure** to `Add.razor` for ayanamsa / house system —
   inert in v1 (Lahiri + Whole Sign), but the slot exists.
6. **One grid component renders every chart type** (D1/D2/D6/D9/D10/D11): pass a
   `CenterTitle`/`CenterCode` (almamesh pattern) for the empty centre 2×2, and drive glyph
   rendering off a `precision`/`showDegrees` flag — varga cells show the **bare glyph**, never a
   fabricated `0°` (our varga `KeyDetails` rows have `DegreesInSignDisplay = NULL`; key off that).
7. **`Disclosure.razor`** (from almamesh `Disclosure.tsx`): `grid-template-rows: 0fr→1fr` height
   animation, `inert` when collapsed, real `<button aria-expanded>`. Plus a **global "For You" /
   "For Astrologer" content-mode toggle** (almamesh `ContentModeToggle`) for interpretation text.

## Open TODOs

- [ ] Run `jyotish-dashboard` locally (`docker compose up`, or `python run.py`) and screenshot
      into `_research/_ui_refs/_screenshots/jyotish-dashboard/` — the rendered dark-gold UI.
- [x] ~~Review `almamesh` and complete section 5~~ — done 2026-08-30 from source (`frontend/apps/web/src`); not run (live demo at `almamesh.com` if screenshots are wanted later).
- [ ] If AstroSage's *results* report is wanted as reference: grant the browser extension
      permission for `ascloud.astrosage.com`, re-run the form, screenshot the tabbed report.
- [ ] Port **almamesh `NorthIndianChartSVG.tsx`** geometry (`HOUSE_CELLS`, unit-square points)
      into a spike `NorthIndianGrid.razor`; port `SouthIndianChartSVG.tsx`'s `centerTitle` /
      `showDegrees` model into `SouthIndianGrid.razor` so one component serves D1–D11.
- [ ] Spike `Disclosure.razor` (grid-rows 0fr→1fr) + a global content-mode toggle.

---
*Last updated: 2026-08-30*

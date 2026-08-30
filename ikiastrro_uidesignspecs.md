# ikiastrro — UI / Design Specifications

Reference doc for the **ikiastrro web workspace** (`Ikiastrro.Web`, Blazor Server). Living
document — update it when the UI changes. Companions: `ikiastrro_techspecs.md`,
`ikiastrro_calculations.md`, `docs/horoscope_compare.md` (competitor UI research),
`src/Ikiastrro.Web/Components/DESIGN.md` (the one-design-language rule).

The workspace was designed over ~6 mockup iterations with rammyps (Aug 2026); this doc
records the settled result and the reasoning.

---

## 1. Design language

One language, everywhere — **parchment ink on deep indigo, serif display, dark-only**.
Originally built for the D1 chart view; every page matches it, nothing invents its own palette.

- **Dark-only** (rammyps's explicit call, 2026-08-28): there is no `prefers-color-scheme`
  split and no light palette. One theme.
- **Tokens** live in `wwwroot/css/tokens.css` as real `:root` custom properties. Components
  read them via `var(--…)` — **never a raw hex, never a CSS named colour, never an inline
  `<style>` block** in `.razor` markup.
- Every component gets its own **CSS-isolation file** (`Component.razor.css`). Isolation is
  what makes bare `table`/`th`/`td` selectors safe.
- Serif display face: system stack `Georgia, 'Iowan Old Style', 'Palatino Linotype', serif`.
  Body/UI: system sans. Data/dates: `ui-monospace` with `tabular-nums`.
- No CSS framework, no component library — **except Syncfusion Blazor for data
  visualization** (charts, gauges; Community License). Layout, tables, the North/South
  Indian chart diagrams, and the LifeWeeks grid remain hand-rolled SVG/CSS. Syncfusion's
  own theme CSS is scoped and its chart palette is driven from `tokens.css`. Rationale,
  screen-by-screen chart mapping, and NuGet list: `ikiastrro_datavizspecs.md`.
- `dotnet format` before committing.

### Token set (`tokens.css`)

| Token | Value | Use |
|---|---|---|
| `--paper` / `--paper-raised` / `--paper-line` | `#14132a` / `#1c1a3a` / `#322f57` | ground / card / hairline |
| `--ink` / `--ink-soft` / `--ink-faint` | `#ece2cb` / `#b3a9cf` / `#7c7398` | text tiers |
| `--accent` / `--accent-line` | `#e2ad4f` / `#c99a4e` | brass gold — headings, active states |
| `--asc-glow` | `rgba(226,173,79,.16)` | Lagna cell wash |
| `--danger` / `--danger-line` | `#f0564a` / `#c23f35` | destructive actions |
| `--dignity-exalted … --dignity-debilitated` | 7-tier green→red ramp | classical dignity |
| `--aspect-faint` | `#8f87ad` | "aspected by" ghost chips |
| `--house-lagna` / `--house-lagna-fg` | `#e2ad4f` / `#1a1730` | **gold** house-from-Lagna badge |
| `--house-moon` / `--house-moon-fg` | `#c7c8d4` / `#1a1730` | **silver** house-from-Moon badge |
| `--dasha-ketu … --dasha-mercury` | 9-hue categorical (Vimshottari cycle order) | Dasha lord swatches, life-weeks grid |

---

## 2. Chart workspace — `/charts/{id}?tab=<t>`

`Components/Pages/ChartWorkspace.razor` (replaced `ChartDetail.razor` + `ChartView.razor`,
2026-08-30). Tab state is a query param — bookmarkable, back-button-correct. Unknown/absent
`tab` → `your-qualities`.

### Layout (top → bottom)

```
┌ MainLayout nav — "ikiastrro" + guru dedication ─────────────────────────┐
│  identity strip   Name · Born dd MMMM yyyy h:mm tt (offset) · Place ·    │
│                   Lahiri ayanamsha · Whole Sign houses                   │
├────────────────────────────────────────────────────────────────────────┤
│  Planetary positions — D1   (PlanetPositionsTable, full width)          │
│  Planet│Rashi│Degrees│Nakshatra│Nak Lord│Nak Pad│Direction│Dignity│     │
│  Malefic/Benefic│Aspected by                                            │
├──────────────┬───────────────────────────────────────┬─────────────────┤
│ LEFT  25%    │  MIDDLE  50%                           │  RIGHT  25%     │
│ Vimshottari  │  TabBar:                               │  Sade Sati,     │
│ Dasha — 3    │  [Your Qualities] Your Relationships    │  Kantaka &      │
│ levels,      │  Your Career & Wealth                  │  Ashtama —      │
│ selectable   │  ─────────────────────────────────     │  SadeSatiTable  │
│ (opens on    │  D1 · Rasi (static)      ← one chart    │  (date-ordered) │
│ the current  │  <primary varga>          per row       │                 │
│ period)      │  <extra varga> …                        │                 │
│              │  shared dignity + gold/silver legend    │                 │
└──────────────┴───────────────────────────────────────┴─────────────────┘
```

- Row 1 (the 3 columns) is a CSS grid `25% / 1fr / 25%`; stacks to one column below 1100px.
- Middle column: **one South Indian grid per row** (not a 2-up gallery). Row 1 is always
  **D1 (static)**; the rest are the active tab's divisional charts, in order.
- Missing chart type → an inline dashed "`<type>` not computed — run `backfill-charts`" card,
  siblings still render.
- Delete action kept from the old page (`DeleteIconButton` + `ConfirmDialog` modal +
  `BirthDetailDeletionService`).

### Tabs (`Components/Shared/TabBar.razor`)

Real `<a href="?tab=…">` anchors. Active tab = **gold-filled box** (`background: --accent`,
foreground `--house-lagna-fg`, rounded top). Order and charts:

| # | Tab (`?tab=`) | Charts shown (after the static D1) |
|---|---|---|
| 1 | **Your Qualities** *(default)* — `your-qualities` | D6 |
| 2 | **Your Relationships** — `your-relationships` | D9 *(+ empty D7 slot — Saptamsa not computed yet)* |
| 3 | **Your Career & Wealth** — `your-career-wealth` | D10, D2, D11 |

(Life-area → chart mapping is `LifeAreaMap` in Core; Career+Money merged Web-side.)

---

## 3. Components

### `PlanetPositionsTable.razor` (Components/Charts/)
The D1 reference table. Columns: **Planet · Rashi · Degrees (+🔥 if combust) · Nakshatra ·
Nak Lord · Nak Pad · Direction ((R)/(D)/—) · Dignity (dot + label) · Malefic/Benefic ·
Aspected by**. Rows from `ChartViewModel.BuildPlanetRows`.

- **Malefic/Benefic** = the *computed* Parashari functional nature
  (`LagnaFunctionalNature.For(natalLagna, planet)`) for the 7 classical grahas; `—` for
  Ascendant/Rahu/Ketu. Rendered as **`{B|M}{rank}-[{ruled houses}]`**, e.g. Jupiter for an
  Aries Lagna = `B1-[9&12]`, Mercury = `M2-[3&6]`, Saturn = `M3-[10&11]`. The `rank` digit
  comes from `tbl_Dim_LagnaFunctionalNature.Rank` (Raman's ordering) when the book classifies
  that planet; omitted otherwise (Moon on an Aries Lagna → `M-[4]`).
  - Colour: Benefic/Yogakaraka → `--dignity-good`; Malefic → `--dignity-enemy`;
    Neutral → `--dignity-neutral`.
  - *Open point:* the malefic ordering is Raman's (`M1 Mercury / M2 Saturn / M3 Venus` for
    Aries), which differs from the `M1 Venus / M2 Mercury / M3 Saturn` sketch in the original
    request — confirm before locking.
- **Aspected by** = the raw incoming-aspect string (`AspectingPlanets`).

### `DashaTimeline.razor` (Components/Charts/)
Vimshottari Dasha, **three levels, selectable**.
- Mahadasha rows are `<button>`s that toggle their Antardasha list; Antardasha rows toggle
  their Pratyantardasha list (rendered only when that antar has children).
- On first render (`OnParametersSet`, guarded by `_seeded`) it opens the chain containing
  `AsOf` — the current Maha → current Antar → current Pratyantar — and badges "current" at
  each level. Any other Mahadasha can then be opened.
- Colour via `DashaLordColors.CssVar` (`--dasha-*`), plus `DashaLegend`. Data:
  `DashaPeriodsRepository.GetTreeByBirthDetailId` (already carries all 3 levels;
  `DashaPeriodRecord.BuildTree` nests the flat rows).

### `SouthIndianGrid.razor` (Components/Charts/)
The fixed 4×4 sign-position grid, one component for every chart type (D1…D11). Per cell:
- Sign name (full, e.g. "Aries" — not abbreviated), Sanskrit name below (hidden in Compact).
- **Two house-number badges, top-right, stacked:** `--house-lagna` (**gold**) = house counted
  from the chart's Lagna; `--house-moon` (**silver**) = house counted from that chart's own
  natal Moon sign (`MoonSign` param; omitted when not supplied). Both via
  `AstroMath.CountFromSignToSign`.
- Planet glyphs (`Su`/`Mo`/…): dignity dot + `(D)`/`(R)` direction suffix + 🔥 combust icon.
- "Aspected by" strip at the cell foot — dashed ghost chips in `Ma(a)-8` notation
  (`--aspect-faint`), only on cells that receive an aspect. Font bumped up 2026-08-30 so
  `a8-Ma` reads clearly.
- Centre 2×2 cell: chart title + a `CenterMeta` fragment (Lagna sign, Moon sign, Moon's
  nakshatra).

### `SadeSatiTable.razor` (Components/Charts/)
Right-column list of Saturn afflictions from the natal Moon, **all merged into windows and
sorted ascending by start date**. Consolidation in `@code`: the 3 Dhaiya rows of a Sade Sati
cycle collapse into one "Sade Sati N" window; `KantakaShani` / `AshtamaShani` rows likewise
(a > 400-day gap starts a new window — retrograde re-entry gaps are months, the gap to the
next occurrence is ~decades). Columns: **Event (+ a sub-line: "12th → 2nd from Moon" /
"4th from Moon · Aquarius" / "8th from Moon · Gemini") · From (MMM yyyy) · To · Age**.
Active-now window highlighted. Source: `SadeSatiRepository.GetByBirthDetailId`
(`tvf_Chart_SadeSatiPeriods`).

### `MainLayout.razor`
Nav shows **`ikiastrro`** (brass, larger) + "dedicated to my guru Sundari Hemachandran"
(italic subtitle) + Home / Saved Charts links.

---

## 4. Ideas explored and dropped

- **"Life Spiral"** — a radial life-timeline (Saturn's motion as an Archimedean spiral over a
  fixed 4000-week scale, Mahadasha band + Antardasha ticker ring + Sade Sati ribbons, clock
  graduations). Built through several mockup rounds, then removed in favour of the plain
  3-level Dasha column + the date-ordered Sade Sati list. If revived, it belongs where the
  left+right columns are, not as a per-tab element.
- **North Indian chart style** — geometry researched (`docs/horoscope_compare.md`, almamesh
  `NorthIndianChartSVG.tsx`), not built.
- **Malefic/Benefic toggle** (computed heuristic ⇄ Raman book) — prototyped, then simplified
  to computed-only.

---

## 5. Other pages (unchanged this pass)

- `/` `Home.razor` — title "ikiastrro", person dropdown jump-to.
- `/add` `Add.razor` — one sectioned entry form; `HandleSubmit` → `ChartGenerationService.GenerateAll`.
- `/charts` `Charts.razor` — saved-people table, per-row View / Delete.
- `/charts/{id}/life-weeks` `LifeWeeks.razor` — 4000-week grid, Dasha-lord coloured.

Deferred (spec §7–§13 of the life-area recreate): per-varga positions/lordship/significator
tables, the full `FunctionalNaturePanel`, Home card gallery, `/reference/*`,
`/charts/{id}/print`.

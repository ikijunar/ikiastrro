# Chart components — catalog

Hand-rolled SVG / CSS-grid visualizations for the ikiastrro workspace. No
component library draws these; the reusable unit is the **Razor component + its
projection logic**, not a static `.svg` asset (the picture is data-driven).

Rules that keep these revertable across releases — see
`docs/uidesign-dataviz.md` §6:

- **Tokens are additive.** Never repurpose a `--wheel-*` / `--cell-*` token's
  meaning. New look ⇒ new token or a dated value change.
- **Geometry helpers version by addition.** Changing what `AngleToXy` returns
  breaks every past component that used it — add `AngleToXyV2` / a parameter.
- Each visual component has a golden SVG at `docs/artifacts/ui/<Name>-sample.svg`,
  rendered from the shared fixture. It is the arbiter of a faithful revert.

All components: **static SSR, no JS** unless noted. Every `.razor` has a
`.razor.css` isolation file. Colours only via `var(--…)` from `wwwroot/css/tokens.css`.

---

## Visual components

### PolarWheel.razor
360° sidereal longitude ring; one glyph per graha, optional aspect chords.

| | |
|---|---|
| **viewBox** | `0 0 320 320`; outer r=150, inner r=96; centre (160,160) |
| **Projection** | `AngleToXy(lonDeg, r)` — `screenDeg = 180 − lonDeg`; `x = 160 + r·cos(screenDeg)`, `y = 160 − r·sin(screenDeg)`. 0° Aries at 9 o'clock, longitude increases counter-clockwise. |
| **Parameters** | `Points: IReadOnlyList<Point>` *(req)* — `Point(string Label, double LongitudeDegrees, string ColorVar)`; `Chords: IReadOnlyList<Chord>` — `Chord(double FromLongitudeDegrees, double ToLongitudeDegrees, string ColorVar)` |
| **Tokens** | `--paper-raised`, `--wheel-ring`, `--wheel-tick` |
| **Degenerate** | empty Points ⇒ ring only; near-conjunct glyphs overlap (accepted, "same as a paper wheel"); longitudes are not range-checked — caller passes 0–360 |
| **A11y** | `role="img"`, `aria-label="Sidereal longitude wheel"` |
| **Used by** | `ChartFrame` (wheel view) on `Workspace`, `VargaView` |

### SouthIndianGrid.razor
Fixed 4×4 South-Indian sign grid — the primary per-varga chart. Dignity-dot
glyphs, gold house-from-Lagna + silver house-from-Moon badges, Lagna highlight,
2×2 centre info cell, optional "aspected by" strip and special-point labels.

| | |
|---|---|
| **Geometry** | `GridCells` — 12 fixed `(sign, col, row)` tuples; signs never move. Internal `ZodiacName` spelling (`Capricornus`). |
| **House numbers** | computed here via `AstroMath.CountFromSignToSign(AscendantSign, cellSign)` — not passed in |
| **Parameters** | `AscendantSign` *(req)*, `MoonSign?`, `PlanetsBySign: IReadOnlyDictionary<string, IReadOnlyList<GridPlanetGlyph>>` *(req)*, `CenterTitle` *(req)*, `CenterMeta: RenderFragment` *(req)*, `Compact: bool`, `AspectedByGlyphs`, `SpecialPointLabels` |
| **Tokens** | `--paper`, `--ink`, `--dignity-*`, `--house-lagna(-fg)`, `--house-moon(-fg)`, `--asc-glow`, `--aspect-faint` (inherited via DOM, not this component's scope) |
| **Renders** | `<PlanetChip>` per graha |
| **Used by** | `VargaView`, `Workspace` (D1 hero), `ChartFrame` grid view |

### MiniGrid.razor
Glyphs-only thumbnail grid; whole grid optionally a link. **Not**
`SouthIndianGrid(Compact)` — no badges, dots, or centre cell at this size.

| | |
|---|---|
| **Parameters** | `PlanetsBySign: IReadOnlyDictionary<string, IReadOnlyList<string>>` *(req)*, `LagnaSign` *(req)*, `Href?`, `Caption?` |
| **Glyphs** | `ChartViewModel.PlanetGlyph(name)` |
| **Used by** | `VargaRail`, `Home` rows, `SavedCharts` |

### CombinedD1D9Grid.razor
Static D1 ⊕ D9 on one grid: D1 occupants as `PlanetChip`s (inner), a D9 strip
pinned top (outer), special-point codes top-left. Read-only, no dignity/aspect.

| | |
|---|---|
| **Parameters** | `D1KeyDetails: IReadOnlyList<ChartKeyDetail>` *(req)*, `D9KeyDetails` *(req)*, `LagnaSign` *(req)* |
| **Tokens** | `--planet-*` only (identity carried by glyph) |
| **Used by** | `Workspace` |

### VargottamaStrip.razor
Row of graha chips; a chip lights when its Dn sign == its D1 sign (true
Vargottama only for `DnCode == "D9"`; otherwise "same sign as D1").

| | |
|---|---|
| **Parameters** | `D1Grahas: IReadOnlyList<ChartKeyDetail>` *(req)*, `DnGrahas` *(req)*, `DnCode` *(req)* |
| **Tokens** | `--vargottama` (lit chip) |
| **Used by** | `VargaView` |

### ChartFrame.razor
Not a chart — a `?view=grid|wheel` toggle around a grid fragment and a wheel
fragment the page already bound. `[Parameter]`s: `View` *(req)*, `BaseHref`
*(req)*, `GridContent` / `WheelContent: RenderFragment` *(req)*. Renders
`<SegmentedToggle>`.

---

## Tables & panels (same folder, not SVG)

Interactive or tabular; specced in `docs/uidesign-specs.md`, not here.

| Component | Role | Interactive |
|---|---|---|
| `PlanetPositionsTable` | D1 reference table (10 columns) | no |
| `DashaTimeline` | Vimshottari, 3 levels, expandable rows | **yes** (`OnParametersSet` seed); `Compact` Maha-only mode is static |
| `DashaLegend` / `DashaLordColors.cs` | 9-hue Vimshottari lord swatches | no |
| `HouseLordshipTable`, `ConjunctionsTable` | per-varga disclosure tables | no |
| `SadeSatiTable` | merged Saturn-affliction windows, date-ordered | no |
| `GocharaPanel` | current transits from `GocharaRepository` | no |

## Helpers

- `GridPlanetGlyph.cs` — record: planet name + dignity token + retrograde + combust flags, consumed by `SouthIndianGrid`.
- `ChartViewModel.PlanetGlyph(string)` — `Ikiastrro.Core.Presentation`; canonical glyph for a planet name. Shared by every glyphs-only component.

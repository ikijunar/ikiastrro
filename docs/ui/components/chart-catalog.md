---
last_updated: 2026-09-29
workstream: ui
togaf: C — component catalogue
---

# Component — hand-rolled chart catalogue

All chart diagrams are hand-drawn inline SVG / CSS grid — no charting library. Source lives in
`src/Ikiastrro.Web/Components/Charts/`; that folder's `README.md` holds the per-component
projection contract (viewBox, geometry math, tokens).

**Naming & versioning:** chart templates, their spec docs and their code files follow the
project chart-module convention in [`../../../project_standards.md`](../../../project_standards.md)
(§ "Chart modules"). A new look is a **new suffixed name + new spec doc**, never a rewrite in
place — so every version stays documented and revertable.

## Catalogue — chart / spec doc / linked files

Paths are repo-relative. `.razor` implies a sibling `.razor.css` isolation file unless noted.

### Visual chart modules (inline SVG / CSS grid)

| Chart / component                                                                                                                                                           | Spec doc                                                                                    | Linked files                                                                                                                                                                                                                                                |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`SindUniDtlGrid`** — enriched 4×4 sign grid, every chart type (`PlanetChip` glyphs, dual house badges, aspect strip, `SpecialPointLabels`, optional `Argala` overlay 2026-10-16)                      | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md)                      | `src/Ikiastrro.Web/Components/Charts/SindUniDtlGrid.razor` · `…/GridPlanetGlyph.cs` (cell model) · golden `docs/artifacts/ui/SindUniDtlGrid-sample.svg` · consumers `Charts/AllChartsGrid.razor` (itself used by `Pages/AllCharts.razor` and `Pages/AstroFacts.razor` step 7, 2026-09-24), `Pages/VargaView.razor` (via `ChartFrame`), `Pages/Compatibility.razor` (D1 and D9 per person, 2026-10-04) |
| **`SindUni1Grid` / `SindUni2Grid` / `SindUni3Grid`** (SIND-UNI) — one square 4×4 chart in three views (compact, reading, micro): dignity chips, sign and nakshatra lines, BEN/MAL/MIX, Ashtakavarga, houses-from dropdown, aspect / Argala lens, multi-select special lagnas. In build. | [`spec_SIND-UNI_GridChart.md`](spec_SIND-UNI_GridChart.md) | `src/Ikiastrro.Web/Components/Charts/SindUni/` · consumers `Pages/KeyInference.razor` (UNI-3 + special lagnas), `Pages/KarakaPolarWheel.razor` (Spl Lagnas grid view, UNI-3) |
| **`PolarWheel`** — 360° sidereal longitude ring, one glyph per graha, optional aspect chords                                                                                | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md) (grid ⇄ wheel pair)  | `src/Ikiastrro.Web/Components/Charts/PolarWheel.razor` · golden `docs/artifacts/ui/PolarWheel-sample.svg` · consumer `Pages/VargaView.razor` (via `ChartFrame` wheel view)                                                                                  |
| **`PolarGridLagnaSelect`** — switchable polar/South Indian chart with five ordered special-Lagna references | [`spec_KarakaPolarWheelChart.md`](spec_KarakaPolarWheelChart.md) | `src/Ikiastrro.Web/Components/Charts/PolarGridLagnaSelect.razor` · consumer `Pages/KarakaPolarWheel.razor` |
| **`SouthIndianGrid_Micro`** — denser 4×4 sign grid for Astro Facts step 4's Grid view only (Gulika/Maandi, Graha Arudha, Arudha Lagna, Naisargika + Chara Karaka tags gated to D1/D9, house-from-Sun badge, inline aspect tags — five pieces independently toggleable via `PolarGridLagnaSelect`'s Grid-display checkboxes) | [`spec_SouthIndianGrid_Micro.md`](spec_SouthIndianGrid_Micro.md) | `src/Ikiastrro.Web/Components/Charts/SouthIndianGrid_Micro.razor` · `…/MicroPlanetGlyph.cs` (cell model) · golden `docs/artifacts/ui/SouthIndianGrid_Micro-sample.svg` · consumer `Charts/PolarGridLagnaSelect.razor` (Grid view) |
| **`ChartMenu`** + **`PointCatalog`** — multi-select chart dropdown (groups, Find, All/None, presets) and the special-point catalogue (upagrahas, Bhṛgu Bindu/Varṇada, 14 sphuṭas, 36 sahams placed on any varga) that drive the Spl Lagnas Wheel and Grid | [`astro-facts.md`](astro-facts.md) (2026-10-07 Spl Lagnas) | `src/Ikiastrro.Web/Components/Charts/ChartMenu.razor` · `…/PointCatalog.cs` · `…/PolarGridLagnaSelect.razor` (points ring) · `…/SindUni/SindUniGrid.razor` (`ExtraPoints`) · consumer `Pages/KarakaPolarWheel.razor` |
| **`Natal_Transit_Comp_WheelChart`** — four-ring natal ↔ current-transit zodiac wheel (`viewBox 0 0 800 800`, data-driven `NatalPoints` / `TransitPoints` / `AscendantSign`) | [`spec_Natal_Transit_Comp_Wheel.md`](spec_Natal_Transit_Comp_Wheel.md)                      | `src/Ikiastrro.Web/Components/Charts/Natal_Transit_Comp_WheelChart.razor` · golden ⚠️ **not yet minted** (`docs/artifacts/ui/Natal_Transit_Comp_WheelChart-sample.svg`) · consumer `Pages/Natal_Transit_Comp_Wheel.razor`                                   |
| **`D1TemplateGrid`** — dense print-style D1 template (karaka header row, `Dg(±N)` dignity scores, `ASP:` + upagraha rows, own `--tmpl-*` light palette)                     | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md) (§ template page)    | `src/Ikiastrro.Web/Components/Charts/D1TemplateGrid.razor` · `…/D1TemplateCellData.cs` (`TemplatePlanetLine` / `TemplatePointLine` / `TemplateDignityMode`) · consumer `Pages/SouthIndianTemplate.razor`                                                    |
| **`MiniGrid`** — glyphs-only thumbnail grid, whole grid optionally a link                                                                                                   | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md) (§ derived variants) | `src/Ikiastrro.Web/Components/Charts/MiniGrid.razor` · golden `docs/artifacts/ui/MiniGrid-sample.svg` · consumer `Components/Workspace/VargaRail.razor`                                                                                                     |
| **`ChartFrame`** — bookmarkable `?view=grid\|wheel` toggle around a grid + a wheel fragment (not a chart)                                                                   | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md)                      | `src/Ikiastrro.Web/Components/Charts/ChartFrame.razor` · renders `<SegmentedToggle>` · golden `docs/artifacts/ui/ChartFrame-sample.svg` · consumer `Pages/VargaView.razor`                                                                                  |
| **`VargottamaStrip`** — graha chips lit when Dn sign == D1 sign                                                                                                             | [`spec_SIND-UNI-DTL.md`](spec_SIND-UNI-DTL.md)                      | `src/Ikiastrro.Web/Components/Charts/VargottamaStrip.razor` · golden `docs/artifacts/ui/VargottamaStrip-sample.svg` · consumer `Pages/VargaView.razor`                                                                                                      |
| **`LunarPhaseCard`** — Pakṣa, eight-phase Moon label, illumination, and persisted Moon Pakṣa Bala meter | [`spec_LunarPhaseCard.md`](spec_LunarPhaseCard.md) | `src/Ikiastrro.Web/Components/Charts/LunarPhaseCard.razor` · consumer `Pages/AstroFacts.razor` step 2.2 · test `tests/Ikiastrro.Web.Tests/LunarPhaseCardTests.cs` |
| **`LifeWeeks`** — 4000-week grid (52 col/row), `--dasha-*` colours, hover date/lord tooltips                                                                                | — (route only; retired in v2)                                                               | `src/Ikiastrro.Web/Components/Pages/LifeWeeks.razor` (page, not a `Charts/` component) · uses `DashaLegend`                                                                                                                                                 |
| **`SindHovGrid`** — 4×4 South-Indian grid with hover/keyboard preview, pin, and house/special-point relevance highlighting for LifeMatters; `SindUniDtlGrid` visuals plus an optional special-lagna track badge and D1 SAV chip | [`specs_sind_hov_grid.md`](specs_sind_hov_grid.md) | `src/Ikiastrro.Web/Components/Charts/SindHovGrid.razor(.cs/.css)` · golden `docs/artifacts/ui/SindHovGrid-sample.svg` · test `tests/Ikiastrro.Web.Tests/SindHovGridTests.cs` · consumer: LifeMatters page ([`specs_key_inference_page.md`](specs_key_inference_page.md)) |

### Dasha / timeline modules

| Chart / component | Spec doc | Linked files |
|---|---|---|
| **`DashaTimeline`** — Vimśottari 3-level tree; `Compact` Mahādaśā-only mode; opens the current chain on first render (only stateful chart module) | [`dasha-sade-sati.md`](dasha-sade-sati.md) | `src/Ikiastrro.Web/Components/Charts/DashaTimeline.razor` · consumer `Pages/Timing.razor` |
| **`DashaLegend`** + **`DashaLordColors.cs`** — 9-hue Vimśottari lord swatches | [`dasha-sade-sati.md`](dasha-sade-sati.md) | `src/Ikiastrro.Web/Components/Charts/DashaLegend.razor` · `…/DashaLordColors.cs` · consumers `Charts/DashaTimeline.razor`, `Pages/LifeWeeks.razor` |
| **`DashaRoundBox`** — one Sade-Sati / Kaṇṭaka round box | [`dasha-sade-sati.md`](dasha-sade-sati.md) | `src/Ikiastrro.Web/Components/Charts/DashaRoundBox.razor` · consumer `Pages/SouthIndianTemplate.razor` |
| **`SadeSatiTable`** — merged Saturn-from-Moon affliction windows, date-ordered | [`dasha-sade-sati.md`](dasha-sade-sati.md) | `src/Ikiastrro.Web/Components/Charts/SadeSatiTable.razor` · source `tvf_Chart_SadeSatiPeriods` ([`../../database/db_view_catalog.md`](../../database/db_view_catalog.md)) · consumer `Pages/Timing.razor` |
| **`GocharaPanel`** — current transit sign + since / next-change | [`dasha-sade-sati.md`](dasha-sade-sati.md) | `src/Ikiastrro.Web/Components/Charts/GocharaPanel.razor` · source `tbl_PlanetSignTransitEvents` / `tvf_PlanetSignAtDate` ([`../../database/db_view_catalog.md`](../../database/db_view_catalog.md)) · consumer `Pages/Timing.razor` |

### UI tables (MudBlazor + tokens — persisted rows, each bound to a DB view)

Every table below reads a persisted view/TVF; the component ⇄ view binding is documented in
[`../../database/db_view_catalog.md`](../../database/db_view_catalog.md).

| Chart / component | Spec doc | Linked files |
|---|---|---|
| **`PlanetDignityTable`** — canonical Astro Facts 2.2 planetary role/condition table | [`astro-facts.md`](astro-facts.md) | `src/Ikiastrro.Web/Components/Charts/PlanetDignityTable.razor` · consumer `Pages/AstroFacts.razor` |
| **`PlanetPositionsTable`** — positional reference table (`ShowAnalysis=false` in Astro Facts 1.1; full planetary analysis belongs to 2.2) | [`evidence-tables.md`](evidence-tables.md) | `src/Ikiastrro.Web/Components/Charts/PlanetPositionsTable.razor` · view `vw_ChartPlanetEvidence` · consumer `Pages/VargaView.razor` |
| **`HouseLordshipTable`** — per-varga house-lord disclosure | [`evidence-tables.md`](evidence-tables.md) | `src/Ikiastrro.Web/Components/Charts/HouseLordshipTable.razor` · consumer `Pages/VargaView.razor` |
| **`ConjunctionsTable`** — per-varga conjunctions | [`evidence-tables.md`](evidence-tables.md) | `src/Ikiastrro.Web/Components/Charts/ConjunctionsTable.razor` · consumer `Pages/VargaView.razor` |
| Transit landing tables — **D1 Birth** / **Current Transit** (two `MudTabPanel`s on the wheel page) | [`spec_Natal_Transit_Comp_Wheel.md`](spec_Natal_Transit_Comp_Wheel.md) | `src/Ikiastrro.Web/Components/Pages/Natal_Transit_Comp_Wheel.razor` · `src/Ikiastrro.Web/Natal_Transit_Comp_WheelMath.cs` (house/motion math) · `src/Ikiastrro.Data/Natal_Transit_Comp_WheelRepository.cs` · tests `tests/Ikiastrro.Web.Tests/Natal_Transit_Comp_WheelMathTests.cs` · views `vw_ChartPlanetEvidence`, `tbl_TransitPositionReference` (via `GocharaRepository`) |
| **`PlanetStrengthChart`** — Astro Facts 3.1, Shadbala rank/bar table (Performance %-of-minimum or Composition stacked-Bala bar) + per-graha expandable component breakdown | [`astro-facts.md`](astro-facts.md) | `src/Ikiastrro.Web/Components/Charts/PlanetStrengthChart.razor` · view `vw_ChartShadbala` + `tbl_Fact_PlanetaryStrengthComponent` (via `PlanetaryStrengthRepository.GetSummaryByBirthDetailId`/`GetComponentsByBirthDetailId`) · consumer `Pages/AstroFacts.razor` |
| **`HouseStrengthChart`** — Astro Facts 3.2, Bhava Bala rank/bar table (House order/Strength rank toggle) + per-house expandable component breakdown | [`astro-facts.md`](astro-facts.md) | `src/Ikiastrro.Web/Components/Charts/HouseStrengthChart.razor` · view `vw_ChartBhavaBala` + `tbl_Fact_BhavaStrengthComponent` (via `BhavaStrengthRepository.GetSummaryByBirthDetailId`/`GetComponentsByBirthDetailId`) · consumer `Pages/AstroFacts.razor` |
| **`RasiNakshatraTable`** — Astro Facts 2.3, one row per graha (+ Lagna): Sign/Nakshatra/Lord Relation/Combined Character, expand-to-reveal the remaining narrative fields | [`astro-facts.md`](astro-facts.md) | `src/Ikiastrro.Web/Components/Charts/RasiNakshatraTable.razor` · table `tbl_Rule_RasiNakshatraCombination` (via `RasiNakshatraCombinationRepository.GetAll`) · consumer `Pages/AstroFacts.razor` |

### Shared helpers

| Helper | Role | File |
|---|---|---|
| `ChartViewModel.PlanetGlyph(string)` | canonical glyph for a planet name — every glyphs-only module | `src/Ikiastrro.Core/Presentation/ChartViewModel.cs` |
| `BhavaTile`, `PlanetCard`, `LordChain` (+ `LordNature`) | AF-v10 building blocks: a house with its argala force meter, a planet with alertness ring and nature edge, the planet-to-lords chain | `src/Ikiastrro.Web/Components/AstroV10/` · page `Pages/AFv10.razor` · spec [`../af-v10-design.md`](../af-v10-design.md) · tests `AstroV10ComponentTests.cs` |
| `GridPlanetGlyph` (record + component) | one dignity-dot glyph inside a `SindUniDtlGrid` cell | `src/Ikiastrro.Web/Components/Charts/GridPlanetGlyph.cs` |
| `AstroMath.CountFromSignToSign` | whole-sign house count — `SindUniDtlGrid`, `D1TemplateGrid` | `src/Ikiastrro.Core/Engines/Astronomy/` |

## Golden-snapshot flow

Every visual component commits `docs/artifacts/ui/<Component>-sample.svg`, rendered from one
fixed fixture (never changed) so a diff is a pure rendering change. Harness:
`tests/Ikiastrro.Web.Tests` (bUnit `ChartSnapshotTests` + `ChartFixture` + `SnapshotAssert`),
run from VS Test Explorer or `dotnet test`. Mint / update with env
`IKIASTRRO_UPDATE_SNAPSHOTS=1`; review the SVG diff before committing. Full flow:
[`../../artifacts/ui/README.md`](../../artifacts/ui/README.md).

Outstanding: `Natal_Transit_Comp_WheelChart` has **no golden yet** — add a `[Fact]` to
`ChartSnapshotTests` and mint one.

## Revert

`git checkout <ref> -- src/Ikiastrro.Web/Components/Charts/<C>.razor <C>.razor.css`, render,
diff against `git show <ref>:docs/artifacts/ui/<C>-sample.svg`. Byte-match ⇒ done; mismatch ⇒
a token or geometry helper moved — reconcile those (they version by addition, so this is rare).

# ikiastrro — Architecture & Current Implementation

> Internal engineering reference: current architecture, stack, data layer, and known
> limitations. The public-facing project overview is in [`README.md`](README.md).
> _(Formerly this repo's `README.md`; moved here 2026-08-30 when a public README was added.)_

Vedic astrology app (**ikiastrro**; code namespace `Ikiastrro.*`, database `ikiastrro`).
Takes standard birth details (DOB, time, place), computes and stores **21 position chart
types** — D1, D2, D2-US, D3–D60 (the Shodashavarga plus the JHora-extra vargas) —
plus Vimshottari Dasha, classical dignity, house-lordship, conjunctions, aspects,
retrograde/combustion, Sade Sati and Lagna functional benefic/malefic — via a CLI and a
Blazor Server web workspace. (Renamed from `vedic_horo_gen` / `VedicHoroGen` on 2026-08-30.)

## Reference specs

> **Full doc index:** [`master_ikiastrro.md`](master_ikiastrro.md) — every project `.md`, grouped by category, with purpose / path / date / status. The naming convention is STANDARDS §M.1.

| Doc | What it's for |
|---|---|
| `docs/rationale-techstack.md` | **Why** the stack is what it is — .NET/C#, SwissEphNet, SQL Server, Blazor, star-schema, Python-for-comparison, why-not-just-JHora. The reasoning behind the choices `techstack-*` describes factually |
| `docs/techstack-overview.md` | Verified stack snapshot — pinned package versions, per-project dependency lists, checked against the `.csproj` files |
| `docs/techstack-details.md` | Architecture, projects, data layer, DB, build/run, verification |
| `docs/dbdesign-star-schema-rules-engine.md` | Database design — the versioned `tbl_Dim_*` / `tbl_Rule_*` / `tbl_Fact_*` layer (Phase 1) and its open Phase 2 |
| `docs/uidesign-specs.md` | Web workspace design — layout, design tokens, every component, the decisions behind them |
| `docs/uidesign-dataviz.md` | Charting stack — Syncfusion Blazor pick + rationale, screen-by-screen chart mapping, palette reconciliation, NuGet list |
| `docs/reference-calculations.md` | Every astrological calculation: ayanamsha, houses, dignity, vargas, Dasha, combustion, retrograde, Sade Sati, functional nature — with sources |
| `docs/scope-requirements-gap.md` | Current requirements gap vs. a full JHora natal export — what's still to compute/store/present |
| `docs/scope-jhora-coverage.md` | Block-by-block coverage of a JHora export (~7 of ~35 blocks) with build tiers |
| `docs/scope-nakshatra-linkage-divisional-charts.md` | Product scope for the nakshatra-linkage + divisional-chart work |

## Related documents

This document covers current architecture only. Other docs cover history, process, and design —
project-scoped design/scope/coverage docs live in `docs/` here, short decision records (ADRs)
in `decisions/`; the history and workspace-wide docs sit at `D:\@ClaudeSpace`. Read the one
that matches what you need rather than re-deriving it:

| Doc | What it's for |
|---|---|
| `ikiastrro.md` | Full running build/decision history, dated sections, every session's "what changed and why" |
| `methods_prodmag.md` | Product-management process (Vision → JTBD → Opportunity Backlog → ICE scoring → Now/Next/Later roadmap) — read before picking the next feature |
| `docs/scope-bhava-coverage.md` | Scorecard of what's built vs. missing against the classical 8-point Bhava-analysis checklist — the framework behind several current backlog items |
| `docs/reference-vedic-data-tables.md` | Design doc for the classical reference/master-data tables (`tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras`/Padas/SubLords, `tbl_PlanetSignTransitEvents`) |
| `docs/dbdesign-star-schema-rules-engine.md` | Design doc for the versioned `tbl_Rule_*` rules-engine layer (Phase 1) and its still-open Phase 2 (wiring calculators to read from it) |
| `decisions/001-star-schema-rules-engine.md` | ADR / decision record behind the above — the context, the star-schema decision, and its consequences in one page (the `docs/` file is the implementation plan) |
| `docs/reference-house-lagna-significations.md` | Design/action-plan doc for house+planet significations, Sthira Karaka mapping, and Lagna functional benefic/malefic — sourced from `BookExtracts\how-to-judge-a-horoscope-1.md`, migration 030 (031's Raman mirror table was removed 2026-08-31) |
| `docs/research-horoscope-software-compare.md` | UI/UX & feature comparison of existing Vedic-astrology software (VedAstro, AstroSage, jyotish-dashboard, …) — what to borrow / avoid for `Ikiastrro.Web`, mapped to our components. Reference repos + screenshots under `_research/` (git-ignored) |
| `docs/techstack-overview.md` | Verified stack snapshot — pinned package versions and per-project (`Core`/`Data`/`Cli`/`Web`) dependency lists, checked against the `.csproj` files. The `## Stack` section below is the short version |
| `docs/research-topic-coverage.md` | Topic-research master — is a classical technique's raw data captured, and where are the gaps. Topic 1: planetary roles (Naisargika/Chara Karaka, Functional/House/Nakshatra Lord) + Avastha states (Baaladi/Jagradadi/Deeptadi/Lajjitadi/Sayanadi). D2 diagrams under `docs/research/` |
| `STANDARDS.md` | Workspace-wide naming/structure conventions (DB/table/view/proc naming, migration numbering) this project follows |

## Stack
- .NET 8 / C#, 4-project solution (Core / Data / Cli / Web)
- Calculation engine: [SwissEphNet](https://www.nuget.org/packages/SwissEphNet) — a managed C# port of Astrodienst's Swiss Ephemeris, Moshier analytical mode (no ephemeris data files to bundle), embedded NuGet package
- Database: MS SQL Server, Windows Auth, `localhost` default instance
- Ayanamsha: Lahiri (Swiss Ephemeris's own `SE_SIDM_LAHIRI` sidereal mode, applied directly — no correction layer needed) · House system: Whole Sign
- Data visualization: **Syncfusion Blazor** (Charts / Gauge, Community License) — the one sanctioned component library, for strength bars / heatmaps / Dasha timelines / polar longitude wheel only; chart *diagrams* (South/North Indian grids, LifeWeeks) stay hand-rolled SVG. Decision 2026-08-31, `docs/uidesign-dataviz.md`; not yet wired into `src/`.

## Place resolution

City/Country is geocoded to lat/long via [OpenStreetMap Nominatim](https://nominatim.openstreetmap.org/) (free, no API key). UTC offset is then resolved entirely offline from lat/long + birth date via `GeoTimeZone` + `TimeZoneConverter`, so historical DST/offset rules are respected (not just today's offset). If geocoding fails (unreachable, or the place name isn't found), the CLI falls back to prompting for latitude/longitude/UTC-offset manually.

*(v1 originally used VedAstro's own `GeoLocation.FromName`, which chains through 4 providers — its own hosted API → Azure Maps → Google → local file — and silently returns `(0,0)` instead of throwing when all four fail. That proved unreliable in testing and was replaced with the single, directly-testable Nominatim resolver above.)*

## Setup

1. **Create the database** (one-time). Database name is `ikiastrro`.
   - **Fresh machine:** `sqlcmd -S localhost -E -C -i db\ikiastrro.sql`
     — one consolidated script: whole schema + all reference/master data + the
     LifeCalendar dimension. (Replaces the old `db\001..034` migration chain, kept
     under `db\_archive\` for history.)
   - **Existing `vedic_horo_gen` database:** `sqlcmd -S localhost -E -C -i db\00_rename_db_to_ikiastrro.sql`
     — renames it in place, keeping all saved data.
2. **Run the CLI**:
   ```
   dotnet run --project src\Ikiastrro.Cli
   ```
   It prompts for Name, DOB, Time of Birth, an optional corrected time (+ reason), and Place of Birth (City/Country). It then resolves lat/long/UTC-offset, computes **all 21 position chart types (D1, D2, D2-US, D3–D60)** (+ Vimshottari Dasha) via `ChartGenerationService` (see below), stores everything, and prints the results. One-off modes: `-- compute-all <name>` regenerates every chart type + Dasha for one saved person (post birth-time correction); `-- backfill-charts` adds any missing chart type to people saved before it existed; `-- backfill-analytics` / `-- recompute-keydetails` re-derive the 4 analytics tables for every calculable chart type (these two are now identical code paths); `-- verify-vargas` / `-- verify-functional-nature` / `-- verify-avastha` run the worked-example assertion suites (no unit-test project).
3. **Or run the web app**:
   ```
   dotnet run --project src\Ikiastrro.Web
   ```
   Same input flow, plus a native visual D1 chart display and a saved-charts browser — see below.

## Web app: entry form, native D1+D9 charts, saved charts (2026-08-24)

`Ikiastrro.Web` (Blazor Server) is more than a form-in/JSON-out shell — the charts render natively, entirely in .NET, no external service involved:

- **`D1ChartView.razor`** — D1 (Rasi) and D9 (Navamsa) South Indian-style grids side by side (`SouthIndianGrid.razor`, a shared component both use — plain CSS grid, no slider/carousel, stacks to one column below 760px), one shared dignity legend, and the D1 planetary-positions/house-lordship tables below, all rendered in Razor + CSS-isolated component styles (`D1ChartView.razor.css` / `SouthIndianGrid.razor.css`, scoped so bare `table`/`th`/`td`/`.chart`/`.cell` rules can never leak into the rest of the app). This is the same visual design as the charts previously hand-published as Claude Artifacts (one-off static HTML snapshots, listed in `ikiastrro.md`) — now built directly into the app, so no external publishing step is needed to see a chart. Supports `prefers-color-scheme: dark` automatically. The Conjunctions table is intentionally **not** shown on this page (rammyps's call, 2026-08-24) — the data and `tbl_Chart_Conjunctions` table are untouched, just not rendered here.
- **`D1ChartViewModel.BuildPlanetRows`** (Core) derives the "Rules"/outgoing "Aspects" columns from the already-stored `tbl_Chart_HouseLords`/`tbl_Chart_Aspects` rows in plain C# — the Blazor UI doesn't depend on `vw_Chart_Consolidated` at all, only the same per-entity repository methods (`GetByChartResultId` etc.) already used elsewhere.
- **Name field autocomplete** on the entry form (`Home.razor`) — typing 2+ characters queries `BirthDetailsRepository.SearchByNamePrefix` live and shows matching saved people (name, DOB, place) in a dropdown. Selecting one auto-fills the whole form from that stored record and switches the submit button to "View Chart": submitting then **fetches and displays the already-stored chart** (no re-insert, no re-geocode, no recompute) — distinct from typing a brand-new name, which still runs the full generate-and-store flow. Typing past what was selected drops back into "new entry" mode automatically.
- **`/charts`** — lists every saved person (Name, DOB, Place), a "View chart" link to **`/charts/{id}`** (renders `D1ChartView` for that person read straight from the DB), and a **Delete** action (inline confirm, no `window.confirm`) that removes the person and every chart artifact derived from them — see "Deleting a saved chart" below. `/charts/{id}` has the same delete action at the bottom of the page.

## Calculation engine: Swiss Ephemeris via SwissEphNet (replaced VedAstro.Library, 2026-08-24)

v1 originally embedded `VedAstro.Library` (NuGet 1.2.0). Real testing surfaced four confirmed defects in that library over the course of this project: a `CacheManager` version-fragility crash, a quadrant-house-cusp bug in its "Whole Sign" house lookup, a wrong Ketu (South Node) longitude, and a systematic ~1.456° ayanamsha error requiring an in-code correction layer (`AyanamshaCorrection`, since removed). The `CacheManager` issue also turned out to be a **structural** blocker, not just a CLI-pinning workaround: it made the calculation impossible to run at all inside the Blazor Server web host, since ASP.NET Core's shared framework forces a newer `Microsoft.Extensions.Caching.Memory` than VedAstro's internals could tolerate, with no supported override.

**Replaced entirely with [SwissEphNet](https://www.nuget.org/packages/SwissEphNet)** — a direct managed C# port of Astrodienst's actual Swiss Ephemeris (the same precision source behind JHora, Parashara's Light, and Jyotish Dashboard), using its Moshier analytical mode (`SEFLG_MOSEPH`): no external ephemeris data files to bundle/configure, ~1 arcsecond accuracy. Sidereal (Lahiri) longitudes are requested directly via `SEFLG_SIDEREAL` + `SE_SIDM_LAHIRI` — Swiss Ephemeris's own authoritative ayanamsha implementation — so there's no separate correction step anymore; `AstronomicalCalculator`/`AyanamshaCorrection`/`VedAstroTimeFactory` are all gone. All the classical math that used to lean on VedAstro's longitude-to-sign/nakshatra/navamsa helpers (which baked in the same tainted ayanamsha) is now this project's own `AstroMath` — pure, dependency-free arithmetic on a longitude.

- **Rahu/Ketu**: uses the **mean node** (`SE_MEAN_NODE`), not the true/oscillating node — verified against the project's test chart that mean node matches Prokerala/AstroSage to ~0.6 arcmin, vs. ~6.8 arcmin off using the true node, confirming mainstream Vedic tools key off the mean node. Ketu is still derived as `Rahu + 180°` — not a workaround this time, just standard practice, since Ketu isn't a real body Swiss Ephemeris (or anything else) can compute directly.
- **Verified**: re-ran the project's established test chart (22 Apr 1981, 5:30 AM, Chennai) — all 10 points (Ascendant + 7 planets + Rahu + Ketu) land within **~0.4–1.0 arcminutes** of both Prokerala and AstroSage, on par with or slightly better than the old corrected-VedAstro numbers. House placements, nakshatra/pada, dignity, and aspects all reproduce every previously-verified fact exactly (Moon in Anuradha pada 2, Rahu house 4, Ketu house 10, Sun exalted in Aries, Moon debilitated in Scorpio, Mars Moolatrikona, Rahu/Ketu mutually aspecting via the 7th).
- **Blazor blocker resolved**: the web form now completes a full calculate-and-save round trip (confirmed via a live browser test) — the `CacheManager` crash that made this structurally impossible under VedAstro is gone, since SwissEphNet has no such version-sensitive reflection into `MemoryCache` internals.
- Dependency pins that existed solely to work around VedAstro's `CacheManager` conflict have been removed: `Microsoft.Data.SqlClient` is back on current stable (6.0.2), and the `Microsoft.Extensions.Caching.Memory` 6.0.1 pin (with its [GHSA-qj66-m88j-hmgj](https://github.com/advisories/GHSA-qj66-m88j-hmgj) advisory) is gone entirely — nothing in this project pins that package anymore.

## Known limitations (v1 scaffold)

- **`ChartView.razor` is still hardcoded to two charts (D1 + D9)**, not a generic loop over every registered `IChartCalculator` — unlike the write path (§ Extending later), which already handles any chart type with no new code. **D2 (Hora), D6 (Shashtamsa), D10 (Dasamsa), D11 (Rudramsa) were added 2026-08-30 at the DB/CLI level** — they get full analytics + nakshatra linkage for every saved person, but have **no Web UI**. **This is being addressed by the Web UI life-area recreate** — spec `docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md`, Plan 1 (Core/Data/CLI groundwork) merged 2026-08-30 (`ChartViewModel` rename, `ChartGenerationService`, `GetByBirthDetailId` batch reads, `LifeAreaMap`, `LagnaFunctionalNature` + migration 031), Plan 2 (the actual UI — 5 life-area/Timing tabs, generic `VargaChartPanel`, per-planet colour backgrounds, reference browser, print view) not yet started. Carry-forward: `docs/superpowers/plans/groundwork-outcomes-for-plan-2.md`.
- **Varga degree data is now stored; sign-only *display* is still the UI's choice** — as of the divisional-chart completion (Plan A, 2026-09-01) every varga `tbl_Chart_KeyDetails` row carries `VargaLongitudeDegrees` (`Normalize(realLon × N)`) and un-gated `DegreesInSignDecimal`/`Display` (= `VargaLongitudeDegrees mod 30`, the true within-sign varga degree). `tbl_ChartResults` also carries numeric `AyanamshaDegrees` / `SiderealTimeHours` / `VargaMethod`. A classical varga chart still conventionally *shows* sign only — that stays a rendering decision — but the DB is now complete (every computed value has a typed column), which the Python personality-comparison layer depends on. Still D1-only by nature: `Nakshatra`/`NakshatraPada` (a varga cell is a discrete bucket) and conjunction `DegreeSeparation`.
- **D81 / D108 / D144 / D150 → Plan B** — the chart-composition mechanism (a varga *of* a varga) plus D150 are deferred to a not-yet-written Plan B; see `docs/superpowers/specs/2026-08-31-divisional-chart-completion-design.md` §1/§6.
- **Swiss Ephemeris license**: Swiss Ephemeris (and by extension SwissEphNet) is AGPL/commercial dual-licensed. Fine for this private, non-distributed tool; would need revisiting (AGPL compliance or a commercial license) if this project were ever distributed or hosted as a public service.
- **`AstroMath`'s linear precession is not in play anymore** — Lahiri ayanamsha now comes straight from Swiss Ephemeris's own `SE_SIDM_LAHIRI`, so there's no approximation-error residual to track here (unlike the old `AyanamshaCorrection`, which had a documented ~36 arcsec/year drift risk). Any future calculator needing a longitude should call `SwissEphemerisProvider.GetSiderealPositions()` and derive everything else via `AstroMath` — never reach for a raw tropical longitude and subtract an ayanamsha by hand.

## Key-details, house-lordship, conjunctions, and aspects — shared across every chart type

Four tables, populated automatically alongside **every** chart type this project computes (all 21 position chart types — D1, D2, D2-US, D3–D60), not just D1:

- **`tbl_Chart_KeyDetails`** — one row per planet (incl. Ascendant) per chart. Raw position: `NirayanaLongitudeDegrees`, `VargaLongitudeDegrees` (`Normalize(realLon × N)`; == `NirayanaLongitudeDegrees` for D1), `EclipticLatitudeDegrees`, `SpeedLongitudeDegPerDay` (deg/day; negative = retrograde), `IsRetrograde`, `Sign`, `DegreesInSignDecimal`/`Display` (populated for every chart — the within-sign varga degree, = `VargaLongitudeDegrees mod 30`), and the nakshatra block (D1 only). Latitude/speed are real-body values, populated for D1 and every varga alike (like `IsRetrograde`); the Ascendant row leaves them null. Plus classical dignity (own sign, exaltation/debilitation sign, Moolatrikona sign+range, the current sign's lord, and `DignityStatus` — the full Panchadha Maitri scale: Exalted / Moolatrikona / Own Sign / Great Friend / Friend / Neutral / Enemy / Great Enemy / Debilitated), house (all 3 reckonings), combustion, and aspecting planets. Column order is grouped: keys → Planet → longitude/latitude/speed/retrograde → sign/degree → nakshatra → house → dignity → combustion → aspects.
- **`tbl_Chart_HouseLords`** — one row per house (1-12) per chart: which sign occupies it, who rules that sign, and where that ruler actually sits in this chart (house, sign, dignity) — e.g. "Lord of the 5th house (Sun) sits in House 1, Exalted."
- **`tbl_Chart_Conjunctions`** — one row per pair of grahas (the 9 planets; Ascendant excluded) sharing the same sign in this chart, with `DegreeSeparation` (0–180°, **D1 only** — see below) showing how tight the conjunction is.
- **`tbl_Chart_Aspects`** — one row per directional aspect (Graha Drishti): every graha casts a full aspect on the 7th house from itself; Mars additionally casts on the 4th & 8th; Jupiter on the 5th & 9th; Saturn on the 3rd & 10th. `AspectedTarget` can be a graha name or `"Ascendant"`. **Rahu/Ketu use the Jupiter-style convention (5th/7th/9th)** per rammyps's explicit decision (2026-08-24) — their aspect rule, like their exaltation/debilitation, is genuinely disputed across classical texts.

**Avasthas (star-schema, 2026-08-31):** `tbl_Fact_PlanetAvastha` (Fact) holds each planet's
**Baaladi** (D1 only — the "age" from within-sign degree) and **Jagradadi** (every chart type
— the "waking state" from dignity) avastha, written by `PlanetAvasthaComputer` alongside the 4
tables above. Vocabulary in `tbl_Dim_AvasthaState`; the degree bands / dignity map are
`RuleSetId`-scoped `tbl_Rule_BaaladiState` / `tbl_Rule_JagradadiState`. Surfaced on
`vw_Chart_Consolidated` (`BaaladiState`, `BaaladiEffectFraction`, `JagradadiState`). Deeptadi /
Lajjitadi / Sayanadi are follow-on slices — see `docs/research-topic-coverage.md`.

All 4 tables carry a `ChartType` column (`"D1"`, `"D9"`, ...) alongside `ChartResultId`/`BirthDetailId`, so any of them read standalone without a join to `tbl_ChartResults`. (They also used to carry a denormalized `Name` copy of `tbl_BirthDetails.Name` the same way; dropped 2026-08-28 — schema-review finding, nothing read it and nothing kept it in sync, see the dated history for the full list of that day's fixes.)

**Originally D1-only** (`tbl_D1Chart_*`), **generalized 2026-08-24** so D9 gets the exact same tables/analytics instead of a bespoke copy — see "Extending later" below for why this was possible with almost no new code. Two fields are gated to D1 specifically, since they're only meaningful for a continuous-degree rasi chart, not a discrete varga-sign bucket:
- `DegreesInSignDisplay`/`DegreesInSignDecimal` (`tbl_Chart_KeyDetails`) — `NULL` for D9 and any future divisional chart.
- `DegreeSeparation` (`tbl_Chart_Conjunctions`) — `NULL` for D9; two grahas "conjunct" in the same D9 sign can sit far apart in real longitude, so a raw longitude difference wouldn't mean "tightness" there.

`NirayanaLongitudeDegrees` stays populated (`NOT NULL`) for every chart type regardless — it's the same real ecliptic longitude the varga sign was derived from, not a per-chart-type value.

### Nakshatra reference linkage (2026-08-30, migrations 032–034)

`tbl_Chart_KeyDetails` links to the classical nakshatra reference tables:

- **`NakshatraId`** → FK `tbl_Nakshatras`, **`NakshatraPadaId`** → FK `tbl_NakshatraPadas`,
  **`NakshatraSubLordPlanet`** (KP level-2 sub-lord, a planet-name string like `SignLordPlanet`).
- Gating (same rule as `NakshatraLordPlanet` vs `Nakshatra`/`NakshatraPada`): nakshatra identity
  + its lord + its sub-lord are real-longitude facts → populated for **D1 and every varga**;
  `NakshatraPadaId` and the display `Nakshatra`/`NakshatraPada` are **D1-only**.
- `tbl_Chart_KeyDetails.Nakshatra` now stores the canonical `tbl_Nakshatras.NakshatraName`
  spelling (was VedAstro-style `Aswini`/`Pushyami`/`Sravana`/…), sourced from
  `AstroMath.NakshatraCanonicalNames`. Run `-- recompute-keydetails` after deploying.
- **`tbl_Nakshatras`** gains `PrimaryRasiId` (sign of the nakshatra's midpoint) +
  `StraddlesSignBoundary` (the 9 nakshatras whose padas cross a sign boundary).
- **`vw_Chart_HouseNakshatraSpan`** — per chart, per house (1–12): the Rāśi and the 9
  nakshatra-padas spanning it (House → Rāśi → Nakshatra, house-centric). Joins on
  `tbl_SignAttributes.ZodiacEnumValue` (the enum form, e.g. `Capricornus`), not `SignName`.

### Divisional charts D2/D6/D10/D11 (2026-08-30)

D2 (Hora — wealth), D6 (Shashtamsa — health), D10 (Dasamsa — career), D11 (Rudramsa — gains),
each an `IChartCalculator` pair mirroring the D9 one, registered in
`ChartCalculationOrchestrator.CreateDefault()`. Formulas transcribed **verbatim from PyJHora**
(`naturalstupid/PyJHora`, `chart_method=1` "Traditional Parasara"); D2 uses the classical
two-sign Hora (Leo/Cancer only), *not* PyJHora's `method=1` 12-sign "Uma Shambu" variant. New
`AstroMath.GetHoraSign` / `GetShashtamsaSign` / `GetDasamsaSign` / `GetRudramsaSign`, each with
worked examples in its doc-comment and asserted by the `verify-vargas` CLI mode. **No schema
change** — `tbl_ChartResults` + the 4 analytics tables are already chart-type-generic. D11's
formula (Sanjay Rath, the PyJHora default) is the one method-ambiguous piece — a one-line change
in `GetRudramsaSign` if it ever needs the Raman variant.

**`ClassicalDignity.cs`/`ClassicalRelationships.cs` are pure classical reference-table lookups** (own signs, exaltation/debilitation, Moolatrikona ranges, Naisargika Maitri, aspect house-offsets) applied to the signs this project's own `AstroMath`/`SwissEphemerisProvider` pipeline computes, plus Tatkalika Maitri (temporary friendship) computed from sign distance — no dependency on any external library's relationship helpers, and chart-type-agnostic by construction (they only need "which sign is this planet in").

**Rahu/Ketu convention**: exaltation/debilitation is genuinely disputed across classical texts. This project uses the Parashari convention (Rahu exalted Taurus/debilitated Scorpio, Ketu exalted Scorpio/debilitated Taurus), per rammyps's explicit decision (2026-08-24). Rahu/Ketu aren't part of the Naisargika Maitri table in standard Parashari texts, so their `DignityStatus` is limited to Exalted/Debilitated/Neutral — no fine-grained friend/enemy tiering.

**Sanity-checked, not just run**: Rahu and Ketu mutually aspect each other via the 7th in every test chart, in both D1 and D9 (they're always exactly opposite, so this must always hold) — a useful built-in consistency check on the whole pipeline.

## Lagna/Sun/Moon house reckoning

Both `tbl_Chart_KeyDetails` and `tbl_Chart_HouseLords` compute house placement three ways, not just from the Ascendant:
- **`...FromLagna`** — Whole Sign from the Ascendant (the default/original reckoning)
- **`...FromSun`** — Whole Sign from the Sun's sign (Surya Lagna)
- **`...FromMoon`** — Whole Sign from the Moon's sign (Chandra Lagna) — the most commonly used alternate reckoning in practice

## Core table names

`BirthDetails` → **`tbl_BirthDetails`**, `ChartResults` → **`tbl_ChartResults`** (renamed 2026-08-24, migration `008_rename_and_empty_core_tables.sql`) — matching the `tbl_` prefix already used by the analytical tables.

The 4 analytical tables were themselves renamed from `tbl_D1Chart_*` to `tbl_Chart_*` (also 2026-08-24, migration `010_generalize_chart_analytical_tables.sql`) when they were generalized to cover every chart type, not just D1 — see the section above.

## Consolidated view

`vw_Chart_Consolidated` (SQL view, renamed from `vw_D1Chart_Consolidated` in the same 2026-08-24 generalization) joins `tbl_BirthDetails` + `tbl_ChartResults` + `tbl_Chart_KeyDetails` + `tbl_Chart_HouseLords` + `tbl_Chart_Conjunctions` + `tbl_Chart_Aspects` into one wide, human-readable row per planet **per chart type** — name/DOB/place, `ChartType`, sign, degree, nakshatra/pada, all three house reckonings, full dignity, sign-lord, which house(s) that planet itself rules, who it's conjunct with, who it aspects, and who aspects it (all via correlated `STRING_AGG`/`OUTER APPLY`). `SELECT * FROM vw_Chart_Consolidated WHERE Name = 'someone' AND ChartType = 'D9'` gives the complete picture with no manual joins.

## Vimshottari Dasha + life-in-weeks grid (2026-08-27)

Every new entry now also computes **Vimshottari Dasha** (Mahadasha → Antardasha → Pratyantardasha,
3 levels) alongside D1/D9 — `VimshottariDashaCalculator` (Core, pure computation) +
`VimshottariDashaService` (Data, write path). It's deliberately **not** an `IChartCalculator` — that
interface is planet-position/house shaped, and Dasha has no such shape — but it's still logged as a
`tbl_ChartResults` row (`ChartType="VimshottariDasha"`) for delete-cascade/listing consistency.

- **Storage**: `tbl_Chart_DashaPeriods` (self-referencing via `ParentDashaPeriodId`, one row per
  period at any level) + `tbl_Dim_LifeCalendar` (a shared, age-relative date dimension — one row per
  day-offset-from-birth, sliceable by week/month/year — reusable across every person). Reporting layer:
  `vw_Chart_DashaTimeline` (readable period listing) and `tvf_Chart_LifeWeeks(@BirthDetailId)` (one row
  per week 1-4000 with the active lord at all 3 levels).
- **Algorithm**: all 3 levels are simultaneously partial at birth (not just the Mahadasha — a common
  shortcut bug this deliberately avoids). Real dates use 365.2425 days/year (standard solar year).
  Verified against the project's reference chart — Moon's nakshatra (Anuradha) correctly yields Saturn
  as the birth Mahadasha lord, all classical year-durations match exactly, and `tvf_Chart_LifeWeeks`
  agrees with the CLI's independently-computed tree end-to-end.
- **CLI**: `compute-dasha <name>` (recompute + print, e.g. after a birth-time correction),
  `show-dasha <name>` (print already-stored, no recompute), `backfill-dasha` (like
  `backfill-analytics`, for people saved before this feature existed).
- **Web**: a Dasha section on `/charts/{id}` (Mahadasha+Antardasha always shown; Pratyantardasha shown
  only for the current Antardasha; the active period at every level is marked, not just colored) and
  `/charts/{id}/life-weeks` — a 4000-week life-calendar grid (52 columns/row), colored by Mahadasha
  using a validated 9-color categorical palette (`tokens.css`, `--dasha-*`), with hover tooltips for
  exact dates/lords. "4000 weeks" (~76.9 years) is a display framing only — Dasha itself is computed
  out to the full classical 120-year cycle regardless.
- **Product-management process**: this was the first feature picked via an explicit PM process
  (Vision → JTBD → Opportunity Backlog → ICE scoring → roadmap) rather than ad hoc — see
  `D:\@ClaudeSpace\methods_prodmag.md`.

Full build/decision history (scope decisions, the SQL review that caught a real join bug before it
ran, palette-validation detail): see the dated section in `ikiastrro.md`.

## Deleting a saved chart

`BirthDetailDeletionService` (Data) deletes a `tbl_BirthDetails` row and everything derived from it — every chart type at once, since the 4 analytical tables are shared and already scoped by `BirthDetailId`. FK-safe order: the 4 analytical tables (leaves) → `tbl_ChartResults` → `tbl_BirthDetails`. Exposed in the Web UI via `/charts` (row-level Delete, inline confirm) and `/charts/{id}` (bottom-of-page Delete) — both use a plain two-button inline confirm, not a JS `confirm()` dialog. Nothing about the tables/columns/repositories themselves is touched; only the targeted person's rows are removed.

## Extending later

This path has now been walked five times — D9 (2026-08-24), then D2/D6/D10/D11 (2026-08-30, mirroring the D9 pair line-for-line). Adding another chart type (e.g. D3, D7, D12) gets full dignity/house-lordship/conjunction/aspect analytics **and** nakshatra linkage **for free**, with no new tables and almost no new code:
1. Implement `IChartCalculator` in `Ikiastrro.Core/Calculators/` — two methods: `ComputeAnalysisInput` (Ascendant + planet placements, as a `ChartAnalysisInput`) and `BuildResult` (packages that into the `ChartResult` row to store). The varga-sign math is one pure `AstroMath` function (transcribe it verbatim from PyJHora `charts.py` `chart_method=1`, add worked examples to `verify-vargas`).
2. Register it in `ChartCalculationOrchestrator.CreateDefault()`. (`ChartCalculationOrchestrator.Calculators` exposes the list to the CLI.)
3. That's it — the CLI and Web write paths already loop over every registered calculator generically and call `ChartAnalyzer.Compute` on each one's `ChartAnalysisInput`, so the new chart type's `tbl_Chart_KeyDetails`/`HouseLords`/`Conjunctions`/`Aspects` rows get written automatically. No database schema change needed either — `tbl_ChartResults` already stores arbitrary `ChartType` rows, and the 4 analytical tables are chart-type-generic (see above).
4. `dotnet run --project src/Ikiastrro.Cli -- backfill-charts` creates every registered chart type a saved person is missing (ChartResult + all analytics), idempotently — the way D2/D6/D10/D11 were backfilled for the 5 existing people. (`-- backfill-analytics` only fills analytics for ChartResults that already exist; `-- recompute-keydetails` re-derives KeyDetails for every calculable chart type, e.g. after a column is added.)
5. **The visual chart still needs manual wiring** (this part is *not* automatic — see "Known limitations" above): in `ChartView.razor`, add `[Parameter]`s for the new chart's `KeyDetails`/`Aspects` (mirroring `D9KeyDetails`/`D9Aspects`), a computed `D1ChartViewModel.BuildAspectedByGlyphs(...)` property (mirroring `D9AspectedByGlyphs`), and a new `<SouthIndianGrid>` passing that into `AspectedByGlyphs`; then fetch and pass the new chart's rows from `ChartDetail.razor`'s `OnParametersSet` the same way `d9KeyDetails`/`d9Aspects` are fetched today.

See `D:\@ClaudeSpace\ikiastrro.md` for full project history/decisions, including the competitor-tool research and varga comparison matrix.

## Reference/master data tables (migrations 019-021)

A parallel set of classical-astrology reference tables, distinct from the chart-specific
`tbl_Chart_*` tables above (those store *computed results for a specific person's chart*; these
store the underlying classical rules/data): `tbl_Planets` (9 rows), `tbl_SignAttributes` (12
Rashi rows), `tbl_PlanetSignTransitEvents` (Saturn/Jupiter/Rahu sign-boundary crossings —
**table only, 0 rows yet**, backfill needs a new CLI mode), `tbl_Nakshatras` (27),
`tbl_NakshatraPadas` (108, with verified 1:1 Pada↔Navamsa mapping), `tbl_NakshatraSubLords`
(243, KP sub-lord levels 1-2 only). Full design rationale, classical cross-references, and
what's still NULL pending a sourced reference: `D:\@ClaudeSpace\ikiastrro\docs\reference-vedic-data-tables.md`.

Not yet wired into the calculation engine — `ClassicalDignity.cs`/`AstroMath.cs` still hold
their own hardcoded lookups, cross-checked to match these tables' seed data but not reading
from them. Whether to switch the engine over to reading these tables is an open decision.

**2026-08-30 (migrations 032–034):** `tbl_Nakshatras` gained `PrimaryRasiId` +
`StraddlesSignBoundary`; `tbl_Chart_KeyDetails` now FK-links to `tbl_Nakshatras` /
`tbl_NakshatraPadas` and stores the KP sub-lord (`NakshatraSubLordPlanet`); new view
`vw_Chart_HouseNakshatraSpan` gives the house-centric House → Rāśi → Nakshatra chain. Details in
the "Nakshatra reference linkage" subsection above. `GetNakshatraSubLord` in `AstroMath` is an
independent C# implementation of the KP level-2 division, verified row-for-row against
`tbl_NakshatraSubLords` (not reading from it).

`tbl_PlanetSignTransitEvents` is now populated (1930-2060, Saturn/Jupiter/Rahu) via
`dotnet run --project src/Ikiastrro.Cli -- backfill-planet-transits`, following a
`precheck-planet-transits` dry-run cross-checked against published Vedic sidereal transit
dates first. See `PlanetTransitEventFinder` (Core) for the day-walk/bisection algorithm.

**Sade Sati / Kantaka Shani / Ashtama Shani**: `tvf_Chart_SadeSatiPeriods(@BirthDetailId)`
returns Saturn-from-natal-Moon affliction periods (all 3 Sade Sati Dhaiyas + Kantaka [4th from
Moon] + Ashtama [8th from Moon]), built purely from existing tables — no DB-level dependency
beyond what's already here. DB-level only, no CLI/Web wiring yet. Ashtakavarga (the separate
bindu-point transit-strength system, including Shani Ashtakavarga) is **not built yet, but no
longer source-blocked**: the classical benefic-place contribution tables were previously held
back "pending a cited source", and that source is now in the repo —
`_research/jyotishganit/jyotishganit/components/ashtakavarga.py` (MIT-licensed, BPHS-based,
test-covered: `tests/test_ashtakavarga.py`) implements Bhinnashtakavarga + Sarvashtakavarga and
can be ported to `Ikiastrro.Core` with attribution, the same way the varga formulas were
transcribed from PyJHora. Shadbala is in the same position (`.../components/strengths.py`, MIT,
all six balas). Only the Sodhya/Rasi/Graha Pinda *reduction* step (Trikona + Ekadhipatya
Shodhana) still needs BPHS/PyJHora as a reference — jyotishganit stops at the BAV/SAV grids.
Full gap analysis and build tiers: `docs/scope-jhora-coverage.md`.

**Star-schema rules engine (Phase 1)**: classical rules previously only hardcoded in
`ClassicalRelationships.cs`/`ClassicalCombustion.cs`/`ClassicalDignity.cs` are now also
mirrored in `tbl_Rule_*` tables (`tbl_Rule_Sets`, `tbl_Rule_AspectOffset`,
`tbl_Rule_CombustionOrb`, `tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance`),
each scoped by a `RuleSetId` so a future rule change is a new row set, never an edit — read via
`RuleSetRepository`/`AspectRuleRepository`/`CombustionRuleRepository`/
`NaturalRelationshipRuleRepository` (`Ikiastrro.Data`) and inspectable via
`dotnet run --project src/Ikiastrro.Cli -- list-rule-sets` /
`show-rules <id>`. **The calculation engine does not read these tables yet** — the hardcoded
C# dictionaries are still what actually runs; the rule tables are a verified-matching mirror,
not yet the source of truth. Full design + Phase 2 scope (renaming `tbl_Chart_*` into
`tbl_Fact_*`/`tbl_Dim_*` and wiring the calculators to read live):
`D:\@ClaudeSpace\ikiastrro\docs\dbdesign-star-schema-rules-engine.md`. New tables from 2026-08-30 onward
must use the `tbl_Dim_`/`tbl_Rule_`/`tbl_Fact_` infix — see `STANDARDS.md` §D.1.

## House + Lagna significations

Design doc + full sourcing: `D:\@ClaudeSpace\ikiastrro\docs\reference-house-lagna-significations.md`.
Sourced from `BookExtracts\how-to-judge-a-horoscope-1.md` (B.V. Raman).

- **`tbl_Dim_LagnaFunctionalNature` — built 2026-08-30 (migration `031`), removed 2026-08-31.**
  Was Raman's 12-Lagna "Benefics and Malefics for each Lagna" table (84 rows, verbatim), kept as
  a cross-check mirror the engine never read. Torn out in full — table + FKs + CHECK + seed
  dropped from `db/ikiastrro.sql`, `LagnaFunctionalNatureRow`/`LagnaFunctionalNatureRepository`
  and the `compare-functional-nature` CLI mode deleted, `db/_archive/031_*.sql` removed. Existing
  databases: run `db/00_drop_lagna_functional_nature.sql`. Functional benefic/malefic is now
  produced solely by the computed `LagnaFunctionalNature` classifier (below).
- **`tbl_Dim_HouseSignification` / `tbl_Dim_PlanetSignification` / `tbl_Dim_PlanetHouseKaraka`
  (migration `030`) — still reserved/unapplied.** `LifeAreaMap` (Core) currently hardcodes the
  house/karaka-per-life-area data these tables will hold, cross-checked to the same source.

## Functional benefic/malefic classifier + shared write path (2026-08-30, groundwork for the Web UI recreate)

The project is now **under git** (`master`; baseline `363eeeb` = pre-recreate state — all
history before that lives only in `D:\@ClaudeSpace\ikiastrro.md`). This batch is
DB/Core/CLI only — **no Web `.razor`/CSS change** (the Web app still renders the pre-recreate
D1+D9 view; the life-area UI rebuild is the next plan). Spec:
`docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md`.

- **`LagnaFunctionalNature`** (`Core/Calculators/`) — pure Parashari classifier: given a Lagna
  sign + planet, returns `Benefic | Malefic | Neutral | Yogakaraka` derived from which houses the
  planet rules from that Lagna, plus `IsMaraka` / `KendradhipatiDosha` / a rationale string. A
  documented heuristic following Raman's general rules (Vol. 1 p.14-15). **This is the single
  source of the functional-nature verdict across the app** (CLI `verify-functional-nature`; the
  web D1 planet-positions table's Malefic/Benefic column, shown as `B-[9&12]` / `M-[3&6]` /
  `N-[…]`). The per-Lagna Raman mirror table it used to be cross-checked against was removed
  2026-08-31 (see above).
- **`ChartGenerationService`** (`Data/`) — the single "given a persisted `BirthDetails`, compute
  and store every registered chart type + Vimshottari Dasha" pipeline. `GenerateAll` /
  `GenerateMissing` / `RecomputeAnalytics(bd, chartTypeFilter?)` → `GenerationReport`. Composes
  `ChartCalculationOrchestrator` + `ChartAnalyzer` + `VimshottariDashaService` (does not
  reimplement Dasha). **Both `Add.razor` and the CLI** (`compute-all`, `backfill-charts`,
  `backfill-analytics`, `recompute-keydetails`, the interactive add flow) now call it instead of
  each carrying their own copy of the pipeline. Idempotent via delete-first-then-regenerate;
  **no cross-repo DB transaction** (the repo layer opens a connection per call) — a partial
  failure is recovered by re-running the same call. The cutover incidentally fixed two latent
  bugs: `Add.razor` never computed Dasha (web-added people had an empty Dasha section), and old
  `backfill-analytics` threw on any person that had a `VimshottariDasha` ChartResults row.
- **New read repositories** (for the later Web UI; not yet consumed): `GetByBirthDetailId(int)`
  on all four `tbl_Chart_*` repos (one-shot per-person load, ordered by `Id`);
  `SadeSatiRepository` (`tvf_Chart_SadeSatiPeriods`), `HouseNakshatraSpanRepository`
  (`vw_Chart_HouseNakshatraSpan`), `PlanetSignTransitEventsRepository.GetSnapshot` (current
  sign + since/next-change); and `PlanetsReferenceRepository` / `SignAttributesRepository` /
  `NakshatraReferenceRepository` over the `tbl_Planets` / `tbl_SignAttributes` / `tbl_Nakshatras`
  (+Padas+SubLords) dimension tables.
- **`LifeAreaMap`** (`Core/LifeAreas/`) — static `LifeArea` → (houses, karakas, vargas) map for
  the four life-area workspace tabs (Personality & Health / Relationships / Career / Money).
- Rename: `D1ChartViewModel` / `D1PlanetRow` → **`ChartViewModel` / `PlanetRow`** (the types were
  already chart-type-agnostic).

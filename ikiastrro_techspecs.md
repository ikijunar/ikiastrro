# ikiastrro — Technical Specifications

Reference doc for the technical shape of **ikiastrro** (Vedic astrology app; renamed from
`VedicHoroGen` / `vedic_horo_gen` on 2026-08-30). Living document — update it when the
architecture changes. Companions: `ikiastrro_uidesignspecs.md` (web UI),
`ikiastrro_calculations.md` (astrology math), `ikiastrro.md` (dated history),
`docs/techstack.md` (pinned package versions).

---

## 1. What it is

A single-user desktop-class tool that turns birth details (name, DOB, time, place) into a
stored, browsable Vedic horoscope: the D1/D2/D6/D9/D10/D11 divisional charts, Vimshottari
Dasha (3 levels), classical planetary dignity, house-lordship, conjunctions, aspects,
retrograde/combustion flags, nakshatra data, Sade Sati / Kantaka / Ashtama windows, and
Lagna functional benefic/malefic classification. Two front ends over one engine and one
database: a **CLI** (data entry, batch/backfill, verification) and a **Blazor Server web
workspace** (read/browse).

**Out of scope (by design):** interpretation / prediction text, scoring, synthesis. The app
computes and *displays* classical facts; a future engine does judgement.

---

## 2. Solution layout

.NET 8 (`net8.0`), C#, nullable + implicit usings on. Four projects under `src/`:

| Project | SDK | Kind | Depends on | Role |
|---|---|---|---|---|
| `Ikiastrro.Core` | `Microsoft.NET.Sdk` | class lib | — | All astronomy/astrology math, domain models, view-models. No DB, no I/O. |
| `Ikiastrro.Data` | `Microsoft.NET.Sdk` | class lib | Core | Dapper repositories + services over SQL Server. |
| `Ikiastrro.Cli` | `Microsoft.NET.Sdk` (Exe) | console | Core, Data | Interactive add flow + one-off batch/verify modes. |
| `Ikiastrro.Web` | `Microsoft.NET.Sdk.Web` | Blazor Server | Core, Data | The chart workspace UI. |

Solution file: **`Ikiastrro.slnx`** (XML slnx format). Namespaces are `Ikiastrro.*`
(PascalCase). Assembly names default from the `.csproj` filenames.

---

## 3. Key dependencies (pinned — see `docs/techstack.md` for the full list)

- **`SwissEphNet` 2.8.0.2** — managed C# port of Astrodienst's Swiss Ephemeris, **Moshier
  analytical mode** (no ephemeris data files to ship). The calculation engine. Replaced
  `VedAstro.Library` entirely on 2026-08-24 after four confirmed defects there
  (ayanamsha ~1.45° short, wrong Ketu longitude, broken Whole-Sign house counting, and a
  `CacheManager` version conflict that hard-blocked the Blazor host).
- **`Dapper` 2.1.79** + **`Microsoft.Data.SqlClient` 6.0.2** — data access. No EF Core / no
  full ORM. Repositories are hand-written SQL.
- **`GeoTimeZone` 6.1.0** + **`TimeZoneConverter` 7.2.0** — offline IANA-timezone resolution
  from lat/long + date (historical DST respected).
- **`Microsoft.AspNetCore.Components` 6.0.25** referenced *from Core* — so `ChartViewModel`
  and friends (the render-ready shapes) can live in Core and be shared by Web.
- Blazor Server only — no client-side JS framework. Razor components use CSS isolation
  (`Component.razor.css`).

---

## 4. Data layer (`Ikiastrro.Data`)

One repository per table/view, constructed from a shared `SqlConnectionFactory`
(connection-per-call: `CreateOpenConnection()` opens, caller disposes). No unit of work, no
tracked entities. Notable services:

- **`ChartGenerationService`** — the single "given a persisted `BirthDetails`, compute and
  store every chart type + Vimshottari Dasha" pipeline. `GenerateAll` / `GenerateMissing` /
  `RecomputeAnalytics`. Used by both `Add.razor` and five CLI modes; deletes-then-reinserts
  per `(BirthDetailId, ChartType)` so re-runs are idempotent and partial failures self-heal.
  Place resolution + the `BirthDetails` insert stay in the callers (async/network).
- **`VimshottariDashaService`** — computes + stores the 3-level Dasha tree (own table, not an
  `IChartCalculator`), logged as a `tbl_ChartResults` row for delete-cascade consistency.
- **`BirthDetailDeletionService`** — FK-safe cascade delete of a person and everything derived.
- Read repos added for the workspace UI: `SadeSatiRepository`,
  `LagnaFunctionalNatureRepository`, `PlanetSignTransitEventsRepository`,
  `HouseNakshatraSpanRepository`, the reference-data repos, and `GetByBirthDetailId` on the
  four `tbl_Chart_*` analytics repos.

DI: `Ikiastrro.Web/Program.cs` registers the factory as a singleton and every repo/service as
scoped. The CLI is top-level statements with no container — it news up what it needs.

---

## 5. Database

- **Microsoft SQL Server**, Windows Auth, `localhost` (RAMMYPS default instance).
- Database name: **`ikiastrro`** (renamed from `vedic_horo_gen` on 2026-08-30).
  Connection string in `SqlConnectionFactory.CreateDefault()`.
- **Schema is one consolidated baseline: `db/ikiastrro.sql`** — whole schema (20
  tables, 5 views, 4 functions) + all reference/master data (Planets, SignAttributes,
  Nakshatras + Padas + SubLords, PlanetSignTransitEvents, `tbl_Rule_*`,
  `tbl_Dim_LagnaFunctionalNature`) + the `tbl_Dim_LifeCalendar` day dimension (regenerated by
  a recursive CTE, 43 830 rows). Per-person tables are schema-only.
  - Fresh machine: run `db/ikiastrro.sql`.
  - Existing `vedic_horo_gen` DB: run `db/00_rename_db_to_ikiastrro.sql` (renames in
    place, keeps all data).
  - The former numbered migrations `001…034` are frozen under `db/_archive/` for history —
    do not add new numbered migrations; edit `db/ikiastrro.sql` directly.

### Table groups

| Group | Tables | Filled by |
|---|---|---|
| Input | `tbl_BirthDetails` | user (CLI / `Add.razor`) |
| Chart results | `tbl_ChartResults` (one row per person × chart type + one per Dasha run) | engine |
| Chart analytics (chart-type-generic, keyed by `ChartResultId` + `ChartType`) | `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects` | `ChartAnalyzer` |
| Dasha | `tbl_Chart_DashaPeriods` (self-referencing, 3 levels) | `VimshottariDashaService` |
| Reference / master | `tbl_Planets` (9), `tbl_SignAttributes` (12), `tbl_Nakshatras` (27), `tbl_NakshatraPadas` (108), `tbl_NakshatraSubLords` (243, KP L1–L2 only), `tbl_PlanetSignTransitEvents` (Sa/Ju/Ra sign-crossing log 1930–2060) | seed data / CLI backfill |
| Rules engine (versioned; every row carries `RuleSetId`) | `tbl_Rule_Sets`, `tbl_Rule_AspectOffset`, `tbl_Rule_CombustionOrb`, `tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance` | seed (`'Parashari-Classical'`) |
| Cited-source mirrors (engine doesn't read them yet) | `tbl_Dim_LagnaFunctionalNature` (84; Raman's per-Lagna verdict) | seed |
| Dimensions | `tbl_Dim_LifeCalendar` (age-relative day dimension, 0 = birth) | CTE seed |

### Views & functions

- `vw_Chart_Consolidated`, `vw_Chart_DashaTimeline`, `vw_Chart_HouseNakshatraSpan`,
  `vw_KetuSignTransitEvents`, `vw_NakshatraPadaDetails`
- `fn_GetNakshatraRulingPlanetId` (scalar); `tvf_Chart_LifeWeeks(@BirthDetailId)`,
  `tvf_Chart_SadeSatiPeriods(@BirthDetailId)`, `tvf_PlanetSignAtDate(@PlanetId, @AsOfUtc)`
  (inline TVFs)

---

## 6. CLI modes (`dotnet run --project src/Ikiastrro.Cli -- <mode>`)

No args → interactive add (prompts Name / DOB / time / place, resolves lat-long-offset,
computes D1/D2/D6/D9/D10/D11 + Dasha via `ChartGenerationService`, stores, prints).

| Mode | Purpose |
|---|---|
| `compute-all <name>` | Regenerate every chart type + Dasha for one person (post birth-time correction) |
| `backfill-charts` | Add any missing chart type to people saved before it existed (idempotent) |
| `backfill-analytics` / `recompute-keydetails` | Re-derive the 4 analytics tables (now identical code paths) |
| `compute-dasha <name>` / `show-dasha <name>` / `backfill-dasha` | Vimshottari Dasha compute / print / bulk |
| `precheck-planet-transits` / `backfill-planet-transits` | Dry-run vs. real fill of `tbl_PlanetSignTransitEvents` |
| `list-rule-sets` / `show-rules <id>` | Inspect the `tbl_Rule_*` layer |
| `verify-vargas` | Worked-example assertions for the divisional-chart math (exit 1 on FAIL) |
| `verify-functional-nature` | Worked-example assertions for `LagnaFunctionalNature` |
| `compare-functional-nature` | Print where the computed heuristic diverges from `tbl_Dim_LagnaFunctionalNature` |

No unit-test project — these `verify-*` modes are the regression suite.

---

## 7. Build & run

- Build: `dotnet build Ikiastrro.slnx` **or** open `Ikiastrro.slnx` in Visual Studio 2026.
- **Machine constraint (RAMMYPS): Application Control (WDAC) blocks freshly-built assemblies
  from loading in any process *not* launched by Visual Studio.** `dotnet run` / `dotnet exec`
  of this solution fail with `FileLoadException … 0x800711C7`. Consequences:
  - Terminal `dotnet build` is fine (compile only) and is the CI-style gate.
  - Running the app (CLI or Web) must be done from **Visual Studio**: Build ▸ Rebuild
    Solution (so VS, a trusted installer, writes the DLLs), then F5. After any terminal-side
    build, delete `src/*/bin` `src/*/obj` and let VS rebuild.
- Web launch profiles (`src/Ikiastrro.Web/Properties/launchSettings.json`): `http`
  (Kestrel, `http://localhost:5160`), `https` (`https://localhost:7108`), `IIS Express`
  (`http://localhost:59377`, https `44370`). Use the **http** profile when a plain-HTTP URL
  is needed (no dev-cert prompt).

---

## 8. Verification model (no test project)

1. `dotnet build Ikiastrro.slnx` clean (0 warnings / 0 errors).
2. `verify-vargas` + `verify-functional-nature` exit 0.
3. Golden record: **`1 Ramakrishnan`** (22 Apr 1981, Chennai; Aries Lagna, Moon debilitated
   in Scorpio) — a ~12-item fact checklist re-confirmed after UI/engine changes.
4. Live browser smoke test in VS against `/charts/1`, `/charts/2` (dense chart), `/charts/3`
   (born 2010).
5. Colour: the dataviz-skill `validate_palette.js` run recorded for any categorical palette;
   colour is always a scan aid, never the sole signal (every mark carries text too).

---

## 9. Place resolution

City/Country → lat/long via **OpenStreetMap Nominatim** (free, no key). UTC offset then
resolved **fully offline** from lat/long + birth date via `GeoTimeZone` + `TimeZoneConverter`
(historical DST/offset rules respected). Geocoding failure → CLI prompts for
latitude/longitude/UTC-offset manually (`ManualPlaceResolver`).

---

## 10. Known constraints / debt

- Rules-engine **Phase 2 not started** — calculators still use hard-coded C# lookups
  (`ClassicalDignity.cs` etc.); the `tbl_Rule_*` tables are a cross-checked mirror only.
- `tbl_Dim_LagnaFunctionalNature` and `LifeAreaMap` don't read `tbl_Dim_*` yet (blocked on
  migration 030 `tbl_Dim_HouseSignification`, not built).
- Career & Money are merged in the Web layer only; `LifeArea` enum in Core still has them
  separate.

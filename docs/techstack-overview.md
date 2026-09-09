---
last_updated: 2026-09-08
reflects: .NET 10 migration verified with SDK 10.0.400 and runtime 10.0.11
---

# ikiastrro — Technology Stack

Verified against the actual `.csproj` files and restored NuGet graph in
`D:\@ClaudeSpace\ikiastrro\` on 2026-09-08.
See `ikiastrro.md` for build/decision history and the project's own `README.md` for
current architecture.

## Runtime / Language

- **.NET 10** (`net10.0`) across all application and test projects
- SDK pinned by `global.json` to **10.0.400**, with `latestFeature` roll-forward; verified
  against runtime **10.0.11**
- **C#**, nullable reference types + implicit usings enabled everywhere

## Solution layout (5 projects)

| Project | SDK | Type | Depends on |
|---|---|---|---|
| `Ikiastrro.Core` | `Microsoft.NET.Sdk` | class library | — |
| `Ikiastrro.Data` | `Microsoft.NET.Sdk` | class library | Core |
| `Ikiastrro.Cli` | `Microsoft.NET.Sdk` | console exe | Core, Data |
| `Ikiastrro.Web` | `Microsoft.NET.Sdk.Web` | Blazor Server app | Core, Data |
| `Ikiastrro.Web.Tests` | `Microsoft.NET.Sdk` | xUnit + bUnit test project | Web |

## Key libraries / packages

**Core**
- `SwissEphNet` 2.8.0.2 — managed C# port of Astrodienst's Swiss Ephemeris (Moshier mode, no
  ephemeris data files needed); the astronomical/astrological calculation engine. Replaced
  `VedAstro.Library` entirely on 2026-08-24 after confirmed defects there.
- `GeoTimeZone` 6.1.0 — resolves IANA timezone from lat/long
- `TimeZoneConverter` 7.2.0 — IANA ⇄ Windows timezone conversion
- JSON, HTTP, and regular-expression APIs come from the .NET 10 platform. Obsolete direct
  references to `Newtonsoft.Json`, `Microsoft.AspNetCore.Components`, and legacy `System.*`
  packages were removed during the .NET 10 migration.

**Data**
- `Dapper` 2.1.79 — micro-ORM for SQL Server access
- `Microsoft.Data.SqlClient` 7.0.2 — SQL Server driver

**Cli**
- No extra packages; references Core + Data, `OutputType=Exe`

**Web**
- `Microsoft.NET.Sdk.Web` — ASP.NET Core / **Blazor Server**
- UI built with CSS-isolated Razor components (e.g. `D1ChartView.razor`,
  `SouthIndianGrid.razor`)
- `Syncfusion.Blazor.Charts` / `Syncfusion.Blazor.Gauge` / `Syncfusion.Blazor.Themes`
  — Community License; the sole data-visualization library (strength bars, heatmaps,
  Dasha timelines, polar longitude wheel). Native Blazor Server, no JS framework.
  Decision recorded 2026-08-31 in `docs/uidesign-dataviz.md`; **not yet added to the
  `.csproj`** — version pin to follow when wired in.

**Tests**
- `Microsoft.NET.Test.Sdk` 18.9.0
- `xunit` 2.9.3 + `xunit.runner.visualstudio` 4.0.0
- `bunit` 2.9.0

## Upgrade verification

- `dotnet build Ikiastrro.slnx --no-restore` — succeeded with 0 warnings and 0 errors
- `dotnet test Ikiastrro.slnx --no-build --no-restore` — 38 passed, 0 failed, 0 skipped
- NuGet outdated audit — no top-level updates available from configured sources
- NuGet vulnerability audit — no vulnerable direct or transitive packages reported

## Database

- **Microsoft SQL Server**, Windows Authentication, instance `localhost` (RAMMYPS default instance)
- Database: `ikiastrro` (connection string in `SqlConnectionFactory.cs`; renamed from `vedic_horo_gen` 2026-08-30; the
  `cproj_` folder prefix is a workspace naming convention only, not a DB-naming one)
- Tables: `tbl_BirthDetails`, `tbl_ChartResults`, `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`,
  `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects` (generalized from D1-only to chart-type-generic
  on 2026-08-24, `ChartType` column added)
- View: `vw_Chart_Consolidated`
- Schema: single consolidated baseline `db/ikiastrro.sql` (old numbered migrations in `db/_archive/`)
- Access pattern: Dapper over `Microsoft.Data.SqlClient`, no full ORM/EF Core

## Domain conventions (not a library, but core to the stack's behavior)

- Ayanamsha: **Lahiri** (via SwissEphNet `SEFLG_SIDEREAL` + `SE_SIDM_LAHIRI`, no manual correction)
- House system: **Whole Sign**
- Rahu: **mean node** (not true node); Ketu derived as Rahu + 180°
- Rahu/Ketu dignity: Parashari convention; aspects: Jupiter-style (5th/7th/9th) — both by
  rammyps's explicit choice
- Classical dignity/relationship logic (`ClassicalDignity.cs`, `ClassicalRelationships.cs`) is
  original code, not derived from any external engine

## Frontend

- Blazor Server (server-rendered, no separate JS SPA framework) with CSS-isolated `.razor.css`
  components — no client-side JS framework (React/Vue/etc.) in the stack
- No component library for layout/tables/chart diagrams (hand-rolled SVG/CSS from
  `tokens.css`); **Syncfusion Blazor** is the one sanctioned exception, for data
  visualization only (see `docs/uidesign-dataviz.md`)

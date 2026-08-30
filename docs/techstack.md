# vedic_horo_gen — Technology Stack

Verified against the actual `.csproj` files in `D:\@ClaudeSpace\ikiastrro\src\` on 2026-08-26.
See `ikiastrro.md` for build/decision history and the project's own `README.md` for
current architecture.

## Runtime / Language

- **.NET 8** (`net8.0`) across all four projects
- **C#**, nullable reference types + implicit usings enabled everywhere

## Solution layout (4 projects)

| Project | SDK | Type | Depends on |
|---|---|---|---|
| `Ikiastrro.Core` | `Microsoft.NET.Sdk` | class library | — |
| `Ikiastrro.Data` | `Microsoft.NET.Sdk` | class library | Core |
| `Ikiastrro.Cli` | `Microsoft.NET.Sdk` | console exe | Core, Data |
| `Ikiastrro.Web` | `Microsoft.NET.Sdk.Web` | Blazor Server app | Core, Data |

## Key libraries / packages

**Core**
- `SwissEphNet` 2.8.0.2 — managed C# port of Astrodienst's Swiss Ephemeris (Moshier mode, no
  ephemeris data files needed); the astronomical/astrological calculation engine. Replaced
  `VedAstro.Library` entirely on 2026-08-24 after confirmed defects there.
- `GeoTimeZone` 6.1.0 — resolves IANA timezone from lat/long
- `TimeZoneConverter` 7.2.0 — IANA ⇄ Windows timezone conversion
- `Newtonsoft.Json` 13.0.1
- `System.Text.Json` 6.0.10
- `Microsoft.AspNetCore.Components` 6.0.25 — Blazor component model referenced from Core (view
  models shared with the Web project)
- `System.Text.RegularExpressions` 4.3.1, `System.Net.Http` 4.3.4

**Data**
- `Dapper` 2.1.79 — micro-ORM for SQL Server access
- `Microsoft.Data.SqlClient` 6.0.2 — SQL Server driver (back on current stable after the old
  VedAstro-era version pin was removed 2026-08-24)

**Cli**
- No extra packages; references Core + Data, `OutputType=Exe`

**Web**
- `Microsoft.NET.Sdk.Web` — ASP.NET Core / **Blazor Server**
- No extra packages beyond Core + Data; UI built with CSS-isolated Razor components
  (e.g. `D1ChartView.razor`, `SouthIndianGrid.razor`)

## Database

- **Microsoft SQL Server**, Windows Authentication, instance `localhost` (RAMMYPS default instance)
- Database: `vedic_horo_gen` (bare name — connection string in `SqlConnectionFactory.cs`; the
  `cproj_` folder prefix is a workspace naming convention only, not a DB-naming one)
- Tables: `tbl_BirthDetails`, `tbl_ChartResults`, `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`,
  `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects` (generalized from D1-only to chart-type-generic
  on 2026-08-24, `ChartType` column added)
- View: `vw_Chart_Consolidated`
- Schema migrations tracked as numbered `.sql` files (e.g. `010_generalize_chart_analytical_tables.sql`)
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

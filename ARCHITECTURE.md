---
last_updated: 2026-09-09
togaf: C/D — Application & Technology Architecture
safe: Solution Intent (fixed)
---

# ikiastrro — architecture

System overview. Detail lives in the workstream docs (`docs/database/`, `docs/cli/`,
`docs/ui/`); principles in [`docs/architecture/principles.md`](docs/architecture/principles.md);
the cross-stream interface in
[`docs/architecture/domain-contracts.md`](docs/architecture/domain-contracts.md).

## Shape

A single-user, desktop-class tool that turns birth details (name, DOB, time, place) into a
stored, browsable Vedic horoscope and a structured evidence model over it. One engine and one
database behind two front ends:

| Value stream | Projects | Delivers |
|---|---|---|
| **DB + CLI** | `Ikiastrro.Data` (schema, Dapper repos), `Ikiastrro.Core` (all math), `Ikiastrro.Cli` (entry + batch + verification) | every calculation, persisted with full provenance |
| **UI** | `Ikiastrro.Web` (Blazor Server, MudBlazor) | the astrologer-facing read surfaces |

## Stack

- **.NET 10 / C#** (`net10.0`), SDK pinned in `global.json` (`10.0.400`). Solution
  `Ikiastrro.slnx`; four `src/` projects + two test projects.
- **Calculation engine:** [`SwissEphNet`](https://www.nuget.org/packages/SwissEphNet) — a
  managed port of Astrodienst's Swiss Ephemeris, **Moshier analytical mode** (no ephemeris
  data files). Only raw longitudes come from it; all classical logic is original `AstroMath`.
- **Database:** Microsoft SQL Server, Windows Auth. Dapper, hand-written SQL, no ORM.
- **Web:** Blazor Server, **MudBlazor 9.9** (the one component library), warm light brand
  ([`docs/ui/brand.md`](docs/ui/brand.md)). Chart diagrams and the life-weeks grid are
  hand-rolled inline SVG.
- **Ayanāṁśa:** selectable, catalogued in `AyanamsaDefinition` (22 systems) and
  `tbl_Rule_Ayanamsa`. Current default `Jagannatha`. **House system:** Whole Sign everywhere.
- **Place resolution:** OpenStreetMap Nominatim for lat/long; UTC offset resolved fully
  offline from lat/long + date (historical DST respected); manual lat/long/offset fallback.

## Engine stack (`Ikiastrro.Core/Engines/<Name>/`)

Bottom-up; a higher engine only calls down. Each folder = one namespace, so "which engine is
wrong" is answerable at a glance.

| Engine | Owns | State |
|---|---|---|
| Astronomy | Julian Day, ayanāṁśa, sidereal graha positions, Ascendant, sunrise/sunset | built |
| Position | the D1 Rāśi chart | built |
| DivisionalCharts | D2–D60 (20 vargas) via DB-driven `tbl_Rule_VargaScheme` + `IVargaSignRule` + 3 portable interpreters | built |
| Houses | whole-sign houses, house→sign→lord, functional benefic/malefic | built |
| Nakshatras | nakṣatra / pāda / Vimśottari lord / KP sub-lord | built |
| Dignity | 9-tier Pañchadhā Maitrī `DignityStatus`; PVR + Parāśari rule tables | built |
| Karakas | Chara Karakas (Aṣṭa), special points (AL, 12 Arudhas, HL, 11 upagrahas) | built |
| PlanetaryStates | Bālādi + Jāgradādi avasthas | 2 of 5 states |
| Relationships | conjunctions, aspects, conjunction groups, combustion | built |
| Dasha | Vimśottari (3-level, partial-at-birth) | Vimśottari only |
| Strength | Ṣaḍbala / Bhāva Bala foundation; Vimśopaka reserved | in progress |
| Yoga | source-attributed Raman 1–300 + PVR detection | in progress |
| Dispositors | interface only | reserved |

Cross-cutting: `Ikiastrro.Core.Pipeline` (`ChartPipeline` / `ChartBundle` — a DB-free façade
that runs every engine; `verify-pipeline` exercises it), `.Reference` (`TerminologyCatalog`),
`.Presentation` (`ChartViewModel`), `.Models` (DB-row shapes).

## Shared analytics — every chart type

Populated automatically for all 21 position chart types, not just D1:
`tbl_Chart_KeyDetails` (per planet + Ascendant: longitude / latitude / speed / retrograde /
sign / degree / nakṣatra + dignity + 3 house reckonings + combustion + aspects + `CharaKaraka`
+ `PointKind`), `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions` (+ multi-graha groups),
`tbl_Chart_Aspects`, `tbl_Fact_PlanetaryState`. House placement is computed three ways —
from Lagna, from Sun, from Moon. `vw_Chart_Consolidated` joins it all into one wide row per
planet per chart type. Adding a chart type needs an `IChartCalculator` pair and one
`tbl_Rule_VargaScheme` row — no schema change; analytics come for free.

## Known constraints

- **Ayanāṁśa discrepancy** — the `Jagannatha` default resolves to Swiss sidereal mode 26
  (22.745° for 1981) against a JHora reference of 23.595°; a ~0.85° gap that fails
  `verify-vargas` / `verify-jaimini`. Tracked as `FEAT-DATA-04`; fix + chart regeneration
  pending.
- **Rules-engine Phase 2 not started** — `Dignity` / `Relationships` / `Combustion` engines
  still run on hard-coded C#; the `tbl_Rule_*` classical tables are a verified-matching
  mirror. `tbl_Rule_VargaScheme` is the exception — it is live.
- **Reference dimension tables** (`tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras*`) are
  seeded and cross-checked but the engine doesn't read from them yet.
- **D81 / D108 / D144 / D150** (varga-of-a-varga composition) — not built.
- **Swiss Ephemeris** is AGPL/commercial dual-licensed — fine for this private tool; revisit
  before any public distribution.

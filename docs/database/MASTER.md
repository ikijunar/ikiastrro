---
last_updated: 2026-09-09
workstream: database
togaf: C — Data Architecture
safe: Solution Intent (fixed) — data model
---

# Database workstream — MASTER

**Branch** `workstream/database` · **worktree** `D:\@ClaudeSpace\ikiastrro.wt\database` ·
**owns** `db/`, `src/Ikiastrro.Data/`.

The versioned SQL Server schema, its Dapper read/write layer, and the rules-engine tables.
Publishes to the CLI and UI streams under
[`../architecture/domain-contracts.md`](../architecture/domain-contracts.md).

## Docs

| Doc | For |
|---|---|
| [`schema.md`](schema.md) | Table inventory, chart-generic analytics, reference/master data, views & functions, migration policy |
| [`rules-engine.md`](rules-engine.md) | `tbl_Dim_*` / `tbl_Rule_*` / `tbl_Fact_*` model — every rule table, its columns, its source rule |

## Current state

- **Baseline** `db/ikiastrro.sql` + numbered migrations `db/NN_*.sql` applied in order,
  tracked in `dbo.SchemaMigrations` (keyed by `ScriptName`). Migrations `22`–`053` are the
  active layer; earlier flat history is frozen under `db/_archive/`.
- ~80 tables: input, chart results, chart-generic analytics, dasha, reference/master,
  `tbl_Rule_*` (versioned), `tbl_Dim_*`, `tbl_Fact_*` (star-schema).
- **Live rule table:** `tbl_Rule_VargaScheme` (the orchestrator builds one `VargaCalculator`
  per row). All other `tbl_Rule_*` are a verified mirror of the hard-coded C# — Phase 2
  (calculators reading them) not started.
- `tbl_Rule_Ayanamsa` — JHora ayanāṁśa catalogue + system default (`Jagannatha`).

## In flight

- **`FEAT-DATA-04`** — ayanāṁśa / Vimśottari reference benchmark: `tbl_Dim_AyanamsaBenchmarkCases`
  is empty while `tbl_Dim_AyanamsaBenchmarkPositions` (10) + `tbl_Dim_DashaBenchmarkPeriods` (9)
  are orphaned on `CaseId = 1`. Re-seed `BENCH_RAMAKRISHNAN_P_JHORA_1981`
  (`ReferenceAyanamsaDegrees` 23.595, `SRC_JHORA_EXPORT_RAMAKRISHNAN`), then reconcile the
  `Jagannatha` Swiss sidereal mode (26 → the ~0.85° discrepancy) with the CLI stream.
- **`FEAT-DATA-05`** — source-attributed yoga corpus schema (migrations 47–49): applied
  locally; roll to other environments after the corpus completes.

## Planned

- Rules-engine **Phase 2** — calculators read `tbl_Rule_*` instead of hard-coded C#.
- Wire the reference dimension tables (`tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras*`)
  into the engine (currently seeded + cross-checked, not read).
- `tbl_Dim_HouseSignification` / `tbl_Dim_PlanetSignification` / `tbl_Dim_PlanetHouseKaraka`
  (migration 030) — reserved, unapplied; `LifeAreaMap` hardcodes their data today.
- Strength schema completion for the uncomputed Kālabala components and Vimśopaka weights.

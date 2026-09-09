---
last_updated: 2026-09-04
---

# Database scripts

`db/ikiastrro.sql` is the **from-scratch baseline** (SMO script-out of the full schema
+ reference/seed data). A fresh machine runs only this file.

**Instance name:** this dev machine's SQL Server has no default/unnamed instance — the
named instance is `SQLSERVER2025`. `-S localhost` alone will not connect; use
`-S "localhost\SQLSERVER2025"` (matches `SqlConnectionFactory`'s default — see
`INFRASTRUCTURE.md`). Confirmed 2026-09-04 after the instance was found reinstalled
(version regressed RTM, `ikiastrro` DB gone) — rebuilt from this baseline the same day.

## Migrations

Incremental changes are numbered scripts `NN_<verb>_<noun>.sql`, applied in ascending
`NN` order against an existing `ikiastrro` database:

```
sqlcmd -S "localhost\SQLSERVER2025" -E -d ikiastrro -i db/NN_<name>.sql
```

Rules:

- **Idempotent.** Guard every statement (`IF COL_LENGTH`, `IF OBJECT_ID`, `IF NOT EXISTS
  (SELECT 1 FROM sys.indexes WHERE name = …)`, `IF NOT EXISTS (SELECT 1 FROM <seed>)`).
- **Self-recording.** End each script with an insert into `dbo.SchemaMigrations`
  (`WHERE NOT EXISTS`), so `SELECT * FROM dbo.SchemaMigrations` is the applied-history.
- **Never edit an applied migration.** Add a corrective `NN+1` script instead.
- **Fold forward.** Once a script is proven on a real DB, copy its final DDL into
  `db/ikiastrro.sql` so the baseline and the migrated DB converge. `vw_Chart_Consolidated`
  stays the last object in the baseline (it reads tables defined above it).

The historical `db/00_*.sql` one-offs predate this ledger and are not recorded in it.

## Inspection scripts

`checks/check_ayanamsa_dasha_benchmarks.sql` inspects migration 46's durable `BENCH_RAMAKRISHNAN_P_JHORA_1981` golden record: birth inputs, ten JHora longitudes, the six-system dasha catalogue, and nine Vimshottari Mahadasha boundaries. Comparison facts remain empty until the refresh command is implemented.

`db/checks/` holds **read-only, un-numbered** SQL for eyeballing table contents at
different levels (orientation → **chart inputs** → rule layer → per-chart facts →
conjunction deep-dive → sub-planet layer → row-count rollup). Level 0i enumerates
every table whose values feed chart generation: `tbl_BirthDetails` is the only
user-entered input; the rest are engine config (`tbl_Rule_Sets`), the chart-type /
varga catalogue, and static reference / rule masters. These are not migrations, are
never recorded in `dbo.SchemaMigrations`, and write nothing. Run them interactively
in SSMS / ADS.

`checks/49_yoga_chart_applicability.sql` and
`checks/51_yoga_context_requirements.sql` inspect the chart and non-chart inputs
required by each source-attributed yoga variant. A missing required input means
`NOT_EVALUATED`; it must never be interpreted as an absent yoga.

## Verifying a migrated / rebuilt DB

Migration 052 adds optional BirthDetails.Sex and tbl_Fact_YogaInputEvaluations.
The table now stores the complete output of ProductionYogaEngine, one row per
chart and source variant; its original name reflects the migration that introduced it.
Run `dotnet run --project tools/YogaInputVerification` for a transactional
save/load and yoga-fact check; all fixture writes roll back. Run
`dotnet test tests/Ikiastrro.Yoga.Tests` for isolated domain regression tests.
Existing charts acquire the new evaluation facts when regenerated.

After applying migrations (or a from-empty rebuild), run the CLI `verify-*` modes —
they are the regression suite for the schema + rule layer:

```
dotnet run --project src/Ikiastrro.Cli -- verify-schema        # id backfill, domain probes, group layer
dotnet run --project src/Ikiastrro.Cli -- verify-sources       # tbl_Dim_Source + every SourceRefCode resolves
dotnet run --project src/Ikiastrro.Cli -- verify-rules         # tbl_Rule_Catalog coverage + varga JSON round-trip
dotnet run --project src/Ikiastrro.Cli -- verify-dignity       # tbl_Rule_GrahaDignity tiling + score ladders + tbl_SignAttributes cross-check
dotnet run --project src/Ikiastrro.Cli -- verify-avastha       # planetary-state maps (keyed on DignityStatus)
dotnet run --project src/Ikiastrro.Cli -- verify-terminology   # tbl_Astro_Terminology covers the engine enums
```

Each exits non-zero on the first `FAIL`.

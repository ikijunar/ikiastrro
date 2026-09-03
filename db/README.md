# Database scripts

`db/ikiastrro.sql` is the **from-scratch baseline** (SMO script-out of the full schema
+ reference/seed data). A fresh machine runs only this file.

## Migrations

Incremental changes are numbered scripts `NN_<verb>_<noun>.sql`, applied in ascending
`NN` order against an existing `ikiastrro` database:

```
sqlcmd -S localhost -E -d ikiastrro -i db/NN_<name>.sql
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

## Verifying a migrated / rebuilt DB

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

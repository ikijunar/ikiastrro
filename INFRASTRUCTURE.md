# ikiastrro — Infrastructure

How this project is configured and deployed across environments. Companion to
`db/README.md` (migration-script contract) and `scripts/iis-setup.ps1` (IIS host).

## Environments

| Env | Purpose | Data | Deploy | Migrations |
|---|---|---|---|---|
| **dev** | local build + `verify-*` | 5 seed people | manual (`dotnet run`, `scripts/iis-setup.ps1`) | free — direct `sqlcmd`; baseline drop/rebuild allowed |
| **stage** | integration / pre-UAT | anonymised sample | scripted publish (CI) | numbered `db/NN_*.sql` only, ledgered in `SchemaMigrations`; **never** the baseline |
| **uat** | user acceptance | UAT dataset, refreshed from stage | scripted publish | as stage |
| **prod** | live | real | gated scripted publish, **backup first** | as stage; `SchemaMigrations` is the source of truth |

## Database naming

- **Principle (target):** the catalog is always `ikiastrro`. Environments differ by
  **server / instance**, supplied entirely by configuration —
  `dev = localhost` · `stage = <stage-sql-host>` · `uat = <uat-sql-host>` ·
  `prod = <prod-sql-host>`. `db/ikiastrro.sql` then applies **identically** to every
  environment.
- **Single-server fallback:** catalog per environment — `ikiastrro`, `ikiastrro_stage`,
  `ikiastrro_uat`, `ikiastrro_prod` — the name coming from the connection string's
  `Initial Catalog`, never a literal in code or a SQL script.
- **Hard rule (both):** an environment token **never** appears in a `tbl_` / `vw_` / `usp_` /
  `tvf_` / constraint / index name, or in C#. The environment boundary is the catalog (or the
  server). Switching environments is zero code change and zero schema change.

## Configuration & secrets

| Layer | Holds | Committed? |
|---|---|---|
| `src/Ikiastrro.Web/appsettings.json` | dev default — `ConnectionStrings:Ikiastrro = Server=localhost;Database=ikiastrro;Integrated Security=True;TrustServerCertificate=True;` | yes |
| `src/Ikiastrro.Web/appsettings.{Environment}.json` | non-secret per-env overrides (server host) | yes — **no credentials** |
| env var `ConnectionStrings__Ikiastrro` (Web) / `IKIASTRRO_CONNECTION` (CLI) | stage/uat/prod full connection string incl. credentials | **no** — set on the host / Key Vault / user-secrets |
| CLI `--db <name>` | one-off catalog targeting (scratch checks, a stage smoke) | n/a |

`ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` selects the `appsettings.{Environment}.json`
layer for the Web app. `SqlConnectionFactory.Create` precedence: explicit string →
`IKIASTRRO_CONNECTION` → `Server=localhost;Database={--db | IKIASTRRO_DB | ikiastrro};…`.

## Migration application

- **dev:** `sqlcmd -S localhost -E -b -i db/ikiastrro.sql` for a fresh install; the numbered
  `db/NN_*.sql` for an incremental change. A from-empty rebuild check uses the
  `sed 's/\[ikiastrro\]/[ikiastrro_scratch]/g; ...' db/ikiastrro.sql > tmp.sql` substitution
  with the ODBC `sqlcmd`; `sqlcmd -v DbName=<name>` works only with go-sqlcmd. (On the ODBC
  `sqlcmd`, v15/v17, the in-file `:setvar DbName "ikiastrro"` outranks the `-v` command-line
  value — documented Microsoft behavior — so `-v DbName=ikiastrro_scratch` would silently
  target `ikiastrro`. go-sqlcmd v1.x, `winget install sqlcmd`, honours `-v`.)
- **stage / uat / prod:** apply the numbered `db/NN_*.sql` chain **in order**, starting from
  the first number past the last row in `dbo.SchemaMigrations`. Each script self-records.
  **Never** run `db/ikiastrro.sql` (the baseline) against a populated higher environment.
- A **release** = the set of `NN_*.sql` since the last deployed number, applied in a
  transaction where the script allows, with a full backup taken first on prod.
- Baseline `db/ikiastrro.sql` is the "fresh install / dev" artifact and the folding target;
  the numbered chain is the "promote a change" artifact. Both must always describe the same
  end state (the scratch-rebuild check enforces it).

## Local dev quick start

```
sqlcmd -S localhost -E -b -i db/ikiastrro.sql          # create + seed the ikiastrro DB
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli -- compute-all Ramakrishnan   # (repeat per seed person)
dotnet run --project src/Ikiastrro.Cli -- verify-schema             # ... and the other verify-* modes
dotnet run --project src/Ikiastrro.Web                              # https://localhost:...
```

> Note: a from-empty rebuild check uses the
> `sed 's/\[ikiastrro\]/[ikiastrro_scratch]/g; ...' db/ikiastrro.sql > tmp.sql` substitution
> with the ODBC `sqlcmd`; `sqlcmd -v DbName=<name>` works only with go-sqlcmd (the in-file
> `:setvar DbName` outranks `-v` on the ODBC `sqlcmd`).

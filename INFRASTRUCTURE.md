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
  `db/NN_*.sql` for an incremental change. A from-empty rebuild check (`db/ikiastrro.sql`
  against a throwaway `ikiastrro_scratch`):
  - **go-sqlcmd** (v1.x, `winget install sqlcmd`) honours the override —
    `sqlcmd -v DbName=ikiastrro_scratch -i db/ikiastrro.sql`.
  - **ODBC `sqlcmd`** (v15/v17) does **not**: an in-file `:setvar` outranks the `-v`
    command-line value (documented Microsoft behavior), so `-v DbName=…` silently targets
    `ikiastrro`. Substitute the `:setvar` line itself instead:
    `sed 's/:setvar DbName "ikiastrro"/:setvar DbName "ikiastrro_scratch"/' db/ikiastrro.sql > db/_scratch_tmp.sql`
    then `sqlcmd -b -i db/_scratch_tmp.sql`.
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

> Note: a from-empty rebuild check runs `db/ikiastrro.sql` against a throwaway
> `ikiastrro_scratch`. With **go-sqlcmd**: `sqlcmd -v DbName=ikiastrro_scratch -i db/ikiastrro.sql`.
> With the **ODBC `sqlcmd`** (`-v` is outranked by the in-file `:setvar`): substitute the
> `:setvar` line — `sed 's/:setvar DbName "ikiastrro"/:setvar DbName "ikiastrro_scratch"/' db/ikiastrro.sql > db/_scratch_tmp.sql` then `sqlcmd -b -i db/_scratch_tmp.sql`.

## Version control & repository hygiene

Two remotes, both with `master` + `feat/*` branches: **`origin`**
(`github.com/rammyps/ikiastrro`) and **`ikijunar`** (`github.com/ikijunar/ikiastrro`).
`master` is the shared, published line; feature work happens on `feat/<topic>` and is
**not pushed** until it FF-merges to `master`.

### What is tracked vs. ignored

| Tracked (belongs in git) | Ignored (`.gitignore`) |
|---|---|
| `src/**` (all C#, `.csproj`, `.slnx`), `appsettings.json` (dev default, **no secrets**) | `bin/`, `obj/`, `.vs/`, `*.user`, `/publish/` — build output |
| `db/ikiastrro.sql` (baseline), `db/NN_*.sql` (numbered), `db/_archive/**` (frozen pre-consolidation chain), `db/README.md` | `/db/*.ipynb` — ad-hoc DDL notebooks |
| `docs/**` — every `.md`, **and `docs/artifacts/**`** (DB diagrams, rendered `.d2` + source, `reference-charts/` = the JHora golden-record exports + images, UI mockups) | `/scratch/` — pure throwaway; anything a run needs long-term is promoted into `docs/artifacts/` first |
| root docs (`README.md`, `ARCHITECTURE.md`, `PRODUCT.md`, `INFRASTRUCTURE.md`, `master_ikiastrro.md`), `scripts/**`, `decisions/**` | `/build_output.txt`, `/verify_*_output.txt` — CLI stdout redirects |
| `.gitignore` itself | `/_research/` — vendored OSS reference repos; `/.superpowers/` — SDD scratch; `/.claude/worktrees/` |

Rule: a file that a future clone needs to **build, verify, or understand** the project is
tracked; everything a run *produces* or a session *scratches* is ignored. When a scratch file
turns out to be a lasting reference (a vendor export, a diagram, a mockup), it moves into the
matching `docs/artifacts/` subfolder and is committed there — `docs/artifacts/` is the only
sanctioned home for non-prose project inputs/outputs (`STANDARDS.md §M.1`).

### How artifacts reach `master`

`docs/artifacts/**` is ordinary tracked content — it rides the same branch → `master` merge as
the code. No separate mechanism.

- **Binaries** (`.svg`, `.png`) are committed inline. Current sizes are small (largest is a
  ~100 KB reference chart); **git-lfs is not used** and is not needed below a few MB per file.
  If a future artifact is large or churns often (e.g. a regenerated multi-MB render), add a
  `.gitattributes` LFS rule for that path *before* the first commit of it.
- Rendered outputs (`.d2` → `.svg`) are committed **alongside** their source so the image is
  regenerable: `d2 --theme 0 --pad 20 diagrams/<x>.d2 diagrams/<x>.svg`. Re-run and re-commit
  when the source changes; never hand-edit the `.svg`.

### Promoting a feature branch to `master`

```
git checkout master
git merge --ff-only feat/<topic>          # FF only — no merge commits on master
git push origin master && git push ikijunar master
git checkout feat/<topic>                 # continue, or delete if the topic is done
```

If `--ff-only` fails, `master` moved — rebase `feat/<topic>` onto `master` first. Never
`git push --force` a shared branch.

### Before any push to a public remote

1. `git status` clean; `git log --stat origin/master..HEAD` reviewed — no stray files.
2. No secrets: `git grep -nE "password|pwd=|Integrated Security=False|AccountKey=|api[_-]?key" -- ':!*.md'`
   returns nothing; `appsettings.json` carries only the dev Windows-Auth string; real
   credentials live only in `appsettings.{Environment}.json` (untracked) or host env vars.
3. `dotnet build` 0/0 and the `verify-*` sweep green on the tip commit.

### Pending

- **`db/` history scrub** (approved 2026-08-31, not executed): strip every `db/**` SQL from
  both branches' history (`git filter-branch` — no `git filter-repo`/Python here), force-push
  both remotes, `git bundle` backup first. After it runs, `db/ikiastrro.sql` + the `db/00_*.sql`
  become local-only (untracked); the C# is unaffected. Track this in `../ikiastrro.md`.

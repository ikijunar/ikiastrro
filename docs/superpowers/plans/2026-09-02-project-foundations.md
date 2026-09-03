# Project Foundations (Plan 0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Put the project's non-code foundations in place — a documentation taxonomy, a single
source-citation registry, an `INFRASTRUCTURE.md` covering dev/stage/uat/prod, a `PRODUCT.md`
feature catalogue with completion tracking, and environment-agnostic database access — so the
engine reorganization (Plan 1) and everything after it build on a stable base.

**Architecture:** Almost entirely additive. New Markdown files (`PRODUCT.md`,
`INFRASTRUCTURE.md`, `docs/research/reference-sources.md`, `docs/artifacts/*`, two
`_TEMPLATE.md`), new workspace-standard sections (`STANDARDS.md` §D.2 / §M.3 / §M.4 + a §M.1
extension), one small reference table (`tbl_Dim_Source`, migration 15), one new CLI verify mode
(`verify-sources`), and a config-driven `SqlConnectionFactory` that keeps its old default
behaviour byte-for-byte. No `Ikiastrro.Core` file moves. No astrology logic changes.

**Tech Stack:** .NET 8 / C#; SQL Server (Windows Auth, `localhost`, DB `ikiastrro`); Dapper;
`db/` numbered idempotent migrations + `dbo.SchemaMigrations` ledger; `sqlcmd`; verification via
`dotnet build` + CLI `verify-*` modes (no xUnit project — the established project cadence); `d2`
v0.8.2 (`C:\Program Files\D2\d2`) for diagrams.

**Spec:** `docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md` (§16 and
§17 are the Plan 0 detail; §12 "Plan 0 — Foundations" is the task list this plan expands).

## Global Constraints

- Branch: `feat/ikiastrro-workspace-ui` — work directly on it (as every plan since the
  divisional-chart work has).
- Every commit ends with exactly these two trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE
  ```
- Do **not** `git push` unless the user asks.
- Migrations: numbered `NN_<verb>_<noun>.sql` under `db/`, 2-digit (the project restarted
  numbering at `01` after the 2026-08-31 schema normalization; the last applied is `14`). Each
  is idempotent (`IF NOT EXISTS` / `IF COL_LENGTH(...) IS NULL` guards) and self-records in
  `dbo.SchemaMigrations`. This plan adds **`15`** only. Apply to the live DB with
  `sqlcmd -S localhost -E -d ikiastrro -b -i db/15_create_dim_source.sql`.
- **No test project.** "Test" means: `dotnet build Ikiastrro.slnx` is 0 warnings / 0 errors,
  and the relevant `dotnet run --project src/Ikiastrro.Cli -- verify-*` mode prints `ALL PASS`.
  The five existing modes are `verify-schema`, `verify-vargas`, `verify-avastha`,
  `verify-functional-nature`, `verify-jaimini`.
- **No new astrology and no `Ikiastrro.Core` file moves in Plan 0.** Those are Plan 1.
- `STANDARDS.md` lives at `D:\@ClaudeSpace\STANDARDS.md` (workspace-wide, outside the repo — a
  normal file edit, no repo commit for that file; note it in the repo commit message instead).
- `../ikiastrro.md` (`D:\@ClaudeSpace\ikiastrro.md`) and the memory file are outside the repo —
  edit directly, no commit.
- SQL object naming follows `STANDARDS.md` §D / §D.1: `tbl_PascalCase`, plural, and every **new**
  table declares the `Dim` / `Rule` / `Fact` infix. The spec's `tbl_Ref_Source` is therefore
  created as **`tbl_Dim_Source`** (it is a controlled vocabulary = a dimension). Note this
  one-word deviation from the spec in the Task 4 commit.

---

## File Structure

**New — repo root:**
- `PRODUCT.md` — the feature catalogue + completion tracker (Task 11).
- `INFRASTRUCTURE.md` — environments, DB naming, config/secrets, migration policy (Task 10).

**New — `docs/`:**
- `docs/artifacts/` with `db/`, `ui/`, `diagrams/`, `reference-charts/` subfolders (Task 2).
- `docs/research/reference-sources.md` — the human-editable citation master (Task 3).
- `docs/superpowers/specs/_TEMPLATE.md`, `docs/superpowers/plans/_TEMPLATE.md` (Task 2).

**Moved:**
- `docs/dotnet_engine_map.md` → `docs/artifacts/dotnet_engine_map.md` (Task 2).
- `docs/research/*.d2` (3 files) → `docs/artifacts/diagrams/` + a rendered `.svg` beside each
  (Task 2).

**New — `db/`:**
- `db/15_create_dim_source.sql` — idempotent create + seed of `tbl_Dim_Source` (Task 4).

**Modified — `db/`:**
- `db/ikiastrro.sql` — fold `tbl_Dim_Source` (Task 4); parameterize the catalog name with a
  `sqlcmd :setvar` (Task 9).

**Modified — code:**
- `src/Ikiastrro.Data/SqlConnectionFactory.cs` — new `Create(...)` overload (Task 6).
- `src/Ikiastrro.Cli/Program.cs` — `--db` extraction + use `Create(...)` (Task 7); new
  `verify-sources` mode (Task 5).
- `src/Ikiastrro.Web/appsettings.json` + `src/Ikiastrro.Web/Program.cs` — connection string from
  configuration (Task 8).

**Modified — workspace / index docs:**
- `D:\@ClaudeSpace\STANDARDS.md` — §D.2, §M.1 extension, §M.3, §M.4 (Task 1).
- `master_ikiastrro.md`, `D:\@ClaudeSpace\ikiastrro.md`, memory file (Task 12).

---

## Task 1: Workspace standards — naming (§D.2) + docs taxonomy (§M.1 ext, §M.3, §M.4)

**Files:**
- Modify: `D:\@ClaudeSpace\STANDARDS.md` (workspace file — edit, no repo commit; the repo commit
  for this task carries only the note "STANDARDS.md §D.2/§M.1/§M.3/§M.4 added" plus, if you want
  a repo anchor, a one-line pointer added to `master_ikiastrro.md` — but that pointer is Task 12,
  so Task 1 has **no repo file change**. Commit it anyway with `--allow-empty` is wrong; instead
  fold the `master_ikiastrro.md` pointer for §D.2/§M.3/§M.4 into Task 1 so it has a repo
  deliverable — see Step 4.)

**Interfaces:**
- Produces: the written conventions that Tasks 2–12 cite by number — `§D.2` (identifier
  naming), `§M.1` "required root files" now including `PRODUCT.md` + `INFRASTRUCTURE.md` and a
  `docs/artifacts/` location, `§M.3` (`PRODUCT.md` structure + the 5-box checklist), `§M.4`
  (`tbl_Dim_Source` + `docs/research/reference-sources.md` + the `SRC_*` cite-by-code rule).

- [ ] **Step 1: Add `§D.2` after `§D.1` in `STANDARDS.md`**

Find the end of `### D.1 — Table category infix: Dim / Rule / Fact (star schema)` (it ends
before `## E. Git conventions`). Insert:

```markdown
### D.2 — Domain identifier naming (hybrid Sanskrit / English + `Code`)

Adopted 2026-09-02 with `ikiastrro`'s engine reorganization
(`docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md` §5).

| Kind | Identifier rule | Example |
|---|---|---|
| Engine / structural | English | `HouseEngine`, `StrengthEngine`, `ChartPipeline` |
| Greenfield domain concept (no shipped identifier yet) | English | `AgeState { Infant..Dead }`, `PositionalStrength` |
| Established Jyotiṣa noun | the canonical romanized Sanskrit term, one spelling | `Nakshatra`, `Dasha`, `Karaka`, `Navamsa`, `CharaKaraka.AK` |
| Enum member | per the kind above | `ZodiacName.Aries` |

- Every domain concept also has a stable ASCII **`Code`** (`SIGN_ARIES`, `VARGA_D9`,
  `KARAKA_AK`, `AVASTHA_BALA`, `YOGA_GAJAKESARI`). The `Code` is the contract between C#, the
  database, and the terminology table.
- **No display string** — Sanskrit, English, or Tamil — is hard-coded in C#, a `.razor`, or a
  SQL view. Display resolves through the terminology catalogue by `Code`.
- Prefer keeping the Sanskrit noun over an awkward English calque (`Nakshatra`, not
  `LunarMansion`; `CharaKaraka.AK`, not `TemporalSignificator.Soul`). Rename to English only
  where it is a genuine readability win (the avastha *states*: `AgeState`, `WakefulnessState`).
```

- [ ] **Step 2: Extend `§M.1` — required root files + `docs/artifacts/`**

In `§M.1`, the bullet that lists `master_<name>.md` as "**required**": add two siblings and one
location row. After the `master_<name>.md` bullet insert:

```markdown
- **`PRODUCT.md`** (repo root) — **required for a code project past its first feature.** The
  feature catalogue + completion tracker. Structure in §M.3.
- **`INFRASTRUCTURE.md`** (repo root) — **required once a project has more than a local dev
  database.** Environments, database-naming rule, config & secrets layering, migration policy.
```

In the `docs/<category>-<slug>.md` category table, after the table add:

```markdown
  **Non-prose artifacts** (DB diagrams, rendered `.d2` + its source, UI mockups shared by the
  user, exported reference data, screenshots) live under `docs/artifacts/` in a typed subfolder
  — `db/`, `ui/`, `diagrams/`, `reference-charts/`. Artifacts carry **no** classical-source
  attribution text; they cite an `SRC_*` code (§M.4) or link the relevant `research-` doc.
```

- [ ] **Step 3: Add `§M.3` and `§M.4` after `§M.2` in `STANDARDS.md`**

`§M.2` ends before `---` / `## Change log`. Insert:

```markdown
### M.3 — Feature catalogue & completion (`PRODUCT.md`)

`PRODUCT.md` is the answer to "what does the software do, and how much of each part is done".

- One row per feature, ID `FEAT-<AREA>-<NN>` where `<AREA>` is the owning engine / subsystem
  code (`VARGA`, `HOUSE`, `KARAKA`, `STRENGTH`, `YOGA`, `DASHA`, `TERM`, `INFRA`, `DOCS`, …).
- Each feature carries:
  - **Status ladder:** `Planned → Designed → DB → Core → Verified → Web → Done`.
  - **A 5-box checklist** — `DB` (tables / migrations / seed), `Core` (engine logic),
    `Verify` (a `verify-*` mode exists and passes), `Web` (surfaced in the UI), `Docs`
    (reference doc updated). `% done = checked ÷ 5`.
  - **Research flag** — `complete | partial | not started`, with links to the `research-` docs
    it depends on.
  - **Spec** and **Plan** links, and the `verify-*` name.
- A **rollup table** at the top: per area — feature count, average %, and the count still
  missing each checklist dimension (so "how much DB is left" / "how much Web is left" read off
  one column).
- A plan's final task ticks the `PRODUCT.md` boxes its work completed.

### M.4 — Source citation registry

Every classical text, third-party library, or external export the project relies on is **one
row** in `tbl_Dim_Source` (`Code`, `Title`, `Author`, `Edition`, `Tradition`, `Notes`),
mirrored in `docs/research/reference-sources.md` (the human-editable master; a seed mode
regenerates the table from it).

- The `Code` format is `SRC_<UPPER_SNAKE>` (`SRC_BPHS`, `SRC_BPHS_27`, `SRC_RAMAN_HTJH`,
  `SRC_PHALADEEPIKA`, `SRC_PYJHORA`).
- Anything that needs to cite a source — a `tbl_Rule_*` row (`SourceRefCode` column), a
  terminology row, a spec, a plan, a code comment — names the `SRC_*` code.
- Author / title / page / edition strings appear **only** in the registry. Not inline in a
  rule row, a diagram, an artifact, `ARCHITECTURE.md`, or a code comment.
```

- [ ] **Step 4: Add the repo-side pointers to `master_ikiastrro.md`**

So this task has a committable repo deliverable. In `master_ikiastrro.md`, under the "Start
here" table, add rows for `PRODUCT.md` and `INFRASTRUCTURE.md` (mark them "planned — Plan 0"
for now; Task 12 flips them to "living"). Also add a one-line note under the intro:
`Naming + doc conventions: STANDARDS.md §D.2 (identifiers), §M.1/§M.3 (docs & PRODUCT.md), §M.4 (citations).`

- [ ] **Step 5: Verify the standards read consistently**

Re-read `§D`, `§D.1`, `§D.2`, `§M.1`, `§M.2`, `§M.3`, `§M.4` end to end. Check: no section
number collides (there is an existing `§M.2` "Root-level file prefixes" — the new ones are
`§M.3`/`§M.4`, correct); the `§M.1` category table still parses; `docs/artifacts/` is mentioned
once, in `§M.1`. Fix any dangling cross-reference.

- [ ] **Step 6: Commit**

```bash
git add master_ikiastrro.md
git commit -m "$(printf 'docs(standards): add STANDARDS §D.2 (naming), §M.1 ext, §M.3 (PRODUCT.md), §M.4 (citations)\n\nWorkspace STANDARDS.md edited out-of-repo; master_ikiastrro.md carries the\nrepo-side pointers + planned rows for PRODUCT.md / INFRASTRUCTURE.md.\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 2: `docs/artifacts/` tree + move diagrams + spec/plan templates

**Files:**
- Create: `docs/artifacts/db/.gitkeep`, `docs/artifacts/ui/.gitkeep`,
  `docs/artifacts/diagrams/.gitkeep`, `docs/artifacts/reference-charts/.gitkeep`
- Move: `docs/dotnet_engine_map.md` → `docs/artifacts/dotnet_engine_map.md`
- Move: `docs/research/karaka-avastha-linkage.d2`, `docs/research/planetary-avasthas.d2`,
  `docs/research/planetary-roles.d2` → `docs/artifacts/diagrams/`
- Create: `docs/artifacts/diagrams/karaka-avastha-linkage.svg`,
  `docs/artifacts/diagrams/planetary-avasthas.svg`, `docs/artifacts/diagrams/planetary-roles.svg`
- Create: `docs/superpowers/specs/_TEMPLATE.md`, `docs/superpowers/plans/_TEMPLATE.md`

**Interfaces:**
- Produces: the `docs/artifacts/` location `§M.1` now references; the two templates Task 1 §M.1
  and future brainstorming/writing-plans runs use.

- [ ] **Step 1: Create the artifacts tree**

```bash
mkdir -p docs/artifacts/db docs/artifacts/ui docs/artifacts/diagrams docs/artifacts/reference-charts
for d in db ui diagrams reference-charts; do touch "docs/artifacts/$d/.gitkeep"; done
```

- [ ] **Step 2: Move the engine map and the `.d2` sources**

```bash
git mv docs/dotnet_engine_map.md docs/artifacts/dotnet_engine_map.md
git mv docs/research/karaka-avastha-linkage.d2 docs/artifacts/diagrams/
git mv docs/research/planetary-avasthas.d2 docs/artifacts/diagrams/
git mv docs/research/planetary-roles.d2 docs/artifacts/diagrams/
```

- [ ] **Step 3: Render each `.d2` to `.svg`**

```bash
for f in karaka-avastha-linkage planetary-avasthas planetary-roles; do
  d2 --theme 0 --pad 20 "docs/artifacts/diagrams/$f.d2" "docs/artifacts/diagrams/$f.svg"
done
```
Expected: `success: successfully compiled ...` three times. If any `.d2` fails to compile,
**do not hand-fix the astrology content** — record the compile error in the commit message and
move the `.d2` without an `.svg` (the source still belongs in `artifacts/`).

- [ ] **Step 4: Fix the one stale link**

`docs/artifacts/dotnet_engine_map.md` has a note block referencing itself as
`docs/dotnet_engine_map.md`. Update that path to `docs/artifacts/dotnet_engine_map.md`. Grep
the repo for any other reference: `grep -rn "docs/dotnet_engine_map" --include=*.md .` — update
each hit (likely only `master_ikiastrro.md`, handled in Task 12; if `../ikiastrro.md` outside
the repo mentions it, leave it — Task 12).

- [ ] **Step 5: Write `docs/superpowers/specs/_TEMPLATE.md`**

```markdown
# <Feature> — design

**Status:** draft (for plan) · **Created:** YYYY-MM-DD · **Branch:** <branch>
**Features:** FEAT-<AREA>-<NN>[, …]   <!-- rows this spec creates/advances in PRODUCT.md -->

## Research
Status: [ ] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| docs/research-<slug>.md | <what> | <complete / partial — what's missing / not started> |

## 1. Problem
## 2. Goals
## 3. Non-goals
## 4. Design
## 5. Data / schema
## 6. Verification
## 7. Risks & mitigations
## 8. Open decisions
```

- [ ] **Step 6: Write `docs/superpowers/plans/_TEMPLATE.md`**

```markdown
# <Feature> Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** <one sentence>
**Architecture:** <2–3 sentences>
**Tech Stack:** <key tech>
**Spec:** <path>

## Research
Status: [ ] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|

## Global Constraints
- Branch: <branch>
- Commit trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: <url>
  ```
- Do not push unless asked.
- Migrations: numbered `NN_*.sql`, idempotent, self-recording; last applied is `NN`.
- No test project — verify via `dotnet build` + CLI `verify-*` modes.

---

## Task N: <name>
**Files:** Create / Modify / Test
**Interfaces:** Consumes / Produces
- [ ] Step 1: …
- [ ] Step N: Commit

## PRODUCT.md
On completion, tick: FEAT-<AREA>-<NN> → [DB|Core|Verify|Web|Docs] boxes done.
```

- [ ] **Step 7: Build sanity (no code changed)**

Run: `dotnet build Ikiastrro.slnx`
Expected: 0 warnings / 0 errors (nothing in `src/` changed — this just confirms the `git mv`s
didn't disturb a project file).

- [ ] **Step 8: Commit**

```bash
git add docs/artifacts docs/superpowers/specs/_TEMPLATE.md docs/superpowers/plans/_TEMPLATE.md
git commit -m "$(printf 'docs: docs/artifacts/ tree + move engine map & .d2 diagrams + spec/plan templates\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 3: `docs/research/reference-sources.md` — the citation master

**Files:**
- Create: `docs/research/reference-sources.md`

**Interfaces:**
- Produces: the `SRC_*` codes Task 4 seeds into `tbl_Dim_Source` and that every later
  `SourceRefCode` column points at. Columns: `Code | Title | Author | Edition / Version |
  Tradition | Used by | Notes`.

- [ ] **Step 1: Write the file**

```markdown
# Reference sources — citation registry (`SRC_*`)

The single place classical texts, libraries, and external exports are named. Everything else
(rule tables, terminology, specs, plans, code comments) cites the `Code` — never the title or
author inline (`STANDARDS.md §M.4`). Mirrored into `dbo.tbl_Dim_Source` by
`db/15_create_dim_source.sql` / the `seed-sources` path; keep the two in sync.

| Code | Title | Author | Edition / Version | Tradition | Used by | Notes |
|---|---|---|---|---|---|---|
| `SRC_BPHS` | Brihat Parashara Hora Shastra | Parāśara (attrib.) | — | Parāśari | dignity, aspects, vargas, avasthas | umbrella; prefer a chapter-scoped code below when known |
| `SRC_BPHS_26` | BPHS ch. 26 — Graha Dṛṣṭi | — | — | Parāśari | `tbl_Rule_AspectOffset` | 7th full; Mars 4/8, Jupiter 5/9, Saturn 3/10 |
| `SRC_BPHS_27` | BPHS ch. 27 — Ṣaḍbala | — | — | Parāśari | Strength engine (Plan 3) | lookup tables need a specific edition — see `research-topic-coverage.md` |
| `SRC_BPHS_COMBUSTION` | BPHS — Asta (combustion) orbs | — | — | Parāśari | `tbl_Rule_CombustionOrb` | Moon 12°, Mars 17°/8°R, Mercury 14°/12°R, Jupiter 11°, Venus 10°/8°R, Saturn 15° |
| `SRC_BPHS_AVASTHA` | BPHS — Bālādi & Jāgradādi avasthās | — | — | Parāśari | `tbl_Rule_AgeState`, `tbl_Rule_WakefulnessState` | Bāla .25 / Kumāra .50 / Yuva 1 / Vṛddha .125 / Mṛta 0 |
| `SRC_PHALADEEPIKA` | Phaladeepika | Mantreśvara | — | classical | combustion cross-check | alt. orb set |
| `SRC_RAMAN_HTJH` | How to Judge a Horoscope (vols I–II) | B. V. Raman | — | Raman | house / Lagna significations, functional nature | OCR extract under `D:\@ClaudeSpace\BookExtracts\_work\how-to-judje-a-horoscope-i_p1-312\` |
| `SRC_RAMAN_HINDU_PREDICTIVE` | Hindu Predictive Astrology | B. V. Raman | — | Raman | general | — |
| `SRC_PYJHORA` | PyJHora (source) | B. Satya Prakash (`pyjhora`) | vendored `_research/PyJHora` | mixed | varga formulae, special-lagna / upagraha algorithms | AGPL — vendored for reference, not linked |
| `SRC_JHORA` | Jagannatha Hora (desktop) | P. V. R. Narasimha Rao | v8.x | mixed | golden-record verification | — |
| `SRC_JHORA_EXPORT_RAMAKRISHNAN` | JHora natal export — 1_Ramakrishnan | — | 22 Apr 1981 05:30 Chennai | — | `verify-vargas`, `verify-jaimini` golden values | file `scratch/Rammy_Jagannatha.txt` |
| `SRC_RATH_VARGA` | Vedic Astrology / varga methods | Sanjay Rath | — | Jaimini / SJC | D11 (Rudramsa), argala | one method-ambiguous varga |
| `SRC_VEDASTRO` | VedAstro.Library | (open source) | pre-2026-08-24 | mixed | historical — replaced by SwissEphNet | enum spellings (`Capricornus`, `Aswini`) inherited from here |
| `SRC_SWISSEPH` | Swiss Ephemeris / SwissEphNet | Astrodienst / port | SwissEphNet 2.8.0.2 | astronomy | `SwissEphemerisProvider` | Moshier mode, Lahiri sidereal |

Add a row the same change that first cites a new source. A chapter/verse-scoped code
(`SRC_BPHS_27`) is preferred over the umbrella (`SRC_BPHS`) once the location is confirmed.
```

- [ ] **Step 2: Commit**

```bash
git add docs/research/reference-sources.md
git commit -m "$(printf 'docs(research): reference-sources.md — SRC_* citation master\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 4: `tbl_Dim_Source` (migration 15) + seed + fold into the baseline

**Files:**
- Create: `db/15_create_dim_source.sql`
- Modify: `db/ikiastrro.sql`

**Interfaces:**
- Produces: `dbo.tbl_Dim_Source (Id INT IDENTITY PK, Code VARCHAR(40) UNIQUE, Title
  NVARCHAR(200) NOT NULL, Author NVARCHAR(120) NULL, Edition NVARCHAR(80) NULL, Tradition
  VARCHAR(40) NULL, Notes NVARCHAR(400) NULL, IsActive BIT NOT NULL DEFAULT 1)` +
  `CK_Dim_Source_Code` (`Code LIKE 'SRC[_]%'`). Seeded with the rows from
  `docs/research/reference-sources.md`. `verify-sources` (Task 5) consumes it.

- [ ] **Step 1: Write `db/15_create_dim_source.sql`**

```sql
-- =====================================================================
-- 15 — tbl_Dim_Source: the SRC_* citation registry (STANDARDS §M.4). Mirror of
-- docs/research/reference-sources.md. Every SourceRefCode column added in later
-- plans (rule tables, terminology) FKs here by Code. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/15_create_dim_source.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Dim_Source', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_Source (
        Id        INT IDENTITY(1,1) CONSTRAINT PK_Dim_Source PRIMARY KEY,
        Code      VARCHAR(40)   NOT NULL CONSTRAINT UQ_Dim_Source_Code UNIQUE,
        Title     NVARCHAR(200) NOT NULL,
        Author    NVARCHAR(120) NULL,
        Edition   NVARCHAR(80)  NULL,
        Tradition VARCHAR(40)   NULL,
        Notes     NVARCHAR(400) NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Dim_Source_IsActive DEFAULT 1,
        CONSTRAINT CK_Dim_Source_Code CHECK (Code LIKE 'SRC[_]%')
    );
END
GO
-- Seed / re-seed (idempotent MERGE on Code)
;WITH src (Code, Title, Author, Edition, Tradition, Notes) AS (
    SELECT * FROM (VALUES
        ('SRC_BPHS',            N'Brihat Parashara Hora Shastra', N'Parāśara (attrib.)', NULL, 'Parasari', N'Umbrella; prefer a chapter-scoped code where known'),
        ('SRC_BPHS_26',         N'BPHS ch. 26 — Graha Drishti',   NULL, NULL, 'Parasari', N'7th full; Mars 4/8, Jupiter 5/9, Saturn 3/10'),
        ('SRC_BPHS_27',         N'BPHS ch. 27 — Shadbala',        NULL, NULL, 'Parasari', N'Strength engine (Plan 3); lookup tables need a cited edition'),
        ('SRC_BPHS_COMBUSTION', N'BPHS — Asta (combustion) orbs', NULL, NULL, 'Parasari', N'Moon 12, Mars 17/8R, Mercury 14/12R, Jupiter 11, Venus 10/8R, Saturn 15'),
        ('SRC_BPHS_AVASTHA',    N'BPHS — Baladi & Jagradadi avasthas', NULL, NULL, 'Parasari', N'Bala .25 / Kumara .50 / Yuva 1 / Vriddha .125 / Mrita 0'),
        ('SRC_PHALADEEPIKA',    N'Phaladeepika',                  N'Mantreshvara', NULL, 'classical', N'Combustion orb cross-check'),
        ('SRC_RAMAN_HTJH',      N'How to Judge a Horoscope (I–II)', N'B. V. Raman', NULL, 'Raman', N'House/Lagna significations, functional nature; OCR extract in BookExtracts'),
        ('SRC_RAMAN_HINDU_PREDICTIVE', N'Hindu Predictive Astrology', N'B. V. Raman', NULL, 'Raman', N'General'),
        ('SRC_PYJHORA',         N'PyJHora (source)',              N'pyjhora', N'_research/PyJHora', 'mixed', N'Varga formulae, special-lagna/upagraha algorithms; AGPL, vendored for reference'),
        ('SRC_JHORA',           N'Jagannatha Hora (desktop)',     N'P. V. R. Narasimha Rao', N'v8.x', 'mixed', N'Golden-record verification'),
        ('SRC_JHORA_EXPORT_RAMAKRISHNAN', N'JHora natal export — 1_Ramakrishnan', NULL, N'22 Apr 1981 05:30 Chennai', NULL, N'verify-vargas / verify-jaimini golden values; scratch/Rammy_Jagannatha.txt'),
        ('SRC_RATH_VARGA',      N'Vedic Astrology / varga methods', N'Sanjay Rath', NULL, 'Jaimini/SJC', N'D11 (Rudramsa), argala'),
        ('SRC_VEDASTRO',        N'VedAstro.Library',              N'(open source)', N'pre-2026-08-24', 'mixed', N'Historical — replaced by SwissEphNet; enum spellings inherited'),
        ('SRC_SWISSEPH',        N'Swiss Ephemeris / SwissEphNet', N'Astrodienst / port', N'SwissEphNet 2.8.0.2', 'astronomy', N'Moshier mode, Lahiri sidereal')
    ) v (Code, Title, Author, Edition, Tradition, Notes)
)
MERGE dbo.tbl_Dim_Source AS tgt
USING src ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET tgt.Title = src.Title, tgt.Author = src.Author,
    tgt.Edition = src.Edition, tgt.Tradition = src.Tradition, tgt.Notes = src.Notes
WHEN NOT MATCHED THEN INSERT (Code, Title, Author, Edition, Tradition, Notes)
    VALUES (src.Code, src.Title, src.Author, src.Edition, src.Tradition, src.Notes);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '15_create_dim_source.sql', 'tbl_Dim_Source (SRC_* citation registry) + seed'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '15_create_dim_source.sql');
GO
PRINT '15 applied: tbl_Dim_Source has ' + CAST((SELECT COUNT(*) FROM dbo.tbl_Dim_Source) AS VARCHAR(10)) + ' rows.';
GO
```

- [ ] **Step 2: Apply to the live DB**

Run: `sqlcmd -S localhost -E -d ikiastrro -b -i db/15_create_dim_source.sql`
Expected: `15 applied: tbl_Dim_Source has 14 rows.`, no `Msg` lines. Re-run → same count, no
errors (idempotent).

- [ ] **Step 3: Fold into `db/ikiastrro.sql`**

Open `db/ikiastrro.sql`. Find where the other `tbl_Dim_*` tables are created (search
`tbl_Dim_ChartType` — around the folded-migrations block near line 2269). Immediately after that
table's `CREATE` + seed, paste the `CREATE TABLE dbo.tbl_Dim_Source` + the `MERGE` seed from
Step 1 (drop the `USE`/`SchemaMigrations`/`PRINT` lines — the baseline creates schema + seed
only). Add `tbl_Dim_Source` to the header comment's "seed data for the reference/master tables"
list (near line 5–8).

- [ ] **Step 4: Scratch-DB rebuild check**

```bash
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END"
sed 's/\[ikiastrro\]/[ikiastrro_scratch]/g; s/N'"'"'ikiastrro'"'"'/N'"'"'ikiastrro_scratch'"'"'/g' db/ikiastrro.sql > db/_scratch_tmp.sql
sqlcmd -S localhost -E -b -i db/_scratch_tmp.sql
sqlcmd -S localhost -E -d ikiastrro_scratch -h -1 -W -Q "SELECT COUNT(*) FROM dbo.tbl_Dim_Source"
sqlcmd -S localhost -E -Q "ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch;"
rm db/_scratch_tmp.sql
```
Expected: build with no `Msg` lines; the count query prints `14`.
*(Task 9 replaces this sed dance with `-v DbName=ikiastrro_scratch`; use the sed form here since
Task 9 hasn't run yet.)*

- [ ] **Step 5: Commit**

```bash
git add db/15_create_dim_source.sql db/ikiastrro.sql
git commit -m "$(printf 'feat(db): tbl_Dim_Source (migration 15) — SRC_* citation registry\n\nNamed tbl_Dim_Source (not the spec's tbl_Ref_Source) per STANDARDS §D.1 —\na controlled vocabulary is a dimension. Seed mirrors\ndocs/research/reference-sources.md. Folded into the baseline.\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 5: `verify-sources` CLI mode

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (add a new mode block immediately before the
  `if (args.Length > 0 && args[0] == "verify-schema")` block — around line 566)

**Interfaces:**
- Consumes: `connectionFactory` (already in scope at the top of `Program.cs`), `Dapper`
  (already `using`d).
- Produces: CLI mode `verify-sources` — exit 0 / `ALL PASS` when `tbl_Dim_Source` is well-formed.

- [ ] **Step 1: Write the mode block**

Insert before the `verify-schema` block:

```csharp
// --- `dotnet run -- verify-sources` : tbl_Dim_Source well-formedness (STANDARDS §M.4) ---
if (args.Length > 0 && args[0] == "verify-sources")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Check(string label, long violations)
    {
        var ok = violations == 0;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: {violations} violation(s)");
        if (!ok) failures++;
    }
    long Count(string sql) => conn.ExecuteScalar<long>(sql);

    Check("tbl_Dim_Source is seeded (>= 10 rows)",
        Count("SELECT CASE WHEN COUNT(*) >= 10 THEN 0 ELSE 1 END FROM dbo.tbl_Dim_Source"));
    Check("every Code matches SRC_<UPPER_SNAKE>",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Dim_Source
                WHERE Code NOT LIKE 'SRC[_]%' OR Code COLLATE Latin1_General_BIN LIKE '%[^A-Z0-9_]%'"));
    Check("every source has a Title",
        Count("SELECT COUNT(*) FROM dbo.tbl_Dim_Source WHERE Title IS NULL OR LTRIM(RTRIM(Title)) = ''"));
    Check("Code is unique",
        Count("SELECT COUNT(*) - COUNT(DISTINCT Code) FROM dbo.tbl_Dim_Source"));

    // Forward-looking: every SourceRefCode used by a rule/terminology table must resolve.
    // No such columns exist yet (Plan 1) — this loop is a no-op today, a tripwire later.
    var refColumns = conn.Query<(string TableName, string ColumnName)>(@"
        SELECT t.name, c.name
        FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
        WHERE c.name = 'SourceRefCode'").ToList();
    foreach (var (tbl, col) in refColumns)
        Check($"{tbl}.{col} all resolve in tbl_Dim_Source",
            Count($@"SELECT COUNT(*) FROM dbo.[{tbl}] x
                     WHERE x.[{col}] IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_Source s WHERE s.Code = x.[{col}])"));

    Console.WriteLine(failures == 0 ? "\nverify-sources: ALL PASS" : $"\nverify-sources: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

- [ ] **Step 2: Build + run — expect PASS**

```bash
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-sources
```
Expected: build 0/0; 4 `[PASS]` lines; `verify-sources: ALL PASS`.

- [ ] **Step 3: Full verify sweep unchanged**

```bash
for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini verify-sources; do
  echo -n "$m -> "; dotnet run --project src/Ikiastrro.Cli --no-build -- $m 2>&1 | grep -E "ALL PASS|FAILURE"; done
```
Expected: all six `ALL PASS`.

- [ ] **Step 4: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'test(cli): verify-sources — tbl_Dim_Source well-formedness + SourceRefCode tripwire\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 6: `SqlConnectionFactory.Create(...)` — configurable, default unchanged

**Files:**
- Modify: `src/Ikiastrro.Data/SqlConnectionFactory.cs`

**Interfaces:**
- Produces: `static SqlConnectionFactory Create(string? connectionString = null, string?
  dbNameOverride = null)` — precedence: explicit `connectionString` → env
  `IKIASTRRO_CONNECTION` → a string built as
  `Server=localhost;Database={db};Integrated Security=True;TrustServerCertificate=True;` with
  `db = dbNameOverride ?? Environment.GetEnvironmentVariable("IKIASTRRO_DB") ?? "ikiastrro"`.
  `CreateDefault()` stays and now returns `Create()` (byte-identical default string).

- [ ] **Step 1: Replace the class body**

```csharp
using Microsoft.Data.SqlClient;

namespace Ikiastrro.Data;

public class SqlConnectionFactory
{
    private const string DefaultDb = "ikiastrro";

    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    /// <summary>
    /// Resolves the connection string, in order:
    /// 1. <paramref name="connectionString"/> if given (Web passes ConnectionStrings:Ikiastrro);
    /// 2. env var <c>IKIASTRRO_CONNECTION</c> (stage/uat/prod);
    /// 3. a Windows-Auth string against <c>localhost</c>, catalog =
    ///    <paramref name="dbNameOverride"/> (CLI <c>--db</c>) ?? env <c>IKIASTRRO_DB</c> ?? <c>ikiastrro</c>.
    /// No environment token ever appears in a schema object name — only the catalog / server
    /// differs per environment (see INFRASTRUCTURE.md).
    /// </summary>
    public static SqlConnectionFactory Create(string? connectionString = null, string? dbNameOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
            return new SqlConnectionFactory(connectionString);

        var env = Environment.GetEnvironmentVariable("IKIASTRRO_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env))
            return new SqlConnectionFactory(env);

        var db = dbNameOverride
                 ?? Environment.GetEnvironmentVariable("IKIASTRRO_DB")
                 ?? DefaultDb;
        return new SqlConnectionFactory(
            $"Server=localhost;Database={db};Integrated Security=True;TrustServerCertificate=True;");
    }

    /// <summary>Back-compat: the historical default (Windows Auth, localhost, <c>ikiastrro</c>).</summary>
    public static SqlConnectionFactory CreateDefault() => Create();

    public SqlConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

- [ ] **Step 2: Build + prove the default is unchanged**

```bash
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-schema | tail -1
```
Expected: build 0/0; `verify-schema: ALL PASS` (nothing wired `Create` in yet — this confirms
`CreateDefault()` → `Create()` produces the same connection).

- [ ] **Step 3: Commit**

```bash
git add src/Ikiastrro.Data/SqlConnectionFactory.cs
git commit -m "$(printf 'feat(data): SqlConnectionFactory.Create — connection string from arg / env / default\n\nCreateDefault() unchanged behaviour (delegates to Create()). No caller wired yet.\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 7: CLI `--db <name>` override

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (helper near the top, ~line 10; the
  `var connectionFactory = SqlConnectionFactory.CreateDefault();` line, ~line 79)

**Interfaces:**
- Consumes: `SqlConnectionFactory.Create(dbNameOverride:)` from Task 6.
- Produces: `--db <name>` / `--db=<name>` accepted anywhere in the CLI args, stripped before
  mode dispatch; the whole CLI (interactive add + every `verify-*` / `recompute-*` / `compute-*`
  mode) then targets that catalog.

- [ ] **Step 1: Add the extraction helper**

Near the top of `Program.cs`, before `var connectionFactory = ...` (line ~79), after the last
local-function definition, add:

```csharp
// --db <name> / --db=<name> : target a non-default catalog (stage/uat/scratch). Stripped from
// args before mode dispatch so `verify-schema --db foo` still dispatches on "verify-schema".
static string? ExtractDbOverride(ref string[] args)
{
    string? db = null;
    var kept = new List<string>(args.Length);
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--db" && i + 1 < args.Length) { db = args[++i]; continue; }
        if (args[i].StartsWith("--db=", StringComparison.Ordinal)) { db = args[i]["--db=".Length..]; continue; }
        kept.Add(args[i]);
    }
    args = kept.ToArray();
    return string.IsNullOrWhiteSpace(db) ? null : db;
}

var dbOverride = ExtractDbOverride(ref args);
```

- [ ] **Step 2: Use it**

Change:
```csharp
var connectionFactory = SqlConnectionFactory.CreateDefault();
```
to:
```csharp
var connectionFactory = SqlConnectionFactory.Create(dbNameOverride: dbOverride);
if (dbOverride is not null) Console.WriteLine($"(targeting catalog: {dbOverride})");
```

- [ ] **Step 3: Build + test default, explicit, and bad**

```bash
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-schema | tail -1
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-schema --db ikiastrro | tail -2
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-schema --db ikiastrro_nope 2>&1 | tail -3; echo "exit=$?"
```
Expected: (1) `verify-schema: ALL PASS`; (2) `(targeting catalog: ikiastrro)` then
`verify-schema: ALL PASS`; (3) a `SqlException` ("Cannot open database ... ikiastrro_nope") and
a non-zero exit — a clear failure, not a hang or a silent fallback.

- [ ] **Step 4: Full sweep on the default still green**

```bash
for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini verify-sources; do
  echo -n "$m -> "; dotnet run --project src/Ikiastrro.Cli --no-build -- $m 2>&1 | grep -E "ALL PASS|FAILURE"; done
```
Expected: all six `ALL PASS`.

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(cli): --db <name> override for verify / recompute / compute modes\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 8: Web — connection string from configuration

**Files:**
- Modify: `src/Ikiastrro.Web/appsettings.json`
- Modify: `src/Ikiastrro.Web/Program.cs:13`

**Interfaces:**
- Consumes: `SqlConnectionFactory.Create(connectionString:)` from Task 6;
  `builder.Configuration.GetConnectionString("Ikiastrro")` (standard ASP.NET Core layering:
  `appsettings.json` → `appsettings.{Environment}.json` → env var
  `ConnectionStrings__Ikiastrro`).

- [ ] **Step 1: Add the connection string to `appsettings.json`**

`src/Ikiastrro.Web/appsettings.json` becomes:

```json
{
  "ConnectionStrings": {
    "Ikiastrro": "Server=localhost;Database=ikiastrro;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
Leave `appsettings.Development.json` as-is (dev uses the base string). **Do not** add a
stage/uat/prod `appsettings.{Env}.json` here — those carry credentials and are not committed
(documented in `INFRASTRUCTURE.md`, Task 10).

- [ ] **Step 2: Wire it in `Program.cs`**

Line 13 — change:
```csharp
builder.Services.AddSingleton(SqlConnectionFactory.CreateDefault());
```
to:
```csharp
builder.Services.AddSingleton(
    SqlConnectionFactory.Create(builder.Configuration.GetConnectionString("Ikiastrro")));
```

- [ ] **Step 3: Build + Web smoke**

```bash
dotnet build Ikiastrro.slnx
powershell -Command "Get-Process -Name Ikiastrro.Web -ErrorAction SilentlyContinue | Stop-Process -Force"
(dotnet run --project src/Ikiastrro.Web --no-build --urls http://localhost:5199 > /tmp/web.log 2>&1 &)
sleep 14
curl -s -o /dev/null -w "HTTP %{http_code}\n" http://localhost:5199/charts/1
powershell -Command "Get-Process -Name Ikiastrro.Web -ErrorAction SilentlyContinue | Stop-Process -Force"
```
Expected: `HTTP 200` — the Web app read its connection string from `appsettings.json` and hit
the DB exactly as before.

- [ ] **Step 4: Commit**

```bash
git add src/Ikiastrro.Web/appsettings.json src/Ikiastrro.Web/Program.cs
git commit -m "$(printf 'feat(web): connection string from ConnectionStrings:Ikiastrro configuration\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 9: `db/ikiastrro.sql` — parameterize the catalog name

**Files:**
- Modify: `db/ikiastrro.sql` (the header, ~lines 11–15)

**Interfaces:**
- Produces: a baseline whose catalog name is the `sqlcmd` scripting variable `DbName` (default
  `ikiastrro`), so `sqlcmd -v DbName=ikiastrro_scratch -i db/ikiastrro.sql` replaces the
  `sed`-based scratch rebuild used by earlier plans.

> Note: on this machine's ODBC `sqlcmd` (v15/v17) an in-file `:setvar` outranks the `-v`
> command-line value (documented Microsoft behavior), so `sqlcmd -v DbName=ikiastrro_scratch -i
> db/ikiastrro.sql` silently targets `ikiastrro`. `-v DbName=<name>` works only with go-sqlcmd
> (v1.x, `winget install sqlcmd`). With the ODBC `sqlcmd`, a from-empty rebuild check substitutes
> the `:setvar` line itself — the `[ikiastrro]` literal no longer exists in the file after
> Step 1: `sed 's/:setvar DbName "ikiastrro"/:setvar DbName "ikiastrro_scratch"/' db/ikiastrro.sql > db/_scratch_tmp.sql` then `sqlcmd -b -i db/_scratch_tmp.sql`.

- [ ] **Step 1: Replace the `CREATE DATABASE` / `USE` header**

Lines currently:
```sql
IF DB_ID(N'ikiastrro') IS NULL CREATE DATABASE [ikiastrro];
GO
USE [ikiastrro];
GO
```
become:
```sql
-- Catalog name is a sqlcmd scripting variable so this one file builds any environment
-- (dev = ikiastrro, a scratch check = ikiastrro_scratch). REQUIRES SQLCMD MODE: the `sqlcmd`
-- CLI has it on by default; in SSMS, Query > SQLCMD Mode. Override:  sqlcmd -v DbName=<name>
:setvar DbName "ikiastrro"
GO
IF DB_ID(N'$(DbName)') IS NULL CREATE DATABASE [$(DbName)];
GO
USE [$(DbName)];
GO
```
Then `grep -n "\[ikiastrro\]\|N'ikiastrro'" db/ikiastrro.sql` — confirm **zero** remaining
literal catalog references (there were only the two).

- [ ] **Step 2: Fresh scratch build via the variable**

```bash
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END"
sqlcmd -S localhost -E -b -v DbName=ikiastrro_scratch -i db/ikiastrro.sql 2>&1 | grep -iE "Msg|error|^Changed database" | head
sqlcmd -S localhost -E -d ikiastrro_scratch -h -1 -W -Q "SELECT COUNT(*) FROM dbo.tbl_Dim_Source; SELECT COUNT(*) FROM dbo.tbl_Planets"
sqlcmd -S localhost -E -Q "ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch;"
```
Expected: `Changed database context to 'ikiastrro_scratch'.`, **no `Msg` lines**; the counts
print `14` and `9`.

- [ ] **Step 3: Confirm a plain apply to the real DB still works**

```bash
sqlcmd -S localhost -E -b -i db/ikiastrro.sql 2>&1 | grep -iE "Msg|error" | head; echo "exit=$?"
```
Expected: no `Msg` lines, `exit=0` — with no `-v`, `:setvar DbName "ikiastrro"` supplies the
default, so the live DB is targeted and every `IF NOT EXISTS` guard makes it a no-op.

- [ ] **Step 4: Commit**

```bash
git add db/ikiastrro.sql
git commit -m "$(printf 'chore(db): parameterize baseline catalog name with sqlcmd :setvar DbName\n\nsqlcmd -v DbName=ikiastrro_scratch -i db/ikiastrro.sql replaces the sed-based\nscratch rebuild. Default (no -v) still targets ikiastrro.\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 10: `INFRASTRUCTURE.md`

**Files:**
- Create: `INFRASTRUCTURE.md` (repo root)

**Interfaces:**
- Produces: the environments / DB-naming / config / migration-policy reference that Tasks 6–9
  implement and that `master_ikiastrro.md` (Task 12) links as a required root file.

- [ ] **Step 1: Write the file**

```markdown
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
  `db/NN_*.sql` for an incremental change; `sqlcmd -v DbName=ikiastrro_scratch -i db/ikiastrro.sql`
  for a from-empty rebuild check.
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
```

- [ ] **Step 2: Commit**

```bash
git add INFRASTRUCTURE.md
git commit -m "$(printf 'docs: INFRASTRUCTURE.md — environments, DB naming, config/secrets, migration policy\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 11: `PRODUCT.md` — feature catalogue + completion tracker

**Files:**
- Create: `PRODUCT.md` (repo root)

**Interfaces:**
- Produces: the `FEAT-<AREA>-<NN>` catalogue per `STANDARDS.md §M.3`. Consumed by every future
  plan's final task (tick the boxes it completed).

- [ ] **Step 1: Write the file**

```markdown
# ikiastrro — Product features & completion

What the software does, grouped by engine / subsystem, with per-dimension completion. IDs are
`FEAT-<AREA>-<NN>`. Checklist: **DB · Core · Verify · Web · Docs** (`% = checked ÷ 5`). Status
ladder: `Planned → Designed → DB → Core → Verified → Web → Done`. Conventions: `STANDARDS.md §M.3`.

## Rollup

| Area | Features | Avg % | Missing DB | Missing Core | Missing Verify | Missing Web | Missing Docs |
|---|---|---|---|---|---|---|---|
| ASTRO_CALC | 2 | 90% | 0 | 0 | 0 | 1 | 0 |
| POSITION | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| VARGA | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| HOUSE | 3 | 60% | 1 | 1 | 1 | 0 | 1 |
| NAKSHATRA | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| DIGNITY | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| RELATIONSHIP | 4 | 60% | 1 | 1 | 1 | 1 | 1 |
| KARAKA | 4 | 55% | 2 | 2 | 2 | 2 | 2 |
| AVASTHA | 5 | 32% | 3 | 3 | 3 | 5 | 3 |
| DISPOSITOR | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| STRENGTH | 2 | 0% | 2 | 2 | 2 | 2 | 2 |
| DASHA | 2 | 90% | 0 | 0 | 0 | 0 | 0 |
| YOGA | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| TRANSIT | 2 | 80% | 0 | 0 | 0 | 1 | 0 |
| TERM | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| INFRA | 1 | 60% | 0 | 1 | 1 | 0 | 0 |
| DOCS | 1 | 60% | 0 | 0 | 1 | 0 | 0 |

*(Recompute the rollup from the feature rows whenever a box changes — it is a manual mirror.)*

## Features

### ASTRO_CALC
- **FEAT-ASTRO_CALC-01 · Sidereal positions (Swiss Ephemeris)** — Done · 100%
  Verify `verify-schema` · Spec — (pre-spec) · Plan — (pre-plan)
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-ASTRO_CALC-02 · Sunrise / sunset (`swe_rise_trans`)** — Verified · 80%
  Verify `verify-jaimini` · Spec `2026-09-01-jaimini-…` · Plan `2026-09-01-jaimini-…`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete

### POSITION
- **FEAT-POSITION-01 · D1 (Rāśi) chart** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### VARGA
- **FEAT-VARGA-01 · Divisional charts D1–D60 (21 types)** — Verified · 80%
  Verify `verify-vargas` · Spec `2026-08-31-divisional-chart-completion-design` · Plan `2026-08-31-divisional-charts-plan-a`
  DB [x] · Core [x] · Verify [x] · Web [ ] (only D1/D9 rendered) · Docs [x] · Research: complete

### HOUSE
- **FEAT-HOUSE-01 · Whole-sign houses (Lagna/Sūrya/Chandra) + house lords** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-HOUSE-02 · Functional benefic / malefic by Lagna** — Verified · 80% · Verify `verify-functional-nature`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete
- **FEAT-HOUSE-03 · Bhāva significations + Sthira Kāraka mapping (migration 030)** — Designed · 0%
  Spec `reference-house-lagna-significations` · Research: partial (`SRC_RAMAN_HTJH` extract, 3 unsourced cells)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [x]

### NAKSHATRA
- **FEAT-NAKSHATRA-01 · Nakshatra / pāda / Vimśottari lord / KP sub-lord + reference linkage** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### DIGNITY
- **FEAT-DIGNITY-01 · Pañchadhā Maitrī dignity (9-tier)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### RELATIONSHIP
- **FEAT-RELATIONSHIP-01 · Conjunctions (Yuti)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-02 · Aspects (Graha Dṛṣṭi)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-03 · Combustion (Asta)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-04 · Compound Maitrī / argala / sambandha** — Planned · 0%
  Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### KARAKA
- **FEAT-KARAKA-01 · Chara Karakas (8-fold Aṣṭa)** — Verified · 80% · Verify `verify-jaimini`
  Spec/Plan `2026-09-01-jaimini-…`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-KARAKA-02 · Special points (AL + 12 Bhāva Arudhas + HL + Gulika + Maandi)** — Done · 100% · Verify `verify-jaimini`
  DB [x] · Core [x] · Verify [x] · Web [x] (CombinedD1D9Grid) · Docs [x] · Research: complete
- **FEAT-KARAKA-03 · Sthira Kāraka** — Planned · 0% · Research: partial (`SRC_RAMAN_HTJH`)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-KARAKA-04 · Naisargika Kāraka (Sapta vs Aṣṭa — undecided)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### AVASTHA
- **FEAT-AVASTHA-01 · `AgeState` (Bālādi)** — Verified · 80% · Verify `verify-avastha` · Research: complete
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x]
- **FEAT-AVASTHA-02 · `WakefulnessState` (Jāgradādi)** — Verified · 80% · Verify `verify-avastha` · Research: complete
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x]
- **FEAT-AVASTHA-03 · `RadianceState` (Dīptādi)** — Planned · 0% · Research: partial (bands need a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-AVASTHA-04 · `ShameState` (Lajjitādi)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-AVASTHA-05 · `PostureState` (Śayanādi)** — Planned · 0% · Research: not started (needs janma-ghaṭis + a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### DISPOSITOR
- **FEAT-DISPOSITOR-01 · Dispositor chains / final dispositor / mutual reception** — Planned · 0% · Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### STRENGTH
- **FEAT-STRENGTH-01 · Ṣaḍbala (6 components) + Iṣṭa/Kaṣṭa + Bhāva Bala** — Planned · 0% · Research: partial (`SRC_BPHS_27` — needs a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-STRENGTH-02 · Vimśopaka Bala (varga strength)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### DASHA
- **FEAT-DASHA-01 · Vimśottari (3-level, partial-at-birth)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-DASHA-02 · Life-in-weeks grid (4000-week)** — Verified · 80%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete

### YOGA
- **FEAT-YOGA-01 · Yoga detection (Pañcha Mahāpuruṣa + Rāja/Dhana first slice)** — Planned · 0% · Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### TRANSIT
- **FEAT-TRANSIT-01 · Slow-planet sign-transit history (1930–2060)** — Verified · 80% · Verify (`precheck-planet-transits`)
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-TRANSIT-02 · Sade Sati / Kantaka / Ashtama** — Done · 100%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### TERM
- **FEAT-TERM-01 · Terminology catalogue (Sanskrit / English / Tamil)** — Designed · 0%
  Spec `2026-09-02-engine-organization-terminology-design` · Plan — (Plan 1) · Research: complete
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### INFRA
- **FEAT-INFRA-01 · Multi-environment config (dev/stage/uat/prod)** — Core · 60%
  Spec `2026-09-02-engine-organization-terminology-design` §17 · Plan `2026-09-02-project-foundations`
  DB [x] (`tbl_Dim_Source`, `:setvar`) · Core [ ] · Verify [ ] (`verify-sources` partial) · Web [x] · Docs [x] · Research: complete

### DOCS
- **FEAT-DOCS-01 · Documentation taxonomy + `PRODUCT.md` + citation registry** — Verified · 60%
  Spec `2026-09-02-engine-organization-terminology-design` §16 · Plan `2026-09-02-project-foundations`
  DB [x] · Core [x] · Verify [ ] · Web [x] · Docs [x] · Research: complete
```

- [ ] **Step 2: Commit**

```bash
git add PRODUCT.md
git commit -m "$(printf 'docs: PRODUCT.md — feature catalogue + 5-dimension completion tracker\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 12: Index updates — `master_ikiastrro.md`, history doc, memory

**Files:**
- Modify: `master_ikiastrro.md`
- Modify: `D:\@ClaudeSpace\ikiastrro.md` (outside repo — no commit)
- Modify: `C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md` +
  `MEMORY.md` (outside repo — no commit)

**Interfaces:**
- Consumes: every file created in Tasks 1–11.
- Produces: the doc map and history are current; `PRODUCT.md` / `INFRASTRUCTURE.md` flip from
  "planned" to "living".

- [ ] **Step 1: `master_ikiastrro.md`**

- Flip the `PRODUCT.md` and `INFRASTRUCTURE.md` rows (added in Task 1 Step 4) from
  "planned — Plan 0" to `living`.
- Add rows: `docs/artifacts/` (with a one-line "non-prose artifacts — see §M.1"),
  `docs/research/reference-sources.md` (`living`, "SRC_* citation master, mirrors
  `tbl_Dim_Source`"), `docs/superpowers/specs/_TEMPLATE.md` + `plans/_TEMPLATE.md`.
- Update the `docs/dotnet_engine_map.md` row → `docs/artifacts/dotnet_engine_map.md`.
- Add the spec row: `docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md`
  → `snapshot (design)`.
- Add the plan row: `docs/superpowers/plans/2026-09-02-project-foundations.md` →
  `snapshot (in progress)`.
- In the "Not indexed here" note, mention `docs/artifacts/diagrams/*.d2` + rendered `.svg`.

- [ ] **Step 2: `D:\@ClaudeSpace\ikiastrro.md` — dated history section**

Add before "## Pending / not yet done":

```markdown
## 2026-09-02 — Project foundations (Plan 0)

`superpowers:brainstorming` → spec `docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md`
(13-engine target architecture, `tbl_Astro_Terminology`, self-describing rule tables,
`ChartPipeline`; Plans 0→4) → `writing-plans` → `docs/superpowers/plans/2026-09-02-project-foundations.md`.
Plan 0 (this) is docs + config only — no engine code, no astrology.

- **`STANDARDS.md`** — new `§D.2` (hybrid Sanskrit/English identifier naming + `Code`),
  `§M.1` extension (`PRODUCT.md` + `INFRASTRUCTURE.md` required; `docs/artifacts/` location),
  `§M.3` (`PRODUCT.md` structure + 5-box checklist), `§M.4` (`SRC_*` citation registry).
- **Doc taxonomy realized** — `docs/artifacts/{db,ui,diagrams,reference-charts}/`; engine map +
  the 3 research `.d2` moved in (SVGs rendered); `_TEMPLATE.md` for specs & plans (with a
  Research-status block).
- **Citation registry** — `docs/research/reference-sources.md` (human master) + `tbl_Dim_Source`
  (migration 15, folded into the baseline; 14 `SRC_*` rows). CLI `verify-sources`.
- **`INFRASTRUCTURE.md`** — dev/stage/uat/prod; DB-naming rule (catalog is always `ikiastrro`,
  environment = server/instance; single-server fallback = catalog suffix; **no env token in any
  object name or in C#**); config/secrets layering; per-env migration policy.
- **`PRODUCT.md`** — `FEAT-<AREA>-<NN>` catalogue, ~32 features across 17 areas, each with the
  DB/Core/Verify/Web/Docs checklist + Research flag + rollup.
- **Environment-agnostic data access** — `SqlConnectionFactory.Create(connectionString?,
  dbNameOverride?)` (arg → env `IKIASTRRO_CONNECTION` → built default); `CreateDefault()`
  unchanged. CLI `--db <name>`. Web reads `ConnectionStrings:Ikiastrro`. `db/ikiastrro.sql`
  catalog name is now `sqlcmd :setvar DbName` — `sqlcmd -v DbName=ikiastrro_scratch` replaces
  the sed-based scratch rebuild.
- **Verification** — `dotnet build` 0/0; all six `verify-*` (`schema`, `vargas`, `avastha`,
  `functional-nature`, `jaimini`, `sources`) ALL PASS on the default catalog; `--db` explicit +
  bad-name both behave; scratch rebuild via `-v DbName`; Web smoke `/charts/1` 200.
- **Next: Plan 1** — the engine reorg + `tbl_Astro_Terminology` + rule-table portability.
```

Update the `*Last updated:*` line to `2026-09-02`.

- [ ] **Step 3: Memory**

In `memproj_vedic_horo_gen.md`, append a short paragraph before `**Why:**`: Plan 0 shipped
2026-09-02 — `STANDARDS.md §D.2/§M.1/§M.3/§M.4`, `docs/artifacts/`, `docs/research/reference-sources.md`
+ `tbl_Dim_Source` (migration 15), `INFRASTRUCTURE.md` (dev/stage/uat/prod, no env token in any
object name), `PRODUCT.md` (`FEAT-*` + 5-box completion), `SqlConnectionFactory.Create` +
CLI `--db` + Web `ConnectionStrings:Ikiastrro` + `db/ikiastrro.sql` `:setvar DbName`. Full
design in `docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md`
(Plans 0→4). Next is Plan 1 (engine reorg + terminology). Update the `modified:` frontmatter
date and the `MEMORY.md` one-liner for the project.

- [ ] **Step 4: Final full sweep**

```bash
dotnet build Ikiastrro.slnx
for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini verify-sources; do
  echo -n "$m -> "; dotnet run --project src/Ikiastrro.Cli --no-build -- $m 2>&1 | grep -E "ALL PASS|FAILURE"; done
```
Expected: build 0/0; all six `ALL PASS`.

- [ ] **Step 5: Commit**

```bash
git add master_ikiastrro.md
git commit -m "$(printf 'docs: index Plan 0 — PRODUCT.md, INFRASTRUCTURE.md, artifacts, reference-sources, templates\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## PRODUCT.md — completion ticks for this plan

On finishing Task 12, in `PRODUCT.md`:
- **FEAT-DOCS-01** → `Verify [x]` (`verify-sources` exists), Status `Done`, 100%.
- **FEAT-INFRA-01** → `Core [x]` (`SqlConnectionFactory.Create` + `--db` + `:setvar`),
  `Verify [x]`, Status `Verified`, 80% (Web-per-env `appsettings` files land with a real
  stage/uat deploy).
- Recompute the `DOCS` and `INFRA` rollup rows.

---

## Self-Review

**1. Spec coverage** (spec §12 "Plan 0" + §16 + §17):

| Spec item | Task |
|---|---|
| P0.1 doc taxonomy — `§M.2`… (→ `§M.3` here, `§M.2` name taken) + `docs/artifacts/` + templates | Tasks 1, 2 |
| P0.2 citation registry — `tbl_Ref_Source` (→ `tbl_Dim_Source`) + `reference-sources.md` + seed + `verify-sources` | Tasks 3, 4, 5 |
| P0.3 research-status block on spec/plan templates + `PRODUCT.md` | Tasks 2 (templates), 11 (`PRODUCT.md` Research flag) |
| P0.4 `PRODUCT.md` — `FEAT-<AREA>-<NN>`, 5-box checklist, rollup | Task 11 |
| P0.5 `INFRASTRUCTURE.md` | Task 10 |
| P0.6 env-agnostic data access — `SqlConnectionFactory`↔`IConfiguration`, CLI `--db`, `:setvar` | Tasks 6, 7, 8, 9 |
| P0.7 docs — `STANDARDS.md`, `master_ikiastrro.md`, `../ikiastrro.md`, memory | Tasks 1, 12 |
| §16 four buckets + two indexes + citation rule + research block | Tasks 1, 2, 3, 11 |
| §17 environments / DB-naming / config-secrets / migration policy | Task 10 (doc) + Tasks 6–9 (impl) |

No gaps. Spec §15 open decisions resolved as: #5 = A-principle / B-fallback, both documented
(Task 10); #6 = table **and** doc mirror (Tasks 3 + 4); #7 = one plan (this one). #1–#4 belong
to Plan 1, untouched here.

**2. Placeholder scan:** `<stage-sql-host>` / `<uat-sql-host>` / `<prod-sql-host>` in
`INFRASTRUCTURE.md` are intentional fill-ins for the user's real hosts, labelled as such — not
plan placeholders. Every code/SQL step has literal content. No "TODO" / "similar to Task N".

**3. Type consistency:** `SqlConnectionFactory.Create(string? connectionString = null, string?
dbNameOverride = null)` — Task 6 defines it; Task 7 calls `Create(dbNameOverride: dbOverride)`;
Task 8 calls `Create(builder.Configuration.GetConnectionString("Ikiastrro"))` (first positional
= `connectionString`). `CreateDefault()` kept, used nowhere after Tasks 7–8 but retained for
any missed caller. `ExtractDbOverride(ref string[] args) : string?` — Task 7 defines and calls
it once. `tbl_Dim_Source` columns (`Code, Title, Author, Edition, Tradition, Notes, IsActive`)
— identical in Task 4 DDL, Task 4 seed, and Task 5's queries (`Code`, `Title`). Migration
number `15` consistent (Tasks 4, 9 references, Task 12 history). `verify-sources` mode name
consistent across Tasks 5, 11, 12.

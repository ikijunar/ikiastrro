# ikiastrro — Master Doc Index

Every `.md` that documents this project, grouped by category, with what it's for,
where it lives, when it was created, and whether it's kept current.

**Naming + doc conventions:** STANDARDS.md §D.2 (identifiers), §M.1/§M.3 (docs & PRODUCT.md), §M.4 (citations).

**Naming convention:** `<category>-<slug>.md` in `<repo>/docs/` — see
[`STANDARDS.md`](../STANDARDS.md) §M.1. Categories: `research` · `rationale` ·
`techstack` · `dbdesign` · `uidesign` · `scope` · `reference` · `process`, plus
dated `spec-` / `plan-` under `docs/superpowers/`, and numbered ADRs in
`decisions/`.

**Status key:** `living` = kept current · `snapshot` = point-in-time, not
maintained · `superseded` = replaced, kept for history.

---

## Start here

| Doc | For | Created | Status |
|---|---|---|---|
| [`README.md`](README.md) | Public-facing project overview — what it is, how to run it | 2026-08-30 | living |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Internal engineering reference — current architecture, stack, data layer, known limits | 2026-08-30 | living |
| **`master_ikiastrro.md`** (this file) | The doc index — where everything is | 2026-08-31 | living |
| [`PRODUCT.md`](PRODUCT.md) | Feature catalogue + completion tracker — what the software does and how much of each part is done (STANDARDS §M.3) | 2026-09-02 | living |
| [`INFRASTRUCTURE.md`](INFRASTRUCTURE.md) | Environments, database-naming rule, config & secrets layering, migration policy (STANDARDS §M.1) | 2026-09-02 | living |
| [`../ikiastrro.md`](../ikiastrro.md) | Full running build/decision **history**, dated sections, every session's "what changed and why" | 2026-08-24 | living |
| [`../methods_prodmag.md`](../methods_prodmag.md) | Reusable product-management process (Vision → JTBD → ICE → roadmap) — read before picking the next feature | 2026-08-27 | living |
| [`../STANDARDS.md`](../STANDARDS.md) | Workspace-wide naming/structure conventions this project follows | — | living |
| [`docs/artifacts/`](docs/artifacts/) | Non-prose artifacts — DB DDL exports, UI mockups, rendered diagrams, reference charts; see STANDARDS §M.1 | 2026-09-02 | living |

---

## Research — what's out there, what the domain requires

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/research-topic-coverage.md`](docs/research-topic-coverage.md) | Topic-research master — for each classical technique: is its raw data captured, where are the gaps. Topic 1: planetary roles + avastha states. D2 diagrams (source + rendered `.svg`) under `docs/artifacts/diagrams/` | 2026-08-31 | living |
| [`docs/research-horoscope-software-compare.md`](docs/research-horoscope-software-compare.md) | UI/UX & feature comparison of existing Vedic software (VedAstro, AstroSage, jyotish-dashboard, …) — what to borrow/avoid, mapped to our components | 2026-08-30 | snapshot |
| [`docs/research-top5-vedic-software.md`](docs/research-top5-vedic-software.md) | Competitive benchmark of the top 5 Vedic astrology tools | 2026-08-31 | snapshot |
| [`docs/research/reference-sources.md`](docs/research/reference-sources.md) | `SRC_*` citation master — one bibliographic entry per source key; mirrors `tbl_Dim_Source` (STANDARDS §M.4) | 2026-09-02 | living |

## Rationale — why we chose what

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/rationale-techstack.md`](docs/rationale-techstack.md) | Why the stack is what it is — .NET/C#, SwissEphNet (+ VedAstro replacement, Moshier, AGPL/licensing), SQL Server + Dapper, Blazor Server, star-schema, Python-for-comparison, why-not-just-JHora — with per-choice "revisit when" triggers | 2026-08-31 | living |
| [`decisions/001-star-schema-rules-engine.md`](decisions/001-star-schema-rules-engine.md) | ADR — context, the star-schema + data-driven-rules decision, its consequences | 2026-08-30 | living (ADR — not edited after acceptance) |

## Tech stack — what & how (factual)

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/techstack-overview.md`](docs/techstack-overview.md) | Verified stack snapshot — pinned package versions, per-project (Core/Data/Cli/Web) dependency lists, checked against the `.csproj` files | 2026-08-30 | living |
| [`docs/techstack-details.md`](docs/techstack-details.md) | Architecture, projects, data layer, DB, build/run, verification — the long form | 2026-08-30 | living |
| [`docs/artifacts/dotnet_engine_map.md`](docs/artifacts/dotnet_engine_map.md) | Generated map of `src/Ikiastrro.Core/` — a D2 diagram + per-file reference grouped by engine folder (regenerated 2026-09-03 for the Plan 1 `Engines/<Name>/` layout) | 2026-09-02 | snapshot |

## Database design

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/dbdesign-star-schema-rules-engine.md`](docs/dbdesign-star-schema-rules-engine.md) | The versioned `tbl_Dim_*` / `tbl_Rule_*` / `tbl_Fact_*` rules-engine layer — Phase 1 (built) + open Phase 2 (calculators read from it) | 2026-08-30 | living |
| [`docs/superpowers/specs/2026-08-31-chart-schema-normalization-design.md`](docs/superpowers/specs/2026-08-31-chart-schema-normalization-design.md) | Chart-fact schema normalization — integer FKs, rule-set versioning, chart-type vocab, dropped parent-identity columns (migrations 01–09, **done + merged 2026-08-31**) | 2026-08-31 | snapshot (implemented) |
| [`db/README.md`](db/README.md) | The `db/` migration-script convention + the `SchemaMigrations` ledger contract | 2026-08-31 | living |

## UI design

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/uidesign-specs.md`](docs/uidesign-specs.md) | Web workspace design — layout, design tokens, every component, the decisions behind them | 2026-08-30 | living |
| [`docs/uidesign-dataviz.md`](docs/uidesign-dataviz.md) | Charting stack — Syncfusion Blazor pick + rationale, screen-by-screen chart mapping, palette reconciliation, NuGet list | 2026-08-31 | living (not yet wired into `src/`) |
| [`src/Ikiastrro.Web/Components/DESIGN.md`](src/Ikiastrro.Web/Components/DESIGN.md) | The one-line Web UI design rule for component authors | 2026-08-30 | living |

## Scope & requirements — what we're building and why

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/scope-requirements-gap.md`](docs/scope-requirements-gap.md) | Current requirements gap vs. a full JHora natal export — computations, data, config, presentation still to build; recommended delivery order | 2026-08-31 | living |
| [`docs/scope-jhora-coverage.md`](docs/scope-jhora-coverage.md) | Block-by-block coverage of a JHora export (~7 of ~35 blocks) with build tiers | 2026-08-30 | living |
| [`docs/scope-bhava-coverage.md`](docs/scope-bhava-coverage.md) | Scorecard vs. the classical 8-point Bhava-analysis checklist — the framework behind several backlog items | 2026-08-30 | living |
| [`docs/scope-nakshatra-linkage-divisional-charts.md`](docs/scope-nakshatra-linkage-divisional-charts.md) | Product scope for the nakshatra-linkage + D2/D6/D10/D11 divisional-chart work (built 2026-08-30) | 2026-08-30 | snapshot (implemented) |

## Reference — domain data & formulae we implement

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/reference-calculations.md`](docs/reference-calculations.md) | Every astrological calculation with its source: ayanamsha, houses, dignity, vargas, Dasha, combustion, retrograde, Sade Sati, functional nature | 2026-08-30 | living |
| [`docs/reference-vedic-data-tables.md`](docs/reference-vedic-data-tables.md) | Design of the classical reference/master-data tables (`tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras`/Padas/SubLords, `tbl_PlanetSignTransitEvents`) + sourcing status | 2026-08-30 | living |
| [`docs/reference-house-lagna-significations.md`](docs/reference-house-lagna-significations.md) | House+planet significations, Sthira Karaka mapping, Lagna functional benefic/malefic — sourced from B.V. Raman; migration 030 design | 2026-08-30 | living |
| [`docs/reference-chart-reading-method.md`](docs/reference-chart-reading-method.md) | Shared JHora-anchored method for *reading* any divisional chart — prerequisites, the 11-step universal varga loop, chara-karaka overlay, grid mechanics, `(Trd)`/`(US)` method tags, abbreviation glossary, caveats | 2026-09-01 | living |
| [`docs/reference-chart-d1-rasi.md`](docs/reference-chart-d1-rasi.md) | Reading the D1 (Rasi) chart — significance, JHora derivation, step-by-step, house-by-house significator checklist, cross-varga confirmation, worked lens over the Ramakrishnan verification export | 2026-09-01 | living |
| [`docs/reference-chart-d9-navamsa.md`](docs/reference-chart-d9-navamsa.md) | Reading the D9 (Navamsa) chart — marriage/dharma/strength-check use, Vargottama, Karakamsa, significator checklist, cross-varga confirmation, worked lens | 2026-09-01 | living |
| [`docs/reference-chart-d10-dasamsa.md`](docs/reference-chart-d10-dasamsa.md) | Reading the D10 (Dasamsa) chart — career/status, mode of work (job vs business vs authority), Amatya Karaka line, significator checklist, cross-varga confirmation, worked lens | 2026-09-01 | living |
| [`docs/reference-chart-d60-shashtiamsa.md`](docs/reference-chart-d60-shashtiamsa.md) | Reading the D60 (Shashtiamsa) chart — past-life karma / deciding-vote use, the 60 shashtiamsha names + nature, method ambiguity, birth-time-confidence gate, step-by-step, worked lens | 2026-09-01 | living |
| [`docs/reference-chart-varga-index.md`](docs/reference-chart-varga-index.md) | Short "how to read it" note for every other computed varga (D2, D2-US, D3–D8, D11, D12, D16, D20, D24, D27, D30, D40, D45) — grid label, N, strength group, subject houses/karakas, read-in-order, confirm-with; each flagged `full guide: TODO` | 2026-09-01 | living |

## Process — how we work

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/process-codex-review.md`](docs/process-codex-review.md) | The external Codex / ChatGPT advisory review loop — how a review is produced and triaged | 2026-08-30 | living |

## Specs — dated per-feature design (brainstorming output)

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md`](docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md) | Web UI rebuild around 5 life-area tabs. **SUPERSEDED 2026-09-01** by the varga-centric spec; Phase-0 groundwork was built + is reused | 2026-08-30 | superseded |
| [`docs/superpowers/specs/2026-08-31-chart-schema-normalization-design.md`](docs/superpowers/specs/2026-08-31-chart-schema-normalization-design.md) | Chart-fact schema normalization (see DB design above) | 2026-08-31 | snapshot (implemented) |
| [`docs/superpowers/specs/2026-08-31-divisional-chart-completion-design.md`](docs/superpowers/specs/2026-08-31-divisional-chart-completion-design.md) | Complete the divisional-chart set (17 vargas + D2-US), DB-driven varga rules, DB-completeness invariant. Plan A scope; D81/D108/D144/D150 → Plan B (not yet written) | 2026-08-31 | snapshot (Plan A implemented) |
| [`docs/superpowers/specs/2026-09-01-jaimini-chara-karaka-special-points-design.md`](docs/superpowers/specs/2026-09-01-jaimini-chara-karaka-special-points-design.md) | Chara Karakas (Ashta) + special points (AL, 12 Bhava Arudhas, HL, Gulika, Maandi) through all 21 vargas; `PointKind` column; static combined D1+D9 chart. Migration 14. Sequenced before the UI rebuild | 2026-09-01 | snapshot (implemented) |
| [`docs/superpowers/specs/2026-09-01-varga-centric-web-ui-design.md`](docs/superpowers/specs/2026-09-01-varga-centric-web-ui-design.md) | Whole-app Web rebuild: varga-centric (D1 hero + grouped varga rail), enriched South-Indian grid + Syncfusion polar-wheel toggle, dedicated Timing route, dark parchment kept, yellow Home/Charts nav. Consumes the Jaimini spec | 2026-09-01 | draft (for plan) |
| [`docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md`](docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md) | Engine reorganization to a 13-engine target architecture + `tbl_Astro_Terminology` + self-describing rule tables + `ChartPipeline`; sequenced Plans 0→4. **Plan 0 done 2026-09-02; Plan 1 done 2026-09-03** (§9 interpreter table reconciled in-file to the shipped `GRID_VARGA`). Plans 2–4 not started | 2026-09-02 | snapshot (design; Plans 0+1 implemented) |
| [`docs/superpowers/specs/_TEMPLATE.md`](docs/superpowers/specs/_TEMPLATE.md) | Skeleton for a new dated spec — Research-status block + standard sections (Problem → Open decisions) | 2026-09-02 | living |

## Plans — dated per-feature implementation (writing-plans output)

| Doc | For | Created | Status |
|---|---|---|---|
| [`docs/superpowers/plans/2026-08-30-web-ui-recreate-groundwork.md`](docs/superpowers/plans/2026-08-30-web-ui-recreate-groundwork.md) | Plan 1 (Core/Data/CLI groundwork) for the Web UI recreate — **merged 2026-08-30** | 2026-08-30 | snapshot (done) |
| [`docs/superpowers/plans/groundwork-outcomes-for-plan-2.md`](docs/superpowers/plans/groundwork-outcomes-for-plan-2.md) | Carry-forward notes from Plan 1 into the not-yet-started Plan 2 (UI phases) | 2026-08-30 | living (until Plan 2 starts) |
| [`docs/superpowers/plans/2026-08-30-nakshatra-linkage-vargas.md`](docs/superpowers/plans/2026-08-30-nakshatra-linkage-vargas.md) | Nakshatra linkage + D2/D6/D10/D11 — **implemented 2026-08-30** (relocated from workspace root 2026-08-31) | 2026-08-30 | snapshot (done) |
| [`docs/superpowers/plans/2026-08-31-chart-schema-normalization.md`](docs/superpowers/plans/2026-08-31-chart-schema-normalization.md) | 19-task chart-fact normalization plan — **done + merged 2026-08-31** | 2026-08-31 | snapshot (done) |
| [`docs/superpowers/plans/2026-08-31-divisional-charts-plan-a.md`](docs/superpowers/plans/2026-08-31-divisional-charts-plan-a.md) | 20-task divisional-chart completion (Plan A) — D2-US + D3–D60, `tbl_Rule_VargaScheme`, `VargaLongitudeDegrees`, one `VargaChartComputer` — **implemented 2026-09-01** | 2026-08-31 | snapshot (done) |
| [`docs/superpowers/plans/2026-09-01-jaimini-chara-karaka-special-points.md`](docs/superpowers/plans/2026-09-01-jaimini-chara-karaka-special-points.md) | 8-task Chara Karakas + special points — migration 14 (`PointKind` + `CharaKaraka`), `SpecialPoints/` calculators, `swe_rise_trans` sunrise, `CombinedD1D9Grid` — **implemented 2026-09-01** | 2026-09-01 | snapshot (done) |
| [`docs/superpowers/plans/2026-09-02-project-foundations.md`](docs/superpowers/plans/2026-09-02-project-foundations.md) | Plan 0 — docs/config foundations: `STANDARDS §D.2/§M.1/§M.3/§M.4`, `docs/artifacts/` tree + templates, `reference-sources.md` + `tbl_Dim_Source` (migration 15) + `verify-sources`, `INFRASTRUCTURE.md`, `PRODUCT.md`, `SqlConnectionFactory.Create` + CLI `--db` + Web `ConnectionStrings:Ikiastrro` + `:setvar DbName` — **done 2026-09-02** | 2026-09-02 | snapshot (done) |
| [`docs/superpowers/plans/2026-09-02-engine-organization-terminology.md`](docs/superpowers/plans/2026-09-02-engine-organization-terminology.md) | Plan 1 — 14 tasks: `Ikiastrro.Core` → 13 `Engines/<Name>/`, `ChartAnalyzer` split (behaviour-preserving), `ChartPipeline`/`ChartBundle` + `verify-pipeline`, avastha → planetary-state rename (migration 16), `tbl_Astro_Terminology` + `TerminologyCatalog` + `verify-terminology` (migration 17), rule-table portability tail + `tbl_Rule_Catalog` + `GRID_VARGA` interpreters + `verify-rules` (migration 18) — **done 2026-09-03** (`2271eec`..`dd2ad67`) | 2026-09-02 | snapshot (done) |
| [`docs/superpowers/plans/_TEMPLATE.md`](docs/superpowers/plans/_TEMPLATE.md) | Skeleton for a new dated plan — Global Constraints (branch, commit trailer, no-push, migrations, no-test-project) + `Task N` structure + PRODUCT.md tick line | 2026-09-02 | living |
| Plan B (D81/D108/D144 chart-composition + D150) | Not yet written — see spec §1/§6 | — | not started |

## Decision records (ADRs)

`decisions/NNN-kebab-title.md` — one architecturally-significant decision per file,
never edited after acceptance (a reversal is a new record). See STANDARDS §M.1.

| Doc | For | Created | Status |
|---|---|---|---|
| [`decisions/001-star-schema-rules-engine.md`](decisions/001-star-schema-rules-engine.md) | Star schema + data-driven rules engine | 2026-08-30 | living (ADR) |

---

## Not indexed here

- `_research/` — vendored third-party source (PyJHora, VedAstro, jyotishganit, UI
  reference repos) and their own READMEs. Git-ignored, not project docs.
- `db/_archive/` — the pre-consolidation `001..034` migration chain, kept as
  frozen history.
- `docs/artifacts/diagrams/*.d2` — D2 diagram **source**, committed next to the
  rendered `.svg` the prose docs embed; regenerated with `d2`, not prose themselves.
- `../ikiastrro.md` history entries reference docs by their **name at the time** —
  those are a dated record and are not rewritten when a doc is renamed.

---
last_updated: 2026-09-09
togaf: Requirements Management
safe: Feature / Capability register
---

# ikiastrro — Master Product (features & functionality)

Supersedes `PRODUCT.md`. The single answer to "what does the software do, and how much of
each part is done", split across the three workstreams (`STANDARDS.md` §E.1, §M.3).

IDs are `FEAT-<AREA>-<NN>`. Per-feature checklist **DB · Core · Verify · Web · Docs**
(`% = checked ÷ 5`; `—` = not applicable). Status ladder
`Planned → Designed → DB → Core → Verified → Web → Done`. The checklist boxes map to
workstreams: `DB` → **database**, `Core` / `Verify` → **cli**, `Web` → **ui**, `Docs`
cross-cutting.

Features in **Now** (see [`ROADMAP.md`](ROADMAP.md)) also carry an **expanded block** —
Outcome · Priority rationale · ABB → SBB · DB+CLI slice · UI slice · Done-evidence — which
collapses back to the checklist row once the feature leaves Now. This keeps the product-head
reasoning visible on live work without carrying it for all ~40 features.

## Product intent

ikiastrro is an **astrologer-facing horoscope analysis and decision-support engine**. Its
primary job is to turn the large set of chart calculations into a structured statistical
evidence model that surfaces planetary and house strengths, weaknesses, conflicts,
confirmations, and omissions an astrologer might miss during manual analysis.

Chart generation, divisional charts, dashas, dignities, relationships, and special points
are calculation infrastructure for that analysis. They are not the product's end objective
and should not be evaluated only by whether a chart can be displayed to an end user.

The product should therefore:

- preserve every input, rule, source, method, and intermediate value needed to explain a finding;
- combine independent signals into ranked or grouped strengths and weaknesses rather than emit
  isolated textbook rules;
- show supporting and contradicting evidence, including cross-varga confirmation and timing
  relevance;
- make uncertainty, source differences, and unresolved traditions visible to the astrologer;
- help an astrologer review a horoscope comprehensively without replacing professional judgment;
- treat UI chart views as inspection tools for the analytical model, not as the main deliverable.

The central success measure is **reduced analytical omission with traceable evidence**, not
the number of charts rendered.

## Value streams & workstreams

Two product **value streams** over three engineering **workstreams**; local worktree ↔ Git
branch ↔ path scope is 1 : 1 : 1 (`STANDARDS.md` §E.1). **Claude Code is the primary agent**
(§E.2) — it owns the cross-cutting files below and integration to `master`.

- **Value stream · DB + CLI** — the calculation product (schema → engine → CLI → verification)
  - **Database** · `workstream/database` · `…\ikiastrro.wt\database` · owns `db/`, `src/Ikiastrro.Data/` · [`docs/database/MASTER.md`](docs/database/MASTER.md)
  - **CLI** · `workstream/cli` · `…\ikiastrro.wt\cli` · owns `src/Ikiastrro.Core/`, `src/Ikiastrro.Cli/`, `tests/Ikiastrro.Yoga.Tests/` · [`docs/cli/MASTER.md`](docs/cli/MASTER.md)
- **Value stream · UI** — the astrologer-facing product
  - **UI** · `workstream/ui` · `…\ikiastrro.wt\ui` · owns `src/Ikiastrro.Web/`, `tests/Ikiastrro.Web.Tests/` · [`docs/ui/MASTER.md`](docs/ui/MASTER.md)

`Ikiastrro.Core` (the engine) rides with **cli**. A schema change that neither engine nor UI
logic needs yet is a **database**-only change.

Cross-cutting — edited on `master` by the primary agent only: `README.md` · `ARCHITECTURE.md`
· `masterproduct.md` · `ROADMAP.md` · `INFRASTRUCTURE.md` · `MASTER.md` · `personas.md` ·
`value-stream.md` · `docs/architecture/` · `docs/governance/` · `decisions/` ·
`docs/research/sources.md`.

## Cross-stream dependencies

The DB + CLI stream **publishes**; the UI stream **consumes**. The contract:

- **UI reads only persisted rows** — `tbl_Chart_*`, `tbl_Fact_*`, the `vw_*` evidence views,
  `tbl_Rule_Ayanamsa` — and never recomputes. A calculation the UI needs is a DB + CLI
  feature first (`DB` + `Core` + `Verify`), then a UI feature (`Web`).
- **Chart data must be regenerated** after any ayanamsa / rule-set / engine change before the
  UI reflects it (`compute-all <name>` / `backfill-charts`).
- **Schema is additive** (new typed columns, not reshaped), so a DB feature can land ahead of
  the CLI / UI that reads it.
- Full interface catalogue: [`docs/architecture/domain-contracts.md`](docs/architecture/domain-contracts.md).

## Rollup

| Area | Workstream | Features | Avg % | Missing DB | Missing Core | Missing Verify | Missing Web | Missing Docs |
|---|---|---|---|---|---|---|---|---|
| DATA | database | 5 | 76% | 1 | — | 0 | — | 1 |
| ASTRO_CALC | cli | 2 | 90% | 0 | 0 | 0 | 1 | 0 |
| POSITION | cli | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| VARGA | cli | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| HOUSE | cli | 3 | 60% | 1 | 1 | 1 | 0 | 1 |
| NAKSHATRA | cli | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| DIGNITY | cli | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| RELATIONSHIP | cli | 4 | 60% | 1 | 1 | 1 | 1 | 1 |
| KARAKA | cli | 4 | 55% | 2 | 2 | 2 | 2 | 2 |
| AVASTHA | cli | 5 | 32% | 3 | 3 | 3 | 5 | 3 |
| DISPOSITOR | cli | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| STRENGTH | cli | 2 | 0% | 2 | 2 | 2 | 2 | 2 |
| DASHA | cli | 2 | 90% | 0 | 0 | 0 | 0 | 1 |
| YOGA | cli | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| TRANSIT | cli | 2 | 80% | 0 | 0 | 0 | 1 | 0 |
| TERM | cli | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| ENGINE | cli | 2 | 90% | 0 | 0 | 0 | 1 | 0 |
| INFRA | database + cli | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| UI | ui | 12 | 70% | — | — | 4 | 2 | 9 |
| DOCS | cross-cutting | 1 | 100% | 0 | 0 | 0 | 0 | 0 |

*(The rollup is a manual mirror — recompute from the feature rows whenever a box changes.)*

---

## DATA — workstream: database

- **FEAT-DATA-01 · Star schema + data-driven rules engine (`tbl_Dim_*` / `tbl_Rule_*` / `tbl_Fact_*`)** — Done · 100% · Verify `verify-schema`, `verify-rules`
  DB [x] · Core [x] · Verify [x] · Web [—] · Docs [x] · Research: complete
  Versioned rule layer keyed off `tbl_Rule_Sets`; every rule row round-trips to its C# rule.
- **FEAT-DATA-02 · Chart-fact schema normalization (integer FKs, rule-set versioning, chart-type vocab)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [—] · Docs [x] · Research: complete
- **FEAT-DATA-03 · Migration ledger + `SchemaMigrations` contract** — Verified · 80% · Verify `verify-schema`
  DB [x] · Core [—] · Verify [x] · Web [—] · Docs [x] · Research: complete
  Local ledger is partial (baseline from `db/ikiastrro.sql` + migrations 22–054); orphan
  rows exist in `tbl_Dim_AyanamsaBenchmarkPositions` (parent case row missing).
- **FEAT-DATA-04 · Ayanāṁśa default = Lahiri (Swiss mode 1), matching the JHora reference** — Verified · 80% · Verify `verify-vargas`, `verify-jaimini`
  DB [x] · Core [—] · Verify [x] · Web [—] · Docs [x] · Research: complete
  `tbl_Rule_Ayanamsa` had seeded Jagannatha (mode 26 = `SE_SIDM_SS_CITRA`, 22.745° for
  1981) as the active default; migration 054 repoints it to Lahiri (mode 1, 23.595° — the
  frame every `verify-*` chart is built in). Mode 27 (True Chitrapaksha) matches the
  reference but needs `sefstars.txt`, absent from this file-less Moshier build. Same fix
  cleared a regeneration FK-547 (`GenerateAll` now deletes strength / bhava-strength /
  vargottama facts before `tbl_ChartResults`). Both saved people regenerated; 11/11
  `verify-*` green. Remaining 20%: re-seed the `BENCH_RAMAKRISHNAN_P_JHORA_1981` case row
  and add a dedicated `verify-ayanamsa` (deferred).
- **FEAT-DATA-05 · Source-attributed yoga corpus schema (migrations 47–49)** — Designed · 20%
  DB [ ] (migrations prepared, applied locally; apply to other envs after corpus completion) · Core [—] · Verify [x] · Web [—] · Docs [x] · Research: active
  Normalised D1/D9 requirement tracking + formation/outcome/source axes for `tbl_Rule_Yoga`.

## ASTRO_CALC — workstream: cli

- **FEAT-ASTRO_CALC-01 · Sidereal positions (Swiss Ephemeris)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-ASTRO_CALC-02 · Sunrise / sunset (`swe_rise_trans`)** — Verified · 80% · Verify `verify-jaimini`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete

## POSITION — workstream: cli

- **FEAT-POSITION-01 · D1 (Rāśi) chart** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

## VARGA — workstream: cli

- **FEAT-VARGA-01 · Divisional charts D1–D60 (21 types)** — Verified · 80% · Verify `verify-vargas`
  DB [x] · Core [x] · Verify [x] · Web [ ] (only D1/D9 rendered) · Docs [x] · Research: complete
  `verify-vargas` green across all 21 charts for both saved people (after FEAT-DATA-04).

## HOUSE — workstream: cli

- **FEAT-HOUSE-01 · Whole-sign houses (Lagna/Sūrya/Chandra) + house lords** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-HOUSE-02 · Functional benefic / malefic by Lagna** — Verified · 80% · Verify `verify-functional-nature`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete
- **FEAT-HOUSE-03 · Bhāva significations + Sthira Kāraka mapping** — Designed · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [x] · Research: partial (`SRC_RAMAN_HTJH`, 3 unsourced cells)

## NAKSHATRA — workstream: cli

- **FEAT-NAKSHATRA-01 · Nakshatra / pāda / Vimśottari lord / KP sub-lord + reference linkage** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

## DIGNITY — workstream: cli

- **FEAT-DIGNITY-01 · Pañchadhā Maitrī dignity (9-tier)** — Done · 100% · Verify `verify-schema`, `verify-dignity`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

## RELATIONSHIP — workstream: cli

- **FEAT-RELATIONSHIP-01 · Conjunctions (Yuti) + multi-graha conjunction (Graha Saṃyoga) groups** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
  Pair rows (`tbl_Chart_Conjunctions`) + a ≥ 2-graha group/member layer
  (`tbl_Chart_MultiGrahaConjunction` / `…Member`) with per-planet degree / longitude /
  dignity / retrograde / combust, D1 span + per-member orb, and `MemberKey` for
  yoga-subset matching. Groundwork for FEAT-YOGA-01.
- **FEAT-RELATIONSHIP-02 · Aspects (Graha Dṛṣṭi)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-03 · Combustion (Asta)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-04 · Compound Maitrī / argala / sambandha** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: not started

## KARAKA — workstream: cli

- **FEAT-KARAKA-01 · Chara Karakas (8-fold Aṣṭa)** — Verified · 80% · Verify `verify-jaimini`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-KARAKA-02 · Special points (AL + 12 Bhāva Arudhas + HL + all 11 upagrahas)** — Done · 100% · Verify `verify-jaimini`, `verify-upagrahas`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
  PVR Gulika/Maandi convention; `verify-upagrahas` passes all 21 charts (stored charts
  regenerated on the Lahiri default, FEAT-DATA-04).
- **FEAT-KARAKA-03 · Sthira Kāraka** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: partial (`SRC_RAMAN_HTJH`)
- **FEAT-KARAKA-04 · Naisargika Kāraka (Sapta vs Aṣṭa — undecided)** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: partial

## AVASTHA — workstream: cli

- **FEAT-AVASTHA-01 · `AgeState` (Bālādi)** — Verified · 80% · Verify `verify-avastha`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-AVASTHA-02 · `WakefulnessState` (Jāgradādi)** — Verified · 80% · Verify `verify-avastha`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-AVASTHA-03 · `RadianceState` (Dīptādi)** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: partial (bands need a cited edition)
- **FEAT-AVASTHA-04 · `ShameState` (Lajjitādi)** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: partial
- **FEAT-AVASTHA-05 · `PostureState` (Śayanādi)** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: not started (needs janma-ghaṭis + a cited edition)

## DISPOSITOR — workstream: cli

- **FEAT-DISPOSITOR-01 · Dispositor chains / final dispositor / mutual reception** — Planned · 0%
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ] · Research: not started

## STRENGTH — workstream: cli

- **FEAT-STRENGTH-01 · Ṣaḍbala (6 components) + Iṣṭa/Kaṣṭa + Bhāva Bala** — In progress · 65% · Research: sourced
  DB [x] · Core [x] · Verify [x] · Web [x] (summary) · Docs [x]
  Available now: total Ṣaḍbala, Sthāna/Dig/Kāla/Cheṣṭā/Naisargika/Dṛk totals, named
  component rows, Kālabala, strongest-planet ranking, exact longitude, Bhāva Bala.
  Remaining: the seeded-but-uncomputed Tribhāga/Varṣa/Māsa/Dina/Horā/Ayana Kālabala
  components; planetary-war adjustment; per-planet minimum-rūpa thresholds; replace
  simplified Iṣṭa/Kaṣṭa/Cheṣṭā conventions after reference reconciliation.
- **FEAT-STRENGTH-02 · Vimśopaka Bala + Vaiśeṣikāṃśa grades** — Planned · 15% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [x]
  Prerequisite varga charts exist through the Shodashavarga set. Remaining: source the four
  varga-group weights, compute dignity-weighted scores, define Vaiśeṣikāṃśa membership and
  grade names, persist, expose to Yoga.

## DASHA — workstream: cli

- **FEAT-DASHA-01 · Vimśottari (3-level, partial-at-birth)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-DASHA-02 · Life-in-weeks grid (4000-week)** — Verified · 80%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete
  (Grid rendering is tracked under FEAT-UI-11.)

## YOGA — workstream: cli

- **FEAT-YOGA-01 · Source-attributed Raman 1–300 + PVR yoga detection** — In progress · 65% · Research: active
  DB [ ] (migrations 47–49 prepared; apply after corpus completion) · Core [ ] (Raman
  1–300 catalogued; predicates active through 200; initial PVR variants) · Verify [x] ·
  Web [ ] · Docs [x]
  P0 inputs: required vargas, exact longitude, day/night, lunar phase, source-specific
  subject sex; missing P0 context yields `NOT_EVALUATED`, not `ABSENT`. Reuses
  `ChartBundle.Strengths`, `SunTimes.IsNightBirth`, D1/varga longitudes, all registered
  varga charts, dignity / conjunction / graha-aspect engines. Gaps: a source-qualified
  strong/weak policy over Ṣaḍbala thresholds; reusable waxing/waning/full-Moon
  classification; Vaiśeṣikāṃśa output; subject sex on `BirthDetails`; structured
  missing-requirement codes per result. P0 PVR additions: Subha, Asubha, Guru-Mangala,
  Chamara, Khadga, Lagnaadhi, Saarada, Dharma-Karmadhipati, Vipareeta Raja.

## TRANSIT — workstream: cli

- **FEAT-TRANSIT-01 · Slow-planet sign-transit history (1930–2060)** — Verified · 80% · Verify `precheck-planet-transits`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-TRANSIT-02 · Sade Sati / Kantaka / Ashtama Shani** — Done · 100%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

## TERM — workstream: cli

- **FEAT-TERM-01 · Terminology catalogue (Sanskrit / English / Tamil)** — Verified · 80% · Verify `verify-terminology`
  DB [x] (`tbl_Astro_Terminology` + `_Text`) · Core [x] (`TerminologyCatalog`) · Verify [x] · Web [ ] · Docs [x] · Research: complete
  236 concepts seeded `sa` + `en` (`Latn`); Tamil (`ta`/`Taml`) + Devanagari (`sa`/`Deva`)
  are pure inserts later — schema needs no rework.

## ENGINE — workstream: cli

- **FEAT-ENGINE-01 · Engine-stack layering (`Engines/<Name>/`)** — Verified · 80% · Verify `verify-schema` + `verify-pipeline`
  DB [x] · Core [x] (13 engines + `ChartPipeline` / `ChartBundle`; `ChartAnalyzer` split
  into House / Nakshatra / Relationship engines, behaviour-preserving) · Verify [x] ·
  Web [ ] · Docs [x] · Research: complete
  Reserved interface seams: Dispositor, Strength, Yoga, Sthira/Naisargika Karaka.
  `ChartGenerationService.GenerateAll` pipeline adoption deferred.
- **FEAT-ENGINE-02 · Rule-table portability (`tbl_Rule_Catalog` + interpreters)** — Verified · 80% · Verify `verify-rules`
  DB [x] (portability tail on 7 `tbl_Rule_*`; `tbl_Rule_Catalog` + 12-row index; 5 reserved
  rule tables) · Core [x] (3 `IVargaMethodInterpreter` families: `LINEAR_VARGA` /
  `GRID_VARGA` / `BAND_VARGA`; `seed-rule-params` backfills all 20 `RuleParametersJson`) ·
  Verify [x] · Web [—] · Docs [x]

## INFRA — workstream: database + cli

- **FEAT-INFRA-01 · Multi-environment config (dev/stage/uat/prod)** — Verified · 80%
  DB [x] (`:setvar`, `tbl_Dim_Source`) · Core [x] (`SqlConnectionFactory.Create` + CLI
  `--db` + `:setvar DbName`) · Verify [x] · Web [ ] (per-env `appsettings.{Environment}.json`
  land with a real stage/uat deploy) · Docs [x] · Research: complete

## UI — workstream: ui

Checklist for UI features: `DB` / `Core` are `—`; `Verify` = a bUnit / golden-SVG snapshot
test exists; `Web` = route is live; `Docs` = a `docs/ui/` component doc exists.

- **FEAT-UI-01 · App shell + brand system (MudBlazor, `tokens.css`, shared header)** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-02 · Home / entry (`/`) — name filter, saved people, add-new** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-03 · Add / Edit person form (`/add`)** — In progress · 40%
  DB [—] · Core [—] · Verify [ ] · Web [x] · Docs [ ]
  Renders as unstyled native inputs (no MudBlazor, no `Add.razor.css`); no Sex field though
  `tbl_BirthDetails.Sex` exists (migration 052). No geocoding fallback / timezone field.
- **FEAT-UI-04 · Saved charts list (`/charts`) + inline delete** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-05 · Chart workspace (`/charts/{id}`) — D1 hero + grouped varga rail** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-06 · Varga view (`/charts/{id}/varga/{code}`) — grid + polar-wheel toggle** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-07 · South Indian template (`/charts/{id}/south-indian-template`)** — Verified · 80%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [x] (`src/Ikiastrro.Web/Components/Charts/README.md`)
- **FEAT-UI-08 · Timing (`/charts/{id}/timing`) — 3-level dasha tree** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-09 · Astrologer evidence tables (`/charts/{id}/evidence`)** — Verified · 80%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [x] (`docs/ui/components/evidence-tables.md`)
- **FEAT-UI-10 · Transit wheel (`/transit-wheel/{id}`) — natal ↔ transit + dasha selector** — Verified · 80%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [x] (`docs/ui/components/transit-wheel.md`)
- **FEAT-UI-11 · Life-in-weeks grid (`/charts/{id}/life-weeks`)** — Verified · 60%
  DB [—] · Core [—] · Verify [x] · Web [x] · Docs [ ]
- **FEAT-UI-12 · Preferences / ayanamsa setting** — Planned · 0%
  DB [—] · Core [—] · Verify [ ] · Web [ ] · Docs [x] (`docs/ui/components/home.md`)
  Requirement: a Preferences route whose Ayanamsa box defaults to the active
  `tbl_Rule_Ayanamsa` system default, listing that plus the 20 other catalogued options.
  No route exists yet.

## DOCS — cross-cutting

- **FEAT-DOCS-01 · Documentation taxonomy + `masterproduct.md` + citation registry** — Done · 100% · Verify `verify-sources`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

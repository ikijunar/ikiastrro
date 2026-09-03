# Graha Dignity Rule Layer + Conjunction Groups — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `DignityEngine`'s hard-coded dictionaries with a segmented, source-attributed `tbl_Rule_GrahaDignity` (PVR *Integrated Approach* Table 6 as the active rule-set), and add an explicit conjunction group/member layer that carries each participant's degree + dignity for yoga deduction.

**Architecture:** Three additive phases on one branch. Phase 1 lands the reference layer (`tbl_Dim_DignityType`, `tbl_Rule_GrahaDignity` with `PVR_INTEGRATED` active + `BPHS_PARASHARI` inactive) with zero behaviour change. Phase 2 points `DignityEngine` at the active rule-set via a new `IDignityRuleProvider`, which flips active behaviour to PVR and forces a deliberate re-baseline of dignity-dependent golden records. Phase 3 adds `tbl_Chart_ConjunctionGroups` + `tbl_Chart_ConjunctionGroupMembers` + a `ConjunctionGroupId` link on the existing pair table, derived by `RelationshipEngine` and stitched with dignity by `ChartAnalyzer`. Every step gated by `dotnet build` + the `verify-*` suite.

**Tech Stack:** .NET 8 / C# (nullable ref types; top-level statements in CLI), SQL Server (Windows Auth `localhost`), Dapper, `sqlcmd` migrations with `:setvar DbName`, SwissEphNet 2.8.0.2, Blazor Server (Web).

**Spec:** `docs/superpowers/specs/2026-09-04-graha-dignity-rule-layer-and-conjunction-groups-design.md`

## Research
Status: [x] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| `docs/research/dignity-pvr-integrated.md` | Tasks 2–3 (seed values), Task 5 (engine mapping), Task 6 (re-baseline) | complete |

## Global Constraints
- Branch: `feat/graha-dignity-rule-layer` (cut from `master`).
- Commit trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01FkkFi2M4dmZ2S4dxMQN5z7
  ```
- Do not push unless asked.
- Migrations: numbered `NN_*.sql`, idempotent, self-recording into `dbo.SchemaMigrations`; last applied is `21`, so this plan adds `22`, `23`, `24`. Fold each proven migration's final DDL forward into `db/ikiastrro.sql`; keep `vw_Chart_Consolidated` last.
- No test project — verify via `dotnet build -warnaserror` (Debug + Release) + CLI `verify-*` modes + Web smoke `/charts/1`.
- Spec §8 decision 1 (two-table group layer, pairs as children) is settled. Still confirm decision 2
  (all three phases, one branch) before starting Task 7; note decision 6 (table naming — keep
  `tbl_Chart_ConjunctionGroups*` vs rename to `tbl_Chart_MultiGrahaConjunction*`).

---

## Phase 1 — Dignity reference layer (zero behaviour change)

## Task 1: `tbl_Dim_DignityType` + terminology
**Files:** Create `db/22_create_dim_dignitytype.sql` · Modify `db/ikiastrro.sql`
**Interfaces:** Produces `dbo.tbl_Dim_DignityType` (5 rows), `DIGNITYTYPE_*` terminology rows
- [ ] `tbl_Dim_DignityType` (Id, Code, NameSanskrit, NameEnglish, Mood, InterpretationTendency) per spec §5.22, `IF OBJECT_ID` guarded.
- [ ] Seed 5 rows: `EXALTED` / `MOOLATRIKONA` / `OWN` / `NEUTRAL` / `DEBILITATED` with Mood + Tendency text from `docs/research/dignity-pvr-integrated.md` §"Downstream use".
- [ ] `tbl_Astro_Terminology` + `_Text` (`sa` + `en`) rows via the migration-17 pattern.
- [ ] `INSERT dbo.SchemaMigrations` (`WHERE NOT EXISTS`); `PRINT` summary.
- [ ] Apply to dev DB; fold DDL + seed into `db/ikiastrro.sql`.
- [ ] `dotnet build`; `verify-terminology` + `verify-schema` `ALL PASS`.
- [ ] Commit.

## Task 2: `tbl_Rule_GrahaDignity` table + `SRC_PVR_INTEGRATED`
**Files:** Create `db/23_create_rule_grahadignity.sql` (table + constraints only in this task) · Modify `db/ikiastrro.sql`, `docs/research/reference-sources.md`
**Interfaces:** Produces empty `dbo.tbl_Rule_GrahaDignity`; `tbl_Dim_Source` gains `SRC_PVR_INTEGRATED`; `tbl_Rule_Catalog` gains a row
- [ ] Create `tbl_Rule_GrahaDignity` per spec §5.23 — FKs, `CK_RuleGrahaDignity_Degrees`, `CK_RuleGrahaDignity_Deep`, JSON + `SRC_` checks, `UNIQUE (RuleSetId, PlanetId, SignId, DignityTypeId, StartDegree)`, index on `(RuleSetId, PlanetId)`.
- [ ] `MERGE`/insert `SRC_PVR_INTEGRATED` into `tbl_Dim_Source` (title, author "P. V. R. Narasimha Rao", tradition "PVR Integrated").
- [ ] Add the `tbl_Rule_Catalog` row (EngineCode `DIGNITY`, MethodCodes `SEGMENT_LOOKUP`, Purpose, IntroducedIn `23_create_rule_grahadignity.sql`).
- [ ] Add the `SRC_PVR_INTEGRATED` row to `docs/research/reference-sources.md`.
- [ ] `INSERT dbo.SchemaMigrations`; apply to dev DB; fold into baseline.
- [ ] `verify-sources` + `verify-rules` + `verify-schema` `ALL PASS`.
- [ ] Commit.

## Task 3: Seed both rule-sets
**Files:** Create `db/23b_seed_rule_grahadignity.sql` · Modify `db/ikiastrro.sql`
**Interfaces:** Produces `tbl_Rule_GrahaDignity` rows for `PVR_INTEGRATED` (active) + `BPHS_PARASHARI` (inactive)
- [ ] Ensure both `tbl_Rule_Sets` rows exist (`PVR_INTEGRATED`, `BPHS_PARASHARI`); `PVR_INTEGRATED` is the `IsActive`/current dignity set.
- [ ] Seed `PVR_INTEGRATED` from the "Derived segment model" table in `docs/research/dignity-pvr-integrated.md` — every (planet, sign, segment) row, `DeepDegree` on exalt/debil rows, `SourceRefCode = 'SRC_PVR_INTEGRATED'`. Mars note-(4) row: encode Aries; `CalculationNarrative` records the "Leo" typo.
- [ ] Seed `BPHS_PARASHARI` (`IsActive = 0`) from today's `DignityEngine.cs` dict values: Rāhu exalt Taurus / debil Scorpio, Ketu exalt Scorpio / debil Taurus (no node own/moolatrikona rows), Moon moolatrikona Taurus 4–30, Mercury moolatrikona Virgo 16–20, classical own/moolatrikona splits per BPHS. `SourceRefCode = 'SRC_BPHS'`.
- [ ] Seed as `VALUES` CTEs joined to `tbl_Planets` / `tbl_SignAttributes` / `tbl_Dim_DignityType` by natural key; re-run resets to the same rows (idempotent).
- [ ] `INSERT dbo.SchemaMigrations`; apply to dev DB; fold into baseline.
- [ ] Commit.

## Task 4: `verify-dignity` CLI mode
**Files:** Modify `src/Ikiastrro.Cli/Program.cs` · Create `src/Ikiastrro.Cli/fixtures/dignity-fixture.json` (or inline)
**Interfaces:** Consumes `tbl_Rule_GrahaDignity`, `tbl_SignAttributes`; produces `verify-dignity` pass/fail
- [ ] New `verify-dignity` branch, same shape as `verify-rules`.
- [ ] Check A — for each `RuleSetId`: segments for every (PlanetId, SignId) tile `[0,30)` with no gap and no overlap.
- [ ] Check B — every classical planet (Id 1–7) has exactly one whole-sign (`0–30`) `EXALTED` row and one `DEBILITATED` row, in both rule-sets.
- [ ] Check C — a committed fixture chart's expected `DignityStatus` for all 9 grahas matches `DignityEngine.Evaluate` (this passes against BPHS values now; re-checked after Task 5).
- [ ] Check D — `tbl_SignAttributes.ExaltedDegree` / `DebilitatedDegree` / `MooltrikonaRangeStart` / `MooltrikonaRangeEnd` agree with the **active** rule-set for planets 1–7 (nodes exempt).
- [ ] Register `verify-dignity` wherever the verify list is documented (`db/README.md`, `PRODUCT.md` verify column, any "run all verify" helper).
- [ ] `dotnet build`; `verify-dignity` `ALL PASS` (active set is still BPHS-valued until Task 3 flips it — sequence Task 3 before this check or gate Check C/D on the active set).
- [ ] Commit.

---

## Phase 2 — `DignityEngine` data-driven + PVR active

## Task 5: `IDignityRuleProvider` + engine rewire
**Files:** Create `src/Ikiastrro.Core/Engines/Dignity/IDignityRuleProvider.cs`, `src/Ikiastrro.Data/DignityRuleProvider.cs` · Modify `src/Ikiastrro.Core/Engines/Dignity/DignityEngine.cs`, `src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs` (wiring), `src/Ikiastrro.Cli/Program.cs` (composition)
**Interfaces:** Consumes `tbl_Rule_GrahaDignity` (active set); `DignityEngine.Evaluate` signature unchanged, output shape unchanged except two added fields
- [ ] `IDignityRuleProvider` — returns the active rule-set's segments as an in-memory structure keyed by planet → ordered segments (+ deep points, own-sign list, exalt/debil signs).
- [ ] `DignityRuleProvider` (Data) — loads once via `SqlConnectionFactory`; Dapper.
- [ ] `DignityEngine.Evaluate` — replace the private `ExaltationSign` / `DebilitationSign` / `Moolatrikona` dicts with lookups on the provider's data; add the `[StartDegree, EndDegree)` segment walk for own-sign vs moolatrikona (spec §4.3). Keep the varga-chart (`degreeInSign == null`) path identical to today.
- [ ] `DignityResult` gains `DeepDegree` (decimal?) and `DignityTypeCode` (string?, null for Maitrī tiers).
- [ ] Relax the Rāhu/Ketu branch so nodes can resolve to `Own Sign` / `Moolatrikona` when the active set provides those rows; a node not in any dignity still returns `Neutral` (no Panchadhā Maitrī for nodes).
- [ ] Update the `DignityEngine.cs` XML doc-comment: replace the 2026-08-24 Parāśari-convention paragraph with "active rule-set = `tbl_Rule_GrahaDignity` where `IsActive = 1`; currently `PVR_INTEGRATED` (Table 6). BPHS values retained as inactive `BPHS_PARASHARI`."
- [ ] `dotnet build -warnaserror` Debug + Release.
- [ ] Commit.

## Task 6: Re-baseline dignity-dependent golden records
**Files:** Modify affected `verify-*` fixtures / expected values (`verify-avastha`, any golden record reading `DignityStatus`), `src/Ikiastrro.Cli/fixtures/dignity-fixture.json` · Modify `ikiastrro.md`, `docs/superpowers/specs/...-design.md` (tick), memory
**Interfaces:** Produces updated expected values reflecting the PVR switch
- [ ] Run the full `verify-*` suite; capture every diff caused by the switch (expect: Rāhu/Ketu in swapped signs, Moon 3–4° Taurus, Mercury 15–16° Virgo, node Own/Moolatrikona now possible).
- [ ] Confirm each diff is explained by `docs/research/dignity-pvr-integrated.md` §"Divergence" — no unexplained change.
- [ ] Update expected values in the same commit; re-run until every `verify-*` is `ALL PASS`.
- [ ] `recompute-analytics` on the dev DB; Web smoke `/charts/1` → 200.
- [ ] `ikiastrro.md`: add a dated entry — "2026-09-04 switched active dignity rule-set from Parāśari/BPHS to PVR *Integrated Approach* Table 6; supersedes 2026-08-24; Rāhu/Ketu exalt-debil signs changed, nodes gain own + moolatrikona; BPHS retained inactive."
- [ ] Update the `memproj_vedic_horo_gen` memory line.
- [ ] Commit.

---

## Phase 3 — Conjunction group / member layer

## Task 7: Migration `24` — group tables + pair link + backfill
**Files:** Create `db/24_add_conjunction_groups.sql` · Modify `db/ikiastrro.sql`
**Interfaces:** Produces `tbl_Chart_ConjunctionGroups`, `tbl_Chart_ConjunctionGroupMembers`, `tbl_Chart_Conjunctions.ConjunctionGroupId`
- [ ] Create both tables per spec §5.24 — FKs, `ON DELETE CASCADE` on members, CHECKs, `UNIQUE (ChartResultId, SignId)` and `UNIQUE (ConjunctionGroupId, PlanetId)`, index on `ConjunctionGroups(ChartResultId)`. Group carries `PlanetCount`, `MemberKey`, `LongitudeSpanDegrees` (D1-only, nullable, `CHECK >=0 AND <30`). Member carries `OrbFromGroupCenterDegrees` (D1-only, nullable, `CHECK >=0 AND <30`).
- [ ] `ALTER TABLE tbl_Chart_Conjunctions ADD ConjunctionGroupId INT NULL` + FK, `COL_LENGTH`-guarded.
- [ ] Backfill groups + members from `tbl_Chart_KeyDetails` (`PointKind = 'Graha'`, `GROUP BY ChartResultId, SignId HAVING COUNT(*) >= 2`); `MemberKey` = ascending `PlanetId` CSV; member degree / longitude / `DignityStatus` / retrograde / combust copied from the KeyDetails row. For D1 `ChartResultId`s compute `LongitudeSpanDegrees` = `MAX−MIN` of members' `NirayanaLongitudeDegrees` and each member's `OrbFromGroupCenterDegrees` = `ABS(lon − AVG(lon) OVER group)`; leave both null for varga `ChartResultId`s.
- [ ] Backfill `tbl_Chart_Conjunctions.ConjunctionGroupId` by `(ChartResultId, SignId)`.
- [ ] Add a `GRAHA_SAMYOGA` (multi-graha conjunction) `tbl_Astro_Terminology` + `_Text` concept (`sa` + `en`); note "stellium" is UI prose only.
- [ ] `INSERT dbo.SchemaMigrations`; apply to dev DB; fold into baseline (before `vw_Chart_Consolidated`).
- [ ] `verify-schema` `ALL PASS`.
- [ ] Commit.

## Task 8: `RelationshipEngine` — group derivation
**Files:** Modify `src/Ikiastrro.Core/Engines/Relationships/RelationshipEngine.cs` · Create `src/Ikiastrro.Core/Models/ChartConjunctionGroup.cs`, `ChartConjunctionGroupMember.cs`
**Interfaces:** Consumes `ChartAnalysisInput`; produces group + member row models
- [ ] `ConjunctionGroupResult` record + `FindConjunctionGroups(ChartAnalysisInput)` — grahas grouped by sign, `Count >= 2`, each carrying its members' `PlanetPosition`s.
- [ ] `BuildConjunctionGroupRows` / `BuildConjunctionGroupMemberRows` — canonical member order by `PlanetId`; `MemberKey` = ascending `PlanetId` CSV; `PlanetCount` set; member `DegreesInSign` = `NirayanaLongitudeDegrees % 30` (D1) or `VargaLongitudeDegrees % 30` (varga), `NirayanaLongitude` / `VargaLongitude` / `IsRetrograde` from `PlanetPosition`.
- [ ] D1 only: group `LongitudeSpanDegrees` = `max−min` of member `NirayanaLongitudeDegrees`; member `OrbFromGroupCenterDegrees` = `|memberLon − mean(memberLon)|`. Both null for varga (discrete bucket — same rule as `ChartConjunction.DegreeSeparation`).
- [ ] `BuildConjunctionRows` — also emit each pair's owning `MemberKey` so persistence can resolve `ConjunctionGroupId`. Pair rows are the only persisted subset; triples/quads are left for the Yoga engine to enumerate from `MemberKey` (spec §4.5).
- [ ] `dotnet build -warnaserror`.
- [ ] Commit.

## Task 9: `ChartAnalyzer` — stitch member dignity
**Files:** Modify `src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs`
**Interfaces:** Consumes the per-planet `DignityResult` already computed in the same pass; produces group members with `DignityStatus`
- [ ] After computing `DignityResult` per planet, set each `ChartConjunctionGroupMember.DignityStatus` from the matching planet's result (`DignityStatus`; `IsCombust` from the existing combustion pass).
- [ ] Ensure groups/members are emitted into whatever bundle `ChartAnalyzer` returns to the generation service.
- [ ] `dotnet build -warnaserror`.
- [ ] Commit.

## Task 10: Persistence + lifecycle
**Files:** Create `src/Ikiastrro.Data/ChartConjunctionGroupsRepository.cs` · Modify `src/Ikiastrro.Data/ChartConjunctionsRepository.cs` (write `ConjunctionGroupId`), `src/Ikiastrro.Core/.../ChartGenerationService.cs` (RecomputeAnalytics), `BirthDetailDeletionService`, `src/Ikiastrro.Cli/Program.cs` (composition root)
**Interfaces:** Consumes group/member rows; produces persisted rows + pair-link update
- [ ] `ChartConjunctionGroupsRepository` — `InsertAll(groups)`, `InsertAllMembers(members)`, `GetByBirthDetailId`, `DeleteByChartResultId`, `DeleteByBirthDetailId` (mirror `ChartConjunctionsRepository`).
- [ ] `RecomputeAnalytics` — derive + insert groups → members, then `UPDATE tbl_Chart_Conjunctions SET ConjunctionGroupId` by `(ChartResultId, SignId)`; order per spec §4.7; one transaction per `ChartResultId`.
- [ ] `BirthDetailDeletionService` — delete groups by birth-detail (members cascade).
- [ ] Wire the new repo into the CLI composition root next to `ChartConjunctionsRepository`.
- [ ] `dotnet build -warnaserror` Debug + Release; `recompute-analytics` on dev DB.
- [ ] Commit.

## Task 11: `verify-schema` additions + full verification
**Files:** Modify `src/Ikiastrro.Cli/Program.cs`
**Interfaces:** Produces the group-layer invariants in `verify-schema`
- [ ] Every `tbl_Chart_Conjunctions` row has non-null `ConjunctionGroupId`; `Planet1Id` and `Planet2Id` are both in `tbl_Chart_ConjunctionGroupMembers` for that group.
- [ ] `ConjunctionGroups.PlanetCount` = member count = distinct `PointKind='Graha'` planets in that sign per `tbl_Chart_KeyDetails`.
- [ ] `MemberKey` ascending-sorted; entry count = `PlanetCount`.
- [ ] D1 groups: `LongitudeSpanDegrees` non-null, `= max−min` member `NirayanaLongitude`; every member `OrbFromGroupCenterDegrees` non-null and `≤` the group span. Varga groups: both null.
- [ ] D1 members: non-null in-range `DegreesInSign` + `NirayanaLongitude`; member `DignityStatus` = that planet's `tbl_Chart_KeyDetails.DignityStatus` for the same `ChartResultId`.
- [ ] `dotnet build -warnaserror` Debug + Release · **all** `verify-*` (`schema`, `vargas`, `functional-nature`, `jaimini`, `avastha`, `sources`, `pipeline`, `terminology`, `rules`, `dignity`) `ALL PASS` · Web smoke `/charts/1` → 200.
- [ ] Commit.

## Task 12: Docs + ledger
**Files:** Modify `PRODUCT.md`, `research_ikiastrro.md`, `docs/superpowers/specs/2026-09-04-...-design.md` (status), create `.superpowers/sdd/2026-09-04-graha-dignity-rule-layer/progress.md`, update memory
- [ ] `PRODUCT.md`: add **FEAT-DIGNITY-02 · Graha dignity rule table (`tbl_Rule_GrahaDignity`, segmented, source-attributed)**; update FEAT-DIGNITY-01 (data-driven + PVR active, verify `verify-dignity`); update FEAT-RELATIONSHIP-01 (conjunction group/member layer). Tick DB/Core/Verify/Docs boxes as completed.
- [ ] `research_ikiastrro.md`: link `docs/research/dignity-pvr-integrated.md`; note the group layer as FEAT-YOGA-01 groundwork.
- [ ] Spec header → `Status: executed`.
- [ ] SDD `progress.md` ledger: commits, verify results, any deviations.
- [ ] Update the `memproj_vedic_horo_gen` memory line (migrations 22–24, PVR switch, conjunction groups).
- [ ] Commit.

## PRODUCT.md
On completion, tick: FEAT-DIGNITY-01 → [DB|Core|Verify|Docs], FEAT-DIGNITY-02 → [DB|Core|Verify|Docs], FEAT-RELATIONSHIP-01 → [DB|Core|Verify|Docs] done.

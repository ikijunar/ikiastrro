# Graha Dignity Rule Layer + Conjunction Groups — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `DignityEngine`'s hard-coded dictionaries with a segmented, source-attributed `tbl_Rule_GrahaDignity` (PVR *Integrated Approach* Table 6 as the active rule-set), and add an explicit conjunction group/member layer that carries each participant's degree + dignity for yoga deduction.

**Architecture:** Three additive phases. **Phase 3 landed first** (branch `feat/conjunction-groups`, migration `22_add_multigraha_conjunction.sql`): `tbl_Chart_MultiGrahaConjunction` + `tbl_Chart_MultiGrahaConjunctionMember` + `tbl_Chart_Conjunctions.MultiGrahaConjunctionId`, backfilled (268/675/all-linked), `verify-schema` `ALL PASS`, folded into baseline — the engine/repo/`verify-schema`-additions tasks (8–11) remain. **Phase 1** (this revision, branch `feat/graha-dignity-rule-layer`) lands the reference layer as **one migration `23`** — `tbl_Rule_GrahaDignity` (axis-A dignity only: `EXALTED/MOOLATRIKONA/OWN/DEBILITATED` segments + `DeepDegree` + `DignityScore` + `Mood`/`InterpretationTendency`/`Analogy`/`DignityRationale` columns), seeded `PVR_INTEGRATED` active + `BPHS_PARASHARI` inactive, plus `vw_Dignity_Legend` — **no `tbl_Dim_DignityType`**, with zero behaviour change. The five-fold Panchadhā Maitrī axis (`tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance`, `CombineToPanchadha`, the 9-value `DignityStatus` vocabulary, `tbl_Rule_WakefulnessState`, `verify-avastha`) is **not touched**. Phase 2 points `DignityEngine` at the active axis-A rule-set via a new `IDignityRuleProvider`, flips active behaviour to PVR, adds `DignityScore` to `DignityResult`, and forces a deliberate re-baseline of dignity-dependent golden records. Every step gated by `dotnet build` + the `verify-*` suite.

**Tech Stack:** .NET 8 / C# (nullable ref types; top-level statements in CLI), SQL Server (Windows Auth `localhost`), Dapper, `sqlcmd` migrations with `:setvar DbName`, SwissEphNet 2.8.0.2, Blazor Server (Web).

**Spec:** `docs/superpowers/specs/2026-09-04-graha-dignity-rule-layer-and-conjunction-groups-design.md`

## Research
Status: [x] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| `docs/research/dignity-pvr-integrated.md` | Tasks 2–3 (seed values), Task 5 (engine mapping), Task 6 (re-baseline) | complete |

## Global Constraints
- Branch: Phase 3 delivered on `feat/conjunction-groups`; Phase 1–2 on `feat/graha-dignity-rule-layer` (cut from `master`).
- Commit trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01FkkFi2M4dmZ2S4dxMQN5z7
  ```
- Do not push unless asked.
- Migrations: numbered `NN_*.sql`, idempotent, self-recording into `dbo.SchemaMigrations`. Migration `22` (multi-graha conjunction) **applied**; Phase 1 adds **`23`** (`tbl_Rule_GrahaDignity` — applied) and **`24`** (`tbl_Rule_CompoundRelationship`). Fold each proven migration's DDL into `db/ikiastrro.sql`; keep `vw_Chart_Consolidated` last, `vw_Dignity_Legend` just before it.
- No test project — verify via `dotnet build -warnaserror` (Debug + Release) + CLI `verify-*` modes + Web smoke `/charts/1`.
- Spec §8 decisions 1–8 are settled: two-axis model, no `tbl_Dim_DignityType`, `MultiGrahaConjunction*` naming, Phase 3 first; **two separate scores** — `DignityScore` (`tbl_Rule_GrahaDignity`, `−2…+4`: `Exalted +4 · Moolatrikona +3 · Own +2 on every own row · Debilitated −2`) and `RelationshipScore` (`tbl_Rule_CompoundRelationship`, `−2…+2`: `Adhimitra +2 · Mitra +1 · Sama 0 · Śatru −1 · Adhiśatru −2`, full 5 tiers, never merged); `tbl_Rule_NaturalRelationship` Moon row kept as-is. §8.12–13 (`IsPrimary` assignment) are cosmetic — pick during seed authoring.

---

## Phase 1 — Dignity reference layer (zero behaviour change)

## Task 1: Migration `23` — `tbl_Rule_GrahaDignity` (table + both rule-sets + source + catalog + view)
**Files:** Create `db/23_add_rule_graha_dignity.sql` · Modify `db/ikiastrro.sql`, `docs/research/reference-sources.md`
**Interfaces:** Produces `dbo.tbl_Rule_GrahaDignity` (~40 `PVR_INTEGRATED` rows + `BPHS_PARASHARI` mirror), `dbo.vw_Dignity_Legend`; `tbl_Dim_Source` gains `SRC_PVR_INTEGRATED`; `tbl_Rule_Catalog` gains a row
- [ ] Create `tbl_Rule_GrahaDignity` per spec §5.23 — columns incl. `DignityTypeCode VARCHAR(20) CHECK IN ('EXALTED','MOOLATRIKONA','OWN','DEBILITATED')`, `DignityScore SMALLINT NOT NULL CHECK BETWEEN -2 AND 4`, `DeepDegree`, `IsPrimary`, `Mood`/`InterpretationTendency`/`Analogy`/`DignityRationale`, `MethodCode`, `RuleParametersJson` (ISJSON), `CalculationNarrative`, `SourceRefCode` (`SRC[_]%`), `IsActive`. Constraints `CK_RuleGrahaDignity_Degrees` / `_Deep` / `_Score`, `UNIQUE (RuleSetId, PlanetId, SignId, DignityTypeCode, StartDegree)`, index `(RuleSetId, PlanetId)`. `IF OBJECT_ID` guarded.
- [ ] Ensure `tbl_Rule_Sets` has `PVR_INTEGRATED` (`IsActive = 1`) and `BPHS_PARASHARI` (`IsActive = 0`).
- [ ] Seed **`PVR_INTEGRATED`** from `docs/research/dignity-pvr-integrated.md` "Derived segment model" — per planet: exalt 1 + debil 1 + the MT-sign split (MT segment + own segment) + the **"other own sign"** as a whole-sign `OWN 0–30` row (Mercury/Gemini, Mars/Scorpio, Jupiter/Pisces, Venus/Taurus, Saturn/Capricorn, Rahu/Aquarius, Ketu/Scorpio). `DignityScore` `EXALTED +4 · MOOLATRIKONA +3 · OWN +2` (both own segments) `· DEBILITATED −2`. `DeepDegree` on EXALTED/DEBILITATED for planets 1–7, NULL for nodes. `IsPrimary` per spec §8.10–11. `Mood`/`Tendency`/`Analogy` per spec §4.2b; `DignityRationale` on the 8 Jupiter/Mercury/Ketu rows; Mars/Aries MT row `CalculationNarrative` = the note-4 "Leo" typo. **No `FRIEND`/`NEUTRAL`/`ENEMY` rows.**
- [ ] Seed **`BPHS_PARASHARI`** (`IsActive = 0`) from today's `DignityEngine.cs` dicts — Rāhu exalt Taurus / debil Scorpio, Ketu exalt Scorpio / debil Taurus (no node own/MT), Moon MT Taurus 4–30, Mercury MT Virgo 16–20. `SourceRefCode = 'SRC_BPHS'`.
- [ ] Seed as `VALUES` CTEs joined to `tbl_Planets` / `tbl_SignAttributes` by natural key; idempotent (`DELETE` both rule-sets' rows then re-`INSERT`, gated on the ledger entry).
- [ ] `MERGE`/insert `SRC_PVR_INTEGRATED` into `tbl_Dim_Source` (author "P. V. R. Narasimha Rao"); add the row to `docs/research/reference-sources.md`.
- [ ] Add the `tbl_Rule_Catalog` row (EngineCode `DIGNITY`, MethodCode `SEGMENT_LOOKUP`, IntroducedIn `23_add_rule_graha_dignity.sql`).
- [ ] Add `vw_Dignity_Legend` (`SELECT DISTINCT DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy … WHERE IsActive = 1`).
- [ ] `INSERT dbo.SchemaMigrations` (`WHERE NOT EXISTS`); `PRINT` row-count summary.
- [ ] Apply to dev DB; fold DDL + seed + view into `db/ikiastrro.sql` (view before `vw_Chart_Consolidated`).
- [ ] `dotnet build`; `verify-sources` + `verify-rules` + `verify-schema` `ALL PASS`.
- [ ] Commit.

## Task 2: `verify-dignity` CLI mode
**Files:** Modify `src/Ikiastrro.Cli/Program.cs` · Create `src/Ikiastrro.Cli/fixtures/dignity-fixture.json` (or inline)
**Interfaces:** Consumes `tbl_Rule_GrahaDignity`, `tbl_SignAttributes`, `tbl_Rule_WakefulnessState`; produces `verify-dignity` pass/fail
- [ ] New `verify-dignity` branch, same shape as `verify-rules`.
- [ ] **Tiling** — for each `RuleSetId` and each (PlanetId, SignId) that has rows, segments tile `[StartDegree, EndDegree)` with no gap / no overlap. (A planet's other 8 signs have no axis-A row — expected.)
- [ ] **Coverage** — each classical planet (1–7) has exactly one whole-sign `EXALTED` and one `DEBILITATED` row in both rule-sets; 1–2 `OWN` signs; MT on exactly one sign.
- [ ] **Dignity score map** — every `tbl_Rule_GrahaDignity` row's `DignityScore` = its `DignityTypeCode` canonical value (`EXALTED +4`, `MOOLATRIKONA +3`, `OWN +2` on **every** own row, `DEBILITATED −2`).
- [ ] **Relationship score map** — `tbl_Rule_CompoundRelationship` has all 6 (natural × temporary) combos, `RelationshipScore` per the ladder (`ADHIMITRA +2 · MITRA +1 · SAMA 0 · SHATRU −1 · ADHISHATRU −2`), `EnglishName` ∈ the 5 Maitrī `DignityStatus` labels, and `CombineToPanchadha` output = a lookup on it for all 6 cases.
- [ ] **Metadata constancy** — `Mood`/`InterpretationTendency`/`Analogy` single-valued per `DignityTypeCode`; `DignityRationale` non-NULL only on `PVR_INTEGRATED`.
- [ ] **Vocabulary unchanged** — the distinct `DignityStatus` strings `DignityEngine` can emit are exactly the 9 keyed in `tbl_Rule_WakefulnessState` (every one has a wakefulness row).
- [ ] **Fixture** — a committed fixture chart's expected `DignityStatus` + `DignityScore` for all 9 grahas matches `DignityEngine.Evaluate` (passes against BPHS values now; re-checked after Task 5 — gate the active-set checks on `IsActive`).
- [ ] **Seed cross-check** — `tbl_SignAttributes.ExaltedDegree` / `DebilitatedDegree` / `MooltrikonaRange*` agree with the active-set classical-seven rows (nodes exempt).
- [ ] Register `verify-dignity` in `db/README.md`, `PRODUCT.md` verify column, any "run all verify" helper.
- [ ] `dotnet build`; `verify-dignity` `ALL PASS`.
- [ ] Commit.

## Task 3: Migration `24` — `tbl_Rule_CompoundRelationship`
**Files:** Create `db/24_add_rule_compound_relationship.sql` · Modify `db/ikiastrro.sql`
**Interfaces:** Produces `dbo.tbl_Rule_CompoundRelationship` (6 rows, the Pañcadhā Maitrī 2×3 matrix); `tbl_Rule_Catalog` gains a row
- [ ] Create `tbl_Rule_CompoundRelationship` per spec §5.24 — `RuleSetId` FK, `NaturalRelation VARCHAR(10) CHECK IN ('Friend','Neutral','Enemy')`, `IsTemporaryFriend BIT`, `CompoundCode VARCHAR(20) CHECK IN ('ADHIMITRA','MITRA','SAMA','SHATRU','ADHISHATRU')`, `SanskritName`, `EnglishName` (= the `DignityStatus` label), `RelationshipScore SMALLINT CHECK BETWEEN -2 AND 2`, `tbl_Rule_*` template cols, `UNIQUE (RuleSetId, NaturalRelation, IsTemporaryFriend)`. `IF OBJECT_ID` guarded.
- [ ] Seed the 6 rows (RuleSetId 1, `SourceRefCode = 'SRC_PVR_INTEGRATED'`): Friend+TF→ADHIMITRA/Adhimitra/Great Friend/+2 · Friend+TE→SAMA/Sama/Neutral/0 · Neutral+TF→MITRA/Mitra/Friend/+1 · Neutral+TE→SHATRU/Śatru/Enemy/−1 · Enemy+TF→SAMA/Sama/Neutral/0 · Enemy+TE→ADHISHATRU/Adhiśatru/Great Enemy/−2. Idempotent (`IF NOT EXISTS` on the table).
- [ ] Add the `tbl_Rule_Catalog` row (EngineCode `DIGNITY`, MethodCode `MATRIX_LOOKUP`, IntroducedIn `24_add_rule_compound_relationship.sql`).
- [ ] `INSERT dbo.SchemaMigrations`; `PRINT` summary. Assert the 6-row lookup reproduces `CombineToPanchadha`'s truth table.
- [ ] Apply to dev DB; fold into `db/ikiastrro.sql` (near the natural/temporary relationship seeds, or with the tbl_Rule_Catalog block).
- [ ] `verify-sources` + `verify-schema` `ALL PASS`.
- [ ] Commit.

---

## Phase 2 — `DignityEngine` data-driven + PVR active

## Task 5: `IDignityRuleProvider` + engine rewire
**Files:** Create `src/Ikiastrro.Core/Engines/Dignity/IDignityRuleProvider.cs`, `src/Ikiastrro.Data/DignityRuleProvider.cs` · Modify `src/Ikiastrro.Core/Engines/Dignity/DignityEngine.cs`, `src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs` (wiring), `src/Ikiastrro.Cli/Program.cs` (composition)
**Interfaces:** Consumes `tbl_Rule_GrahaDignity` (active set); `DignityEngine.Evaluate` signature unchanged, output shape unchanged except two added fields
- [ ] `IDignityRuleProvider` — returns the active rule-set's segments as an in-memory structure keyed by planet → ordered segments (+ deep points, own-sign list, exalt/debil signs).
- [ ] `DignityRuleProvider` (Data) — loads once via `SqlConnectionFactory`; Dapper.
- [ ] `DignityEngine.Evaluate` — replace the private `ExaltationSign` / `DebilitationSign` / `Moolatrikona` dicts with lookups on the provider's data; add the `[StartDegree, EndDegree)` segment walk for own-sign vs moolatrikona (spec §4.3). Keep the varga-chart (`degreeInSign == null`) path identical to today. **`NaturalRelationship`, `SignDistance`, `IsTemporaryFriend`, and the priority merge stay untouched.**
- [ ] `CombineToPanchadha` — replace the hard-coded 2×3 truth table with a lookup on `tbl_Rule_CompoundRelationship` (via `IDignityRuleProvider`); return `(string Status, int RelationshipScore)` from the matched row. Output strings unchanged (the 5 Maitrī labels).
- [ ] `DignityResult` gains `DeepDegree` (decimal?), `DignityTypeCode` (string?, null for Maitrī tiers), `DignityScore` (int, `−2…+4`, axis A — 0 when no dignity applies), `RelationshipScore` (int?, `−2…+2`, axis B — null for Ascendant / a node with no Maitrī). The two scores are **never** merged.
- [ ] Relax the Rāhu/Ketu branch so nodes can resolve to `Own Sign` / `Moolatrikona` when the active set provides those rows; a node not in any dignity still returns `Neutral` (no Panchadhā Maitrī for nodes). The 9 possible `DignityStatus` strings are unchanged — `tbl_Rule_WakefulnessState` needs no migration.
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

## Phase 3 — Multi-graha conjunction layer

## Task 7: Migration `22` — group tables + pair link + backfill  · **DONE** (branch `feat/conjunction-groups`)
**Files:** `db/22_add_multigraha_conjunction.sql` · `db/ikiastrro.sql`
**Delivered:** `tbl_Chart_MultiGrahaConjunction` + `tbl_Chart_MultiGrahaConjunctionMember` + `tbl_Chart_Conjunctions.MultiGrahaConjunctionId` (FK). Backfilled from `tbl_Chart_KeyDetails` (`PointKind='Graha'`, `HAVING COUNT(*) >= 2`): 268 groups / 675 members / all pair rows linked. Idempotent; `verify-schema` `ALL PASS`; folded into baseline.
- [x] Tables, CHECKs, `UNIQUE (ChartResultId, SignId)` / `UNIQUE (MultiGrahaConjunctionId, PlanetId)`, D1-only `LongitudeSpanDegrees` / `OrbFromGroupCenterDegrees`, `ON DELETE CASCADE` on members.
- [x] `ALTER TABLE tbl_Chart_Conjunctions ADD MultiGrahaConjunctionId INT NULL` + FK, `COL_LENGTH`-guarded.
- [x] Backfill groups + members + pair-link; `INSERT dbo.SchemaMigrations`; fold into baseline.
- [ ] **Deferred to Task 7b:** `GRAHA_SAMYOGA` terminology concept (`tbl_Astro_Terminology` + `_Text`, `sa`+`en`) — not in migration 22; add in a follow-up migration alongside Task 8.

## Task 8: `RelationshipEngine` — group derivation
**Files:** Modify `src/Ikiastrro.Core/Engines/Relationships/RelationshipEngine.cs` · Create `src/Ikiastrro.Core/Models/ChartMultiGrahaConjunction.cs`, `ChartMultiGrahaConjunctionMember.cs`
**Interfaces:** Consumes `ChartAnalysisInput`; produces group + member row models
- [ ] `MultiGrahaConjunctionResult` record + `FindMultiGrahaConjunctions(ChartAnalysisInput)` — grahas grouped by sign, `Count >= 2`, each carrying its members' `PlanetPosition`s.
- [ ] `BuildMultiGrahaConjunctionRows` / `BuildMultiGrahaConjunctionMemberRows` — canonical member order by `PlanetId`; `MemberKey` = ascending `PlanetId` CSV; `PlanetCount` set; member `DegreesInSign` = `NirayanaLongitudeDegrees % 30` (D1) or `VargaLongitudeDegrees % 30` (varga), `NirayanaLongitude` / `VargaLongitude` / `IsRetrograde` from `PlanetPosition`.
- [ ] D1 only: group `LongitudeSpanDegrees` = `max−min` of member `NirayanaLongitudeDegrees`; member `OrbFromGroupCenterDegrees` = `|memberLon − mean(memberLon)|`. Both null for varga (discrete bucket — same rule as `ChartConjunction.DegreeSeparation`).
- [ ] `BuildConjunctionRows` — also emit each pair's owning `MemberKey` so persistence can resolve `MultiGrahaConjunctionId`. Pair rows are the only persisted subset; triples/quads are left for the Yoga engine to enumerate from `MemberKey` (spec §4.5).
- [ ] `dotnet build -warnaserror`.
- [ ] Commit.

## Task 9: `ChartAnalyzer` — stitch member dignity
**Files:** Modify `src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs`
**Interfaces:** Consumes the per-planet `DignityResult` already computed in the same pass; produces group members with `DignityStatus`
- [ ] After computing `DignityResult` per planet, set each `ChartMultiGrahaConjunctionMember.DignityStatus` from the matching planet's result (`DignityStatus`; `IsCombust` from the existing combustion pass).
- [ ] Ensure groups/members are emitted into whatever bundle `ChartAnalyzer` returns to the generation service.
- [ ] `dotnet build -warnaserror`.
- [ ] Commit.

## Task 10: Persistence + lifecycle
**Files:** Create `src/Ikiastrro.Data/ChartMultiGrahaConjunctionRepository.cs` · Modify `src/Ikiastrro.Data/ChartConjunctionsRepository.cs` (write `MultiGrahaConjunctionId`), `src/Ikiastrro.Core/.../ChartGenerationService.cs` (RecomputeAnalytics), `BirthDetailDeletionService`, `src/Ikiastrro.Cli/Program.cs` (composition root)
**Interfaces:** Consumes group/member rows; produces persisted rows + pair-link update
- [ ] `ChartMultiGrahaConjunctionRepository` — `InsertAll(groups)`, `InsertAllMembers(members)`, `GetByBirthDetailId`, `DeleteByChartResultId`, `DeleteByBirthDetailId` (mirror `ChartConjunctionsRepository`).
- [ ] `RecomputeAnalytics` — derive + insert groups → members, then `UPDATE tbl_Chart_Conjunctions SET MultiGrahaConjunctionId` by `(ChartResultId, SignId)`; order per spec §4.7; one transaction per `ChartResultId`.
- [ ] `BirthDetailDeletionService` — delete groups by birth-detail (members cascade).
- [ ] Wire the new repo into the CLI composition root next to `ChartConjunctionsRepository`.
- [ ] `dotnet build -warnaserror` Debug + Release; `recompute-analytics` on dev DB.
- [ ] Commit.

## Task 11: `verify-schema` additions + full verification
**Files:** Modify `src/Ikiastrro.Cli/Program.cs`
**Interfaces:** Produces the group-layer invariants in `verify-schema`
- [ ] Every `tbl_Chart_Conjunctions` row has non-null `MultiGrahaConjunctionId`; `Planet1Id` and `Planet2Id` are both in `tbl_Chart_MultiGrahaConjunctionMember` for that group.
- [ ] `tbl_Chart_MultiGrahaConjunction.PlanetCount` = member count = distinct `PointKind='Graha'` planets in that sign per `tbl_Chart_KeyDetails`.
- [ ] `MemberKey` ascending-sorted; entry count = `PlanetCount`.
- [ ] D1 groups: `LongitudeSpanDegrees` non-null, `= max−min` member `NirayanaLongitude`; every member `OrbFromGroupCenterDegrees` non-null and `≤` the group span. Varga groups: both null.
- [ ] D1 members: non-null in-range `DegreesInSign` + `NirayanaLongitude`; member `DignityStatus` = that planet's `tbl_Chart_KeyDetails.DignityStatus` for the same `ChartResultId`.
- [ ] `dotnet build -warnaserror` Debug + Release · **all** `verify-*` (`schema`, `vargas`, `functional-nature`, `jaimini`, `avastha`, `sources`, `pipeline`, `terminology`, `rules`, `dignity`) `ALL PASS` · Web smoke `/charts/1` → 200.
- [ ] Commit.

## Task 12: Docs + ledger
**Files:** Modify `PRODUCT.md`, `research_ikiastrro.md`, `docs/superpowers/specs/2026-09-04-...-design.md` (status), create `.superpowers/sdd/2026-09-04-graha-dignity-rule-layer/progress.md`, update memory
- [ ] `PRODUCT.md`: add **FEAT-DIGNITY-02 · Graha dignity rule table (`tbl_Rule_GrahaDignity`, axis-A segmented, source-attributed, `DignityScore`)**; update FEAT-DIGNITY-01 (data-driven + PVR active + `DignityScore`, verify `verify-dignity`); update FEAT-RELATIONSHIP-01 (multi-graha conjunction layer). Tick DB/Core/Verify/Docs boxes as completed.
- [ ] `research_ikiastrro.md`: link `docs/research/dignity-pvr-integrated.md`; note the group layer as FEAT-YOGA-01 groundwork; note the two-axis (dignity vs. Panchadhā Maitrī) model.
- [ ] Spec header → `Status: executed`.
- [ ] SDD `progress.md` ledger: commits, verify results, any deviations.
- [ ] Update the `memproj_vedic_horo_gen` memory line (migrations 22–23, PVR axis-A switch, `DignityScore`, multi-graha conjunctions, axis B untouched).
- [ ] Commit.

## PRODUCT.md
On completion, tick: FEAT-DIGNITY-01 → [DB|Core|Verify|Docs], FEAT-DIGNITY-02 → [DB|Core|Verify|Docs], FEAT-RELATIONSHIP-01 → [DB|Core|Verify|Docs] done.

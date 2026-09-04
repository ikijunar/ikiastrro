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
- Migrations: numbered `NN_*.sql`, idempotent, self-recording into `dbo.SchemaMigrations`. Migration `22` (multi-graha conjunction) **applied**; Phase 1 adds **`23`** (`tbl_Rule_GrahaDignity`), **`24`** (`tbl_Rule_CompoundRelationship`), **`25`** (repoint `tbl_Rule_NaturalRelationship.SourceRefCode` → `SRC_PVR_INTEGRATED`), **`26`** (`tbl_Dim_GrahaAttribute` + `tbl_Rule_GrahaAttribute` + `tbl_Rule_DigBala` — graha characters, PVR-consolidated) — all applied. Fold each proven migration's DDL into `db/ikiastrro.sql`; keep `vw_Chart_Consolidated` last, `vw_Dignity_Legend` just before it.
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

## Task 2: `verify-dignity` CLI mode  · **DONE**
**Files:** Modify `src/Ikiastrro.Cli/Program.cs` (inline fixture — no separate JSON), `db/README.md`, `docs/techstack-details.md`, `ARCHITECTURE.md`
**Interfaces:** Consumes `tbl_Rule_GrahaDignity`, `tbl_Rule_CompoundRelationship`, `tbl_SignAttributes`, `tbl_Rule_WakefulnessState`; produces `verify-dignity` pass/fail
- [x] New `verify-dignity` branch, same shape as `verify-rules` (28 checks).
- [x] **Tiling** — per (`RuleSetId`, `PlanetId`, `SignId`) with rows, segments tile `[0,30)` gap-/overlap-free (`LEAD` window).
- [x] **Coverage** — classical seven, both sets: `EXALTED` on exactly one sign, one whole-sign `DEBILITATED`, `MOOLATRIKONA` on ≤ 1 sign (BPHS mirror legitimately omits Moon/Mercury MT), `OWN` 1–2 signs; + set 2 has all 7 MT signs; nodes: set 2 all four types, set 3 only `EXALTED`/`DEBILITATED`.
- [x] **DeepDegree placement** — `EXALTED`/`DEBILITATED` only; present for 1–7, NULL for nodes.
- [x] **Dignity score map** — `DignityScore` = `+4/+3/+2/−2` per `DignityTypeCode` on every row.
- [x] **Relationship score map** — `tbl_Rule_CompoundRelationship`: all 6 combos, ladder `+2/+1/0/−1/−2`, `EnglishName` ∈ the 5 Maitrī labels, 6 rows reproduce `CombineToPanchadha`'s truth table.
- [x] **Metadata constancy** — `Mood`/`InterpretationTendency`/`Analogy` single-valued per `DignityTypeCode`; `DignityRationale` non-NULL only on `PVR_INTEGRATED` (set 2).
- [x] **Vocabulary unchanged** — the engine's 9 `DignityStatus` strings ⇔ `tbl_Rule_WakefulnessState` keys (bidirectional).
- [x] **Fixture** — inline 9-graha chart, all placements axis-A; asserts `DignityEngine.Evaluate` (BPHS behaviour today; Task 5 re-baselines the node rows).
- [x] **Seed cross-check** — active-set (`RuleSetId 2`) `EXALTED`/`DEBILITATED` sign + deep degree strict vs `tbl_SignAttributes`; MT range strict for Su/Ma/Ju/Ve/Sa; the 2 PVR divergences (Moon MT 3° vs seed NULL, Mercury MT 15° vs seed 16°) reported `[KNOWN]`, not failed. **`tbl_SignAttributes` still carries the BPHS Moon/Mercury MT values — a later cleanup migration could align it once Phase 2 lands.**
- [x] Registered in `db/README.md` (new Verification section), `docs/techstack-details.md` mode table, `ARCHITECTURE.md`.
- [x] `dotnet build src/Ikiastrro.Cli -warnaserror` 0/0; `verify-dignity` `ALL PASS`; `verify-schema`/`verify-rules`/`verify-sources`/`verify-avastha` still `ALL PASS`.
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

## Task 3b: Migration `25` — repoint `tbl_Rule_NaturalRelationship` citation to PVR  · **DONE**
`UPDATE … SET SourceRefCode = 'SRC_PVR_INTEGRATED'` (42 rows, data unchanged); baseline `UPDATE` line converged; from-empty rebuild → all 42 PVR; `verify-sources` ALL PASS. Committed `68003f1`.

## Task 4: Migration `26` — graha characters (PVR-consolidated)  · **DONE**
**Files:** `db/26_add_rule_graha_attributes.sql` · `db/ikiastrro.sql` · `docs/research/graha-characters-pvr.md`
- [x] `docs/research/graha-characters-pvr.md` — cleaned/normalized worksheet (16 attributes, canonical `ValueCode`s, value vocabularies, 2 divergence flags).
- [x] `tbl_Dim_GrahaAttribute` (16) + `tbl_Rule_GrahaAttribute` (111 rows, `UNIQUE (RuleSetId, GrahaId, AttributeCode)`, `SourceRefCode = SRC_PVR_INTEGRATED`) + `tbl_Rule_DigBala` (7, `DigBalaHouse` 1–12). Spec §5.26.
- [x] 2 `tbl_Rule_Catalog` rows (`GRAHA`/`ATTR_LOOKUP`, `STRENGTH`/`HOUSE_LOOKUP`). `tbl_Planets` unchanged.
- [x] Applied + idempotent; folded; from-empty scratch rebuild clean (16 / 111 / 7); `verify-sources` / `verify-schema` / `verify-rules` ALL PASS.
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

## Task 8: `RelationshipEngine` — group derivation  · **DONE**
**Files:** `src/Ikiastrro.Core/Engines/Relationships/RelationshipEngine.cs` · `src/Ikiastrro.Core/Models/ChartMultiGrahaConjunction.cs`, `ChartMultiGrahaConjunctionMember.cs` (new)
- [x] `RelationshipEngine.BuildMultiGrahaConjunctions(ChartAnalysisInput input, IReadOnlyList<ChartKeyDetail> keyDetails)` — grahas grouped by `SignId`, `Count >= 2`. **Deviation:** takes the already-built `keyDetails` graha rows (not a fresh `PlanetPosition` walk) — they already carry the stitched `DignityStatus`/`IsCombust`, so Task 9's "stitch" collapses into this one call with no `DignityEngine` plumbing.
- [x] Canonical member order by `PlanetId`; `MemberKey` = ascending `PlanetId` CSV; `PlanetCount` set; member `DegreesInSign` from `DegreesInSignDecimal`, `NirayanaLongitude` / `VargaLongitude` / `IsRetrograde` / `DignityStatus` / `IsCombust` from the KeyDetail row.
- [x] D1 only (`input.ChartType == "D1"`): group `LongitudeSpanDegrees` = `max−min` member real longitude; member `OrbFromGroupCenterDegrees` = `|memberLon − mean(memberLon)|`. Both null for varga.
- [x] **Deviation:** `BuildConjunctionRows` **not** touched — the pair→group link is resolved in persistence by a `(ChartResultId, SignId)` SQL join (same as the migration-22 backfill), so no `MemberKey` plumbing through `ChartConjunction` was needed.
- [x] `dotnet build -warnaserror` Debug — 0/0.

## Task 9: stitch member dignity  · **DONE (folded into Task 8)**
- [x] Member `DignityStatus` + `IsCombust` come straight from the matching planet's `ChartKeyDetail` row (built by `ChartAnalyzer.Compute` in the same pass) — no change to `ChartAnalyzer.cs`, no new dignity computation. `verify-schema` asserts `member.DignityStatus == kd.DignityStatus` for the same `ChartResultId`.

## Task 10: Persistence + lifecycle  · **DONE**
**Files:** `src/Ikiastrro.Data/ChartMultiGrahaConjunctionRepository.cs` (new) · `ChartGenerationService.cs` · `BirthDetailDeletionService.cs` · `src/Ikiastrro.Cli/Program.cs` + `src/Ikiastrro.Web/Program.cs` (composition roots)
- [x] `ChartMultiGrahaConjunctionRepository` — `InsertAll(groups)` (per-group `OUTPUT INSERTED.Id`, then its members), `LinkPairRows(chartResultId)` (the `(ChartResultId, SignId)` UPDATE join), `GetByBirthDetailId` (groups + stitched members), `DeleteByChartResultId`, `DeleteByBirthDetailId`.
- [x] `ChartGenerationService.PersistAnalytics` — order per spec §4.7: KeyDetails → HouseLords → Conjunctions → **Groups → Members** → Aspects, then `LinkPairRows`. Delete calls added to `GenerateAll` (by birth-detail) + `RecomputeAnalytics` (by ChartResultId), **after** the pair-row delete (pair rows FK the groups). No cross-repo transaction — same as the rest of the service.
- [x] `BirthDetailDeletionService` — group delete inserted after `_conjunctionsRepo.DeleteByBirthDetailId`; members cascade via FK.
- [x] Wired into the CLI `new ChartGenerationService(...)` and Web `AddScoped<ChartMultiGrahaConjunctionRepository>()`.
- [x] `dotnet build -warnaserror` Debug (Core/Data/Cli/Web/Tests) — 0/0; `recompute-keydetails` on dev DB → 268 groups / 675 members, 0 pairs unlinked.

## Task 11: `verify-schema` additions + full verification  · **DONE**
**Files:** `src/Ikiastrro.Cli/Program.cs`
- [x] 14 new `verify-schema` checks: every pair linked; both pair planets are group members; `PlanetCount` = member count = distinct grahas in that sign; `PlanetCount >= 2`; `MemberKey` = ascending PlanetId CSV; `MemberKey` entry count = `PlanetCount`; D1 `LongitudeSpanDegrees` non-null `= max−min` member longitude (varga null); D1 member `OrbFromGroupCenterDegrees` non-null `<= span` (varga null); D1 member `DegreesInSign`/`NirayanaLongitude` non-null in-range; member `DignityStatus` = KeyDetails `DignityStatus`.
- [x] `dotnet build -warnaserror` Debug 0/0 · every `verify-*` (`schema` `vargas` `functional-nature` `jaimini` `avastha` `sources` `pipeline` `terminology` `rules` `dignity`) `ALL PASS`, before **and** after `recompute-keydetails`.
- [ ] Web smoke `/charts/1` → 200 — **pending manual (VS F5)**; Web project builds clean, only change is one DI registration.
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

# Graha dignity rule layer + conjunction groups — design

**Status:** Phase 3 delivered · Phase 1 revised (for plan) · **Created:** 2026-09-04 · **Branch:** Phase 3 `feat/conjunction-groups` · Phase 1 `feat/graha-dignity-rule-layer`
**Features:** FEAT-DIGNITY-01 (advances — data-driven + PVR + degree segments), FEAT-DIGNITY-02 (new — `tbl_Rule_GrahaDignity`), FEAT-RELATIONSHIP-01 (advances — conjunction group/member layer + per-planet degrees)
**Supersedes:** the 2026-08-24 decision (in `DignityEngine.cs` doc-comment + `ikiastrro.md`) to use the Parāśari/BPHS convention for Rāhu/Ketu exaltation. Active dignity rule-set becomes PVR *Integrated Approach* Table 6.

### Status update — 2026-09-04

- **Phase 3 (conjunction groups) landed first**, ahead of Phases 1–2, on branch `feat/conjunction-groups` as **migration `22_add_multigraha_conjunction.sql`**. Final table names differ from this spec's original draft:
  - `tbl_Chart_MultiGrahaConjunction` (was `tbl_Chart_ConjunctionGroups`)
  - `tbl_Chart_MultiGrahaConjunctionMember` (was `tbl_Chart_ConjunctionGroupMembers`)
  - `tbl_Chart_Conjunctions.MultiGrahaConjunctionId` (was `ConjunctionGroupId`)
  - Backfilled: 268 groups / 675 members / all pair rows linked; `verify-schema` `ALL PASS`; folded into `db/ikiastrro.sql`.
  - **Still pending** from Phase 3: the engine/repo/`verify-schema`-additions tasks (plan Tasks 8–11).
- **Phase 1 is revised below** per the 2026-09-04 discussion:
  - **No `tbl_Dim_DignityType`** (rammyps: "not create a new table"). Dignity-type metadata (`Mood` / `InterpretationTendency` / `Analogy`) and the new `DignityScore` are **columns on `tbl_Rule_GrahaDignity`**.
  - `tbl_Rule_GrahaDignity.DignityTypeCode` is a **4-value** set — `EXALTED / MOOLATRIKONA / OWN / DEBILITATED` — i.e. **axis A only** (see §4.2c). `FRIEND / NEUTRAL / ENEMY` are **not** in this table.
  - The five-fold **Panchadhā Maitrī axis is untouched**: `tbl_Rule_NaturalRelationship` (42 rows), `tbl_Rule_TemporaryFriendshipDistance` (12 rows), `DignityEngine.CombineToPanchadha`, the 9-value `DignityStatus` vocabulary, the `tbl_Rule_WakefulnessState` join, `verify-avastha`, and the `DignityState` terminology (9 concepts) all stay exactly as they are.
  - New **`DignityScore`** — rammyps's coarse ordinal `−2…+4` over the *final* 9-value `DignityStatus` (§4.2d): `Exalted +4 · Moolatrikona +3 · Own +2 (every own row) · Friend +1 · Neutral 0 · Enemy −1 · Debilitated −2`, with `Great Friend`→`+1` / `Great Enemy`→`−1`. Axis-A values stored on `tbl_Rule_GrahaDignity`; axis-B values from `CombineToPanchadha`.
  - Phase 1 migrations renumber: the dignity table is **migration `23`** (single file — table + both rule-sets + source + catalog).

## Research
Status: [x] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| `docs/research/dignity-pvr-integrated.md` | Phase 1–2 — Table 6, the 7 special-degree rules, the derived segment model, and every divergence from today's engine and the `tbl_SignAttributes` seed | complete |
| `docs/research/reference-sources.md` | needs a `SRC_PVR_INTEGRATED` row (Phase 1, Task 1) | pending — trivial add |

---

## 1. Problem

Two gaps, one shared root cause.

**a. Dignity is hard-coded and diverges from its own reference data.**
`DignityEngine.Evaluate` reads private C# dictionaries (`ExaltationSign`, `DebilitationSign`,
`Moolatrikona`), not a table. It cannot express the own-sign-vs-moolatrikona *degree split*
that PVR notes (1)–(7) require — e.g. Sun at 10° Leo (Moolatrikona) vs 25° Leo (Own Sign) —
beyond one BPHS range. Its Rāhu/Ketu exaltation signs and its Moon (4°) / Mercury (16°)
moolatrikona starts differ from PVR Table 6, and from the values already seeded (unused) in
`tbl_SignAttributes`. There is no `tbl_Rule_*` table for dignity, no source attribution, no
way to carry two traditions side by side.

**b. `tbl_Chart_Conjunctions` records only pairs, and has no per-planet position.**
A 4-graha cluster (e.g. Sun + Venus + Mercury + Mars in one sign) becomes 6 pair rows.
There is no first-class "N grahas together in sign X" fact, so count-based yogas (Sanyāsa,
stellium intensity) need connected-component queries every time. The table also stores no
planet degree, so exaltation / debilitation / own-house reasoning about a conjunction
participant cannot be done from the row.

Both want the same thing: a **data-driven, source-attributed dignity layer with degree
segments**, and a **place to hang per-planet degree + dignity** for a conjunction.

## 2. Goals

1. `tbl_Rule_GrahaDignity` — one segmented, source-attributed, rule-set-versioned table holding
   **axis-A dignity** (exaltation / debilitation / moolatrikona / own-sign segments + deep-degree
   points), seeded with **two** rule-sets: `PVR_INTEGRATED` (active) and `BPHS_PARASHARI` (inactive,
   kept for provenance and future toggle). Follows the established `tbl_Rule_*` template. Carries
   inline: `DignityTypeCode` (4-value — `EXALTED / MOOLATRIKONA / OWN / DEBILITATED`), `DignityScore`,
   and the interpretation metadata `Mood` / `InterpretationTendency` / `Analogy` / `DignityRationale`.
2. **No new dimension table.** Dignity-type metadata lives on `tbl_Rule_GrahaDignity` (goal 1); a
   `vw_Dignity_Legend` (`SELECT DISTINCT`) exposes the per-type legend for the UI without a physical
   table. The five-fold **Panchadhā Maitrī axis (axis B)** — `tbl_Rule_NaturalRelationship`,
   `tbl_Rule_TemporaryFriendshipDistance`, `DignityEngine.CombineToPanchadha` — is **not modified**.
3. `DignityEngine` reads the active rule-set for axis A instead of its `ExaltationSign` /
   `DebilitationSign` / `Moolatrikona` dicts; gains exact own-sign / moolatrikona degree-segment
   resolution; **active axis-A behaviour switches to PVR**. `DignityResult` gains `DignityTypeCode`,
   `DignityScore`, `DeepDegree`. The axis-A / axis-B merge into the single 9-value `DignityStatus`
   is unchanged; `CombineToPanchadha` additionally returns the axis-B score.
4. **DELIVERED (migration 22, branch `feat/conjunction-groups`).** `tbl_Chart_MultiGrahaConjunction`
   + `tbl_Chart_MultiGrahaConjunctionMember` — an explicit **multi-graha conjunction** (Graha Saṃyoga)
   layer. One group per Rāśi that holds ≥ 2 grahas, covering the 2-planet case with no special-casing.
   Members carry per-planet `DegreesInSign`, `NirayanaLongitude`, `VargaLongitude`,
   `OrbFromGroupCenterDegrees`, `DignityStatus` (matching `tbl_Chart_KeyDetails`), retrograde, combust
   — **once per planet**, not per pair. The group carries `PlanetCount`, `MemberKey`, and a D1
   `LongitudeSpanDegrees`. See `db/22_add_multigraha_conjunction.sql`.
5. **DELIVERED (migration 22).** `tbl_Chart_Conjunctions` keeps its pair rows and gains
   `MultiGrahaConjunctionId` (FK) linking each pair to its group. Triple / quadruple / larger subsets
   are **not** persisted — the Yoga engine enumerates them from `MemberKey` on demand (§4.5).
   "Stellium" is a Western term; it may appear as UI prose but the internal name is
   *multi-graha conjunction*.
6. `verify-dignity` (new — axis-A tiling, score-map, `DignityStatus`-vocabulary-unchanged checks)
   + `verify-schema` group-layer additions (still pending, Phase 3 Tasks 8–11); all existing
   `verify-*` stay `ALL PASS` after a deliberate re-baseline of dignity-dependent golden records.

## 3. Non-goals

- No shadbala / virūpa strength model. `DignityScore` is a **coarse ordinal (−2…+4)** for a future
  Planet ↔ Rāśi relationship score, not a shadbala component. No deep-exaltation falloff curve —
  `DeepDegree` is stored, not consumed.
- **No change to axis B (Panchadhā Maitrī).** The natural + temporary friendship rule tables, the
  9-value `DignityStatus` vocabulary, the `tbl_Rule_WakefulnessState` coupling, `verify-avastha`, and
  the `DignityState` terminology (9 concepts) are all out of scope. Phase 1 adds the axis-A segment
  layer and a score over the *existing* compound outcome — nothing more.
- No Panchadhā Maitrī for Rāhu/Ketu — a node not in a dignity stays `Neutral` (PVR Table 6 is dignity-only).
- No yoga detection, no subset enumeration, no `tbl_Rule_Yoga` population — this layer only provides
  the structural input (`tbl_Chart_ConjunctionGroupMembers` + `MemberKey` + per-member orb/dignity).
  FEAT-YOGA-01 consumes it; the reserved condition-based `tbl_Rule_Yoga` (migration 18,
  `RequirementJson` / `CancellationJson`) is where named-yoga rules will live.
- No combined-cluster interpretation output — reading the whole group as one planetary environment
  is a later interpretation-engine concern; this work just records the group.
- No Web UI beyond keeping `/charts/1` green; surfacing groups/mood in the workspace is a follow-on.
- `tbl_SignAttributes` dignity columns are not dropped — `verify-dignity` cross-checks them; a later
  cleanup can retire them once nothing reads them.

## 4. Design

### 4.1 Layering

```
AXIS A — dignity (sign + degree)          AXIS B — Panchadhā Maitrī (chart-specific)   [UNCHANGED]
tbl_Rule_GrahaDignity                     tbl_Rule_NaturalRelationship (42 rows)
  DignityTypeCode ∈ {EXALTED,               + tbl_Rule_TemporaryFriendshipDistance (12 rows)
    MOOLATRIKONA, OWN, DEBILITATED}         + DignityEngine.CombineToPanchadha
  segments per rule-set (PVR active,          → Great Friend / Friend / Neutral / Enemy / Great Enemy
    BPHS inactive); DeepDegree;
    DignityScore; Mood/Tendency/Analogy
        \_______________________  ______________________/
                                \/
              DignityEngine.Evaluate   (axis A wins by priority; else axis B)
                        │
                        ▼
              DignityStatus  (the SAME 9 values as today) + DignityScore (−2…+4)
                        │
        ┌───────────────┴───────────────┐
        ▼                               ▼
  tbl_Chart_KeyDetails.DignityStatus    tbl_Chart_MultiGrahaConjunctionMember.DignityStatus
  (+ tbl_Rule_WakefulnessState join,    (delivered, migration 22)
     verify-avastha — all unchanged)
```

### 4.2 Rule-set switch (PVR becomes active)

`tbl_Rule_GrahaDignity` rows are tagged by `RuleSetId` (FK `tbl_Rule_Sets`, same mechanism as every
other rule table). Seed:

- **`PVR_INTEGRATED`** — `IsActive = 1`. All rows from `docs/research/dignity-pvr-integrated.md`
  "Derived segment model". `SourceRefCode = 'SRC_PVR_INTEGRATED'`.
- **`BPHS_PARASHARI`** — `IsActive = 0`. Today's `DignityEngine.cs` dict values (Rāhu exalt Taurus,
  Moon moolatrikona 4°, Mercury 16°, no node own/moolatrikona). `SourceRefCode = 'SRC_BPHS'`.
  Purpose: provenance + a one-row toggle back if ever needed.

`DignityEngine` selects the single `IsActive = 1` set. No config knob in this work — switching
sets later is an `UPDATE tbl_Rule_Sets`. **PVR does not change axis B** — it uses standard Parāśari
naisargika maitrī, so `tbl_Rule_NaturalRelationship` needs no PVR rule-set. PVR changes only the
node exalt/debil signs, the node Own/Moolatrikona rows, and the Moon (4°→3°) / Mercury (16°→15°)
moolatrikona start.

### 4.2b Dignity-type metadata (on `tbl_Rule_GrahaDignity`)

Every `tbl_Rule_GrahaDignity` row carries three descriptive columns, constant per `DignityTypeCode`
(`verify-dignity` asserts the constancy). Repetition across ~40 seeded rows is accepted — this is a
small reference table. `vw_Dignity_Legend` = `SELECT DISTINCT DignityTypeCode, DignityScore, Mood,
InterpretationTendency, Analogy FROM tbl_Rule_GrahaDignity` gives the UI its legend.

| `DignityTypeCode` | `Mood` | `InterpretationTendency` | `Analogy` (PVR home/office/picnic) |
|---|---|---|---|
| `EXALTED` | Elevated | Performs exceptionally and enthusiastically. | Favourite picnic/party — excited, at its best. |
| `MOOLATRIKONA` | Dutiful | Powerful, purposeful, responsible. | Office — executes its formal duty, like it or not. |
| `OWN` | Comfortable | Natural, authentic, relaxed. | Home — most natural and at ease. |
| `DEBILITATED` | Uncomfortable | Struggles to express its natural qualities. | Worst party — unhappy, stuck where it hates to be. |

`DignityRationale` (`NVARCHAR(MAX) NULL`) holds PVR's per-planet reasoning **only where the source
gives it** — the Jupiter (Pisces / Sagittarius / Cancer / Capricorn), Mercury (Gemini / Virgo) and
Ketu (Scorpio / Pisces) worked examples from *Integrated Approach* Part 1. NULL on every other row.
Where a (planet, sign) spans two segment rows the same text is copied to both.

Axis-B tiers (`Great Friend … Great Enemy`) get their mood/label from the existing `DignityState`
terminology `Description` and a static presentation-layer map — **not** from this table.

### 4.2c Two axes — dignity vs. Panchadhā Maitrī

| | **Axis A — Rāśi dignity** | **Axis B — Panchadhā Maitrī** |
|---|---|---|
| Values | `EXALTED` / `MOOLATRIKONA` / `OWN` / `DEBILITATED` (+ "none") | `Great Friend` / `Friend` / `Neutral` / `Enemy` / `Great Enemy` |
| Depends on the chart? | No — planet + sign + degree | **Yes** — Tatkālika needs the sign-lord's live position |
| Source of truth | **NEW** `tbl_Rule_GrahaDignity` (this spec) | **EXISTING** `tbl_Rule_NaturalRelationship` + `tbl_Rule_TemporaryFriendshipDistance` + `CombineToPanchadha` |
| Covers how many of a planet's 12 signs? | 3–4 (exalt 1, debil 1, own 1–2) | the other 8–9 |
| Status | to build (Phase 1) | **do not touch** |

`DignityEngine.Evaluate` already merges them by priority — axis A wins where it applies, otherwise
axis B — producing the one 9-value `DignityStatus`. That merge stays. `tbl_Rule_GrahaDignity`
**never** holds `FRIEND` / `NEUTRAL` / `ENEMY` rows; re-deriving axis B statically would (a) drop
`Great Friend` / `Great Enemy` (they need the temporal layer), (b) duplicate
`tbl_Rule_NaturalRelationship`, (c) collide on the words "Friend"/"Enemy" which mean different
things on the two axes.

### 4.2d `DignityScore`

A coarse ordinal (rammyps's ladder, 2026-09-04). Axis A carries four values; the axis-B Panchadhā
tiers fill the `+1 … −1` band between `Own` and `Debilitated`. The two **"great"** tiers keep their
distinction in the `DignityStatus` *string* but collapse to their base tier's number for scoring —
`Great Friend` scores as `Friend` (`+1`), `Great Enemy` as `Enemy` (`−1`):

| `DignityStatus` | Score | Axis |
|---|--:|---|
| Exalted | **+4** | A |
| Moolatrikona | **+3** | A |
| Own Sign (primary segment **and** secondary "other own sign") | **+2** | A |
| Great Friend / Friend | **+1** | B |
| Neutral | **0** | B |
| Enemy / Great Enemy | **−1** | B |
| Debilitated | **−2** | A |

Storage, honouring "no new table":
- The **4 axis-A scores** (`EXALTED +4`, `MOOLATRIKONA +3`, `OWN +2`, `DEBILITATED −2`) are a stored
  `DignityScore SMALLINT` column on `tbl_Rule_GrahaDignity` rows. `OWN` is `+2` on **every** own row
  — the own segment of a moolatrikona sign (e.g. Leo 20–30° for Sun) and the whole-sign "other own
  sign" (e.g. Gemini for Mercury), regardless of `IsPrimary`.
- The **axis-B scores** (`+1` / `0` / `−1`) are returned by `DignityEngine.CombineToPanchadha`
  alongside the tier string — already computed there; no table.
- `verify-dignity` holds the canonical 9-row map (`Great Friend`→`+1`, `Great Enemy`→`−1`) and
  asserts both sources agree with it.

`DignityResult` surfaces `DignityScore` (int). Consumers that want mood/analogy join
`vw_Dignity_Legend` (axis A) or the static map (axis B).

### 4.3 Own-sign / moolatrikona segment resolution

Today the engine picks `Moolatrikona` only inside one BPHS range and otherwise falls to `Own Sign`
for the whole sign. With segments it walks the rows for (planet, sign) ordered by `StartDegree`
and picks the segment whose `[StartDegree, EndDegree)` contains `degreeInSign`. For a varga chart
(`degreeInSign` null) it keeps today's behaviour: no moolatrikona, sign falls through to
Exalted / Debilitated / Own Sign / Panchadhā Maitrī.

### 4.4 Multi-graha conjunction groups

- A **group** = the maximal set of ≥ 2 grahas sharing a Rāśi in a chart. Classical conjunction is
  co-Rāśi (equivalently co-bhāva under whole-sign), so the query is
  `GROUP BY SignId HAVING COUNT(*) ≥ 2` over the chart's grahas (Ascendant excluded, `PointKind='Graha'`).
  **Group membership is Rāśi-based, deliberately** — it is not redefined by degree proximity. Instead
  the tightness data below lets a rule decide the *effective* conjunction.
- Every group therefore has `SameRasi = TRUE` implicitly (it is the defining condition, so it is not
  stored). What *is* stored is the spread:
  - group `LongitudeSpanDegrees` = `max(memberLon) − min(memberLon)` across members — **D1 only**
    (a varga sign is a discrete bucket; real-longitude spread is meaningless there, so null — same
    reasoning as `ChartConjunction.DegreeSeparation`). Range `[0, 30)`.
  - member `OrbFromGroupCenterDegrees` = `|memberLon − mean(memberLon)|` — **D1 only**, `≤ LongitudeSpanDegrees`.
  - This distinguishes a tight degree-based conjunction (Sun 5° Ar, Mars 7° Ar → span 2°) from a
    wide same-Rāśi one (Sun 5° Ar, Venus 27° Ar → span 22°).
- **`MemberKey`** = ascending `PlanetId` CSV (e.g. `"1,3,4,6"`) — canonical; enables idempotent
  upsert and direct yoga-subset matching (`PlanetCount >= N AND <grahas> ⊆ MemberKey`).
- A 2-planet conjunction is a group with `PlanetCount = 2`; no special case. `PlanetCount`
  discriminates the pair / triple / quadruple / larger-cluster hierarchy — no separate tables.
- Each existing pair row gets `ConjunctionGroupId`; the pair's two planets are always members of
  that group (a `verify-schema` invariant).
- `DegreeSeparation` stays only on the pair (a group has no single separation).

### 4.5 Subsets and the Yoga engine

A group of `n` members has `2ⁿ − n − 1` sub-combinations of size ≥ 2 (n=4 → 6 pairs + 4 triples +
1 quad = 11). These are **candidate combinations**, not yogas, and are **not persisted**:

- **Pairs** are already materialised in `tbl_Chart_Conjunctions` (kept for tightness, Graha Yuddha,
  existing Web/engine consumers) and are now children of their group via `ConjunctionGroupId`.
- **Triples and larger** are enumerated on demand by the future Yoga engine from `MemberKey`, then
  tested against `tbl_Rule_Yoga` conditions (`Planet`, `PlanetCount`, `Conjunction`, `House`,
  `Lordship`, `Aspect`, `Sign`, `Dignity`, `Exclusion`). A 3-planet conjunction is not automatically
  a "3-planet yoga".
- Whole-cluster interpretation (all `n` grahas as one environment) is a separate downstream output;
  the group row + members are its structural input.

This spec stops at providing that structural input.

### 4.6 Engine changes

`RelationshipEngine`:
- `FindConjunctionGroups(ChartAnalysisInput) : List<ConjunctionGroupResult>` — grahas grouped by sign,
  ≥ 2, each with its members' `PlanetPosition` data.
- `BuildConjunctionGroupRows` / `BuildConjunctionGroupMemberRows` — row builders, mirroring
  `BuildConjunctionRows` (canonical member ordering by `PlanetId`). For D1 they compute
  `LongitudeSpanDegrees` (group) and `OrbFromGroupCenterDegrees` (member) from member
  `NirayanaLongitudeDegrees`; for a varga chart both are null.
- `BuildConjunctionRows` unchanged except it now also stamps the `MemberKey` of the group each pair
  belongs to, so the repo can resolve `ConjunctionGroupId` after the groups are inserted.

`ChartAnalyzer` (composer) — already computes a `DignityResult` per planet in the same pass; it
stitches each group member's `DignityStatus` from that result. No new dignity computation.

`DignityEngine`:
- New `IDignityRuleProvider` (Data layer) supplies the **active axis-A segment set** (replaces the
  `ExaltationSign` / `DebilitationSign` / `Moolatrikona` dicts).
- **Unchanged:** `NaturalRelationship`, `SignDistance`, `IsTemporaryFriend`, and the priority merge
  in `Evaluate`. `CombineToPanchadha` keeps its exact truth table but now returns
  `(string Status, int Score)` instead of just the string.
- `DignityResult` gains `DignityTypeCode` (string?, the 4-value axis-A code — null when the status
  is an axis-B Maitrī tier), `DignityScore` (int, from the 9-row canonical map), `DeepDegree`
  (decimal?, exalt/debil only). Signature of `Evaluate` unchanged.
- The 9 `DignityStatus` strings it can emit are **exactly** today's set, so the
  `tbl_Rule_WakefulnessState` join and `verify-avastha` keep working with no migration.

### 4.7 Persistence

- New `ChartConjunctionGroupsRepository` (groups + members, Dapper, same shape as
  `ChartConjunctionsRepository`).
- `ChartGenerationService.RecomputeAnalytics` re-derives groups + members alongside the existing
  four analytics tables; order: KeyDetails → HouseLords → Conjunctions → **Groups → GroupMembers** →
  Aspects, then a pass to set `tbl_Chart_Conjunctions.ConjunctionGroupId`.
- `BirthDetailDeletionService` — cascade delete groups (members via FK `ON DELETE CASCADE`).
- `IDignityRuleProvider` implementation loads once per process (rule data is tiny, static per run).

## 5. Data / schema

Migration `22` (conjunction groups) is **delivered**. Phase 1 adds **migration `23`** — one
idempotent, self-recording file, folded forward into `db/ikiastrro.sql` after proving on the dev DB.
`vw_Chart_Consolidated` stays the last object in the baseline; `vw_Dignity_Legend` is added before it.

### 22 — multi-graha conjunction layer  · **DELIVERED** (`db/22_add_multigraha_conjunction.sql`, branch `feat/conjunction-groups`)

Final names: `tbl_Chart_MultiGrahaConjunction` (`Id`, `ChartResultId`→FK, `SignId`→FK,
`HouseNumberFromLagna` `CHECK 1..12`, `PlanetCount` `CHECK >= 2`, `MemberKey VARCHAR(40)` ascending
PlanetId CSV, `LongitudeSpanDegrees DECIMAL(7,4) NULL` `CHECK [0,30)` — D1 only,
`UNIQUE (ChartResultId, SignId)`, index on `ChartResultId`) · `tbl_Chart_MultiGrahaConjunctionMember`
(`MultiGrahaConjunctionId`→FK `ON DELETE CASCADE`, `PlanetId`→FK, `DegreesInSign`, `NirayanaLongitude`,
`VargaLongitude`, `OrbFromGroupCenterDegrees` — D1 only, `DignityStatus VARCHAR(20)`, `IsRetrograde`,
`IsCombust`, `UNIQUE (MultiGrahaConjunctionId, PlanetId)` + 4 range CHECKs) ·
`tbl_Chart_Conjunctions.MultiGrahaConjunctionId INT NULL` → FK. Backfilled from `tbl_Chart_KeyDetails`
(`PointKind='Graha'`, `HAVING COUNT(*) >= 2`): 268 groups / 675 members / all pair rows linked.
Pending: engine/repo/`verify-schema` wiring (Phase 3 Tasks 8–11).

### 23 — `tbl_Rule_GrahaDignity` (table + both rule-sets + `SRC_PVR_INTEGRATED` + `tbl_Rule_Catalog` + `vw_Dignity_Legend`)

```
Id                     INT IDENTITY PK
RuleSetId              INT NOT NULL   FK tbl_Rule_Sets
PlanetId              TINYINT NOT NULL FK tbl_Planets
SignId               TINYINT NOT NULL FK tbl_SignAttributes
DignityTypeCode      VARCHAR(20) NOT NULL  CHECK (DignityTypeCode IN ('EXALTED','MOOLATRIKONA','OWN','DEBILITATED'))
StartDegree          DECIMAL(5,2) NOT NULL   -- 0..30
EndDegree            DECIMAL(5,2) NOT NULL   -- >Start, <=30
DeepDegree           DECIMAL(5,2) NULL       -- EXALTED / DEBILITATED rows only (NULL for nodes)
DignityScore         SMALLINT NOT NULL       -- EXALTED +4 · MOOLATRIKONA +3 · OWN +2 (every own row) · DEBILITATED -2
IsPrimary            BIT NOT NULL DEFAULT 1  -- 1 = the planet's "home" own sign per PVR prose; 0 = the own segment of its moolatrikona sign when a separate home sign exists. Does NOT affect DignityScore.
Mood                   VARCHAR(20)   NULL    -- constant per DignityTypeCode (§4.2b)
InterpretationTendency NVARCHAR(200) NULL    -- constant per DignityTypeCode
Analogy                NVARCHAR(400) NULL    -- constant per DignityTypeCode
DignityRationale     NVARCHAR(MAX) NULL      -- PVR per-planet "why"; NULL where the source is silent
MethodCode           VARCHAR(30) NULL        -- 'SEGMENT_LOOKUP'
RuleParametersJson   NVARCHAR(MAX) NULL      CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1)
CalculationNarrative NVARCHAR(MAX) NULL      -- e.g. the note-4 "Leo" typo on Mars/Aries
SourceRefCode        VARCHAR(40) NULL        CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
IsActive             BIT NOT NULL DEFAULT 1
CONSTRAINT CK_RuleGrahaDignity_Degrees CHECK (StartDegree >= 0 AND EndDegree > StartDegree AND EndDegree <= 30)
CONSTRAINT CK_RuleGrahaDignity_Deep    CHECK (DeepDegree IS NULL OR (DeepDegree >= 0 AND DeepDegree <= 30))
CONSTRAINT CK_RuleGrahaDignity_Score   CHECK (DignityScore BETWEEN -2 AND 4)
-- 0–30 tiling per (RuleSetId,PlanetId,SignId) + score↔type + metadata-constancy enforced in verify-dignity
UNIQUE (RuleSetId, PlanetId, SignId, DignityTypeCode, StartDegree)
INDEX (RuleSetId, PlanetId)
```

- **Seed `PVR_INTEGRATED`** (`IsActive = 1`, `SourceRefCode = 'SRC_PVR_INTEGRATED'`) — the ~40
  axis-A segment rows from `docs/research/dignity-pvr-integrated.md` "Derived segment model":
  per planet, exalt (1) + debil (1) + the moolatrikona-sign split (MT segment + own segment) +
  **the "other own sign"** as a whole-sign `OWN 0–30` row. `DignityScore`: `EXALTED +4`,
  `MOOLATRIKONA +3`, `OWN +2` (on **both** the MT-sign own segment and the other-own-sign row),
  `DEBILITATED −2`. The 7 other-own-sign rows: Mercury/Gemini, Mars/Scorpio, Jupiter/Pisces,
  Venus/Taurus, Saturn/Capricorn, Rahu/Aquarius, Ketu/Scorpio — `IsPrimary = 1` on the PVR "home"
  sign (Gemini, Pisces, Taurus, Capricorn for Me/Ju/Ve/Sa; the sole own sign for Sun/Moon/nodes),
  `IsPrimary = 0` on the MT-sign own segment where a separate home exists (Mars/Aries flagged —
  §8.11). `DeepDegree` on EXALTED/DEBILITATED for the classical seven; NULL for nodes. Mars/Aries
  MT row: `CalculationNarrative` records the book's note-4 "Leo" typo. `DignityRationale` on the 8
  Jupiter/Mercury/Ketu rows. **No `FRIEND` / `NEUTRAL` / `ENEMY` rows** — those signs are axis B.
- **Seed `BPHS_PARASHARI`** (`IsActive = 0`, `SourceRefCode = 'SRC_BPHS'`) — today's
  `DignityEngine.cs` dict values: Rāhu exalt Taurus / debil Scorpio, Ketu exalt Scorpio / debil
  Taurus (no node own/MT rows), Moon MT Taurus 4–30, Mercury MT Virgo 16–20, classical own/MT
  splits per BPHS. Provenance + one-`UPDATE` rollback.
- Seed rows as `VALUES` CTEs joined to `tbl_Planets` / `tbl_SignAttributes` by natural key;
  re-run is idempotent (`DELETE` the two rule-sets' rows then re-`INSERT`, or `MERGE`).
- Insert `SRC_PVR_INTEGRATED` into `tbl_Dim_Source` (title, author "P. V. R. Narasimha Rao").
- Register `tbl_Rule_GrahaDignity` in `tbl_Rule_Catalog` (EngineCode `DIGNITY`, MethodCode
  `SEGMENT_LOOKUP`, IntroducedIn `23_add_rule_graha_dignity.sql`).
- Add `vw_Dignity_Legend` = `SELECT DISTINCT DignityTypeCode, DignityScore, Mood,
  InterpretationTendency, Analogy FROM dbo.tbl_Rule_GrahaDignity WHERE IsActive = 1`.
- **Not touched:** `tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance`,
  `tbl_Rule_WakefulnessState`, `tbl_Astro_Terminology` `DignityState` rows.

## 6. Verification

- **`verify-dignity`** (new CLI mode):
  - **Tiling** — for each rule-set and each (planet, sign) that *has* rows, the segments tile
    `[StartDegree, EndDegree)` with no gap / no overlap (a planet's other 8 signs have no axis-A row
    — that is expected, they are axis B).
  - **Coverage** — every classical planet (Id 1–7) has exactly one whole-sign `EXALTED` and one
    `DEBILITATED` row in both rule-sets; each has 1–2 `OWN` signs; MT appears on exactly one sign.
  - **Score map** — every row's `DignityScore` equals the canonical value for its `DignityTypeCode`
    (`EXALTED +4`, `MOOLATRIKONA +3`, `OWN +2` on every own row, `DEBILITATED −2`); and the axis-B
    scores from `CombineToPanchadha` match the canonical map (`Great Friend`/`Friend` → `+1`,
    `Neutral` → `0`, `Enemy`/`Great Enemy` → `−1`).
  - **Metadata constancy** — `Mood` / `InterpretationTendency` / `Analogy` are single-valued per
    `DignityTypeCode`; `DignityRationale` is non-NULL only on `PVR_INTEGRATED` rows.
  - **Vocabulary unchanged** — the set of distinct `DignityStatus` strings `DignityEngine` can emit
    is exactly the 9 that `tbl_Rule_WakefulnessState` is keyed on (assert every one has a
    wakefulness row); confirms axis B and `verify-avastha` are not disturbed.
  - **Fixture** — active-set (`PVR_INTEGRATED`) rows reproduce a committed fixture chart's expected
    `DignityStatus` + `DignityScore` for all 9 grahas.
  - **Seed cross-check** — `tbl_SignAttributes.ExaltedDegree` / `DebilitatedDegree` /
    `MooltrikonaRange*` agree with the active-set classical-seven rows (nodes exempt).
- **`verify-schema`** additions (still pending — Phase 3 Tasks 8–11):
  - Every `tbl_Chart_Conjunctions` row has a non-null `MultiGrahaConjunctionId`; its `Planet1Id` and
    `Planet2Id` are both members of that group.
  - `tbl_Chart_MultiGrahaConjunction.PlanetCount` = member count = distinct grahas in that sign per
    `tbl_Chart_KeyDetails`.
  - `MemberKey` is ascending-sorted, `PlanetCount` commas + 1 entries.
  - D1 group members have non-null in-range `DegreesInSign` + `NirayanaLongitude`; each member's
    `DignityStatus` equals that planet's `tbl_Chart_KeyDetails.DignityStatus` for the same `ChartResultId`.
  - D1 groups: `LongitudeSpanDegrees` non-null and `= max−min` of members' `NirayanaLongitude`;
    every member's `OrbFromGroupCenterDegrees` non-null and `≤ LongitudeSpanDegrees`. Varga groups:
    both null.
- **Re-baseline (deliberate):** `verify-avastha` and any golden record reading `DignityStatus`
  change only for charts with Rāhu/Ketu in the swapped signs, Moon 3–4° Taurus, or Mercury 15–16°
  Virgo. The 9-value `DignityStatus` vocabulary itself does **not** change, so
  `tbl_Rule_WakefulnessState` needs no migration. Record the new expected values in the same commit;
  note in the SDD ledger.
- `dotnet build -warnaserror` Debug + Release · every `verify-*` `ALL PASS` · Web smoke `/charts/1` → 200.

## 7. Risks & mitigations

| Risk | Mitigation |
|---|---|
| PVR switch silently changes existing users' charts | Explicit re-baseline task; `BPHS_PARASHARI` kept inactive for one-`UPDATE` rollback; `ikiastrro.md` + memory + `DignityEngine.cs` doc-comment updated in the same commit; supersede note in this spec header. |
| Node Panchadhā Maitrī undefined under PVR | Non-goal — node not in dignity → `Neutral`, same as today. Documented in research note §"Divergence" + spec §3. |
| Range-overlap can't be a per-row CHECK | `verify-dignity` gap/overlap assertion is the gate; `UNIQUE (RuleSetId,PlanetId,SignId,DignityTypeCode,StartDegree)` blocks exact dupes. |
| Re-deriving axis B (Friend/Neutral/Enemy) into `tbl_Rule_GrahaDignity` | Rejected — would drop `Great Friend`/`Great Enemy`, duplicate `tbl_Rule_NaturalRelationship`, and collide on the words. Axis A holds only `EXALTED/MOOLATRIKONA/OWN/DEBILITATED`; §4.2c. |
| More `DignityStatus` tiers than score rungs | The two "great" Maitrī tiers collapse to their base number for scoring (`Great Friend`→`+1`, `Great Enemy`→`−1`); the 9-value string keeps the distinction. Ladder is rammyps's `−2…+4`. §4.2d; `verify-dignity` pins each value. |
| Book typo (note 4 "Leo") encoded literally | Encoded as Aries per Table 6; `CalculationNarrative` on that row records the discrepancy. |
| `Mood`/`Analogy` repeated across ~40 rows | Accepted (small reference table); `verify-dignity` asserts constancy per `DignityTypeCode`; `vw_Dignity_Legend` is the de-duplicated read. |

## 8. Open decisions

### Resolved

1. **Group layer shape** — two tables, pairs kept as group children. Accepted 2026-09-04.
   **Delivered** as migration 22 with names `tbl_Chart_MultiGrahaConjunction` /
   `tbl_Chart_MultiGrahaConjunctionMember` / `…Conjunctions.MultiGrahaConjunctionId` (the rename
   from `ConjunctionGroups*` was taken).
2. **Phase depth** — resolved: **Phase 3 landed first** (branch `feat/conjunction-groups`, migration
   22) against the current hard-coded engine; Phases 1–2 follow on `feat/graha-dignity-rule-layer`.
3. **Grouping is Rāśi-based, not degree-based** — resolved; tightness carried by
   `LongitudeSpanDegrees` + `OrbFromGroupCenterDegrees`. No `SameRasi` flag.
4. **`tbl_Dim_DignityType`** — dropped. Metadata + score are columns on `tbl_Rule_GrahaDignity`;
   `vw_Dignity_Legend` is the de-duplicated read (rammyps, 2026-09-04).
5. **Two axes** — `tbl_Rule_GrahaDignity` = axis A only (`EXALTED/MOOLATRIKONA/OWN/DEBILITATED`);
   axis B (Panchadhā Maitrī) untouched. §4.2c.
6. **`DignityScore` ladder** — resolved (rammyps, 2026-09-04): `Exalted +4 · Moolatrikona +3 ·
   Own +2 · Friend +1 · Neutral 0 · Enemy −1 · Debilitated −2`, with `Great Friend`→`+1` and
   `Great Enemy`→`−1` (the string keeps the distinction, the number does not). Every `OWN` row is
   `+2` — the MT-sign own segment and the "other own sign" alike. §4.2d.

### Open — not blocking migration 23

7. **Axis-B tier mood/analogy** — take from the existing `DignityState` terminology `Description` +
   a static presentation map (proposed), or add `Mood`/`Analogy` to `tbl_Rule_NaturalRelationship`
   later.
8. **Retire `tbl_SignAttributes` dignity columns?** — out of scope; `verify-dignity` cross-checks
   them. Flag a later cleanup migration.
9. **`DignityRationale` inline vs. side table** — inline `NVARCHAR(MAX)` column (proposed, text is
   short and single-sourced). A `tbl_Rule_GrahaDignity_Note` side table only earns its keep if
   multiple sourced notes per row are later wanted.

### Open — pick during seed authoring (cosmetic, no score impact)

10. **`IsPrimary` for dual-own planets** — proposed: `1` on the PVR "home" sign (Gemini, Pisces,
    Taurus, Capricorn for Me/Ju/Ve/Sa; sole own sign for Sun/Moon/nodes), `0` on the MT-sign own
    segment where a home exists.
11. **Mars `IsPrimary`** — PVR gives Mars no "home" prose. Proposed: Aries (MT sign) own segment
    `IsPrimary = 1`, Scorpio `IsPrimary = 0` — inverse of the Me/Ju/Ve/Sa pattern. Flag if you'd
    rather Scorpio be primary.

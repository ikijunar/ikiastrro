# Graha dignity rule layer + conjunction groups — design

**Status:** draft (for plan) · **Created:** 2026-09-04 · **Branch:** `feat/graha-dignity-rule-layer`
**Features:** FEAT-DIGNITY-01 (advances — data-driven + PVR + degree segments), FEAT-DIGNITY-02 (new — `tbl_Rule_GrahaDignity`), FEAT-RELATIONSHIP-01 (advances — conjunction group/member layer + per-planet degrees)
**Supersedes:** the 2026-08-24 decision (in `DignityEngine.cs` doc-comment + `ikiastrro.md`) to use the Parāśari/BPHS convention for Rāhu/Ketu exaltation. Active dignity rule-set becomes PVR *Integrated Approach* Table 6.

## Research
Status: [x] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| `docs/research/dignity-pvr-integrated.md` | Phase 1–2 — Table 6, the 7 special-degree rules, the derived segment model, and every divergence from today's engine and the `tbl_SignAttributes` seed | complete |
| `docs/research/reference-sources.md` | needs a `SRC_PVR_INTEGRATED` row (Phase 1, task 2) | pending — trivial add |

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
   exaltation / debilitation / moolatrikona / own-sign segments + deep-degree points, seeded with
   **two** rule-sets: `PVR_INTEGRATED` (active) and `BPHS_PARASHARI` (inactive, kept for provenance
   and future toggle). Follows the established `tbl_Rule_*` template.
2. `tbl_Dim_DignityType` — closed dimension for the segment types, carrying **Mood** +
   **InterpretationTendency** metadata for the interpretation engine, with terminology rows.
3. `DignityEngine` reads the active rule-set instead of its dicts; gains exact own-sign /
   moolatrikona degree-segment resolution; **active behaviour switches to PVR**.
4. `tbl_Chart_ConjunctionGroups` + `tbl_Chart_ConjunctionGroupMembers` — an explicit **multi-graha
   conjunction** (Graha Saṃyoga) layer. One group per Rāśi that holds ≥ 2 grahas, covering the
   2-planet case with no special-casing. Members carry per-planet `DegreesInSign`,
   `NirayanaLongitude`, `VargaLongitude`, `OrbFromGroupCenterDegrees`, `DignityStatus` (the fuller
   value — Exalted / Moolatrikona / Own Sign / Debilitated / Great Friend … Great Enemy, matching
   `tbl_Chart_KeyDetails`), retrograde, combust — **once per planet**, not per pair. The group
   carries `PlanetCount`, `MemberKey`, and a D1 `LongitudeSpanDegrees` so the rule engine can tell a
   tight degree-based conjunction from a wide same-Rāśi one.
5. `tbl_Chart_Conjunctions` keeps its pair rows (tightness / Graha Yuddha / existing consumers) and
   gains `ConjunctionGroupId` (FK) linking each pair to its group. Triple / quadruple / larger
   subsets are **not** persisted — the Yoga engine enumerates them from `MemberKey` on demand (§4.5).
   "Stellium" is a Western term; it may appear as UI prose but the internal name is
   *multi-graha conjunction*.
6. `verify-dignity` (new) + `verify-schema` additions; all existing `verify-*` stay `ALL PASS`
   after a deliberate re-baseline of dignity-dependent golden records.

## 3. Non-goals

- No strength / shadbala scoring, no deep-exaltation falloff curve — `DeepDegree` is stored, not consumed.
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
tbl_Dim_DignityType         (closed: EXALTED / MOOLATRIKONA / OWN / NEUTRAL / DEBILITATED + Mood + Tendency)
        ▲
tbl_Rule_GrahaDignity       (segments per rule-set; PVR active, BPHS inactive; SourceRefCode; DeepDegree)
        ▲
DignityEngine.Evaluate      (reads active rule-set via IDignityRuleProvider; unchanged signature + output shape)
        ▲
ChartAnalyzer               (already calls DignityEngine per planet — now also feeds conjunction members)
        ▼
tbl_Chart_ConjunctionGroups ──< tbl_Chart_ConjunctionGroupMembers   (per-planet degree + DignityStatus, once)
        ▲
tbl_Chart_Conjunctions.ConjunctionGroupId   (pair → group link; pairs unchanged otherwise)
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
sets later is an `UPDATE tbl_Rule_Sets`.

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

`DignityEngine` — new `IDignityRuleProvider` (Data layer) supplies the active segment set;
`DignityResult` gains `DeepDegree` (nullable) and `DignityTypeCode` (the closed-set code, null when
the status is a Maitrī tier). Signature of `Evaluate` unchanged.

### 4.7 Persistence

- New `ChartConjunctionGroupsRepository` (groups + members, Dapper, same shape as
  `ChartConjunctionsRepository`).
- `ChartGenerationService.RecomputeAnalytics` re-derives groups + members alongside the existing
  four analytics tables; order: KeyDetails → HouseLords → Conjunctions → **Groups → GroupMembers** →
  Aspects, then a pass to set `tbl_Chart_Conjunctions.ConjunctionGroupId`.
- `BirthDetailDeletionService` — cascade delete groups (members via FK `ON DELETE CASCADE`).
- `IDignityRuleProvider` implementation loads once per process (rule data is tiny, static per run).

## 5. Data / schema

Migrations `22`–`24` (next free number is `22`). Each idempotent, self-recording into
`dbo.SchemaMigrations`, folded forward into `db/ikiastrro.sql` after proving on the dev DB.
`vw_Chart_Consolidated` stays the last object in the baseline.

### 22 — `tbl_Dim_DignityType` + terminology
```
Id            TINYINT       PK
Code          VARCHAR(20)   UNIQUE   -- EXALTED / MOOLATRIKONA / OWN / NEUTRAL / DEBILITATED
NameSanskrit  NVARCHAR(40)
NameEnglish   NVARCHAR(40)
Mood          NVARCHAR(30)           -- Elevated / Dutiful / Comfortable / Functional / Uncomfortable
InterpretationTendency NVARCHAR(200)
```
Seed 5 rows. Add matching `tbl_Astro_Terminology` + `_Text` (`sa` + `en`) rows via the
migration-17 pattern (`DIGNITYTYPE_*` concept codes).

### 23 — `tbl_Rule_GrahaDignity` (+ `SRC_PVR_INTEGRATED`, `tbl_Rule_Catalog`)
```
Id                   INT IDENTITY PK
RuleSetId            INT NOT NULL   FK tbl_Rule_Sets
PlanetId             TINYINT NOT NULL FK tbl_Planets
DignityTypeId        TINYINT NOT NULL FK tbl_Dim_DignityType
SignId               TINYINT NOT NULL FK tbl_SignAttributes
StartDegree          DECIMAL(5,2) NOT NULL   -- 0..30
EndDegree            DECIMAL(5,2) NOT NULL   -- >Start, <=30
DeepDegree           DECIMAL(5,2) NULL       -- exalt/debil rows only
IsPrimary            BIT NOT NULL DEFAULT 1  -- the planet's headline sign for that dignity type
MethodCode           VARCHAR(30) NULL
RuleParametersJson   NVARCHAR(MAX) NULL   CHECK (… ISJSON = 1)
CalculationNarrative NVARCHAR(MAX) NULL
SourceRefCode        VARCHAR(40) NULL     CHECK (… LIKE 'SRC[_]%')
IsActive             BIT NOT NULL DEFAULT 1
CONSTRAINT CK_RuleGrahaDignity_Degrees CHECK (StartDegree >= 0 AND EndDegree > StartDegree AND EndDegree <= 30)
CONSTRAINT CK_RuleGrahaDignity_Deep    CHECK (DeepDegree IS NULL OR (DeepDegree >= 0 AND DeepDegree <= 30))
-- no-overlap enforced in verify-dignity (a range-overlap CHECK is not expressible per-row)
UNIQUE (RuleSetId, PlanetId, SignId, DignityTypeId, StartDegree)
```
Seed both rule-sets (§4.2). Insert `SRC_PVR_INTEGRATED` into `tbl_Dim_Source`; register
`tbl_Rule_GrahaDignity` in `tbl_Rule_Catalog` (EngineCode `DIGNITY`, MethodCodes `SEGMENT_LOOKUP`).

### 24 — conjunction group layer
```
tbl_Chart_ConjunctionGroups
  Id                    INT IDENTITY PK
  ChartResultId         INT NOT NULL   FK tbl_ChartResults
  SignId                TINYINT NOT NULL FK tbl_SignAttributes
  HouseNumberFromLagna  TINYINT NOT NULL   CHECK 1..12
  PlanetCount           TINYINT NOT NULL   CHECK >= 2
  MemberKey             VARCHAR(40) NOT NULL   -- ascending PlanetId CSV, e.g. "1,3,4,6"
  LongitudeSpanDegrees  DECIMAL(7,4) NULL   CHECK (>= 0 AND < 30)   -- D1 only; max-min member NirayanaLongitude; null for varga
  UNIQUE (ChartResultId, SignId)
  INDEX (ChartResultId)

tbl_Chart_ConjunctionGroupMembers
  Id                    INT IDENTITY PK
  ConjunctionGroupId    INT NOT NULL   FK tbl_Chart_ConjunctionGroups ON DELETE CASCADE
  PlanetId              TINYINT NOT NULL FK tbl_Planets
  DegreesInSign             DECIMAL(7,4) NULL   CHECK (>= 0 AND < 30)
  NirayanaLongitude         FLOAT NULL         CHECK (>= 0 AND < 360)
  VargaLongitude            DECIMAL(9,6) NULL  CHECK (>= 0 AND < 360)
  OrbFromGroupCenterDegrees DECIMAL(7,4) NULL  CHECK (>= 0 AND < 30)   -- D1 only; |memberLon - mean(memberLon)|; null for varga
  DignityStatus             VARCHAR(20) NULL   -- matches tbl_Chart_KeyDetails.DignityStatus values
  IsRetrograde          BIT NULL
  IsCombust             BIT NULL
  UNIQUE (ConjunctionGroupId, PlanetId)

ALTER TABLE tbl_Chart_Conjunctions ADD ConjunctionGroupId INT NULL FK tbl_Chart_ConjunctionGroups
```
**Backfill:** derive groups + members from `tbl_Chart_KeyDetails` (grahas per `ChartResultId` + `SignId`,
`HAVING COUNT(*) >= 2`); set `ConjunctionGroupId` on existing pair rows by `(ChartResultId, SignId)`.

## 6. Verification

- **`verify-dignity`** (new CLI mode):
  - For each rule-set: segments for every (planet, sign) tile `[0,30)` with no gap / no overlap.
  - Every classical planet has exactly one whole-sign `EXALTED` and one `DEBILITATED` row.
  - Active-set (`PVR_INTEGRATED`) rows reproduce a fixed fixture chart's expected `DignityStatus`
    for all 9 grahas (fixture committed alongside).
  - Cross-check `tbl_SignAttributes.ExaltedDegree` / `MooltrikonaRange*` against the classical-seven
    active rows (nodes exempt — new).
- **`verify-schema`** additions:
  - Every `tbl_Chart_Conjunctions` row has a non-null `ConjunctionGroupId`; its `Planet1Id` and
    `Planet2Id` are both members of that group.
  - `tbl_Chart_ConjunctionGroups.PlanetCount` = member count = distinct grahas in that sign per
    `tbl_Chart_KeyDetails`.
  - `MemberKey` is ascending-sorted, `PlanetCount` commas + 1 entries.
  - D1 group members have non-null in-range `DegreesInSign` + `NirayanaLongitude`; each member's
    `DignityStatus` equals that planet's `tbl_Chart_KeyDetails.DignityStatus` for the same `ChartResultId`.
  - D1 groups: `LongitudeSpanDegrees` non-null and `= max−min` of members' `NirayanaLongitude`;
    every member's `OrbFromGroupCenterDegrees` non-null and `≤ LongitudeSpanDegrees`. Varga groups:
    both null.
- **Re-baseline (deliberate):** `verify-avastha` and any golden record reading `DignityStatus`
  change only for charts with Rāhu/Ketu in the swapped signs, Moon 3–4° Taurus, or Mercury 15–16°
  Virgo. Record the new expected values in the same commit; note in the SDD ledger.
- `dotnet build -warnaserror` Debug + Release · every `verify-*` `ALL PASS` · Web smoke `/charts/1` → 200.

## 7. Risks & mitigations

| Risk | Mitigation |
|---|---|
| PVR switch silently changes existing users' charts | Explicit re-baseline task; `BPHS_PARASHARI` kept inactive for one-`UPDATE` rollback; `ikiastrro.md` + memory + `DignityEngine.cs` doc-comment updated in the same commit; supersede note in this spec header. |
| Node Panchadhā Maitrī undefined under PVR | Non-goal — node not in dignity → `Neutral`, same as today. Documented in research note §"Divergence" + spec §3. |
| Range-overlap can't be a per-row CHECK | `verify-dignity` gap/overlap assertion is the gate; `UNIQUE (RuleSetId,PlanetId,SignId,DignityTypeId,StartDegree)` blocks exact dupes. |
| Backfill mis-groups a chart with an odd KeyDetails row (special points) | Group derivation filters `PointKind = 'Graha'` only; `verify-schema` count cross-check catches drift. |
| `recompute-analytics` ordering / partial failure | Groups + members + pair-link derived in one transaction per `ChartResultId`, same as the existing four tables. |
| Book typo (note 4 "Leo") encoded literally | Encoded as Aries per Table 6; `CalculationNarrative` on that row records the discrepancy. |

## 8. Open decisions

1. **Group layer shape** — two tables (`tbl_Chart_ConjunctionGroups` +
   `tbl_Chart_ConjunctionGroupMembers`), pairs kept as group children. Elaborated and accepted over
   discussion (2026-09-04). Alternative rejected: `GroupSize`/`GroupKey` columns on pair rows (keeps
   per-planet dignity duplicated, no first-class count).
2. **Phase depth** — assumed: all three phases, one branch, in sequence (Phase 1 → 2 → 3).
   Alternative: land Phase 3 first against the current hard-coded engine. *Confirm.*
5. **Grouping is Rāśi-based, not degree-based** — resolved. A group = all grahas in one Rāśi;
   `LongitudeSpanDegrees` (group) + `OrbFromGroupCenterDegrees` (member) quantify tightness so a rule
   can derive an "effective" tight conjunction. Group table stores no `SameRasi` flag (always true).
6. **Table / model naming** — tables stay `tbl_Chart_ConjunctionGroups*` (FK-friendly, unambiguous);
   the concept is *multi-graha conjunction* / Graha Saṃyoga in docs + a terminology row; "stellium"
   only as optional UI prose. Flag if a rename to `tbl_Chart_MultiGrahaConjunction*` is preferred.
3. **`IsPrimary` semantics for nodes** — PVR gives each node exactly one sign per dignity type, so
   every node row is `IsPrimary = 1`. Fine as-is; noting it's not a meaningful discriminator for nodes.
4. **Retire `tbl_SignAttributes` dignity columns?** — out of scope here; `verify-dignity` cross-checks
   them instead. Flag a later cleanup migration.

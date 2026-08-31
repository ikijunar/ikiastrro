# Divisional-Chart Completion — Design

**Status:** draft for review
**Date:** 2026-08-31
**Author:** Claude (Sonnet 5) with rammyps
**Predecessors:** `2026-08-30-web-ui-life-area-recreate-design.md`, `2026-08-31-chart-schema-normalization-design.md`
**Source gap list:** `requirements_gap_ikiastrro.md` §4 (Shodashavarga set) + §1 (varga within-sign degree, provenance)

---

## 1. Goal

Add every remaining classical divisional chart so the engine covers the full
Shodashavarga set plus the Jagannatha Hora export extras, computed
**database-driven** (varga rules are seeded data, not hard-coded), with **all
computed information available at the DB level** — no value that lives only in
`ChartResult.ResultJson` or in C# memory.

New chart types this batch (17 + 1):

| | Charts |
|---|---|
| **Shodashavarga completion** | D3, D4, D7, D12, D16, D20, D24, D27, D30, D40, D45, D60 |
| **JHora-export extras** | D5, D8, D81, D108, D144 |
| **Hora variant** | `D2-US` — Uma Shambu Hora, alongside the existing classical two-sign `D2` |

Deferred: **D150 (Nadiamsa)** — 8 competing schemes, no JHora-export cross-check
(JHora's own export stops at D144). Revisit when a specific Nadiamsa method and a
citable source are chosen.

After this batch the engine computes **24 position chart types** per person
(D1, D2, D2-US, D6, D9, D10, D11 + the 17 above).

---

## 2. The DB-completeness invariant

> **Every computed quantity has a typed column in a relational table.**
> `ChartResult.ResultJson` is a frozen audit snapshot — it is never read back as
> the source of truth, and every field it contains is also a column.

Concretely, after this design a caller (C#, SQL, or the future Python comparison
layer) can answer all of the following with a query and **no JSON parsing**:

- Which sign / house / nakshatra is planet P in, in chart Dn, for person X?
- What is planet P's degree *within its Dn sign*? Its Dn-space longitude?
- Is planet P Vargottama (same sign in D1 and D9)? In D1 and D10?
- How tight (in Dn-space degrees) is a same-sign conjunction in Dn?
- Which method/rule-set produced chart Dn, and what is its cited source?
- What numeric ayanamsha and sidereal time underlie person X's charts?

---

## 3. Schema design

### 3.1 The picture

```
tbl_BirthDetails ──1:N── tbl_ChartResults ──1:N── tbl_Chart_KeyDetails      (planet × chart)
   (person)              (person × chart type)  ├─1:N── tbl_Chart_HouseLords
                                                ├─1:N── tbl_Chart_Conjunctions
                                                ├─1:N── tbl_Chart_Aspects
                                                └─1:N── tbl_Fact_PlanetAvastha

reference / rule layer
  tbl_Dim_ChartType     — controlled vocabulary of chart types (D1..D144, D2-US)
  tbl_Rule_Sets         — rule-set versions (existing)
  tbl_Rule_VargaScheme  — NEW: how each varga chart type is computed, per rule-set
```

No new fact tables. The 17 new charts are **additive rows** in the already
chart-type-generic `tbl_Chart_*` / `tbl_Fact_PlanetAvastha` tables (integer FKs
from the schema-normalization work: `PlanetId`, `SignId`, `ChartTypeId`,
`NakshatraId`, …). The shared `ChartAnalyzer` pipeline (dignity, house-lordship,
conjunctions, aspects) and `PlanetAvasthaComputer` run for every new type
unchanged.

### 3.2 `tbl_Rule_VargaScheme` (new)

One row per varga chart type per rule-set. Read by both C# and the future Python
layer, so "why is planet P in this varga sign" is inspectable data.

```sql
CREATE TABLE dbo.tbl_Rule_VargaScheme (
    Id             TINYINT      NOT NULL CONSTRAINT PK_Rule_VargaScheme PRIMARY KEY,
    RuleSetId      TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_RuleSet   FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    ChartTypeId    TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_ChartType FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
    DivisionFactor TINYINT      NOT NULL,                       -- N (1..144)
    MethodCode     VARCHAR(40)  NOT NULL,                       -- 'ParasaraTraditional' | 'UmaShambu' | 'LinearCyclic' | 'ClassicalTwoSign' | ...
    MethodSource   VARCHAR(200) NOT NULL,                       -- 'BPHS; PyJHora trimsamsa method 1'
    SignRuleKind   VARCHAR(10)  NOT NULL,                       -- 'Linear' | 'Special'
    SpecialRuleKey VARCHAR(40)  NULL,                           -- for Special: names the C# IVargaSignRule (e.g. 'TrimsamsaD30')
    CONSTRAINT UQ_Rule_VargaScheme UNIQUE (RuleSetId, ChartTypeId),
    CONSTRAINT CK_Rule_VargaScheme_Kind CHECK (
        (SignRuleKind = 'Linear'  AND SpecialRuleKey IS NULL) OR
        (SignRuleKind = 'Special' AND SpecialRuleKey IS NOT NULL))
);
```

Seed (RuleSetId 1, `Parashari-Classical`), one row per position chart type:

| ChartType | N | MethodCode | SignRuleKind | SpecialRuleKey | MethodSource |
|---|---|---|---|---|---|
| D1  | 1  | Identity            | Linear  | –             | Rasi (identity) |
| D2  | 2  | ClassicalTwoSign   | Special | `HoraD2Classic` | BPHS two-sign (Cn/Le) |
| D2-US | 2 | UmaShambu         | Special | `HoraD2UmaShambu` | Parasara Uma Shambu; PyJHora d2 default |
| D3  | 3  | ParasaraTraditional| Special | `DrekkanaD3`    | BPHS 1st/5th/9th; PyJHora d3 method 1 |
| D4  | 4  | LinearCyclic       | Linear  | –             | BPHS chaturthamsa; PyJHora d4 method 1 |
| D5  | 5  | LinearCyclic       | Linear  | –             | Parasara panchamsa; PyJHora d5 method 1 |
| D6  | 6  | ParasaraTraditional| Linear  | –             | BPHS shashtamsa; PyJHora d6 method 1 |
| D7  | 7  | ParasaraTraditional| Special | `SaptamsaD7`   | BPHS odd-from-self / even-from-7th; PyJHora d7 method 1 |
| D8  | 8  | LinearCyclic       | Linear  | –             | Parasara ashtamsa; PyJHora d8 method 1 |
| D9  | 9  | ParasaraTraditional| Special | `NavamsaD9`    | BPHS navamsa; existing `AstroMath.GetNavamsaSign` |
| D10 | 10 | ParasaraTraditional| Special | `DasamsaD10`   | BPHS odd-from-self / even-from-9th; existing `AstroMath.GetDasamsaSign` |
| D11 | 11 | SanjayRath         | Special | `RudramsaD11`  | Sanjay Rath; existing `AstroMath.GetRudramsaSign` |
| D12 | 12 | ParasaraTraditional| Special | `DwadasamsaD12`| BPHS 12-from-self; PyJHora d12 method 1 |
| D16 | 16 | LinearCyclic       | Linear  | –             | BPHS shodasamsa; PyJHora d16 method 1 |
| D20 | 20 | LinearCyclic       | Linear  | –             | BPHS vimsamsa; PyJHora d20 method 1 |
| D24 | 24 | ParasaraTraditional| Special | `SiddhamsaD24` | BPHS odd-from-Leo / even-from-Cancer; PyJHora d24 method 1 |
| D27 | 27 | ParasaraTraditional| Special | `NakshatramsaD27` | BPHS from-Aries by element; PyJHora d27 method 1 |
| D30 | 30 | ParasaraTraditional| Special | `TrimsamsaD30` | BPHS unequal 5-part; PyJHora d30 method 1 |
| D40 | 40 | LinearCyclic       | Linear  | –             | BPHS khavedamsa; PyJHora d40 method 1 |
| D45 | 45 | LinearCyclic       | Linear  | –             | BPHS akshavedamsa; PyJHora d45 method 1 |
| D60 | 60 | ParasaraTraditional| Special | `ShashtyamsaD60` | BPHS floor(2·deg) from sign; PyJHora d60 method 1 |
| D81 | 81 | LinearCyclic       | Linear  | –             | Nava-navamsa; PyJHora d81 method 1 |
| D108| 108| LinearCyclic       | Linear  | –             | Ashtottaramsa; PyJHora d108 method 1 |
| D144| 144| LinearCyclic       | Linear  | –             | Dwadas-dwadasamsa; PyJHora d144 method 1 |

The final `MethodCode` / `SpecialRuleKey` per varga is **locked during
implementation** by matching the Ramakrishnan JHora export grid (see §5); the
table above is the expected outcome, not a guess to ship blindly.

### 3.3 `tbl_Dim_ChartType` — 18 new seed rows

The six existing rows (Id 1..6) stay. Add Id 7..24 for D2-US and the 17 vargas,
each with `DivisionalFactor = N`, `Category = 'Varga'`, `DisplayOrder` in the
conventional Shodashavarga order (D1, D2, D3, D4, D7, D9, D10, D12, D16, D20,
D24, D27, D30, D40, D45, D60, then D5/D8/D81/D108/D144 as extras, then D2-US next
to D2 for display). `DisplayName` uses the Sanskrit varga name
(Drekkana, Chaturthamsa, Saptamsa, Dwadasamsa, Shodasamsa, Vimsamsa, Siddhamsa,
Nakshatramsa, Trimsamsa, Khavedamsa, Akshavedamsa, Shashtyamsa, …) cross-checked
against whatever the Web already shows.

### 3.4 `tbl_ChartResults` — provenance columns

Add:

| column | type | meaning |
|---|---|---|
| `VargaMethod`      | `VARCHAR(40) NULL` | denormalized copy of `tbl_Rule_VargaScheme.MethodCode` for the chart's type — convenience, same precedent as the denormalized `ChartType` string |
| `AyanamshaDegrees` | `DECIMAL(9,6) NULL` | numeric ayanamsha at birth moment (JHora export: `23-34-49.57`). One value per person; denormalized across the person's chart rows |
| `SiderealTimeHours`| `DECIMAL(9,6) NULL` | local sidereal time at birth (JHora export: `19:20:55`) |

`Ayanamsha` (the name string) stays. Both new numeric fields come straight from
the `SwissEphemerisProvider` call already made — no extra ephemeris work.

Full panchanga (Tithi / Karana / Yoga / Nakshatra-% / Vedic weekday / sunrise /
sunset / Janma Ghatis / lunar month — `requirements_gap_ikiastrro.md` §2) is
**out of scope here**, but these three columns establish the "computation
context" shape it will extend (a future `tbl_Fact_BirthPanchanga` keyed by
`BirthDetailId`, or additional `tbl_ChartResults` columns).

### 3.5 `tbl_Chart_KeyDetails` — one new column, two un-gated

| change | detail |
|---|---|
| **NEW** `VargaLongitudeDegrees DECIMAL(9,6) NOT NULL` | `(NirayanaLongitudeDegrees × N) mod 360` — the planet's longitude in this chart's own 360° space. For D1 (N=1) it equals `NirayanaLongitudeDegrees`. Today it is computed on `PlanetPosition` and used for combustion-in-varga but **never persisted**. |
| **UN-GATE** `DegreesInSignDecimal` | currently populated for D1 only. Now populated for every chart type as `VargaLongitudeDegrees mod 30` — the degree within *this chart's* sign. Matches PyJHora's `d_long` for every varga, including the table-based D2/D30/D60 (sign is a lookup, degree stays linear). |
| **UN-GATE** `DegreesInSignDisplay` | the `"12°34'56\""` string form of `DegreesInSignDecimal`, now for every chart type. |

Un-gated for the Ascendant/varga-lagna row too (`Planet = 'Ascendant'`).

Not touched, and staying D1-only (they are real-longitude display fields that a
varga sign genuinely does not carry): `Nakshatra`, `NakshatraPada`,
`NakshatraPadaId`. `NakshatraLordPlanet` / `NakshatraId` /
`NakshatraSubLordPlanet` stay populated for every chart type (already the case —
derived from the real longitude).

The existing `verify-schema` CHECK
`DegreesInSignDecimal IS NULL OR (>= 0 AND < 30)` remains valid; add
`VargaLongitudeDegrees >= 0 AND < 360`.

### 3.6 `ChartResult.ResultJson`

Kept, written as today, but explicitly **non-authoritative** — an audit snapshot.
Every field the varga JSON holds (`VargaLagna.Sign`, and per planet:
`Sign`, `NirayanaLongitudeDegrees`, `EclipticLatitudeDegrees`,
`SpeedLongitudeDegPerDay`, `VargaLongitudeDegrees`, `HouseNumber`,
`IsRetrograde`) now has a column in `tbl_Chart_KeyDetails` or `tbl_ChartResults`.
The doc comment on `ChartResult.ResultJson` is updated to say so.

### 3.7 Migrations

Numbered continuing from the schema-normalization chain (last was `09`):

| # | script | content |
|---|---|---|
| `10` | `10_add_varga_provenance_columns.sql` | `tbl_ChartResults` + `VargaMethod`/`AyanamshaDegrees`/`SiderealTimeHours`; `tbl_Chart_KeyDetails` + `VargaLongitudeDegrees` (NULLable), backfill existing rows (`NirayanaLongitudeDegrees` for D1; `× N` for existing D2/D6/D9/D10/D11 via the seed N), then `NOT NULL` + CHECK |
| `11` | `11_seed_varga_charttypes.sql` | `tbl_Dim_ChartType` Id 7..24 |
| `12` | `12_create_rule_vargascheme.sql` | `tbl_Rule_VargaScheme` + seed (RuleSetId 1) |
| `13` | `13_ungate_degrees_in_sign.sql` | recompute `DegreesInSignDecimal`/`DegreesInSignDisplay` for existing non-D1 rows from `VargaLongitudeDegrees` |

Each idempotent, self-recording in `dbo.SchemaMigrations`, folded into
`db/ikiastrro.sql` after it passes on the live DB. `vw_Chart_Consolidated` stays
the last object in the baseline; surface `VargaLongitudeDegrees` /
`DegreesInSignDecimal` there.

---

## 4. Code architecture (Approach C)

Keep the `IChartCalculator` + orchestrator-registration pattern (it is the
extension point for `verify-vargas`, `backfill-charts`, `compute-all`). Collapse
the 17 near-identical computers into one parameterized computer + a small set of
sign-rule strategies driven by `tbl_Rule_VargaScheme`.

```
Core/Astro/
  IVargaSignRule.cs        SignFor(int rasiSignIndex, double degreesInRasiSign) -> ZodiacName
  LinearVargaSignRule.cs   the ~11 'Linear' vargas; (rasiSign*N + segment) % 12, parameterised
  DrekkanaD3SignRule.cs    1st / 5th / 9th
  SaptamsaD7SignRule.cs    odd: from self; even: from 7th
  SiddhamsaD24SignRule.cs  odd: from Leo; even: from Cancer
  NakshatramsaD27SignRule.cs  from Aries, by movable/fixed/dual
  TrimsamsaD30SignRule.cs  unequal 5-part table (odd/even)
  ShashtyamsaD60SignRule.cs  floor(2 * degInSign) signs from the rasi sign
  HoraD2ClassicSignRule.cs   existing classical two-sign logic, moved behind the interface
  HoraD2UmaShambuSignRule.cs  Parasara Uma Shambu
  (NavamsaD9 / DasamsaD10 / RudramsaD11 wrap the existing AstroMath methods)

Core/Calculators/
  VargaChartComputer.cs    Compute(BirthDetails, int factor, IVargaSignRule) -> ChartAnalysisInput
                           ascendant + 9 planets: varga sign (rule), VargaLongitudeDegrees
                           ((real × N) mod 360), DegreesInSign (mod 30), whole-sign house from
                           varga lagna; passes through latitude / speed / retrograde
  VargaCalculator.cs       generic IChartCalculator: ctor(chartType, VargaScheme);
                           ComputeAnalysisInput delegates to VargaChartComputer;
                           BuildResult stamps ChartResult.VargaMethod + ChartTypeId + the
                           per-person AyanamshaDegrees / SiderealTimeHours

Core/Models/
  VargaScheme.cs           read model for tbl_Rule_VargaScheme (+ resolved IVargaSignRule)

Data/
  VargaSchemeRepository.cs  GetAll(ruleSetId) -> IReadOnlyList<VargaScheme>; builds the rule instances
```

`ChartCalculationOrchestrator.CreateDefault()` loads the scheme rows once and
registers `new VargaCalculator("D3", schemeD3)`, … for all 24 types. The
existing bespoke `D6/D9/D10/D11` computer+calculator pairs are **replaced** by
scheme rows + the generic path (D9/D10/D11 via a `Special` rule wrapping their
current `AstroMath` method so the maths is byte-identical); `D2` classical is
likewise moved behind `HoraD2ClassicSignRule`. Net: ~10 new files, ~8 deleted.

`PlanetPosition.VargaLongitudeDegrees` stays (still needed by `ChartAnalyzer` for
combustion-in-varga) and is now also persisted via a new
`ChartKeyDetail.VargaLongitudeDegrees` property that `ChartAnalyzer` fills for
every chart type; `DegreesInSignDecimal`/`Display` lose their `isRasiChart` gate
and are computed from `VargaLongitudeDegrees % 30`.

Because `backfill-charts` / `recompute-keydetails` / `compute-all` iterate
`orchestrator.Calculators`, they pick up the new types with no change — this is
**verified** in the plan, not assumed (the `tbl_Dim_ChartType` join in
`ChartGenerationService` must resolve for every new code, or generation throws).

---

## 5. Verification

No xUnit project (consistent with the repo's model); extend the CLI `verify-*`
self-checks.

1. **`verify-vargas` — grid match.** Transcribe all 18 divisional-chart grids
   from `D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt` (Ramakrishnan,
   22 Apr 1981, Chennai) into an expected `chartType -> {planet -> sign}` table.
   For each of the 24 chart types assert every planet's computed **sign** equals
   the expected sign. Per varga, whichever `MethodCode` reproduces the grid is
   the one seeded into `tbl_Rule_VargaScheme` (most resolve to
   `ParasaraTraditional`; the export tags them `(Trd)`).
2. **PyJHora cross-run** (documented, not automated) — spot-check a few vargas
   against `_research/PyJHora` with `chart_method` = the locked method, as a
   second independent reference. Note where a JHora default (e.g. D2-US) has no
   clean citable formula and the match is "closest".
3. **Degree sanity.** For every chart type and planet:
   `VargaLongitudeDegrees ∈ [0, 360)`, `DegreesInSignDecimal ∈ [0, 30)`,
   `DegreesInSignDecimal = VargaLongitudeDegrees mod 30` (±1e-4),
   `FLOOR(VargaLongitudeDegrees / 30)` sign index consistent with the stored
   `SignId` **for the Linear vargas** (the Special vargas legitimately diverge —
   assert only the range there).
4. **Vargottama.** `SELECT` where `d1.SignId = d9.SignId` for Ramakrishnan and
   eyeball against the export's Navamsa column.
5. **Regression.** `recompute-keydetails` then `verify-schema` + `verify-avastha`
   + `verify-vargas` + `verify-functional-nature` all ALL PASS; row counts for
   D1..D11 unchanged; new types add the expected row counts
   (`10 bodies × 24 types` in `tbl_Chart_KeyDetails` per person, etc.).
6. **Web smoke.** `/charts/{id}` still renders (no UI for the new charts yet —
   that is the next effort — but nothing regresses).

---

## 6. Non-goals (each its own effort)

- **D150 (Nadiamsa)** — deferred pending a chosen scheme + source.
- **`.se1` ephemeris switch** — `VargaLongitudeDegrees = long × N` inherits
  Moshier's ~1″ error; ×60 for D60 ≈ 1′. Fine for sign placement; flagged for a
  later precision pass. (Also: licensing hygiene — vendor SwissEphNet 2.8.0.2
  source, add `LICENSE`/`NOTICE` — is a separate housekeeping task.)
- **Personality-comparison layer** (Python, `tbl_KnownPersonality`, attribute
  vectors, similarity model) — its own brainstorm. This design deliberately
  shapes the fact tables to feed it (integer FKs, one row per person × chart ×
  planet, every attribute a column) without building it.
- **Full panchanga / §2** — Tithi, Karana, Yoga, sunrise/sunset, Janma Ghatis,
  lunar month. Only numeric ayanamsha + sidereal time land here.
- **Runtime ayanamsha / chart-style selection** (`requirements_gap` §1).
- **Web UI for the new charts** — explicitly after this batch.
- **xUnit test project** — staying with `verify-*` for now.

---

## 7. Risks / open items

| item | resolution |
|---|---|
| A JHora varga grid does not match any PyJHora method | Record the divergence, seed the closest method + a note in `MethodSource`, flag at plan review — same discipline used for D11 (Sanjay Rath) and D2. |
| D30/D60 "degree within sign" is notional (sign from a table, degree linear) | Store it anyway (`VargaLongitudeDegrees % 30`) — it is what PyJHora/JHora use internally; §5.3 asserts range only for Special vargas. |
| Backfilling `VargaLongitudeDegrees NOT NULL` on existing non-D1 rows | Migration `10` computes `NirayanaLongitudeDegrees × N` using the seeded `DivisionFactor`; guarded, then flips `NOT NULL`. |
| `ChartGenerationService` chart-type → `ChartTypeId` map throws on an unseeded code | Migration `11` (Dim rows) applied before any regen; plan Task ordering enforces it. |
| 24 chart types × N people is a lot of `compute-all` work | Acceptable (seconds); `backfill-charts` is idempotent and only fills gaps. |

---

## 8. Self-review

- **Placeholders:** none. The `tbl_Rule_VargaScheme` method column is "locked
  during implementation via grid match" — a defined procedure (§5.1), not a TBD.
- **Consistency:** `VargaLongitudeDegrees` defined once (§3.5) and used
  identically in §4, §5.3. `DegreesInSignDecimal = VargaLongitudeDegrees mod 30`
  stated in §3.5 and asserted in §5.3. The 24-type count is consistent in §1, §4,
  §5.
- **Scope:** single implementation plan's worth — ~10 new files, 4 migrations,
  `verify-vargas` extension, baseline fold. No cross-cutting redesign.
- **Ambiguity:** "which method per varga" is the one genuine unknown, and it is
  resolved by a mechanical check (§5.1) with a documented fallback (§7), not left
  to interpretation.

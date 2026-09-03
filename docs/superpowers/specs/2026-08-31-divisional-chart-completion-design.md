# Divisional-Chart Completion — Design

**Status:** approved — **Plan A** scope (D2-US + the 14 single-sign-rule vargas). D81 / D108 / D144 (chart-composition mechanism) + D150 → **Plan B**, a follow-up.
**Date:** 2026-08-31
**Author:** Claude (Sonnet 5) with rammyps
**Predecessors:** `2026-08-30-web-ui-life-area-recreate-design.md`, `2026-08-31-chart-schema-normalization-design.md`
**Source gap list:** `docs/scope-requirements-gap.md` §4 (Shodashavarga set) + §1 (varga within-sign degree, provenance)

---

## 1. Goal

Add every remaining classical divisional chart so the engine covers the full
Shodashavarga set plus the Jagannatha Hora export extras, computed
**database-driven** (varga rules are seeded data, not hard-coded), with **all
computed information available at the DB level** — no value that lives only in
`ChartResult.ResultJson` or in C# memory.

New chart types this batch (14 + 1):

| | Charts |
|---|---|
| **Shodashavarga completion** | D3, D4, D7, D12, D16, D20, D24, D27, D30, D40, D45, D60 |
| **Parasara extras** | D5, D8 |
| **Hora variant** | `D2-US` — Uma Shambu Hora, alongside the existing classical two-sign `D2` |

Every chart above is computed by **one shared mechanism**: a per-planet
`(rasi sign, degrees-in-sign) → varga sign` rule, seeded in `tbl_Rule_VargaScheme`.

**Split out to Plan B** (a follow-up — a different mechanism, so a different plan):

- **D81 / D108 / D144** — PyJHora's traditional-Parasara for these is *chart
  composition*, not a single sign-rule: D81 = Kalachakra-navamsa applied twice,
  D108 = D9 ∘ D12, D144 = D12 ∘ D12. They share a "varga composition" machinery
  that this plan does not build.
- **D150 (Nadiamsa)** — 8 competing schemes, no JHora-export cross-check (the
  export stops at D144). Revisit with a chosen method + citable source.

After this batch the engine computes **21 position chart types** per person
(D1, D2, D2-US, D6, D9, D10, D11 + the 14 above). Plan B adds D81 / D108 / D144
(→ 24) and later D150.

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
  tbl_Dim_ChartType     — controlled vocabulary of chart types (D1..D60, D2-US)
  tbl_Rule_Sets         — rule-set versions (existing)
  tbl_Rule_VargaScheme  — NEW: how each varga chart type is computed, per rule-set
```

No new fact tables. The 14 new charts are **additive rows** in the already
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
    DivisionFactor TINYINT      NOT NULL,                       -- N (2..60)
    MethodCode     VARCHAR(40)  NOT NULL,                       -- 'ParasaraTraditional' | 'UmaShambu' | 'ClassicalTwoSign'
    MethodSource   VARCHAR(200) NOT NULL,                       -- 'BPHS; PyJHora trimsamsa method 1'
    SignRuleKind   VARCHAR(10)  NOT NULL,                       -- descriptive tag: 'Linear' (pure (sign + l*stride)) | 'Special' (element/parity branch or table)
    SignRuleKey    VARCHAR(40)  NOT NULL,                       -- names the C# IVargaSignRule that VargaSignRuleFactory builds (e.g. 'TrimsamsaD30')
    CONSTRAINT UQ_Rule_VargaScheme UNIQUE (RuleSetId, ChartTypeId)
);
```

D1 is **not** in this table — it is the identity rasi, computed by
`D1RasiCalculator` (real longitude, nakshatra/pada display, houses from the real
Ascendant), not a varga scheme.

Seed (RuleSetId 1, `Parashari-Classical`), one row per position chart type
(N=division factor, `l` = `int(degInRasiSign / (30/N))`, `sign` = 0-indexed rasi
sign; all traced to PyJHora `horoscope/chart/charts.py`):

| ChartType | N | MethodCode | Kind | SignRuleKey | Rule (traditional Parasara / PyJHora method 1) |
|---|---|---|---|---|---|
| D2    | 2  | ClassicalTwoSign | Special | `HoraD2Classic`     | odd 0–15°→Leo, 15–30°→Cancer; even reversed (existing `AstroMath.GetHoraSign`) |
| D2-US | 2  | UmaShambu        | Special | `HoraD2UmaShambu`   | Uma Shambu variation (PyJHora `hora_chart` d2 default) — no clean single formula; port from PyJHora |
| D3    | 3  | ParasaraTraditional | Linear  | `DrekkanaD3`     | `(sign + l*4) % 12` |
| D4    | 4  | ParasaraTraditional | Linear  | `ChaturthamsaD4`| `(sign + l*3) % 12` |
| D5    | 5  | ParasaraTraditional | Special | `PanchamsaD5`   | odd → `[Ar,Aq,Sg,Ge,Li][l]`; even → `[Ta,Vi,Pi,Cp,Sc][l]` (PyJHora `panchamsa_odd_signs=[0,10,8,2,6]` / `_even=[1,5,11,9,7]`) |
| D6    | 6  | ParasaraTraditional | Special | `ShashtamsaD6`  | odd → `l % 12` (from Aries); even → `(l+6) % 12` (from Libra) (existing `AstroMath.GetShashtamsaSign`) |
| D7    | 7  | ParasaraTraditional | Special | `SaptamsaD7`    | odd → `(sign + l) % 12`; even → `(sign + l + 6) % 12` (from 7th) |
| D8    | 8  | ParasaraTraditional | Special | `AshtamsaD8`    | movable → `l % 12` (Aries); fixed → `(l+8) % 12` (Sagittarius); dual → `(l+4) % 12` (Leo) |
| D9    | 9  | ParasaraTraditional | Special | `NavamsaD9`     | existing `AstroMath.GetNavamsaSign` |
| D10   | 10 | ParasaraTraditional | Special | `DasamsaD10`    | existing `AstroMath.GetDasamsaSign` (odd from self, even from 9th) |
| D11   | 11 | SanjayRath          | Special | `RudramsaD11`   | existing `AstroMath.GetRudramsaSign` (Sanjay Rath) |
| D12   | 12 | ParasaraTraditional | Linear  | `DwadasamsaD12` | `(sign + l) % 12` |
| D16   | 16 | ParasaraTraditional | Special | `ShodasamsaD16` | movable → `l % 12` (Aries); fixed → `(l+4) % 12` (Leo); dual → `(l+8) % 12` (Sagittarius) |
| D20   | 20 | ParasaraTraditional | Special | `VimsamsaD20`   | movable → `l % 12` (Aries); dual → `(l+4) % 12` (Leo); fixed → `(l+8) % 12` (Sagittarius) — dual/fixed swapped vs D16 |
| D24   | 24 | ParasaraTraditional | Special | `SiddhamsaD24`  | odd → `(4 + l) % 12` (from Leo); even → `(3 + l) % 12` (from Cancer) |
| D27   | 27 | ParasaraTraditional | Special | `NakshatramsaD27` | fire → `l % 12` (Aries); earth → `(l+3) % 12` (Cancer); air → `(l+6) % 12` (Libra); water → `(l+9) % 12` (Capricorn) |
| D30   | 30 | ParasaraTraditional | Special | `TrimsamsaD30`  | odd 5-part `[(0,5,Ar),(5,10,Aq),(10,18,Sg),(18,25,Ge),(25,30,Li)]`; even `[(0,5,Ta),(5,12,Vi),(12,20,Pi),(20,25,Cp),(25,30,Sc)]` (on `degInRasiSign`) |
| D40   | 40 | ParasaraTraditional | Special | `KhavedamsaD40` | odd → `l % 12` (Aries); even → `(l+6) % 12` (Libra) |
| D45   | 45 | ParasaraTraditional | Special | `AkshavedamsaD45` | movable → `l % 12` (Aries); fixed → `(l+4) % 12` (Leo); dual → `(l+8) % 12` (Sagittarius) — same shape as D16 |
| D60   | 60 | ParasaraTraditional | Linear  | `ShashtyamsaD60` | `(sign + l) % 12` (from own sign) |

`l`-part vargas store `degInVargaSign = (NirayanaLongitude × N) mod 30` regardless
of the sign rule (PyJHora's `d_long`). The `SignRuleKey` above is the **expected**
mapping; each is confirmed against the Ramakrishnan JHora export grid during
implementation (§5) and re-pointed if it diverges (`MethodSource` records the
final choice). D3/D4/D12/D60 (Kind = `Linear`) all share one
`LinearVargaSignRule(N, stride)`; the rest get a dedicated class.

### 3.3 `tbl_Dim_ChartType` — 15 new seed rows

The six existing rows (Id 1..6 = D1, D2, D6, D9, D10, D11) stay. Add Id 7..21 for
`D2-US` and the 14 new vargas (D3, D4, D5, D7, D8, D12, D16, D20, D24, D27, D30,
D40, D45, D60), each with `DivisionalFactor = N`, `Category = 'Varga'`,
`DisplayOrder` in the conventional Shodashavarga order (D1, D2, D3, D4, D7, D9,
D10, D12, D16, D20, D24, D27, D30, D40, D45, D60, then D5/D8 as extras, then D2-US
next to D2 for display). `DisplayName` uses the Sanskrit varga name (Drekkana,
Chaturthamsa, Panchamsa, Saptamsa, Ashtamsa, Dwadasamsa, Shodasamsa, Vimsamsa,
Siddhamsa, Nakshatramsa, Trimsamsa, Khavedamsa, Akshavedamsa, Shashtyamsa)
cross-checked against whatever the Web already shows. Ids 22–24 (D81/D108/D144)
are reserved for Plan B.

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
sunset / Janma Ghatis / lunar month — `docs/scope-requirements-gap.md` §2) is
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
| `11` | `11_seed_varga_charttypes.sql` | `tbl_Dim_ChartType` Id 7..21 (D2-US + the 14 new vargas); Ids 22–24 left free for Plan B |
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
the near-identical per-varga computers into one parameterized computer + a set of
sign-rule strategies driven by `tbl_Rule_VargaScheme`.

```
Core/Astro/
  IVargaSignRule.cs          ZodiacName SignFor(double siderealLongitude)  -- rule derives sign+degInSign internally
  LinearVargaSignRule.cs     ctor(int factor, int stride); (sign + l*stride) % 12 — covers D3(4), D4(3), D12(1), D60(1)
  PanchamsaD5SignRule.cs     odd/even 5-entry lookup tables
  ShashtamsaD6SignRule.cs    wraps AstroMath.GetShashtamsaSign
  SaptamsaD7SignRule.cs      odd: from self; even: from 7th
  AshtamsaD8SignRule.cs      movable/fixed/dual → from Aries/Sagittarius/Leo
  ShodasamsaD16SignRule.cs   movable/fixed/dual → from Aries/Leo/Sagittarius
  VimsamsaD20SignRule.cs     movable/dual/fixed → from Aries/Leo/Sagittarius
  SiddhamsaD24SignRule.cs    odd: from Leo; even: from Cancer
  NakshatramsaD27SignRule.cs fire/earth/air/water → from Aries/Cancer/Libra/Capricorn
  TrimsamsaD30SignRule.cs    unequal 5-part table (odd/even), on degrees-in-rasi-sign
  KhavedamsaD40SignRule.cs   odd: from Aries; even: from Libra
  AkshavedamsaD45SignRule.cs movable/fixed/dual → from Aries/Leo/Sagittarius
  NavamsaD9SignRule.cs       wraps AstroMath.GetNavamsaSign
  DasamsaD10SignRule.cs      wraps AstroMath.GetDasamsaSign
  RudramsaD11SignRule.cs     wraps AstroMath.GetRudramsaSign
  HoraD2ClassicSignRule.cs   wraps AstroMath.GetHoraSign
  HoraD2UmaShambuSignRule.cs Parasara Uma Shambu — ported from PyJHora hora_chart d2 default
  VargaSignRuleFactory.cs    (SignRuleKind, SignRuleKey, DivisionFactor) -> IVargaSignRule  (pure, no I/O)

Core/Calculators/
  VargaChartComputer.cs    Compute(BirthDetails, int factor, IVargaSignRule) -> ChartAnalysisInput
                           ascendant + 9 planets: varga sign (rule), VargaLongitudeDegrees
                           ((real × N) mod 360), DegreesInSign (mod 30), whole-sign house from
                           varga lagna; passes through latitude / speed / retrograde
  VargaCalculator.cs       generic IChartCalculator: ctor(string chartType, VargaScheme);
                           ComputeAnalysisInput delegates to VargaChartComputer;
                           BuildResult stamps ChartResult.VargaMethod (from the scheme)

Core/Models/
  VargaScheme.cs           read model for tbl_Rule_VargaScheme (ChartType, DivisionFactor,
                           MethodCode, SignRuleKind, SignRuleKey)

Data/
  VargaSchemeRepository.cs  GetAll(ruleSetId) -> IReadOnlyList<VargaScheme>
```

`ChartCalculationOrchestrator.CreateDefault(IReadOnlyList<VargaScheme> schemes)`
takes the pre-loaded scheme rows and registers `new D1RasiCalculator()` plus
`new VargaCalculator(s.ChartType, s)` for every scheme — 21 calculators. Callers
load the schemes: `Cli/Program.cs` builds a `VargaSchemeRepository` from the
`connectionFactory` already in scope; `Web/Program.cs` resolves it from the
service provider in its `AddScoped` factory. The existing bespoke
`D2/D6/D9/D10/D11` computer+calculator pairs are **deleted** — their maths lives
on behind a `*SignRule` wrapper (byte-identical), reached through the generic
path. `AyanamshaDegrees` / `SiderealTimeHours` are stamped once per person by
`ChartGenerationService` (§3.4), not per calculator.

Net: ~19 new files (17 sign rules + factory + computer + calculator + model +
repo, minus overlaps), ~10 deleted (`D2HoraCalculator`/`D2HoraChartComputer` …
`D11RudramsaCalculator`/`D11RudramsaChartComputer`). `D1RasiCalculator` /
`D1ChartComputer` untouched.

`PlanetPosition.VargaLongitudeDegrees` stays (still needed by `ChartAnalyzer` for
combustion-in-varga) and is now also persisted via a new
`ChartKeyDetail.VargaLongitudeDegrees` property that `ChartAnalyzer` fills for
every chart type; `DegreesInSignDecimal`/`Display` lose their `isRasiChart` gate
and are computed from `VargaLongitudeDegrees % 30`.

Because `backfill-charts` / `recompute-keydetails` / `compute-all` iterate
`orchestrator.Calculators`, they pick up the new types with no change — this is
**verified** in the plan, not assumed (the `tbl_Dim_ChartType` join in
`ChartGenerationService` must resolve for every new code, or generation throws).

`IVargaSignRule.SignFor` takes the full sidereal longitude (0–360) so wrapper
rules can call the existing `AstroMath.GetXSign(double)` methods unchanged; each
rule computes its own `sign` / `degInRasiSign` / `l` internally.

---

## 5. Verification

No xUnit project (consistent with the repo's model); extend the CLI `verify-*`
self-checks.

1. **`verify-vargas` — grid match.** Transcribe the 15 divisional-chart grids
   relevant here (D2, D2-US, D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D16, D20,
   D24, D27, D30, D40, D45, D60 — the ones the export prints below D144, minus
   D81/D108/D144) from `D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt`
   (Ramakrishnan, 22 Apr 1981, Chennai) into an expected
   `chartType -> {planet -> sign}` table. For each of the 21 chart types assert
   every planet's computed **sign** equals the expected sign (D2-US against the
   export's `D-2 (US)` grid; classical D2 has no export grid — assert against the
   existing hand-computed `AstroMath.GetHoraSign` checks instead). Per varga,
   whichever `SignRuleKey` reproduces the grid is the one seeded into
   `tbl_Rule_VargaScheme`; the export tags the traditional ones `(Trd)`.
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
   D1..D11 unchanged (the `*SignRule` wrappers are byte-identical); new types add
   the expected rows (`10 bodies × 21 types` in `tbl_Chart_KeyDetails` per person,
   etc.).
6. **Web smoke.** `/charts/{id}` still renders (no UI for the new charts yet —
   that is the next effort — but nothing regresses).

---

## 6. Non-goals (each its own effort)

- **D81 / D108 / D144 (→ Plan B)** — traditional-Parasara for these is chart
  *composition*: D81 = Kalachakra-navamsa ∘ Kalachakra-navamsa, D108 = D9 ∘ D12,
  D144 = D12 ∘ D12 (PyJHora `nava_navamsa_chart` / `ashtotharamsa_chart` /
  `dwadas_dwadasamsa_chart`). Needs a `CompositeVargaSignRule` + the
  `kalachakra_navamsa` table ported — a separate plan. `tbl_Dim_ChartType` Ids
  22–24 are reserved for them.
- **D150 (Nadiamsa)** — deferred pending a chosen scheme + source (Plan B or
  later).
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
| A JHora varga grid does not match the expected `SignRuleKey` | Record the divergence, re-point to whichever rule reproduces it + note in `MethodSource`, flag at plan review — same discipline used for D11 (Sanjay Rath) and D2. |
| D2-US has no clean single formula | Ported from PyJHora `hora_chart` (d2 default); verified "closest" against the `D-2 (US)` export grid; note in `MethodSource`. |
| D30 "degree within sign" is notional (sign from a table, degree linear) | Store it anyway (`VargaLongitudeDegrees % 30`) — it is what PyJHora/JHora use internally; §5.3 asserts range only for Special vargas. |
| Backfilling `VargaLongitudeDegrees NOT NULL` on existing non-D1 rows | Migration `10` computes `NirayanaLongitudeDegrees × N` with an inline `CASE ChartType` for the 6 existing types (D1→1 … D11→11); guarded, then flips `NOT NULL`. |
| `ChartGenerationService` chart-type → `ChartTypeId` map throws on an unseeded code | Migration `11` (Dim rows) applied before any regen; plan Task ordering enforces it. |
| 21 chart types × N people is a lot of `compute-all` work | Acceptable (seconds); `backfill-charts` is idempotent and only fills gaps. |

---

## 8. Self-review

- **Placeholders:** none. Each `SignRuleKey` has an explicit formula in §3.2;
  "locked during implementation via grid match" (§5.1) is a defined procedure
  with a documented fallback (§7), not a TBD.
- **Consistency:** `VargaLongitudeDegrees` defined once (§3.5), used identically
  in §3.2, §4, §5.3. `DegreesInSignDecimal = VargaLongitudeDegrees mod 30` stated
  in §3.5 and asserted in §5.3. The **21-type** count is consistent in §1, §3.3,
  §4, §5. `SignRuleKey` (not `SpecialRuleKey`) throughout §3.2/§4/§5/§7.
- **Scope:** one implementation plan — ~19 new files, 4 migrations,
  `verify-vargas` extension, baseline fold. D81/D108/D144 + D150 split to Plan B
  (§1, §6). No cross-cutting redesign.
- **Ambiguity:** "which sign rule per varga" is the one genuine unknown; §3.2
  gives the expected mapping with formulae, §5.1 confirms it mechanically, §7 has
  the fallback.

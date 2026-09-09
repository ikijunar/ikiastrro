---
last_updated: 2026-09-09
workstream: cli
togaf: C — Application Architecture (engine)
---

# CLI — calculations reference

Every calculation ikiastrro performs, its convention, and its source. All code is in
`Ikiastrro.Core`; only raw longitudes come from the ephemeris.

## 1. Ephemeris & frame

- **Engine:** `SwissEphNet` (`SwissEphemerisProvider`), **Moshier analytical mode** — no
  ephemeris data files.
- **Sidereal / ayanāṁśa:** selectable. `AyanamsaDefinition` catalogues 22 systems, each with
  its Swiss `swe_set_sid_mode` id; `tbl_Rule_Ayanamsa` holds the system default. Current
  default **`Jagannatha`**. Every `tbl_ChartResults` row records `AyanamshaDegrees` +
  `SiderealTimeHours`. *(Open: `FEAT-DATA-04` — the `Jagannatha` entry maps to Swiss mode 26,
  ~0.85° off the JHora reference; fix + regeneration pending.)*
- **Nodes:** Rahu = **mean node** (`SE_MEAN_NODE`); Ketu = Rahu + 180° (derived, never stored
  separately). `vw_KetuSignTransitEvents` = Rahu events + 6 signs.
- **Speed & latitude:** from the same `swe_calc_ut` call (`SEFLG_SPEED`); persisted per planet
  for D1 and every varga. Speed drives retrograde + combustion-orb selection.
- **House system:** **Whole Sign** everywhere. House *n* from a reference sign =
  `AstroMath.CountFromSignToSign` (the sign holding the reference point is house 1). Confirmed
  for house placement + placement-rule interpretation.
- **Time:** birth local wall-clock + place; lat/long via Nominatim; UTC offset resolved
  offline from lat/long + date (historical DST respected). Stored `DATETIME2`, local time of
  day, no offset column.
- `ZodiacName` keeps the Latin spelling `Capricornus`.

## 2. Divisional charts (21 types)

**D1** (real sidereal longitude) + **20 vargas**: D2, D2-US, D3, D4, D5, D6, D7, D8, D9, D10,
D11, D12, D16, D20, D24, D27, D30, D40, D45, D60.

- Data-driven: `tbl_Rule_VargaScheme` is the source of truth (one row per varga per rule set —
  `DivisionFactor`, `MethodCode`, `SignRuleKind`, `SignRuleKey`).
  `ChartCalculationOrchestrator.CreateDefault(schemes)` builds one shared `VargaCalculator`
  (`IChartCalculator`) per row; every varga gets the full shared analytics.
- `SignRuleKey` → C# `IVargaSignRule` via `VargaSignRuleFactory`. `LinearVargaSignRule` covers
  D3 / D4 / D12 / D60; D2 / D6 / D9 / D10 / D11 wrap `AstroMath.Get*Sign`; the rest are bespoke
  `Special` rules (`HoraD2UmaShambu`, `PanchamsaD5`, `SaptamsaD7`, `AshtamsaD8`, `ShodasamsaD16`,
  `VimsamsaD20`, `SiddhamsaD24`, `NakshatramsaD27`, `TrimsamsaD30`, `KhavedamsaD40`,
  `AkshavedamsaD45`). Formulas transcribed from PyJHora `chart_method=1` "Traditional Parāśara".
- **D2** uses the classical two-sign Leo/Cancer Hora; **D2-US** is JHora's default Uma Shambu
  parivṛtti. **D11** uses the PVR/BPHS traditional Rudramsa (not the Sanjay Rath variant).
- Stored: `VargaLongitudeDegrees` (`Normalize(realLon × N)`), `DegreesInSignDecimal`
  (`= VargaLongitudeDegrees mod 30`, populated every chart type). The varga sign is the
  `IVargaSignRule`, **not** `FLOOR(VargaLongitudeDegrees/30)`.
- Not built → varga-of-a-varga composition (D81 / D108 / D144) and D150.

Significations: D2 wealth · D3 siblings/courage · D4 property · D5 fame/authority · D6 health ·
D7 children · D8 sudden events/longevity · D9 marriage/dharma/strength · D10 career · D11 gains ·
D12 parents · D16 vehicles/comforts · D20 spiritual practice · D24 education · D27
strengths/weaknesses · D30 misfortunes · D40 maternal legacy · D45 paternal legacy · D60
past-life karma.

## 3. Dignity

9-tier `DignityStatus` ∈ Exalted · Moolatrikona · Own Sign · Great Friend · Friend · Neutral ·
Enemy · Great Enemy · Debilitated. Pañchadhā Maitrī = natural (permanent) + temporary
(from-Moon) friendship; sign-dignity only, not affliction. PVR Table 6 segments live in
`tbl_Rule_GrahaDignity` (`PvrDignityEvaluator`); the Parāśari exaltation/debilitation degrees,
Moolatrikoṇa ranges and own signs are also hard-coded and cross-checked to `tbl_SignAttributes`.
**Rahu/Ketu** use the Parāśari convention (Rahu exalted Taurus, Ketu exalted Scorpio) — an
explicit choice; their `DignityStatus` is limited to Exalted / Debilitated / Neutral.
Check: `verify-dignity`.

## 4. House lordship, conjunctions, aspects

- **House lordship** per house: its sign's ruler and where that ruler sits — counted from
  Lagna, Sun and Moon.
- **Conjunctions:** grahas sharing a sign; `DegreeSeparation` D1-only. Plus an explicit
  ≥ 2-graha **group/member** layer with `MemberKey` for yoga-subset matching.
- **Aspects (graha dṛṣṭi):** every graha aspects the 7th from itself; Mars +4/8, Jupiter +5/9,
  Saturn +3/10. **Rahu/Ketu use the Jupiter-style 5/7/9 convention** — an explicit choice.

## 5. Retrograde & combustion

Retrograde = `speed < 0`. Combustion (asta) for the 6 applicable planets (not Sun / Rahu /
Ketu / Ascendant); BPHS / Phaladeepika orbs with the **narrower orb auto-selected when
retrograde** (Moon 12 · Mars 17/8 · Mercury 14/12 · Jupiter 11 · Venus 10/8 · Saturn 15).
Stored: `IsCombust`, `DistanceFromSunDegrees`, `CombustionOrbUsedDegrees`.

## 6. Nakshatras

27 nakṣatras (Abhijit excluded), 13°20′ each. `GetNakshatraLord` / `NakshatraLordOrder` is
the single source of truth (Vimśottari uses it). `Nakshatra` / `NakshatraPada` display are
**D1-only**; `NakshatraLordPlanet` + `NakshatraSubLordPlanet` (KP level-2) are populated for
every chart type. `tbl_NakshatraPadas` (108) has a verified 1:1 Pada ↔ Navamsa mapping.
Descriptive fields (Guna/Gana/Yoni/Nadi/…) left NULL pending a cited source.

## 7. Vimshottari Dasha

`VimshottariDashaCalculator` (pure) + `VimshottariDashaService` (write path). Not an
`IChartCalculator`; logged as a `tbl_ChartResults` row for delete-cascade consistency.
**3 levels** (Mahā → Antar → Pratyantar), **all three correctly partial-at-birth**. Cycle:
Ketu 7 · Venus 20 · Sun 6 · Moon 10 · Mars 7 · Rahu 18 · Jupiter 16 · Saturn 19 · Mercury 17
(120 yr). Real dates use 365.2425 d/yr. Storage `tbl_Chart_DashaPeriods` (self-referencing,
age-relative + absolute); reporting `vw_Chart_DashaTimeline`, `tvf_Chart_LifeWeeks` (week
1–4000). Reference: `BENCH_RAMAKRISHNAN_P_JHORA_1981` (`db/checks/check_ayanamsa_dasha_benchmarks.sql`).

## 8. Slow-planet transit history

Saturn / Jupiter / Rahu sign-boundary-crossing **event log** (not periods, so retrograde
re-entries are correct), **1930–2060**. Built by day-walk + bisection. CLI:
`precheck-planet-transits` (dry-run vs published dates) then `backfill-planet-transits`. Ketu
derived (`vw_KetuSignTransitEvents`); point-in-time sign `tvf_PlanetSignAtDate`.

## 9. Sade Sati, Kantaka & Ashtama Shani

`tvf_Chart_SadeSatiPeriods(@BirthDetailId)` from the stored natal Moon sign +
`tbl_PlanetSignTransitEvents` — no new reference data. Sade Sati = Saturn in the 12th / 1st /
2nd from natal Moon (three Dhaiyas); Kantaka = 4th; Ashtama = 8th. The TVF splits windows on
retrograde re-entries; the UI re-consolidates. **Ashtakavarga** (bindu-point transit strength)
is out of scope pending a cited source for the contribution tables — the vendored MIT
`jyotishganit` now unblocks the port.

## 10. Functional benefic / malefic

`LagnaFunctionalNature` — Parāśari **functional** nature for a given Lagna from which houses a
planet rules. `enum { Benefic, Malefic, Neutral, Yogakaraka }` + `IsMaraka` /
`KendradhipatiDosha` / rationale. Rahu/Ketu out of scope. Heuristic from B.V. Raman *How to
Judge a Horoscope* Vol. 1 p.14–18; the sole source of the verdict (the per-Lagna mirror table
was removed). Computed on demand, not persisted. Check: `verify-functional-nature`.

## 11. Avasthas (planetary states)

Star-schema (`tbl_Dim_PlanetaryState` + `tbl_Rule_AgeState` / `tbl_Rule_WakefulnessState` →
`tbl_Fact_PlanetaryState`), written by `PlanetaryStateComputer`.

- **Bālādi** (age from within-sign degree) — odd signs 0–6 Bāla / 6–12 Kumāra / 12–18 Yuva /
  18–24 Vṛddha / 24–30 Mṛta; even signs reversed. Effect fraction from `tbl_Rule_AgeState`.
  **D1 only.**
- **Jāgradādi** (waking state from `DignityStatus`) — Exalted/MT/Own → Jāgrat; friend tiers →
  Svapna; enemy tiers → Suṣupti. **Every chart type.**
- Not built: Dīptādi, Lajjitādi (need a shared benefic/malefic classifier), Śayanādi (needs
  janma-ghaṭis). Check: `verify-avastha`.

## 12. Jaimini Chara Karakas & special points

`CharaKaraka` + `PointKind` columns on `tbl_Chart_KeyDetails` (migration 14). Check:
`verify-jaimini` (against the `1_Ramakrishnan` JHora export).

- **Chara Karakas** (Aṣṭa, 8-karaka) — rank the 8 grahas (Sun…Saturn, Rahu) by degree within
  sign, descending; **Rahu's key is `30 − degreeInSign`**. AK → AmK BK MK PiK PK GK → DK.
  Ketu not ranked. Computed once per person from D1, stamped on every chart type.
- **Special points** — one D1 longitude each, then projected into all 21 vargas with the same
  `IVargaSignRule` a planet uses:
  - **AL + 12 Bhāva Arudhas** (`ArudhaCalculator`, `PointKind = Arudha`) — Parāśari pada per
    house, with the 1st/7th → 10th exception; A1 emitted as `AL`.
  - **Hora Lagna** (`HoraLagnaCalculator`, `PointKind = SpecialLagna`, code `HL`) — Sun's
    sidereal longitude at the Vedic day's opening sunrise + 0.5° per clock-minute since it.
  - **All 11 upagrahas** (`SubPlanetCalculator`, `PointKind = Upagraha`) from migration-27
    rules under `SRC_PVR_INTEGRATED`: Sun chain (Dhūma / Vyatīpāta / Parivesha / Indrachāpa /
    Upaketu) + 6 time points (Kāla / Mṛtyu / Ardhaprahara / Yamaghaṇṭaka / Gulika = Saturn's
    eighth midpoint, Maandi = its start). Check: `verify-upagrahas` (in-memory);
    `verify-jaimini` expects stored charts regenerated under PVR names.
- Sunrise/sunset from `SwissEphemerisProvider.GetSunTimes` (`swe_rise_trans`,
  `SE_BIT_DISC_CENTER | SE_BIT_NO_REFRACTION`).
- Not built: Karakāṁśa / Swāṁśa chart, Jaimini rāśi dashas, other special-lagna families.

## 13. Strength (Ṣaḍbala / Bhāva Bala)

`ShadbalaCalculator` / `BhavaBalaCalculator`, PVR-first profile in `tbl_Rule_ShadbalaComponent`
/ `tbl_Rule_BhavaBalaComponent` with formula provenance, → `tbl_Fact_PlanetaryStrength*` /
`tbl_Fact_BhavaStrength*`. Available: total Ṣaḍbala, the six sub-totals, named component rows,
Kālabala, strongest-planet ranking, Bhāva Bala, `tbl_Fact_Vargottama` (explicit D1/D9 same-sign
facts). Remaining: uncomputed Tribhāga/Varṣa/Māsa/Dina/Horā/Ayana Kālabala; planetary war;
per-planet minimum-rūpa thresholds; Iṣṭa/Kaṣṭa/Cheṣṭā reconciliation; Vimśopaka Bala.

## 14. Yoga

`ProductionYogaEngine` is the composition root over every implemented Raman/PVR evaluator +
the Raman 201–300 ledger. Chart generation persists all source variants per chart
(`tbl_Fact_YogaInputEvaluations`, `tbl_Rule_Yoga` + applicability/context-requirement tables).
P0 inputs: required vargas, exact longitude, day/night, lunar phase, source-specific subject
sex — missing P0 context yields `NOT_EVALUATED`, not `ABSENT`. In progress: predicates
201–300, PVR P0 additions (Subha, Asubha, Guru-Mangala, Chamara, Khadga, Lagnaadhi, Saarada,
Dharma-Karmādhipati, Vipareeta Rāja), structured missing-requirement codes.

## Chart-reading method (JHora-anchored)

The universal varga-reading loop, per-varga significators, and worked lenses over the
`1_Ramakrishnan` reference export are consolidated here at a high level; the deep per-chart
reading guides (D1 / D9 / D10 / D60 + the short index for the rest) are retained under
`docs/research/domain/` as reference material.

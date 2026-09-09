---
last_updated: 2026-09-09
workstream: cli
togaf: E — gap analysis
safe: Program backlog input
---

# CLI — gap & coverage vs a full JHora natal export

Baseline: the Jagannatha Hora natal export for `1_Ramakrishnan`. It is a **feature-parity
checklist**, not the numeric source of truth (longitudes are independently verified against
Swiss-ephemeris-compatible tools). Each item below maps to a `FEAT-<AREA>` row in
[`../../masterproduct.md`](../../masterproduct.md).

## Delivered (not gaps)

Sidereal longitudes (Ascendant, 7 planets, nodes) · D1 signs/degrees, nakṣatra/pada/lord + KP
L2 sub-lord · **all 21 position charts** (D1 + 20 vargas, Plan A) with per-varga within-sign
degree · dignity, whole-sign house lordship (3 reckonings), conjunctions (+ groups), graha
dṛṣṭi, retrograde, combustion · Vimśottari dasha (3 levels, partial-at-birth) · slow-planet
transit events · Sade Sati / Kantaka / Ashtama · functional benefic/malefic · Bālādi +
Jāgradādi avasthas · Chara Karakas (Aṣṭa) · AL + 12 Arudhas + Hora Lagna + **all 11
upagrahas** · Ṣaḍbala / Bhāva Bala foundation · source-attributed yoga inputs · provenance
(ayanāṁśa degrees, sidereal time, rule set, method) on every chart.

## Priority gaps

1. **Precision & config foundation** — ayanāṁśa correctness (`FEAT-DATA-04`); ayanāṁśa
   *selectable in the UI* (`FEAT-UI-12`); chart-style selectable; documented selectable D2
   Hora method if JHora's Uma Shambu output must be reconciled.
2. **Panchanga / time layer** — sunrise/sunset (built for special points; not surfaced),
   janma ghaṭis, tithi + fraction, karana, nitya yoga, Vedic weekday, samvatsara + lunar-month
   convention, Hora Lord / Kaala Lord. `jyotishganit` (MIT) is an implementation reference.
3. **Jaimini base layer** — Karakāṁśa from AK in D9; the remaining special lagnas (Bhava,
   Ghati, Vighati, Varnada V1–V12, Sree, Pranapada, Indu); Bhṛgu Bindu. Dependency for
   Jaimini rāśi dashas.
4. **Strength systems** — Bhinnāṣṭakavarga + Sarvāṣṭakavarga; Trikoṇa / Ekādhipatya Śodhana;
   Piṇḍa reductions; full Ṣaḍbala rūpas + Iṣṭa/Kaṣṭa; Vimśopaka Bala + Vaiśeṣikāṁśa.
   `jyotishganit` covers BAV/SAV + Ṣaḍbala; Piṇḍa + weighted-varga conventions still need a
   cited source.
5. **Avastha & karaka reference** — Dīptādi + Lajjitādi (need a shared benefic/malefic
   classifier); Śayanādi (needs persisted janma ghaṭis, source-blocked); apply the designed
   `tbl_Dim_HouseSignification` / Sthira / Naisargika reference data (migration 030) instead
   of the hard-coded `LifeAreaMap`; decide Sapta vs Aṣṭa Naisargika coverage.

## Parity gaps (lower priority)

- **Sphutas** — Prana, Deha, Mrityu, Sookshma Tri-Sphuta, Tithi, Yoga, Kshetra, Beeja, Tri,
  Chatus, Pancha, Kunda, Avayoga and Dhūma-derived points.
- **Additional dashas** — Ashtottari, Yogini (applicability rules); Jaimini rāśi dashas
  (Moola, Narayana, Sudasa); Kālachakra. Each uses the generic dasha-period storage, carries
  its system + rule-set identity, and has worked reference assertions. PyJHora is a
  cross-check reference only (AGPL).
- **Varga composition** — D81 / D108 / D144 (varga-of-a-varga) and D150.

## Open engineering

- Rules-engine **Phase 2** — calculators read the versioned `tbl_Rule_*` tables instead of
  hard-coded C#.
- **Yoga detection** design pass — turn source prose into seed-ready rule rows with explicit
  orbs and conflict/precedence.
- **Synthesis layer** — assemble stored facts + significations into per-house / life-area
  judgements. Deliberately distinct from calculation; its own interpretation policy.
- Automated reference regression for every new technique (JHora only where conventions match,
  plus independent ephemeris checks).
- Decide whether altitude belongs in birth details (matters only for elevation-sensitive
  rise/set).

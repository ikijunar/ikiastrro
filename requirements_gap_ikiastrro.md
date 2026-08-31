# ikiastrro — Remaining Requirements Gap

**Baseline:** Jagannatha Hora natal-chart export for `1_Ramakrishnan` (22 Apr 1981,
05:30, Chennai), supplied in `D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt`.

**Purpose:** A current requirements gap list for the Vedic/Jyotisha engine. The JHora
export is a feature-parity checklist, not the numeric source of truth. Longitudes are
already independently verified against Swiss Ephemeris-compatible tools. This document
therefore identifies computations, data, configuration, and presentation still required
after the current implementation described in `ARCHITECTURE.md` and
`ikiastrro_calculations.md` (reviewed 2026-08-31).

## Current baseline — already delivered

The following JHora-equivalent foundations are not gaps: Lahiri sidereal longitudes for
Ascendant, seven planets and nodes; D1 signs/degrees, nakshatra/pada/lord and KP level-2
sub-lord; D1/D2/D6/D9/D10/D11 charts; dignity, whole-sign house lordship, conjunctions,
graha drishti, retrograde and combustion; Vimshottari Dasha through Pratyantardasha;
slow-planet transit events; Sade Sati/Kantaka/Ashtama Shani; functional nature; and
Baaladi plus Jagradadi avasthas.

## Priority implementation requirements

### 1. Complete the precision and configuration foundation

- Compute and persist each varga's within-sign degree. This is the current prerequisite
  for strict Vargottama verification and meaningful same-sign conjunction tightness in
  D9 and other vargas.
- Surface the numeric ayanamsha value used for a chart, along with sidereal time.
- Make ayanamsha selectable and record the chosen convention with every calculation.
- Make chart style selectable (at minimum the supported Indian chart presentation styles).
- Add a documented selectable Hora method if JHora's D2 **Uma Shambu** output must be
  reconciled; the current implementation intentionally uses classical two-sign Hora.
- Preserve calculation provenance: ephemeris mode/version, ayanamsha, house system,
  varga method, time-zone resolution, and rounding rules must be queryable with a result.

### 2. Add the panchanga/time layer

These values occur at the top of the JHora export and unlock multiple downstream systems.

- Sunrise and sunset for the birth place/date, including a documented elevation policy.
- Janma Ghatis (time elapsed since sunrise).
- Tithi and remaining fraction.
- Karana and remaining fraction.
- Nitya Yoga and remaining fraction.
- Vedic weekday (sunrise-to-sunrise, rather than merely the civil weekday).
- Lunar year/month: Samvatsara and the chosen lunar-month convention (amanta or
  purnimanta must be explicit).
- Hora Lord, Mahakala Hora and Kaala Lord.

Acceptance: retain the input/convention metadata and reproduce the reference export for
the Ramakrishnan chart under its documented settings. `jyotishganit`'s MIT panchanga
implementation is an available implementation reference; SwissEphNet provides the needed
rise/set and sidereal-time primitives.

### 3. Build the Jaimini base layer

- Implement the eight-karaka Chara Karaka calculation, including Rahu's reverse-degree
  treatment and a stated tie-breaking convention.
- Persist/display the Karaka portfolio (AK through DK) and derive Karakamsa from AK in D9.
- Add the special lagnas: Bhava, Hora, Ghati, Vighati, Varnada (and V2–V12), Sree,
  Pranapada and Indu Lagna.
- Add Bhrigu Bindu.

This layer is a dependency for Jaimini rasi dashas and substantial JHora parity.

### 4. Finish the classical Shodashavarga set

Implement the twelve missing Shodashavarga charts: D3, D4, D7, D12, D16, D20, D24,
D27, D30, D40, D45 and D60. D30 and D60 require an explicit convention decision before
implementation. Follow with the JHora extras D5, D8, D81, D108 and D144 if full export
parity remains the goal.

Acceptance: every varga must have a source/method identifier, regression examples, and
the same shared analytics pipeline used by existing chart calculators.

### 5. Implement strength systems

- Bhinnashtakavarga for Lagna and the seven planets, plus Sarvashtakavarga.
- Trikona and Ekadhipatya Shodhana; Sodhya, Rasi and Graha Pinda.
- Shadbala: six component balas, rupas, percentage, Ishta Phala and Kashta Phala.
- Vimsopaka Bala and Vaiseshikamsa, after the Shodashavarga prerequisite is complete.

The MIT-licensed, tested `jyotishganit` sources cover Bhinna/Sarva Ashtakavarga and
Shadbala. Pinda reductions and the weighted varga conventions still need a cited,
documented rule source.

### 6. Finish the avastha and karaka-reference layers

- Deeptadi (mood) and Lajjitadi avasthas, starting with a reusable natural
  benefic/malefic, node and luminary classifier and documented precedence rules.
- Sayanadi/Cheshta avasthas, after Janma Ghatis is persisted; this remains source-blocked
  until a cited edition and convention are selected.
- Apply the designed house significations and Sthira/Naisargika Karaka reference data
  (migration 030) rather than leaving `LifeAreaMap` as a hard-coded subset.
- Decide and document Sapta (7) versus Ashta (8) Naisargika Karaka coverage and source a
  general planet-signification table.

## Remaining parity requirements

### Derived longitude points

- Upagrahas: Maandi, Gulika, Dhooma, Vyatipata, Parivesha, Indra Chapa, Upaketu, Kaala,
  Mrityu, Ardha Prahara and Yama Ghantaka. Gulika/Maandi depend on the sunrise/sunset
  layer; the remainder require documented longitude formulae.
- Sphutas: Prana, Deha, Mrityu, Sookshma Tri-Sphuta, Tithi, Yoga, Rahu-Tithi, Kshetra,
  Beeja, Tri, Chatus, Pancha, Kunda, Avayoga and related Dhooma-derived points.

### Additional dashas

- Ashtottari and Yogini dashas (select/document applicability rules).
- Jaimini rasi dashas: Moola, Narayana and Sudasa; these depend on Chara Karakas, while
  Sudasa also depends on Sree Lagna.
- Kalachakra Dasa, including Savya/Apasavya, Deha/Jiva and Paramayush rules.
- Optional extension of Vimshottari from three to five levels (Sookshma and Prana).

Each dasha must use the existing generic dasha-period storage model, carry its system and
rule-set identity, and have worked reference-chart assertions. PyJHora can be a
cross-check/porting reference only subject to its AGPL obligations; it is not a drop-in
code source for this project.

## Product and engineering requirements still open

- Design and implement Yoga detection (Rajayoga, doshas and named yogas) as a separate
  rules-design effort. Source material exists, but it is prose rather than a seed-ready
  table; conjunction orbs and conflict/precedence rules must be explicit.
- Implement a synthesis layer that assembles stored facts and significations into
  per-house/life-area judgments. This is intentionally distinct from calculation and
  requires its own interpretation policy.
- Add Bhava Bala only after selecting a cited method/source; the current Raman extract
  expressly defers it and does not support guessing a formula.
- Complete rules-engine Phase 2: calculators should read versioned rule-set tables rather
  than duplicate hard-coded relationship, combustion and dignity data. Keep rule-set facts
  immutable and versioned.
- Decide whether altitude belongs in birth details. It is negligible for most chart work
  but matters if rise/set calculations are elevation-sensitive.
- Surface persisted computations consistently in the web workspace, including chart
  selection, strengths, panchanga, special points and dasha timelines as each is delivered.
- Add automated reference regression tests for every new technique, including the supplied
  JHora chart only where conventions match, and separate independent ephemeris checks.

## Source and decision blockers

| Item | Blocker / required decision |
|---|---|
| Sayanadi | Select a cited edition and persist Janma Ghatis. |
| Deeptadi/Lajjitadi | Document the classical precedence and reusable natural-benefic/malefic classification. |
| Bhava Bala | Obtain a cited Bhava Bala method; do not derive it from the current source extract. |
| D30/D60 | Choose and document the varga method before coding. |
| Vimsopaka/Vaiseshikamsa | Complete Shodashavarga and select/record weights and grade rules. |
| Pinda reductions | Source BPHS-compatible Trikona/Ekadhipatya Shodhana details. |
| Extra dashas / special lagnas / upagrahas | Select a license-compatible primary source or an independently authored implementation; PyJHora is AGPL. |
| D2 JHora parity | Decide whether selectable Uma Shambu Hora is a product requirement. |

## Recommended delivery order

1. D9/varga within-sign degree, calculation provenance, and selected configuration options.
2. Sunrise/sunset, sidereal time, Janma Ghatis and panchanga.
3. Chara Karakas, Bhrigu Bindu and the common special-lagna infrastructure.
4. The twelve missing Shodashavarga charts, with D30/D60 decisions first.
5. Ashtakavarga and Shadbala, then Vimsopaka/Vaiseshikamsa.
6. Upagrahas, sphutas and the remaining JHora export-only points.
7. Additional dasha engines, grouped by their shared Jaimini dependencies.
8. Yoga detection and the interpretation/synthesis layer once their rule policy is scoped.

## Explicit non-requirements for this gap analysis

This document does not treat the JHora output as requiring identical values where the
project intentionally selects a different documented convention (currently the D2 Hora
method). It also does not recommend copying AGPL source into ikiastrro. Any newly added
calculation must state its source, method, convention and verification evidence.

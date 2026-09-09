---
last_updated: 2026-09-08
status: planned
---

# Ashtottari Dasha Implementation Plan

**Goal:** Implement conditional Ashtottari Dasha with explicit applicability evidence, three timing levels, versioned rules, JHora comparison, persistence, and UI presentation.

**Architecture:** Add an Ashtottari calculator beside the existing Vimshottari engine, but share generic period-tree models and persistence where their contracts truly coincide. Applicability is evaluated before calculation from `tbl_Rule_DashaApplicability`; a result must say applicable, inapplicable, or indeterminate rather than silently emitting periods for every chart.

**Tech Stack:** .NET 8, SwissEphNet, SQL Server, Dapper, Blazor Server, xUnit/CLI verification.

## Existing benchmark foundation

Migration `46_create_ayanamsa_dasha_benchmarks.sql` is applied locally and creates the shared dasha catalogue plus `BENCH_RAMAKRISHNAN_P_JHORA_1981`. It stores ten JHora sidereal longitudes and nine Vimshottari Mahadasha boundaries. Ashtottari is only catalogued as `Planned`; its dates remain absent until applicability, settings, and a dedicated JHora golden export are confirmed. Use `db/checks/check_ayanamsa_dasha_benchmarks.sql` for inspection.

## Research

Status: [ ] complete   [x] partial   [ ] not started

| Research source | Needed for | Status |
|---|---|---|
| P.V.R. Narasimha Rao, *Vedic Astrology: An Integrated Approach* (`SRC_PVR_INTEGRATED`) | Sequence, durations, start balance, interpretation boundary | Partial |
| PVR, “Unified Nakshatra Dasa Approach” | Applicability and selection among conditional nakshatra dashas | Partial; its Pushya-paksha dependency must remain explicit |
| Ramakrishnan P JHora export (`SRC_JHORA_EXPORT_RAMAKRISHNAN`) | Applicability example and future golden dates | Available, deliberately not seeded into the ayanamsa benchmark yet |
| BPHS licensed/usable edition | Independent classical confirmation | Not started |

## Decisions required before implementation

- Confirm the exact classical applicability condition and how PVR's later unified selection method relates to it.
- Decide whether the first release follows the natal Moon seed only or exposes supported seed variations.
- Confirm the dasha-year convention and boundary rounding used by the selected JHora reference profile.
- Decide whether three levels are sufficient for parity with current Vimshottari or whether deeper levels belong in a later phase.
- Record whether Ashtottari is applicable to Ramakrishnan P and the evidence; do not infer applicability merely because JHora can print the table.

## Global constraints

- Branch: current working branch; preserve unrelated work.
- Do not push unless asked.
- Migrations are numbered, idempotent and self-recording; migration 46 establishes the catalogue and benchmark foundation.
- Every calculation records `RuleSetId`, ayanamsa code, engine version, year convention and applicability result.
- PyJHora is reference-only because of its AGPL licence; do not copy implementation code.
- No hard-coded display strings in Web components; use terminology codes.

## Task 1: Complete and record the rule research

**Files:** `docs/reference-calculations.md`, `docs/research/reference-sources.md`, this plan.

- [ ] Extract the Ashtottari lord order, durations, total cycle, starting-nakshatra rule and balance-at-birth formula from primary sources.
- [ ] Extract every applicability predicate and document disagreements by source.
- [ ] Define boundary semantics and tolerances against JHora.
- [ ] Mark research complete only after every executable rule has a source.

## Task 2: Seed versioned applicability and duration rules

**Files:** new numbered migration, `db/ikiastrro.sql` after live proof.

- [ ] Add the Ashtottari applicability predicate under a new rule-set version if existing published rules would otherwise be reinterpreted.
- [ ] Add data-driven lord order and duration rows or a documented shared dasha-rule structure.
- [ ] Add source keys and constraints; verify every duration sums to the classical total.

## Task 3: Add Core applicability and calculator APIs

**Files:** `src/Ikiastrro.Core/Engines/Dasha/`.

- [ ] Introduce a typed applicability result: `Applicable`, `Inapplicable`, `Indeterminate`, plus evidence.
- [ ] Implement the start balance and complete mahadasha sequence.
- [ ] Extend to Antardasha and Pratyantardasha without assuming Vimshottari-specific ratios.
- [ ] Keep astronomy input injectable enough for deterministic reference tests.

## Task 4: Persist without conflating dasha systems

**Files:** `src/Ikiastrro.Data/`, migration if the existing period table needs a `DashaSystemId`.

- [ ] Generalize storage so Vimshottari and Ashtottari rows are independently addressable.
- [ ] Persist applicability even when no periods are emitted.
- [ ] Preserve existing Vimshottari rows and read paths.

## Task 5: Add golden verification

**Files:** tests and/or CLI `verify-ashtottari`; benchmark repositories.

- [ ] Obtain a dedicated JHora Ashtottari export/settings record for Ramakrishnan P.
- [ ] Seed its reference periods only after the convention and applicability are confirmed.
- [ ] Verify lord order, birth balance, Maha/Antar/Pratyantar boundaries and non-overlap.
- [ ] Confirm an inapplicable fixture emits evidence and no misleading timeline.

## Task 6: Present applicability and periods

**Files:** Timing page and shared dasha components.

- [ ] Show Ashtottari only with its applicability status and reason.
- [ ] Allow switching dasha systems without merging their active periods.
- [ ] Keep Vimshottari visibly identified as the general default.

## Completion evidence

- [ ] `dotnet build Ikiastrro.slnx`
- [ ] Full test suite passes.
- [ ] Database source/rule/schema verification passes.
- [ ] JHora golden comparison is stored and within the documented tolerance.
- [ ] README, architecture, calculation reference, product tracker, master index and history are updated.

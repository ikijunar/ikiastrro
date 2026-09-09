---
last_updated: 2026-09-09
togaf: Preliminary — Architecture Principles
safe: Architectural guardrails
---

# ikiastrro — architecture principles

Every design choice answers to these. A change that breaks one needs an ADR.

## 1. Provenance over convenience

Every stored calculation carries what produced it: ephemeris mode, ayanāṁśa code, house
system, varga method, rule-set id, computation timestamp. A result is only useful if its
computational history can be reconstructed.

## 2. The classical logic is ours

Only raw longitudes come from Swiss Ephemeris. Signs, nakṣatras, vargas, dignity,
relationships, dashas, karakas are original `AstroMath` — pure, dependency-free arithmetic on
a longitude. No external library's *interpretation* helpers.

## 3. Rules are data, versioned and immutable

Classical rules live in `tbl_Rule_*` tables scoped by `RuleSetId`. A rule never changes in
place — a new convention is a new `RuleSetId`. `tbl_Rule_VargaScheme` is already the live
source of truth; the rest are a verified mirror pending Phase 2.

## 4. Schema is additive

New typed columns and new tables, never reshaped ones. A divisional chart or a strength
system can land ahead of the code that reads it. `ResultJson` is a frozen audit snapshot,
never authoritative — every value has a typed column.

## 5. One analytics path for every chart type

D1 and all 20 vargas run through the same `ChartAnalyzer`. Adding a chart type is an
`IChartCalculator` pair + one `tbl_Rule_VargaScheme` row. Only genuinely continuous-degree
facts (`Nakshatra`/`NakshatraPada`, conjunction `DegreeSeparation`) are D1-gated.

## 6. Disputed conventions are chosen explicitly and recorded

Where classical texts disagree (Rahu/Ketu dignity and aspects, D2 Hora method, Gulika/Maandi
midpoint vs start), the choice is the owner's, cited in the rule row and the calculation doc —
not silently baked in.

## 7. Verification is executable

`verify-*` CLI modes + two test projects are the regression suite. A feature is not done
until its `verify-*` mode passes and the golden record `1_Ramakrishnan` still reproduces.

## 8. The engine computes; the human judges

ikiastrro assembles evidence — strengths, weaknesses, confirmations, conflicts, omissions —
with uncertainty visible. It does not predict, score, or synthesise a reading.

## 9. Two front ends, one engine

The CLI and the web app are peers over `Ikiastrro.Core` + `Ikiastrro.Data`. The UI reads
persisted rows only and never recomputes ([`domain-contracts.md`](domain-contracts.md)).

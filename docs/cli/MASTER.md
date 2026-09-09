---
last_updated: 2026-09-09
workstream: cli
togaf: C — Application Architecture (engine)
safe: Solution Intent (fixed)
---

# CLI workstream — MASTER

**Branch** `workstream/cli` · **worktree** `D:\@ClaudeSpace\ikiastrro.wt\cli` ·
**owns** `src/Ikiastrro.Core/`, `src/Ikiastrro.Cli/`, `tests/Ikiastrro.Yoga.Tests/`.

The calculation engine (`Ikiastrro.Core`), its entry + batch + verification front end
(`Ikiastrro.Cli`), and the engine test suites. Consumes the database schema; publishes
persisted rows to the UI stream ([`../architecture/domain-contracts.md`](../architecture/domain-contracts.md)).

## Docs

| Doc | For |
|---|---|
| [`calculations.md`](calculations.md) | Every calculation — convention and source: ephemeris, vargas, dignity, houses, relationships, combustion, nakṣatras, dasha, transits, Sade Sati, functional nature, avasthas, Jaimini karakas & special points, strength, yoga |
| [`commands.md`](commands.md) | The CLI command catalogue — entry, `compute-*` / `backfill-*` / `seed-*`, and every `verify-*` mode |

## Current state

- **`Ikiastrro.Core`** — 13 named engines under `Engines/<Name>/`, `ChartPipeline` /
  `ChartBundle` DB-free façade. All classical logic original; only raw longitudes from
  `SwissEphNet` (Moshier mode).
- **21 position chart types** computed and persisted per person (D1 + 20 vargas), plus
  3-level Vimśottari dasha, dignity, house lordship, conjunctions (+ groups), aspects,
  retrograde / combustion, nakṣatra linkage, Chara Karakas + special points + 11 upagrahas,
  Bālādi + Jāgradādi avasthas, Ṣaḍbala / Bhāva Bala foundation, source-attributed yoga
  inputs, slow-planet transits, Sade Sati / Kantaka / Ashtama, functional benefic/malefic.
- **Verification:** `verify-*` CLI modes + `tests/Ikiastrro.Yoga.Tests` (124) +
  `tests/Ikiastrro.Web.Tests` (166). `dotnet build` / `dotnet test` run from the terminal.
- **Green now:** all 11 `verify-*` modes — `verify-schema`, `verify-vargas`, `verify-jaimini`,
  `verify-dignity`, `verify-rules`, `verify-pipeline`, `verify-sources`, `verify-terminology`,
  `verify-avastha`, `verify-functional-nature`, `verify-upagrahas`.

## In flight

- **`FEAT-DATA-04` (with database)** — ayanāṁśa default fixed (migration 054: Jagannatha
  mode 26 → Lahiri mode 1); both saved people regenerated via `compute-all`; `verify-vargas`
  / `verify-jaimini` green. Still open: re-seed the `BENCH_RAMAKRISHNAN_P_JHORA_1981`
  benchmark case row and add a `verify-ayanamsa` mode (deferred).
- **`FEAT-STRENGTH-01`** — implement the seeded-but-uncomputed Kālabala components; planetary
  war; per-planet minimum-rūpa thresholds; reconcile Iṣṭa/Kaṣṭa/Cheṣṭā.
- **`FEAT-YOGA-01`** — Raman predicates 201–300, PVR P0 additions, structured
  missing-requirement codes, source-qualified strength policy.

## Planned

- `ChartGenerationService.GenerateAll` adopts the `ChartPipeline` bundle path.
- Reserved engine seams: Dispositor, Vimśopaka, Sthira/Naisargika Karaka, additional avasthas
  (Dīptādi / Lajjitādi / Śayanādi).
- Ashtakavarga + full Ṣaḍbala port from the vendored MIT `jyotishganit` (attribution).
- Panchanga / time layer (tithi, karana, nitya yoga, Vedic weekday, janma ghaṭis).

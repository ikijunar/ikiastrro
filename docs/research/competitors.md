---
last_updated: 2026-09-09
togaf: Preliminary — landscape scan
safe: Exploration enabler
---

# Research — competitor tools

What other Vedic-astrology tools do, and what ikiastrro borrowed. Reference clones and
screenshots live under `_research/` (git-ignored); captures under `reports/`.

## The field

| Tool | Kind | What it's good at | Relevance |
|---|---|---|---|
| **Jagannatha Hora (JHora)** | free desktop (Windows) | the de-facto completeness benchmark — panchanga, every varga, all dashas, Ashtakavarga, Shadbala, sphutas, special lagnas | the parity checklist (`docs/cli/gap-and-coverage.md`); the golden export for `verify-*` |
| **PyJHora** | Python port of JHora (AGPL) | reference for varga formulas, panchanga, Ashtakavarga | formulas transcribed with attribution (varga `chart_method=1`); not a code dependency |
| **jyotishganit** | Python (MIT), BPHS-based, test-covered | Bhinna/Sarva Ashtakavarga, all six Ṣaḍbalas, panchanga | port target for the strength + Ashtakavarga gap, with attribution |
| **VedAstro** | open-source .NET + web | broad API surface; its own chart SVGs | the v1 engine (`VedAstro.Library`) — dropped for four confirmed defects; UI is a reference only |
| **jyotish-dashboard** | web app (MIT) | closest structural analogue — chart-centric workspace | UI reference for layout |
| **AstroSage / Cosmic Insights / Prokerala** | consumer web/app | polished consumer UX; independent longitude cross-check | position cross-checks only; consumer framing is a non-goal |
| **almamesh** | web app (MIT) | North-Indian chart SVG geometry, predictive-engine plan | North-Indian chart geometry reference (style not built) |

## What ikiastrro took

- **Completeness target** from JHora — but presented as an *evidence model*, not a report.
- **Varga + panchanga formulas** from PyJHora / jyotishganit, reimplemented as original
  `AstroMath` with worked examples and `verify-*` assertions.
- **Chart-centric workspace** layout idea from jyotish-dashboard.
- **Swiss Ephemeris (Moshier)** as the precision source, same as JHora / Parashara's Light.

## What ikiastrro deliberately does not copy

Consumer "your day ahead" framing · auto-generated interpretation / prediction text · a
single blended score · closed calculation internals · North-Indian chart as the default style.

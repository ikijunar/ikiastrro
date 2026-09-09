---
last_updated: 2026-09-09
---

# ikiastrro — documentation index

One screen. Current state and what's planned — no dated history (Git and
`D:\@ClaudeSpace\ikiastrro.md` hold that). Conventions: `STANDARDS.md` §E.1 / §E.2 / §M.

## Cross-cutting (root — primary agent only)

| Doc | For |
|---|---|
| [`README.md`](README.md) | Public overview — what it is, why it exists |
| [`masterproduct.md`](masterproduct.md) | Feature & functionality register + completion, by workstream |
| [`ROADMAP.md`](ROADMAP.md) | Now / Next / Later, velocity, cadence |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | System overview — value streams, engine stack, constraints |
| [`personas.md`](personas.md) | Who ikiastrro is for |
| [`value-stream.md`](value-stream.md) | The astrologer's reading workflow every feature serves |
| [`INFRASTRUCTURE.md`](INFRASTRUCTURE.md) | Environments, DB naming, config, migration policy |
| [`docs/architecture/principles.md`](docs/architecture/principles.md) | The rules every design choice answers to |
| [`docs/architecture/domain-contracts.md`](docs/architecture/domain-contracts.md) | The DB+CLI ↔ UI interface |
| [`docs/governance/operating-model.md`](docs/governance/operating-model.md) | TOGAF↔SAFe↔Git, the two-agent rule, the weekly ritual |
| [`docs/research/sources.md`](docs/research/sources.md) | `SRC_*` citation registry (mirrors `tbl_Dim_Source`) |
| [`decisions/`](decisions/) | ADRs — one architecturally-significant decision per file |

## Value stream · DB + CLI

| Workstream | Master | Detail |
|---|---|---|
| **Database** (`workstream/database` — `db/`, `src/Ikiastrro.Data`) | [`docs/database/MASTER.md`](docs/database/MASTER.md) | `schema.md` · `rules-engine.md` |
| **CLI** (`workstream/cli` — `src/Ikiastrro.Core`, `src/Ikiastrro.Cli`) | [`docs/cli/MASTER.md`](docs/cli/MASTER.md) | `calculations.md` · `commands.md` |

## Value stream · UI

| Workstream | Master | Detail |
|---|---|---|
| **UI** (`workstream/ui` — `src/Ikiastrro.Web`) | [`docs/ui/MASTER.md`](docs/ui/MASTER.md) | `wkstream_UI_v1.md` · `brand.md` · `dataviz.md` · `design-language.md` · `components/*` |

## Research (reference library — trimmed to current relevance)

`docs/research/domain/` — classical-source rule research (dignity, graha characters,
rāśi/nakṣatra, house placement, transit events, planetary roles, PVR coverage, yoga corpus).
`docs/research/competitors.md` — what other Vedic tools do and what was borrowed.

## Artifacts (non-prose)

`docs/artifacts/` — DDL exports, engine map, rendered diagrams, golden UI snapshots,
verification evidence.

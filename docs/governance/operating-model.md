---
last_updated: 2026-09-09
togaf: G/H — Implementation Governance & Change Management
safe: Lean-Agile governance / Inspect & Adapt
---

# ikiastrro — operating model

How work is prioritised, delivered, governed, and shared between agents. Workspace policy is
`STANDARDS.md` §E.1 (workstream model), §E.2 (multi-agent control), §M (docs).

## TOGAF ↔ SAFe ↔ Git

TOGAF refines abstract **Architecture Building Blocks** into concrete **Solution Building
Blocks**; SAFe delivers those blocks incrementally; Git is the ledger.

| TOGAF | SAFe / Git artifact | Where it lives |
|---|---|---|
| Architecture Vision | Portfolio / Solution Vision | `README.md`, `personas.md`, `value-stream.md` |
| Architecture Principles | Architectural guardrails | `docs/architecture/principles.md` |
| Architecture Building Block | Rules engine, chart generation, provenance, terminology | `ARCHITECTURE.md`, `docs/database/`, `docs/cli/` |
| Solution Building Block | A migration, a C# engine, a CLI command, a Blazor component | the workstream branches |
| Requirements Repository | Feature register, GitHub issues, research gaps | `masterproduct.md`, GitHub |
| Standards Information Base | Schema / code / domain / UI standards | `STANDARDS.md`, `docs/architecture/`, `docs/ui/design-language.md` |
| Architecture Roadmap | PI-style roadmap | `ROADMAP.md` |
| Governance Log | ADRs, releases | `decisions/`, GitHub Releases |
| Solutions Landscape | Released code, DB baseline, UI, verification evidence | `master`, `docs/artifacts/` |

## State vs flow

- **State** ("what is true now") — `masterproduct.md`, the workstream `MASTER.md` files,
  `ARCHITECTURE.md`, `docs/**`. Prose, no dates, overwritten in place. Git history is the
  time machine.
- **Flow** ("what's moving, how fast") — `ROADMAP.md` (Now / Next / Later + `## Cadence`) and
  GitHub (Issues, Project board, Milestones = epics, Releases). Velocity is *derived*: rolling
  4-week merged-PR average.

No hand-maintained `CHANGELOG.md`. A ship = a `git tag` + `gh release --generate-notes` + one
`## Cadence` line.

## Delivery — pure flow

No sprints, no named increments. An opportunity is filed as a GitHub issue, ICE-scored at
triage, slotted Now / Next / Later. A Now/Next item is a **Milestone** (an epic) and a
`FEAT-<AREA>-NN` row in `masterproduct.md`. Work happens as issues on the flow board, on the
owning workstream's branch/worktree, closed along the ladder
`Planned → Designed → DB → Core → Verified → Web → Done`.

### The showcase pipeline

```
prioritised feature → architecture decision (ADR if significant) → DB/Core implementation
→ deterministic verification (verify-* / tests) → UI component → demonstrable increment
→ git tag + release notes + Cadence line
```

That trace — not commit volume — is the product-management record.

### Weekly ritual (~15 min)

1. **GitHub Project** — move issues across `Later → Next → Now → In progress → In review → Done`.
2. **`masterproduct.md`** — tick checklist boxes for merged work; recompute affected rollup rows.
3. **`ROADMAP.md`** — move items between Now / Next / Later; if a ship happened, add a
   `## Cadence` line + `git tag` + `gh release`.
4. **workstream `MASTER.md`** — refresh the 2–3-line "in flight now" list.

Nothing else. No per-feature dated file.

## Definition of shippable

- `dotnet build Ikiastrro.slnx` clean (0 warnings / 0 errors).
- The feature's `verify-*` mode and the two test projects green.
- `masterproduct.md` boxes ticked, rollup recomputed.
- Affected current-state docs updated in the same PR.
- Golden record `1_Ramakrishnan` fact checklist re-confirmed after an engine/UI change.

## Two-agent control (`STANDARDS.md` §E.2)

- **Claude Code = primary.** Owns `master`, all cross-cutting/root files, integration, releases.
- **Codex = contributor.** Works inside one assigned workstream worktree + its path scope, or
  advises (read + propose, no writes). Never edits shared files or another workstream's paths.
- **Handoff is in the repo** — `git status` + a handoff note (changed files, validation run and
  result, remaining work, next action). Never agent memory. One product history.
- Before writing, an agent inspects `git status` and the active handoff and reconciles stale
  statements from evidence.

## External review

The optional Codex / ChatGPT advisory pass: a review is produced against the current diff,
triaged into issues or ADRs, and either applied on a workstream branch or recorded as
`won't-do` with a reason. It is advisory — it does not gate a merge.

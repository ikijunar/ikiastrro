# 001. Star schema + data-driven rules engine

**Date:** 2026-08-30   **Status:** Accepted (Phase 1 shipped; Phase 2 not started)

## Context

Classical Parashari rules (aspect offsets, combustion orbs, planetary friendship, dignity)
were hardcoded as dictionaries in C# (`ClassicalRelationships.cs`, `ClassicalCombustion.cs`,
`ClassicalDignity.cs`). Changing a rule meant a code change and a redeploy, and there was no
way to change one without silently altering charts that had already been computed under the
old rule.

## Decision

Restructure the database as a star schema with three table categories:

- **`tbl_Dim_*`** — fixed classical vocabulary (Planets, SignAttributes, Nakshatras).
- **`tbl_Rule_*`** — the parameterised classical logic, each row scoped to a `RuleSetId` FK
  into `tbl_Rule_Sets`. A rule change is a new `RuleSetId` (new rows), never an edit in place.
- **`tbl_Fact_*`** — per-chart computed results, each row recording the `RuleSetId` it was
  computed under.

This is formalised workspace-wide in `STANDARDS.md` §D.1 (naming infix, mandatory `RuleSetId`
FK, immutability rule = SCD Type 2).

## Consequences

- A rule change no longer requires a code deploy, and never reinterprets an already-computed
  chart — each Fact row stays readable under the `RuleSetId` that produced it.
- Cost: an extra join layer, and the calculators must eventually read from these tables
  instead of their hardcoded dictionaries.
- **Phase 1 (shipped, migrations 024–028):** `tbl_Rule_Sets` + four rule tables, C# values
  transcribed verbatim, 4 Dapper repos, CLI `list-rule-sets` / `show-rules <id>`.
- **Phase 2 (not started):** rename `tbl_Chart_*` → `tbl_Fact_*` and wire the calculators to
  read the rule tables. Today's app still runs on the hardcoded C#, behaviour unchanged.

Full implementation plan and record: [`../docs/dbdesign-star-schema-rules-engine.md`](../docs/dbdesign-star-schema-rules-engine.md).

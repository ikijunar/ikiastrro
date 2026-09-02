# <Feature> Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** <one sentence>
**Architecture:** <2–3 sentences>
**Tech Stack:** <key tech>
**Spec:** <path>

## Research
Status: [ ] complete   [ ] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|

## Global Constraints
- Branch: <branch>
- Commit trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: <url>
  ```
- Do not push unless asked.
- Migrations: numbered `NN_*.sql`, idempotent, self-recording; last applied is `NN`.
- No test project — verify via `dotnet build` + CLI `verify-*` modes.

---

## Task N: <name>
**Files:** Create / Modify / Test
**Interfaces:** Consumes / Produces
- [ ] Step 1: …
- [ ] Step N: Commit

## PRODUCT.md
On completion, tick: FEAT-<AREA>-<NN> → [DB|Core|Verify|Web|Docs] boxes done.

<!--
Title: prefer  "<FEAT-AREA-NN>: <what changed>"  or  "fix: <what>" for fix-only pushes.
A release is a push to origin/master tagged vX.Y.Z (SemVer, pre-1.0). See CHANGELOG.md.
-->

## What & why

<!-- One paragraph. Link the issue: Closes #NN -->

## Feature rows advanced

<!-- List each PRODUCT.md row this PR moves, with the status-ladder transition. -->

| `FEAT-<AREA>-<NN>` | Ladder move | New % |
|---|---|---|
| FEAT-… | Designed → Core | 60% |

## Checklist

- [ ] `PRODUCT.md` — feature row(s) and the **Rollup** table updated (manual mirror)
- [ ] `CHANGELOG.md` — entry added under `## [Unreleased]`, grouped by `FEAT-<AREA>-<NN>`
- [ ] Design spec / plan linked (`docs/superpowers/…`) or noted as `(pre-spec)`
- [ ] ADR added under `decisions/NNN-*.md` if an architectural choice was made

### Database (if touched)
- [ ] New migration script added, applied idempotently, and recorded in `dbo.SchemaMigrations`
- [ ] `db/ikiastrro.sql` baseline regenerated if applicable
- [ ] `INFRASTRUCTURE.md` migration policy followed (dev → stage → uat → prod)

### Verification
- [ ] `verify-*` / `precheck-*` run and passing — name it: `______`
- [ ] New invariants added for new logic; reference case(s) included
- [ ] For calculation changes: expected vs. computed vs. reference authority, with tolerance, in the PR body

### Classical / astronomical logic (if touched)
- [ ] Source or tradition documented, with `SRC_*` citation (`STANDARDS.md §M.4`)
- [ ] Method / formula stated; conventions (ayanamsa, node type, house system, edition) called out
- [ ] Unsourced cells seeded NULL / flagged, not guessed

### Docs & UI
- [ ] Reference/scope docs updated; `master_ikiastrro.md` index updated if a doc was added
- [ ] Web (Blazor) components updated, or `Web [ ]` left unchecked deliberately

## Reference / verification evidence

<!-- Paste verify-script output, or the expected-vs-reference comparison table. -->

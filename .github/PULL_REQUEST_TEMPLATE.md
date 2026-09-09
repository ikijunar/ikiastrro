<!--
Title: prefer  "<FEAT-AREA-NN>: <what changed>"  or  "fix: <what>" for fix-only pushes.
A ship is a git tag vX.Y.Z + a GitHub Release (auto notes). See STANDARDS.md §E.1.
-->

## What & why

<!-- One paragraph. Link the issue: Closes #NN -->

**Workstream:** database | cli | ui | cross-cutting

## Feature rows advanced

<!-- List each masterproduct.md row this PR moves, with the status-ladder transition. -->

| `FEAT-<AREA>-<NN>` | Ladder move | New % |
|---|---|---|
| FEAT-… | Designed → Core | 60% |

## Checklist

- [ ] Stayed inside this workstream's path scope (`STANDARDS.md` §E.1)
- [ ] `masterproduct.md` — feature row(s) and the **Rollup** table updated (manual mirror)
- [ ] Feature detail lives in the owning workstream's docs (no dated plan/spec file)
- [ ] ADR added under `decisions/NNN-*.md` if an architectural choice was made

### Database (if touched)
- [ ] New migration script added, applied idempotently, recorded in `dbo.SchemaMigrations`
- [ ] `db/ikiastrro.sql` baseline regenerated if applicable
- [ ] `INFRASTRUCTURE.md` migration policy followed (dev → stage → uat → prod)

### Verification
- [ ] `verify-*` / `precheck-*` run and passing — name it: `______`
- [ ] New invariants added for new logic; reference case(s) included
- [ ] For calculation changes: expected vs. computed vs. reference authority, with tolerance, in the PR body

### Classical / astronomical logic (if touched)
- [ ] Source or tradition documented, with `SRC_*` citation (`STANDARDS.md` §M.4)
- [ ] Method / formula stated; conventions (ayanāṁśa, node type, house system, edition) called out
- [ ] Unsourced cells seeded NULL / flagged, not guessed

### Docs & UI
- [ ] `docs/cli/` or `docs/database/` or `docs/ui/` updated; `MASTER.md` index updated if a doc was added
- [ ] Web (Blazor) components updated, or `Web [ ]` left unchecked deliberately

## Reference / verification evidence

<!-- Paste verify-script output, or the expected-vs-reference comparison table. -->

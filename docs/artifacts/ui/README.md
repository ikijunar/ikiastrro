# docs/artifacts/ui — chart golden snapshots

One rendered SVG per visual chart component, refreshed every release:

```
<Component>-sample.svg      e.g. PolarWheel-sample.svg
```

Purpose and the revert workflow: `docs/ui/dataviz.md` and `docs/ui/components/chart-catalog.md`.
Component list and contracts: `src/Ikiastrro.Web/Components/Charts/README.md`.

## The fixture

All snapshots render from **one fixed birth chart, defined once and never
changed** — otherwise `v0.5.3` and `v0.10.4` snapshots can't be compared. Proposed
fixture: the `rammyps` reference chart already used by the `verify-*` CLI checks
and `docs/artifacts/reference-charts/`. Lock the exact `BirthDetails` in whichever
harness is chosen below.

## Rules

- Regenerate **all** snapshots whenever a chart component, its `.razor.css`, a
  shared geometry helper, or a `--*` token it reads changes. Commit the diff.
- A snapshot diff in a PR = a deliberate visual change. Note it in the PR body against the
  relevant `FEAT-…` row and in the release notes.
- Never hand-edit a snapshot. It is output.

## Harness — `tests/Ikiastrro.Web.Tests` (bUnit)

The repo's first xUnit project. `ChartSnapshotTests` renders each visual component
with `ChartFixture` (one fixed synthetic Aries-Lagna chart, hand-built so engine
changes don't move it) and `SnapshotAssert.MatchesGolden(name)` writes / compares
`<name>-sample.svg` here. Scope-hash attributes and whitespace are normalised out,
so `tokens.css` colour edits don't churn snapshots — only markup structure does.

### Running

- **Run** from `dotnet test` or Visual Studio Test Explorer. `dotnet build Ikiastrro.slnx` is
  the compile gate.
- **Mint / update the baseline:** set env `IKIASTRRO_UPDATE_SNAPSHOTS=1`, run the tests once,
  review the generated `*-sample.svg`, commit them.
- **On a failure:** the actual markup is written as `*-sample.received.svg` beside the golden
  for diffing (gitignored). If the change is intended, re-run with the flag and commit; note
  the visual change against its `FEAT-…` row in the PR body.

### Adding a component

Add a `[Fact]` to `ChartSnapshotTests`, extend `ChartFixture` only if it needs
new input, run with the update flag, commit the new golden + a catalog row in
`src/Ikiastrro.Web/Components/Charts/README.md`.

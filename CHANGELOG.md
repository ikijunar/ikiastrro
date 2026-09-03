# Changelog

All notable changes to **ikiastrro**. One entry per release; a **release is a push
to `origin/master` tagged `vX.Y.Z`**. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions are
[SemVer](https://semver.org/) (pre-1.0 — bump *minor* for a feature batch, *patch*
for a fix-only push).

Group each entry by the `FEAT-<AREA>-<NN>` rows it advances (see `PRODUCT.md`),
then by change type. Every feature line links its design spec, the DB migrations it
added, and the UI components it touched. Draft with
`scripts/release.ps1 -Version vX.Y.Z`, edit for readability, then tag.

## [Unreleased]

_Changes on `master` since `v0.1.0`. Run `scripts/release.ps1` to consolidate._

### Added
### Changed
### Fixed
### DB
### UI

## [0.1.0] - 2026-09-04

Baseline — first tagged release; everything built before changelog discipline
began. Earlier detail lives in `git log --first-parent` and the frozen design
specs under `docs/superpowers/specs/`.

### Engine (`Ikiastrro.Core.Engines.*`)
Astronomy, divisional charts (D1–D60 across 21 varga types), houses, nakshatras,
dignity, Jaimini chara karakas + special points, planetary-state (avastha),
Vimshottari dasha. DB-free `ChartPipeline.Run(BirthDetails)`.

### DB
Schema baseline `db/ikiastrro.sql`; migrations `00`–`21` (schema normalization,
divisional completion, Jaimini + `PointKind`, project foundations + `tbl_Dim_Source`,
engine reorg / terminology / rule portability, sign-attribute classifications).
Ledger: `dbo.SchemaMigrations` keyed by `ScriptName`. Pre-normalization flat
history archived under `db/_archive/`.

### Web (`Ikiastrro.Web`, Blazor Server)
Varga-centric workspace: `Workspace`, `VargaView`, `Timing`, `PrintChart`,
`SavedCharts`, `Home`. Hand-rolled SVG/CSS chart components — `PolarWheel`,
`SouthIndianGrid`, `MiniGrid`, `ChartFrame`, `VargottamaStrip`, `CombinedD1D9Grid`
(catalog: `src/Ikiastrro.Web/Components/Charts/README.md`). Dark-only; tokens in
`wwwroot/css/tokens.css`. No component library, no CSS framework.

### CLI (`Ikiastrro.Cli`)
`verify-*` checks (schema, vargas, pipeline, jaimini, avastha, terminology, rules,
sources, functional-nature); `seed-*` and `backfill-*` commands.

[Unreleased]: https://github.com/rammyps/ikiastrro/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/rammyps/ikiastrro/releases/tag/v0.1.0

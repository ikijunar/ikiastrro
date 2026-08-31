# Why ikiastrro Is Built the Way It Is

**Status:** living
**First written:** 2026-08-31 (consolidated from `ARCHITECTURE.md`, `../ikiastrro.md` history, and the 2026-08-31 stack review)

This doc records the **reasoning** behind the technology choices. `docs/techstack-overview.md`
and `docs/techstack-details.md` describe the stack *factually* (what, versions, layout);
this one says *why*, and *when each choice should be revisited*.

---

## Why build at all — not just use Jagannatha Hora / PyJHora

Jagannatha Hora (JHora) is a mature, precise, feature-complete freeware desktop tool
(Java + Swing, Swiss Ephemeris with full `.se1` data files, per-chart `.jhd` **flat
files**, Windows-only, single-user, closed source). PyJHora is the author's separate
Python reimplementation (AGPL, `pyswisseph` + PyQt6).

Neither can do the thing ikiastrro exists for: **store every chart's facts in a
queryable relational database and search across people** — e.g. "find charts whose
attributes resemble a known personality". JHora's flat files would have to be parsed
one by one; there is no join, no aggregate, no bulk analysis. PyJHora is AGPL and
UI-coupled. So ikiastrro re-computes the classical maths (verified against both) into
a schema built for cross-chart analysis.

**Cost of this choice:** re-deriving and re-verifying every varga / dasha / bala
formula that PVR already solved over ~20 years, solo. Mitigated by using PyJHora
(vendored, `_research/`) as a read-only reference and the JHora export as a
feature-parity checklist.

---

## .NET 8 / C#, 4-project solution (Core / Data / Cli / Web)

- **Strong typing + performance + cross-platform runtime.** The classical logic
  (dignity, aspects, varga rules) is intricate; a typed language catches whole
  classes of error the astrology domain is prone to (sign off-by-one, odd/even
  reversal, node sign).
- **Core / Data / Cli / Web split** so one calculation library feeds every surface.
  The CLI is the fast iteration + verification harness (`verify-*` modes); the Web
  is the human surface; a future REST API reuses Core unchanged. JHora, by
  contrast, is one fused binary.
- **Revisit if:** the team ever standardises on a single language end to end
  (see the Python note below) — then Core could move too. Not on the table now.

---

## Calculation engine: Swiss Ephemeris via SwissEphNet (Moshier mode)

v1 embedded `VedAstro.Library`. Four confirmed defects forced it out (2026-08-24):
a `CacheManager` version-fragility crash that was a **structural** blocker inside the
Blazor host (ASP.NET Core forces a newer `Caching.Memory` than VedAstro tolerates),
a Whole-Sign house-cusp bug, a wrong Ketu longitude, and a systematic ~1.456°
ayanamsha error needing an in-code correction layer.

**SwissEphNet** — a managed C# port of Astrodienst's Swiss Ephemeris (the same
source behind JHora, Parashara's Light, Jyotish Dashboard) — replaced it. Sidereal
Lahiri longitudes come directly from `SEFLG_SIDEREAL | SE_SIDM_LAHIRI` (no
correction layer). Verified on the golden chart to ~0.4–1.0 arcmin of Prokerala /
AstroSage.

**Why Moshier mode (`SEFLG_MOSEPH`), not the `.se1` data files:** zero deployment
friction — no ephemeris files to bundle or path-configure. Accuracy is ~1 arcsecond
near the present.

**Ceilings / when to revisit:**
- **Precision.** Moshier degrades for the Moon and for dates far from ~2000
  (pre-1800, distant future). Invisible for 30°-wide signs/nakshatras; can flip an
  edge case for exact Vargottama boundaries, precise dasha balance at birth, or
  historical charts. *Fix without changing stacks:* SwissEphNet can load `.se1`
  files — switch the flag, ship the data. Worth doing before leaning on
  varga-degree precision.
- **Licensing.** SwissEphNet 2.8.0.2 carries Astrodienst's dual licence:
  **GPL-2.0-or-later** *or* the paid **Swiss Ephemeris Professional Licence**
  (one-time, ~CHF 750 / ~$800 per major version). GPL grants are irrevocable for
  released versions, so the version in use is free forever; "becomes paid" is not a
  real risk for what is already pinned. SwissEphNet itself is frozen (last release
  2019), so there is effectively no future version to change terms. The **real**
  obligation is copyleft: distributing the software or activating a public service
  triggers "open the whole project or buy the Professional Licence". ikiastrro's
  repos are **already public**, so GPL is ~satisfied — pending a `LICENSE` /
  `NOTICE` file (AGPL-3.0 chosen, consistent with the PyJHora references) and
  preserved Swiss Ephemeris copyright notices. Budget the ~$800 one-time cost
  *only if* ikiastrro is ever closed-sourced. Zero-copyleft escape hatch: swap the
  engine for a permissive one (`CosineKitty/AstronomyEngine` MIT, or a
  public-domain VSOP87/ELP2000 port) — real porting work, only if closed
  distribution becomes a firm goal.

---

## Database: MS SQL Server + Dapper

- **A relational store is the whole point** (see the JHora comparison above). The
  personality-comparison feature is a `JOIN` / aggregate problem; flat files can't
  serve it.
- **Dapper** (micro-ORM), not EF: the schema is hand-designed and migration-driven;
  Dapper keeps SQL explicit and the mapping thin, which suits a schema this
  deliberate.
- **Star-schema discipline** (`tbl_Dim_*` / `tbl_Rule_*` / `tbl_Fact_*`, STANDARDS
  §D.1): reference vocabularies and computation *rules* are versioned data
  (`RuleSetId`), not hard-coded constants — so a rule change is a new row-set, never
  an edit to rows a stored fact already references. Design: `docs/dbdesign-star-schema-rules-engine.md`.
- **Chart-type-generic fact tables**: `tbl_Chart_KeyDetails` / `HouseLords` /
  `Conjunctions` / `Aspects` / `tbl_Fact_PlanetAvastha` hold one row per
  `(person × chart-type × planet)` with integer FKs. Adding a divisional chart is
  **additive rows**, never a schema change. This is what lets "add the full
  Shodashavarga set" be a bounded task and what shapes the tables to feed the
  comparison engine.
- **DB-completeness rule** (from 2026-08-31): every computed quantity has a typed
  column; `ChartResult.ResultJson` is a frozen audit snapshot, never the source of
  truth.

**Ceilings / when to revisit:**
- SQL Server is heavy for a single-user analytical tool — Express caps (10 GB/db,
  1 socket, ~1.4 GB RAM), Windows-leaning, licensing cost at scale. **SQLite** or
  **Postgres** would be lighter and more portable. Fine now (local dev); a
  constraint if ikiastrro ships as a product or moves to the cloud. Migrating now
  costs more than it saves.

---

## Web: Blazor Server

- Same Core library, C# top to bottom, native chart rendering (Razor + CSS-isolated
  components — the South/North Indian grids are hand-rolled SVG), no JS build.
- Browser-accessible, no install (vs. JHora's desktop-only).

**Ceilings / when to revisit:**
- Stateful SignalR socket per user; needs an always-on server; latency-sensitive;
  weak offline story; does not scale cheaply to many concurrent users. Fine for
  personal / small use. If it becomes a product: **Blazor WASM** or a **JS SPA +
  REST API** — but that is a UI rewrite.

---

## Data visualization: Syncfusion Blazor (Community Licence)

The one sanctioned component library, and only for strength bars / heatmaps / Dasha
timelines / a polar longitude wheel. Chart *diagrams* stay hand-rolled SVG.
Decision 2026-08-31, `docs/uidesign-dataviz.md`; not yet wired into `src/`.
Community Licence is free under a revenue/headcount threshold — revisit if that
threshold is ever approached.

---

## Place resolution: OpenStreetMap Nominatim + offline tz

City/Country → lat/long via Nominatim (free, no key); UTC offset then resolved
**offline** from lat/long + birth date (`GeoTimeZone` + `TimeZoneConverter`), so
historical DST rules are respected. Replaced VedAstro's 4-provider chain that
silently returned `(0,0)` on total failure.

---

## Planned: a Python layer for personality comparison

The cross-chart similarity engine (`tbl_KnownPersonality` corpus, per-person
attribute vectors, a similarity model) is a **separate effort**, and the current
intent is to build it in **Python** (pandas / numpy / scikit) reading SQL Server —
the DB is a neutral integration boundary, no FFI.

**Trade-off being accepted:** two runtimes, two dependency ecosystems, two things to
deploy. The single-stack alternative is ML.NET in C#. The decision is deferred to
that effort's own brainstorm; the fact schema is being shaped to feed either
(integer FKs, one row per attribute, every value a column).

---

## Testing: `verify-*` CLI modes, no xUnit project

Verification today is `dotnet build` + CLI `verify-schema` / `verify-avastha` /
`verify-vargas` / `verify-functional-nature` (worked-example assertions) + a live
Web smoke + golden-record (`1_Ramakrishnan`) checks.

**Ceiling / when to revisit:** this does not scale to the feature volume planned
(full Shodashavarga, Ashtakavarga, Shadbala, more dashas). A real test project is
the highest-leverage gap to close before those batches. Flagged, not yet acted on.

---

## Summary — revisit triggers

| Choice | Revisit when |
|---|---|
| SwissEphNet Moshier mode | varga-degree precision or historical charts matter → switch to `.se1` |
| SwissEphNet / Swiss Ephemeris licence | ikiastrro is ever closed-sourced → buy Professional Licence (~$800) or swap to a permissive engine |
| SQL Server | ships as a product / goes cloud → SQLite or Postgres |
| Blazor Server | many concurrent users → Blazor WASM or SPA + API |
| `verify-*` only | before the next large calculation batch → add a test project |
| Python for comparison | that effort's brainstorm → confirm vs. ML.NET single-stack |

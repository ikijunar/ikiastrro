# Sign benefic/malefic — dignity color is not the answer

Research note capturing rammyps's question (2026-09-18): does the planet dignity color shown
in the South Indian detailed chart (and, by extension, every chart style) tell you whether a
*sign* is benefic or malefic? No — dignity, naisargika karaka, and benefic/malefic are three
separate axes in this domain, and sign-level benefic/malefic is not something the app computes
today. This note is a synthesis, not a book extract — no `SRC_*` citation code, unlike
[[dignity-pvr]] and [[naisargika-karaka-pvr]]. Flag for a source pass before any schema/engine
work leans on it.

## Three axes, not one

**2026-09-23 update:** a source pass found grounding for the proposed synthesis below — B.V.
Raman's *How to Judge a Horoscope*, "Considerations in Judging a House" (p.14-15,
`SRC_RAMAN_HTJH`), whose 8-point checklist's points 1/3/5 map onto 3 of the 4 proposed steps
(lord's condition; occupants + aspects; dignity). Stated as qualitative weighing, not an
enumerable rule. **Decided (rammyps): proceed, cited to that passage** — `FEAT-HOUSE-06`.

1. **Dignity** (`DignityStatus` — Exalted/Moolatrikona/Own/…/Debilitated, axis A in
   [[dignity-pvr]]) — positional *strength*, driven by planet + sign + degree. This is what the
   South Indian grid's color coding renders (`SindUniDtlGrid`, spec at
   `docs/ui/components/spec_SIND-UNI-DTL.md`), and it's chart-style-agnostic — the
   same `DignityStatus` value would color North/East Indian layouts too. It says nothing about
   good/bad.
2. **Naisargika karaka** ([[naisargika-karaka-pvr]]) — what a planet *signifies* (Sun=soul,
   Jupiter=children, …). Orthogonal to both dignity and benefic/malefic.
3. **Benefic/malefic (subha/papa)** — splits again:
   - **Naisargika (natural)** — fixed, chart-independent: Jupiter/Venus always benefic; Sun/
     Mars/Saturn always malefic; Mercury conditional (benefic unless afflicted); Moon
     conditional (benefic when waxing). `reference-data-tables.md`'s proposed
     `RulingPlanetNature` column captures exactly this split but **is not built** — no such
     column in `db/ikiastrro.sql`, no code reference outside that doc. The two natural-benefic
     sets that *are* live in code drop the conditionals and agree with each other:
     `LagnaFunctionalNature.cs:14-15` and `BhavaBalaCalculator.cs:83` both hard-code
     `{Moon, Mercury, Jupiter, Venus}` as benefic with no waxing/combustion check.
   - **Functional (Lagna-relative)** — chart-specific, driven by house lordship (kendra/trikona
     = benefic-leaning, dusthana/3-6-11 = malefic-leaning, kendradhipati dosha demotes a
     natural benefic that owns only an angle). **This is already built**, planet-level, in
     `Core/Calculators/LagnaFunctionalNature.cs` (superseding the removed migration-031 seed
     table — see `house-lagna-significations.md`). It answers "is *this planet* functionally
     benefic for *this* Lagna," not "is this sign benefic."

Dignity modulates axis 3's *intensity*, it doesn't create it — a strong (exalted) functional
malefic still acts as a malefic, just more capably (can even produce Neecha-Bhanga-style
reversals for a debilitated planet); dignity color is an input to the synthesis below, never
the verdict.

## What determines a sign's (house's) benefic/malefic nature — built 2026-09-23

**Built 2026-09-23:** `HouseBeneficMaleficCalculator` (`Core/Engines/Houses`, `workstream/cli`)
implements steps 1–4 below. Live-only, no persisted table — same "hardcode + cite" pattern as
`LagnaFunctionalNature` itself (which since 2026-10-12 reads PVR Table 30, `SRC_PVR_INTEGRATED` p.167, instead of the Raman heuristic); this house-level count is still cited to `SRC_RAMAN_HTJH` p.14-15. Step 3
(aspects) reuses `RelationshipEngine`'s own `AspectOffsets` table via a new public
`AspectsSign` helper rather than a separate implementation, so an aspect onto an *empty* house
(no occupant, so no `tbl_Chart_Aspects`/`AspectingPlanets` row) is still detected. Verdict is
Benefic/Malefic/Mixed/Neutral by equal-weight count of (lord + occupants + aspectors), matching
the "no blended score" decision in Open Questions below; dignity/combustion is exposed
separately (`LordDignityStatus`), never folded into the count. `verify-house-benefic-malefic`
(CLI) checks house/sign/lord consistency, real occupancy, no double-counting, and that Verdict
follows deterministically from the result's own lists — ALL PASS on every D1 chart on file.
Web rendering not built this session.

### Original proposed method (superseded by the above; kept for the reasoning trail)

No engine or table in this repo currently derives a sign-level or house-level benefic/malefic
verdict. `LagnaFunctionalNature` stops at the planet. Synthesizing one would combine, roughly
in order of weight:

1. **Functional nature of the sign lord** — `LagnaFunctionalNature.For(lagnaSign, lord)`,
   already available, is the dominant input.
2. **Occupants of the sign** — benefic occupants (by axis 3, functional-first) help; malefic
   occupants stress it. `d1-rasi.md:64` ("Benefics and malefics by house") already reads this
   direction for house-by-house judgment text; no reusable occupant-scan helper exists yet.
3. **Aspects (graha drishti) on the sign** — Jupiter's aspect auspicious, Saturn/Mars/nodes
   afflicting. `tbl_Chart_Aspects` holds the raw data; no benefic/malefic weighting layer
   consumes it for this purpose today.
4. **Strength/condition of the sign lord** — `DignityStatus` + `IsCombust` feed in here as
   *modifiers* on how cleanly the lord's functional nature expresses, not as the nature itself.
5. Secondary/Jaimini-layer refinements (argala, odd/even, movable/fixed/dual) — not scoped
   anywhere in this repo; lowest priority if this is ever built.

`planetary-roles-avastha.md:84` already flags a **different, narrower** gap — "no natural
benefic/malefic classifier as a reusable unit," needed for Deeptadi/Lajjitadi avasthas (i.e.
axis 3's *natural* half, with the waxing-Moon/afflicted-Mercury conditionals reinstated). That
gap and this one (a sign/house-level synthesis across all of steps 1–4) are related but
distinct — closing the Deeptadi gap does not by itself produce a sign benefic/malefic verdict.

## Open questions

- ~~Does "sign benefic/malefic" mean the *rashi* in isolation, or always the *bhava* (house) it
  occupies for a given Lagna?~~ **Resolved 2026-09-23:** always the bhava, per-Lagna —
  `HouseBeneficMaleficCalculator.ComputeAll(ascendantSign, grahas)` takes the Lagna as a
  required input; a sign has no benefic/malefic nature on its own outside a chart.
- ~~Score/weight model, if any~~ **Resolved 2026-09-23:** followed `dignity-pvr.md`'s
  `DignityScore`/`RelationshipScore` precedent — raw ordinal factors (lord nature, occupant
  list, aspector list) kept separate on the result record; `Verdict` is an equal-weight count
  of those factors, not a blended number, and `LordDignityStatus` is exposed but never counted.
- Whether `RulingPlanetNature`'s conditional cases (waxing Moon, unafflicted Mercury) get
  reinstated here, in the Deeptadi classifier, or in both — **still open**, deliberately out of
  scope for this build to avoid a third divergent hard-coded set. `HouseBeneficMaleficCalculator`
  classifies occupants/aspectors by `LagnaFunctionalNature` (functional axis) for the 7
  classical grahas and the natural-malefic convention for Rahu/Ketu, same as everywhere else in
  this codebase.

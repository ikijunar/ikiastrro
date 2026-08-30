# ikiastrro — Calculations Reference

Every astrological calculation ikiastrro performs, its convention, and its source. Living
document. Companions: `ikiastrro_techspecs.md`, `ikiastrro_uidesignspecs.md`,
`docs/vedic-reference-tables.md`, `docs/star-schema-rules-engine.md`,
`docs/house-lagna-significations.md`, `docs/bhava-coverage.md`, `key_comp_astro.md` (feature
gap vs. Jagannatha Hora).

All calculation code is in **`Ikiastrro.Core`** and takes no dependency on any external
engine's *relationship* methods — only raw longitudes come from the ephemeris; the classical
logic is original.

---

## 1. Ephemeris & frame

- **Engine:** `SwissEphNet` 2.8.0.2 (`SwissEphemerisProvider.cs`), **Moshier analytical
  mode** — no ephemeris data files.
- **Sidereal / ayanamsha:** **Lahiri**, obtained directly via `SEFLG_SIDEREAL` +
  `SE_SIDM_LAHIRI`. The provider returns sidereal (nirayana) longitudes; there is **no
  manual ayanamsha-correction layer** (the VedAstro-era correction, which was ~1.45° short,
  is gone).
- **Nodes:** **Rahu = mean node** (`SE_MEAN_NODE`) — verified closer to Prokerala / AstroSage
  than the true node. **Ketu = Rahu + 180°** (derived, never fetched or stored separately;
  DB view `vw_KetuSignTransitEvents` = Rahu events + 6 signs).
- **Per-planet motion speed** (`xx[3]`, deg/day) and **ecliptic latitude** (`xx[1]`, deg)
  come from the same `swe_calc_ut` call (`SEFLG_SPEED`, no extra cost) →
  `SiderealPositions.PlanetSpeeds` / `.PlanetLatitudes`. Speed drives retrograde and
  combustion-orb selection; both are now also **persisted** on `tbl_Chart_KeyDetails`
  (`SpeedLongitudeDegPerDay`, `EclipticLatitudeDegrees`) for D1 and every varga — real-body
  values, so identical across chart types, like `IsRetrograde`. Ketu takes Rahu's speed sign
  and the opposite-signed latitude (mean node sits on the ecliptic, so ≈ 0).
- **House system:** **Whole Sign** everywhere. House *n* from any reference sign =
  `AstroMath.CountFromSignToSign(referenceSign, targetSign)` (1-based, the sign that holds the
  reference point is house 1).
- **Time:** birth local wall-clock + place. Lat/long via Nominatim; UTC offset resolved
  offline from lat/long + date (historical DST respected). Stored `DATETIME2`, no offset
  column — local time of day is what's kept, matching how `tbl_BirthDetails` stores it.

`ZodiacName` enum keeps VedAstro's spelling (`Capricornus`, Latin form) so pre-swap stored
data stays compatible.

---

## 2. Divisional charts (vargas)

Registered in `ChartCalculationOrchestrator.CreateDefault()` in numeric order:
**D1, D2, D6, D9, D10, D11.** Each is an `IChartCalculator` split into
`ComputeAnalysisInput` + `BuildResult`, so a new varga registered here gets the full
analytics treatment (dignity / house-lordship / conjunctions / aspects) for free via the
shared `ChartAnalyzer`.

| Chart | Name | Signification | Formula source |
|---|---|---|---|
| **D1** | Rasi | the birth chart; everything is read against it | real sidereal longitude |
| **D2** | Hora | wealth | **classical two-sign Leo/Cancer** split (not PyJHora's 12-sign default) |
| **D6** | Shashtamsa | health, affliction | PyJHora `chart_method=1`, transcribed verbatim |
| **D9** | Navamsa | marriage, dharma, inner strength | PyJHora `chart_method=1` |
| **D10** | Dasamsa | career, status | PyJHora `chart_method=1` |
| **D11** | Rudramsa | gains, income | Sanjay Rath method (the one method-ambiguous varga — a one-line switch to Raman's if needed) |

- A varga cell is a **discrete bucket**, not a 30° span: `DegreesInSignDisplay`,
  `Nakshatra`, `NakshatraPada` are **D1-only** (null for every other chart). The grid keys
  off that (no fabricated `0°00'`).
- `NakshatraLordPlanet`, `IsRetrograde`, `IsCombust`, `DistanceFromSunDegrees`,
  `CombustionOrbUsedDegrees`, `NakshatraSubLordPlanet` (KP L2) **are populated for every
  chart type** — all derived from the shared real longitude, so a planet's D9 value equals
  its D1 value.
- Verification: `verify-vargas` (26 worked-example checks), plus historical cross-checks —
  40/40 hand-check vs. D1 longitudes, external D2 10/10 and D6 10/10 vs. astro-seek.
- Out of scope: D3/D7/D12/D30/D60 and the rest of the Shodashavarga (see `key_comp_astro.md`).

---

## 3. Classical dignity (`ClassicalDignity.cs`)

Pure classical-rule lookups, no external engine. Dignity status ∈ **Exalted · Moolatrikona ·
Own Sign · Great Friend · Friend · Neutral · Enemy · Great Enemy · Debilitated** (the
`--dignity-*` 7-colour ramp collapses Own/Moolatrikona/Great Friend into one "good" token
for the UI).

- **Panchadha Maitri** (five-fold friendship) = natural (permanent) friendship + temporary
  (from-Moon-position) friendship. Sign-dignity only — this is *not* combustion/affliction.
- **Rahu/Ketu** use the **Parashari exaltation/debilitation convention** — rammyps's explicit
  choice (classical texts disagree).
- Exaltation/debilitation degrees, Moolatrikona ranges, own signs: in code, cross-checked
  against `tbl_SignAttributes` before that table was seeded (caught a 5-row debilitation bug
  and a Cancer animal-type error pre-seed).

---

## 4. House lordship, conjunctions, aspects (`ClassicalRelationships.cs`, `ChartAnalyzer.cs`)

- **House lordship:** for each house, its sign's ruler, and where that ruler sits — the
  "Lord of the Nth house is placed in H_m" reading. Counted from Lagna, and also from Sun and
  from Moon (`HouseNumberFromLagna` / `FromSun` / `FromMoon`).
- **Conjunctions:** planets sharing a sign; `DegreeSeparation` nullable (only meaningful in
  D1). Present in the DB; the workspace UI does not surface them yet (per rammyps's call).
- **Aspects (Drishti):** full-sign classical aspects.
  - All planets aspect the **7th** from themselves (shown unnumbered: `Ma(a)`).
  - **Special aspects** (shown numbered: `Ma(a)-8`): Mars 4th & 8th, Jupiter 5th & 9th,
    Saturn 3rd & 10th.
  - **Rahu/Ketu** use the **Jupiter-style 5th/7th/9th** aspect convention — rammyps's
    explicit choice.
  - `ChartViewModel.BuildAspectedByGlyphs` reduces `tbl_Chart_Aspects` to, per sign, the
    distinct aspecting-planet chips for the grid's "Aspected by" strip; same-sign aspects
    dropped.

---

## 5. Retrograde & combustion (`ClassicalCombustion.cs`)

- **Retrograde:** `PlanetPosition.IsRetrograde = speed < 0` (from `PlanetSpeeds`). Rahu/Ketu
  reuse Rahu's speed sign. Shown as `(R)` / `(D)`.
- **Combustion (Asta):** proximity to the Sun, applied to the 6 applicable planets
  (**not** Sun / Rahu / Ketu / Ascendant). BPHS / Phaladeepika orbs, with the **narrower orb
  auto-selected when the planet is retrograde**:

  | Planet | Direct orb | Retrograde orb |
  |---|---|---|
  | Moon | 12° | — |
  | Mars | 17° | 8° |
  | Mercury | 14° | 12° |
  | Jupiter | 11° | — |
  | Venus | 10° | 8° |
  | Saturn | 15° | — |

  Stored: `IsCombust`, `DistanceFromSunDegrees`, `CombustionOrbUsedDegrees`. Shown as 🔥.
- Mirror table (engine doesn't read it yet): `tbl_Rule_CombustionOrb` under
  `RuleSetId = 'Parashari-Classical'`.

---

## 6. Nakshatras (`AstroMath.cs`, `tbl_Nakshatras` + Padas + SubLords)

- 27 nakshatras (Abhijit excluded), 13°20' each. `GetNakshatraLord` / `NakshatraLordOrder`
  is the single source of truth (Vimshottari dwells this) — `VimshottariDashaCalculator`
  points at it, no duplicate array.
- `Nakshatra` / `NakshatraPada` (pada 1–4) — **D1 only**. Canonical spellings from
  `tbl_Nakshatras.NakshatraName` (was VedAstro's `Aswini` / `Pushyami` / `Sravana`).
- `NakshatraLordPlanet` — every chart type, including the Ascendant row.
- **KP sub-lord:** `NakshatraSubLordPlanet` = level-2 only (Nakshatra Lord + Sub Lord).
  Deeper levels (Pratyantar/Sookshma/Prana/Deha) deliberately not built (1.5M+ rows).
- `tbl_NakshatraPadas` (108) — genuine 1:1 Pada ↔ Navamsa mapping.
- Descriptive fields (Guna/Gana/Yoni/Nadi/Varna/Tatva/Direction/RisingType) left **NULL** —
  legitimate classical data not confidently reproducible without a cited source; no code
  library closes this gap.

---

## 7. Vimshottari Dasha (`VimshottariDashaCalculator.cs` / `VimshottariDashaService.cs`)

- **Not** an `IChartCalculator` (Dasha has no planet-position/house shape) — a dedicated
  calculator + service, still logged as a `tbl_ChartResults` row for delete-cascade
  consistency.
- **3 levels** — Mahadasha → Antardasha → Pratyantardasha — with **all three levels
  correctly partial-at-birth simultaneously** (not just the Mahadasha). 120-year total
  coverage (comfortably exceeds the 4000-week / ~76.9-year display window).
- Lord years: `AstroMath.VimshottariYearsByLord` (single source; `VimshottariDashaCalculator`
  no longer has its own array). Cycle order: Ketu 7, Venus 20, Sun 6, Moon 10, Mars 7,
  Rahu 18, Jupiter 16, Saturn 19, Mercury 17 (120 total).
- Storage: `tbl_Chart_DashaPeriods` — self-referencing via `ParentDashaPeriodId`,
  `LevelNumber` 1/2/3, `StartDayOffset`/`EndDayOffset` age-relative + `StartDate`/`EndDate`
  absolute. `DashaPeriodRecord.BuildTree` rebuilds the tree from a flat, offset-ordered query.
- Age-relative date dimension: `tbl_Dim_LifeCalendar` (day-offset from birth; 7-day weeks,
  30-day months, 365-day years — a fixed simplification, week/month/year numbers derived
  independently from `DayOffset`). Reporting: `vw_Chart_DashaTimeline`,
  `tvf_Chart_LifeWeeks(@BirthDetailId)` (one row per week 1–4000, scoped by `ChartResultId`
  so a recompute never double-counts).

---

## 8. Slow-planet transit history (`PlanetTransitEventFinder.cs`, `tbl_PlanetSignTransitEvents`)

- **Saturn / Jupiter / Rahu** sign-boundary-crossing **event log** (not start/end periods —
  so retrograde re-entries are represented correctly), **1930–2060**. Mars excluded.
- Built by a day-walk + bisection. CLI: `precheck-planet-transits` (dry-run, cross-checked
  against 3 independently published sidereal transit dates — e.g. Saturn → Capricorn
  2020-01-24 — exact matches) then `backfill-planet-transits` (~27 s: Saturn 109 rows / 55
  re-entries, Jupiter 229 / 96, **Rahu 84 / 0** — the zero is itself a correctness signal:
  the mean node can't structurally double-dip).
- Ketu derived (`vw_KetuSignTransitEvents`, +6 signs). Point-in-time sign:
  `tvf_PlanetSignAtDate(@PlanetId, @AsOfUtc)`.

---

## 9. Sade Sati, Kantaka & Ashtama Shani (`tvf_Chart_SadeSatiPeriods`)

Built **purely from** the stored natal **Moon sign** (`tbl_Chart_KeyDetails`) +
`tbl_PlanetSignTransitEvents` — no new reference data.

- **Sade Sati** — Saturn transiting the **12th, 1st, 2nd** signs from the natal Moon; the
  three Dhaiyas: `SadeSati_Dhaiya1_Rising` (12th), `SadeSati_Dhaiya2_Peak` (1st),
  `SadeSati_Dhaiya3_Setting` (2nd). ~7.5 years per occurrence; ~3 occurrences in a life.
- **Kantaka Shani** — Saturn in the **4th** from the natal Moon.
- **Ashtama Shani** — Saturn in the **8th** from the natal Moon.
- The TVF splits each window on retrograde re-entries; the UI (`SadeSatiTable.razor`)
  re-consolidates contiguous rows (> 400-day gap = new window) and lists everything in
  ascending date order.
- Verified end-to-end against `1 Ramakrishnan` (Moon in Scorpio → Dhaiyas in
  Libra/Scorpio/Sagittarius; Kantaka in Aquarius; Ashtama in Gemini) — phases chain with
  zero gaps.
- **Ashtakavarga** (bindu-point transit strength, incl. Shani Ashtakavarga) is explicitly
  **out of scope** until a cited source for the 56 contribution tables exists.

---

## 10. Functional benefic / malefic (`LagnaFunctionalNature.cs` + `tbl_Dim_LagnaFunctionalNature`)

Parashari **functional** nature of a planet *for a given Lagna* — distinct from natural
nature — from which houses it rules from that Lagna. `enum FunctionalNature { Benefic,
Malefic, Neutral, Yogakaraka }`; `FunctionalNatureResult(Nature, RuledHouses(int[]),
IsMaraka, KendradhipatiDosha, Rationale)`. Rahu/Ketu out of scope (no sign rulership).

**Heuristic rules** (from B.V. Raman, *How to Judge a Horoscope* Vol. 1, p.14–18):

1. Rules a **kendra (4/7/10)** *and* a **trikona (5/9)** → **Yogakaraka**. (Only Mars for
   Cancer/Leo Lagna, Saturn for Taurus/Libra, Venus for Capricorn/Aquarius.)
2. Else rules the **1st** and nothing in {3,6,11} → **Benefic** (Moon → **Neutral**).
3. Else rules the **5th or 9th** and nothing in {3,6,11} → **Benefic**.
4. Else rules **only** kendras → natural benefic (Ju/Ve/Me/Mo) becomes **Malefic**
   (*kendradhipati doṣa*), natural malefic becomes **Benefic**.
5. Else rules any of **3/6/11** → **Malefic**.
6. Else rules only **2/8/12** → **Malefic** (Sun/Moon → **Neutral**).
7. `IsMaraka` additionally true whenever the planet rules the 2nd or 7th.
8. Catch-all (mixed kendra + maraka/dusthana, no trikona) → **Malefic**.

- The heuristic is a *computable baseline*; the authoritative per-Lagna verdict is
  **`tbl_Dim_LagnaFunctionalNature`** (migration 031, 84 rows = 12 Lagnas × 7 planets),
  seeded **verbatim from Raman p.16–18**, with a `Rank` column (1 = best benefic / worst
  malefic) and **3 rows left NULL** — Aries→Moon, Gemini→Saturn, Aquarius→Saturn — because
  the book never classifies them (not guessed).
- Computed vs. book **diverge on 17 of 84 cells** (+ the 3 NULL). Every divergence is a
  mixed-lordship planet on the Neutral boundary; **no Benefic↔Malefic flips, no Yogakaraka
  flips**. Expected and documented. `compare-functional-nature` prints the full set.
- UI: `PlanetPositionsTable` shows the **computed** nature as `{B|M}{Rank?}-[{ruledHouses}]`
  (e.g. Aries Lagna → Jupiter `B1-[9&12]`, Mercury `M2-[3&6]`).
- Verified: `verify-functional-nature` worked examples, e.g. `(Taurus, Saturn) → Yogakaraka
  {9,10}`, `(Aries, Mercury) → Malefic {3,6}`, `(Cancer, Moon) → Neutral {1}`.

---

## 11. Reference / master data (`docs/vedic-reference-tables.md`)

Parallel dimension layer, distinct from the chart-specific `tbl_Chart_*`:

- `tbl_Planets` (9; incl. `VimshottariYears`, `VimshottariSequenceOrder`)
- `tbl_SignAttributes` (12; element, chara/sthira/dwiswabhava, exaltation/debilitation/
  Moolatrikona) — cross-checked live against `ClassicalDignity.cs` / `ZodiacName.cs` before
  seeding.
- `tbl_Nakshatras` (27) / `tbl_NakshatraPadas` (108) / `tbl_NakshatraSubLords` (243, KP L1–2)
- `tbl_PlanetSignTransitEvents` (§8)
- Deliberately NULL (unsourced): `tbl_SignAttributes.RisingType`; `tbl_Nakshatras`'
  Guna/Gana/Yoni/Nadi/Varna/Tatva/Direction.

**Status: not wired into the engine.** `ClassicalDignity.cs` / `AstroMath.cs` still hold
their own hard-coded lookups (cross-checked to match, not reading these tables).

---

## 12. Rules engine (`docs/star-schema-rules-engine.md`, `decisions/001-*`)

`tbl_Rule_Sets` version dimension (1 row: `'Parashari-Classical'`, active) + `tbl_Rule_*`
tables (`AspectOffset` 19, `CombustionOrb` 6, `NaturalRelationship` 42,
`TemporaryFriendshipDistance` 12), each row carrying a `RuleSetId` FK. Transcribes
`ClassicalRelationships.cs` / `ClassicalCombustion.cs` / `ClassicalDignity.cs` verbatim,
hand-diffed to match.

**Immutability rule:** never edit a `RuleSetId`'s rows once any fact references them — ship a
new `RuleSetId` instead. **Phase 2 (calculators reading these tables) is not started** —
today the app still runs on the hard-coded C#; the tables are a cross-check mirror.

---

## 13. Coverage vs. classical / vs. Jagannatha Hora

- **Bhava-analysis 8-point checklist** (`docs/bhava-coverage.md`): 1 fully covered, 5 partial,
  2 not covered. Bhava Bala (point 2) confirmed blocked — not in Raman's Vol. 1. Yoga
  detection (point 4) has usable material, needs its own design pass.
- **vs. a full JHora natal export** (`key_comp_astro.md`): ~7 of ~35 blocks covered. Absent:
  panchanga, Jaimini chara karakas + rasi dashas, upagrahas, special lagnas, ~14 sphutas,
  Ashtakavarga, Shadbala, Vimsopaka, Avasthas, 12 of the 16 Shodashavarga charts, 5 other
  dasha systems. The vendored MIT `_research/jyotishganit/` unblocks Ashtakavarga / Shadbala /
  Panchanga; Avasthas / Ashtottari / Yogini and the descriptive nakshatra fields remain
  source-blocked.

---

## 14. Backlog (scored in `methods_prodmag.md`)

Shipped: Vimshottari Dasha, retrograde + combustion + nakshatra lord, D2/D6/D10/D11,
functional benefic/malefic, reference tables, transit history, Sade Sati/Kantaka/Ashtama,
rules-engine Phase 1, the web workspace UI.

Open (top items): **D9 within-sign degree** (ICE 7.7 — blocks Vargottama verification and
judging D9 conjunction tightness), house+planet significations / Sthira Karaka
(migration 030), Yoga detection design pass, ayanamsha-selectable, chart-style selectable.

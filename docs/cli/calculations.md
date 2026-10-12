---
last_updated: 2026-09-22
workstream: cli
togaf: C — Application Architecture (engine)
---

# CLI — calculations reference

Every calculation ikiastrro performs, its convention, and its source. All code is in
`Ikiastrro.Core`; only raw longitudes come from the ephemeris.

## 1. Ephemeris & frame

- **Engine:** `SwissEphNet` (`SwissEphemerisProvider`), **Moshier analytical mode** — no
  ephemeris data files.
- **Sidereal / ayanāṁśa:** **locked to Traditional Lahiri** (Swiss mode 1, 23.5959° for the
  1981 benchmark vs JHora's 23.5950°, 3.4″ apart) — `decisions/004-ayanamsa-traditional-lahiri.md`.
  `AyanamsaDefinition` still catalogues 22 systems and `tbl_Rule_Ayanamsa` keeps them for
  provenance, but generation takes no ayanāṁśa argument and `AyanamsaRuleRepository` refuses any
  default other than `AYANAMSA_LAHIRI`. True Chitrapaksha (mode 27) is closer still but needs `sefstars.txt`, which the
  file-less Moshier build omits. Every `tbl_ChartResults` row records `AyanamshaDegrees` +
  `SiderealTimeHours`.
- **Frame:** grahas are projected onto the **solar-system (invariable) plane**
  (`SE_SIDBIT_SSY_PLANE`) as **true positions** (`SEFLG_TRUEPOS`), as JHora computes them with
  Traditional Lahiri — every planet within 3.5″ of JHora's export (decision 009). The ascendant
  and houses stay on the plain sidereal ecliptic.
- **Nodes:** Rahu = **true node** (`SE_TRUE_NODE`), as JHora uses (decision 008; the mean node
  until then); Ketu = Rahu + 180° (derived, never stored separately). Both are always flagged
  retrograde, even when the true node's speed turns positive. `vw_KetuSignTransitEvents` = Rahu events + 6 signs.
- **Speed & latitude:** from the same `swe_calc_ut` call (`SEFLG_SPEED`); latitude is measured
  from the solar-system plane (decision 009), as JHora shows it; persisted per planet
  for D1 and every varga. Speed drives retrograde + combustion-orb selection.
- **House system:** **Whole Sign** everywhere. House *n* from a reference sign =
  `AstroMath.CountFromSignToSign` (the sign holding the reference point is house 1). Confirmed
  for house placement + placement-rule interpretation.
- **Time:** birth local wall-clock + place; lat/long via Nominatim; UTC offset resolved
  offline from lat/long + date (historical DST respected). Stored `DATETIME2`, local time of
  day, no offset column.
- `ZodiacName` keeps the Latin spelling `Capricornus`.

## 2. Divisional charts (21 types)

**D1** (real sidereal longitude) + **20 vargas**: D2, D2-US, D3, D4, D5, D6, D7, D8, D9, D10,
D11, D12, D16, D20, D24, D27, D30, D40, D45, D60.

- Data-driven: `tbl_Rule_VargaScheme` is the source of truth (one row per varga per rule set —
  `DivisionFactor`, `MethodCode`, `SignRuleKind`, `SignRuleKey`).
  `ChartCalculationOrchestrator.CreateDefault(schemes)` builds one shared `VargaCalculator`
  (`IChartCalculator`) per row; every varga gets the full shared analytics.
- `SignRuleKey` → C# `IVargaSignRule` via `VargaSignRuleFactory`. `LinearVargaSignRule` covers
  D4 / D12; D6 / D9 / D11 wrap `AstroMath.Get*Sign`; the rest are bespoke
  `Special` rules (`HoraD2UmaShambu`, `DrekkanaD3UmaShambu`, `PanchamsaD5`, `SaptamsaD7EvenReverse`, `AshtamsaD8`,
  `DasamsaD10EvenReverse`, `ShodasamsaD16EvenReverse`, `VimsamsaD20`, `SiddhamsaD24EvenReverse`, `NakshatramsaD27`,
  `TrimsamsaD30`, `KhavedamsaD40`, `AkshavedamsaD45`, `ShashtyamsaD60EvenReverseFromAries`). Formulas transcribed from PyJHora `chart_method=1` "Traditional Parāśara".
- **D2**, **D3** and **D7** use JHora's default schemes — Uma-Shambhu Hora, Uma-Shambhu
  Drekkana, Saptāṃśa with even signs reversed (migration 164, `decisions/005`); **D2-US** now
  computes the same chart as D2. **D10**, **D16**, **D24** and **D60** use the schemes JHora
  tags `D-10 (5-8)`, `D-16 (Rev)`, `D-24 (Rev)` and `D-60 (RvAr)` — even signs counted backward,
  D60 counted from Aries (migration 166, `decisions/007`). **D11** uses the PVR/BPHS traditional Rudramsa (not the Sanjay Rath variant).
- Stored: `VargaLongitudeDegrees` (`Normalize(realLon × N)`), `DegreesInSignDecimal`
  (`= VargaLongitudeDegrees mod 30`, populated every chart type). The varga sign is the
  `IVargaSignRule`, **not** `FLOOR(VargaLongitudeDegrees/30)`.
- Not built → varga-of-a-varga composition (D81 / D108 / D144) and D150.

Significations: D2 wealth · D3 siblings/courage · D4 property · D5 fame/authority · D6 health ·
D7 children · D8 sudden events/longevity · D9 marriage/dharma/strength · D10 career · D11 gains ·
D12 parents · D16 vehicles/comforts · D20 spiritual practice · D24 education · D27
strengths/weaknesses · D30 misfortunes · D40 maternal legacy · D45 paternal legacy · D60
past-life karma.

## 3. Dignity

9-tier `DignityStatus` ∈ Exalted · Moolatrikona · Own Sign · Great Friend · Friend · Neutral ·
Enemy · Great Enemy · Debilitated. Pañchadhā Maitrī = natural (permanent) + temporary
(from-Moon) friendship; sign-dignity only, not affliction. PVR Table 6 segments live in
`tbl_Rule_GrahaDignity` (`PvrDignityEvaluator`); the Parāśari exaltation/debilitation degrees,
Moolatrikoṇa ranges and own signs are also hard-coded and cross-checked to `tbl_SignAttributes`.
**Rahu/Ketu** use the Parāśari convention (Rahu exalted Taurus, Ketu exalted Scorpio) — an
explicit choice; their `DignityStatus` is limited to Exalted / Debilitated / Neutral.
Check: `verify-dignity`.

## 4. House lordship, conjunctions, aspects

- **House lordship** per house: its sign's ruler and where that ruler sits — counted from
  Lagna, Sun and Moon.
- **Conjunctions:** grahas sharing a sign; `DegreeSeparation` D1-only. Plus an explicit
  ≥ 2-graha **group/member** layer with `MemberKey` for yoga-subset matching.
- **Aspects (graha dṛṣṭi):** every graha aspects the 7th from itself; Mars +4/8, Jupiter +5/9,
  Saturn +3/10. **Rahu/Ketu use the Jupiter-style 5/7/9 convention** — an explicit choice.

## 5. Retrograde & combustion

Retrograde = `speed < 0`. Combustion (asta) for the 6 applicable planets (not Sun / Rahu /
Ketu / Ascendant); BPHS / Phaladeepika orbs with the **narrower orb auto-selected when
retrograde** (Moon 12 · Mars 17/8 · Mercury 14/12 · Jupiter 11 · Venus 10/8 · Saturn 15).
Stored: `IsCombust`, `DistanceFromSunDegrees`, `CombustionOrbUsedDegrees`.

## 6. Nakshatras

27 nakṣatras (Abhijit excluded), 13°20′ each. `GetNakshatraLord` / `NakshatraLordOrder` is
the single source of truth (Vimśottari uses it). `Nakshatra` / `NakshatraPada` display are
**D1-only**; `NakshatraLordPlanet` + `NakshatraSubLordPlanet` (KP level-2) are populated for
every chart type. `tbl_NakshatraPadas` (108) has a verified 1:1 Pada ↔ Navamsa mapping.
Descriptive fields (Guna/Gana/Yoni/Nadi/…) left NULL pending a cited source. The KP-2 sub-lord
division (`AstroMath.GetNakshatraSubLord`) reuses `VimshottariYearsByLord` for its proportional
split within one nakshatra — same citation as §7's Vimshottari cycle (`tbl_Rule_VimshottariPeriod`).

## 7. Vimshottari Dasha

`VimshottariDashaCalculator` (pure) + `VimshottariDashaService` (write path). Not an
`IChartCalculator`; logged as a `tbl_ChartResults` row for delete-cascade consistency.
**3 levels** (Mahā → Antar → Pratyantar), **all three correctly partial-at-birth**. Cycle:
Ketu 7 · Venus 20 · Sun 6 · Moon 10 · Mars 7 · Rahu 18 · Jupiter 16 · Saturn 19 · Mercury 17
(120 yr). Real dates use 365.2425 d/yr. Storage `tbl_Chart_DashaPeriods` (self-referencing,
age-relative + absolute); reporting `vw_Chart_DashaTimeline`, `tvf_Chart_LifeWeeks` (week
1–4000). Reference: `BENCH_RAMAKRISHNAN_P_JHORA_1981` (`db/checks/check_ayanamsa_dasha_benchmarks.sql`).
The 9-planet order/120-year table is cited by `tbl_Rule_VimshottariPeriod` (migration 085,
`SRC_PVR_INTEGRATED` §16.2 Table 38) — `AstroMath.NakshatraLordOrder`/`VimshottariYearsByLord`
stay hardcoded (verified-mirror pattern); CLI `verify-dasha` cross-checks them against the
table. `tbl_Rule_DashaApplicability` is the separate, still-unseeded *conditional*-dasha
eligibility layer. The KP-2 sub-lord division (§6) reuses the same table's citation.

## 8. Slow-planet transit history

Saturn / Jupiter / Rahu sign-boundary-crossing **event log** (not periods, so retrograde
re-entries are correct), **1930–2060**. Built by day-walk + bisection. CLI:
`precheck-planet-transits` (dry-run vs published dates) then `backfill-planet-transits`. Ketu
derived (`vw_KetuSignTransitEvents`); point-in-time sign `tvf_PlanetSignAtDate`.

## 9. Sade Sati, Kantaka & Ashtama Shani

`tvf_Chart_SadeSatiPeriods(@BirthDetailId)` from the stored natal Moon sign +
`tbl_PlanetSignTransitEvents` — no new reference data. Sade Sati = Saturn in the 12th / 1st /
2nd from natal Moon (three Dhaiyas); Kantaka = 4th; Ashtama = 8th. The TVF splits windows on
retrograde re-entries; the UI re-consolidates. The offset rule is inlined in the T-SQL function
body — it carries no `SourceRefCode` and isn't in `tbl_Rule_Catalog`'s scope (which only tracks
`tbl_Rule_*` tables), so a non-SQL port must still reimplement it from this doc.

## 9a. Ashtakavarga (Parāśari)

`AshtakavargaCalculator.Calculate(d1)` — pure, over the D1 natal signs of the seven grahas
+ Lagna → the seven **Bhinnāṣṭakavargas** (7 recipients × 8 contributors), the
**Sarvāṣṭakavarga** (per-sign total of the seven, Lagna excluded; grand total 337),
**Ṭrikoṇa + Ekādhipatya Śodhana**, and **Rāśi / Graha / Sodhya Piṇḍa**. The benefic-places
matrix, rāśimāna `(7,10,8,4,10,5,7,8,9,5,11,12)` and grahamāna `[5,5,8,5,10,7,5]` live in
`AshtakavargaTables` (the verified mirror of `tbl_Rule_AshtakavargaContribution` /
`tbl_Rule_AshtakavargaReduction`). Moon and Venus carry the standard Parāśari corrections
(Moon ±9 from Moon/Mars, +2/−12 from Jupiter; Venus +4/−5 from Mars) — the variant JHora
uses. `SRC_BPHS_ASHTAKAVARGA`. Persisted on the D1 row: `tbl_Fact_BhinnaAshtakavarga`
(+ the 1/0 `…Contribution` explain layer), `tbl_Fact_SarvaAshtakavarga`,
`tbl_Fact_AshtakavargaPinda`; UI view `vw_ChartAshtakavarga`. Check: `verify-ashtakavarga`
reproduces the JHora export for `1_Ramakrishnan` **exactly** — every BAV cell, the SAV, and
all seven Rāśi/Graha/Sodhya Piṇḍa triples.

## 9b. Pañchāṅga (Tithi / Karaṇa / Nitya Yoga / Vedic Weekday / Hora Lord)

`PanchangaCalculator.Calculate(birth, positions, sunTimes)` — pure, over the D1 Sun/Moon
sidereal longitudes and `SunTimes`. `SRC_PVR_INTEGRATED` §1.3.8-1.3.12, the book's only
panchanga chapter:

- **Tithi** — `floor((Moon−Sun)/12°) + 1` (1-30; 1-15 Śukla, 16-30 Kṛṣṇa). `tbl_Dim_Tithi.Id`
  is seeded in the same order, so the index is the FK directly.
- **Nitya Yoga** — `floor((Sun+Moon)/13°20') + 1` (1-27), same direct-FK seeding.
- **Karaṇa** — each tithi splits into 2 half-tithis; the 7 movable karaṇas
  (`tbl_Dim_Karana.Id` 1-7) repeat 8× (56 slots) from the 2nd half of the month's 1st tithi;
  the 4 fixed karaṇas (Id 8-11) cover the 2nd half of tithi 29 through the 1st half of the
  next month's tithi 1.
- **Vedic weekday** — the calendar day `SunTimes.Sunrise` falls on; `tbl_Dim_VedicWeekday.Id`
  1=Sunday..7=Saturday matches `.NET DayOfWeek + 1`.
- **Hora Lord** — 24 equal horas from sunrise to next sunrise; hora 1 is the weekday lord,
  then the cycle Saturn→Jupiter→Mars→Sun→Venus→Mercury→Moon (decreasing geocentric speed,
  mirrors `tbl_Dim_HoraSequence`) repeats.
- **Janma Ghaṭis** — minutes elapsed since `SunTimes.Sunrise` / 24.

**Deliberately not computed** — no PVR §1.3 source found: Karaṇa lord, Nitya Yoga lord,
Samvatsara (60-year cycle name), lunar month (PVR's own Table 4 extract is OCR-garbled —
contradicts classical Pauṣa/Māgha/Phālguna nakṣatra pairings, needs a clean source pass),
Mahākāla Hora / Kāala Lord (JHora extensions past PVR's 24-hora scheme). Persisted on the D1
row: `tbl_Chart_Panchanga`; UI view `vw_ChartPanchanga`. Check: `verify-panchanga` reproduces
the JHora export for `1_Ramakrishnan` — Kṛṣṇa Tritīyā, Vanija, Vyatīpāta, Tuesday, Hora Lord
Venus **exactly**; Janma Ghaṭis within JHora's own two-decimal rounding.

## 9b. Pañchāṅga (Tithi / Karaṇa / Nitya Yoga / Vedic Weekday / Hora Lord)

`PanchangaCalculator.Calculate(birth, positions, sunTimes)` — pure, over the D1 Sun/Moon
sidereal longitudes and `SunTimes`. `SRC_PVR_INTEGRATED` §1.3.8-1.3.12, the book's only
panchanga chapter:

- **Tithi** — `floor((Moon−Sun)/12°) + 1` (1-30; 1-15 Śukla, 16-30 Kṛṣṇa). `tbl_Dim_Tithi.Id`
  is seeded in the same order, so the index is the FK directly.
- **Nitya Yoga** — `floor((Sun+Moon)/13°20') + 1` (1-27), same direct-FK seeding.
- **Karaṇa** — each tithi splits into 2 half-tithis; the 7 movable karaṇas
  (`tbl_Dim_Karana.Id` 1-7) repeat 8× (56 slots) from the 2nd half of the month's 1st tithi;
  the 4 fixed karaṇas (Id 8-11) cover the 2nd half of tithi 29 through the 1st half of the
  next month's tithi 1.
- **Vedic weekday** — the calendar day `SunTimes.Sunrise` falls on; `tbl_Dim_VedicWeekday.Id`
  1=Sunday..7=Saturday matches `.NET DayOfWeek + 1`.
- **Hora Lord** — 24 equal horas from sunrise to next sunrise; hora 1 is the weekday lord,
  then the cycle Saturn→Jupiter→Mars→Sun→Venus→Mercury→Moon (decreasing geocentric speed,
  mirrors `tbl_Dim_HoraSequence`) repeats.
- **Janma Ghaṭis** — minutes elapsed since `SunTimes.Sunrise` / 24.

**Deliberately not computed** — no PVR §1.3 source found: Karaṇa lord, Nitya Yoga lord,
Samvatsara (60-year cycle name), lunar month (PVR's own Table 4 extract is OCR-garbled —
contradicts classical Pauṣa/Māgha/Phālguna nakṣatra pairings, needs a clean source pass),
Mahākāla Hora / Kāala Lord (JHora extensions past PVR's 24-hora scheme). Persisted on the D1
row: `tbl_Chart_Panchanga`; UI view `vw_ChartPanchanga`. Check: `verify-panchanga` reproduces
the JHora export for `1_Ramakrishnan` — Kṛṣṇa Tritīyā, Vanija, Vyatīpāta, Tuesday, Hora Lord
Venus **exactly**; Janma Ghaṭis within JHora's own two-decimal rounding.

## 10. Functional benefic / malefic

`LagnaFunctionalNature` — **functional** nature for a given Lagna, read from PVR Table 30
(`SRC_PVR_INTEGRATED` §13.2, p.167). The primary `enum { Benefic, Malefic, Neutral, Yogakaraka }` verdict is accompanied
by independent ownership evidence: `MarakaHouses` (2/7), `DusthanaHouses` (6/8/12),
`TrishadayaHouses` (3/6/11), `IsBaadhakaLord` plus the modality-derived `BaadhakaHouse`
(movable 11, fixed 9, dual 7), `KendradhipatiDosha`, and an aggregate rationale. The evidence
remains visible even when a stronger lordship determines the primary verdict—for example, a
planet can be Yogakaraka and also the baadhaka lord. `IsMaraka` remains as a compatibility
convenience derived from `MarakaHouses`.

Rahu/Ketu are out of scope. The verdict is Table 30's own 12 Lagna rows (yogakaraka / benefic /
neutral / malefic), not a re-derivation from lordship: PVR says to "judiciously combine" the two
rasis a planet owns and the table is that result (e.g. Jupiter, 6th + 9th lord, is benefic for
Cancer). The Moon is unlisted for Aries/Libra/Capricorn because it depends on phase; the optional
`moonWaxing` argument picks Malefic (waxing, kendrādhipati) or Neutral (waning). `MoonPhase` supplies
it to every caller (house verdicts, TargetInfluences, D1 Promise, Argala table, Planets dignity table,
Key Inference, marriage matters): exact from the natal Sun and Moon longitudes (waxing = Moon 0–180°
ahead of the Sun, read from D1 even for a varga chart), or whole-sign (waning past six signs, as
`ArgalaCalculator`) where only signs exist. With no phase at all it defaults to waxing and says so
in the rationale. The earlier Raman-based heuristic was removed 2026-10-12.
Baadhaka follows PVR §13.3 via `BaadhakaCalculator`. This is the sole source of the verdict (the
per-Lagna mirror table was removed). Computed on demand, not persisted. Check:
`verify-functional-nature` (all 12 Lagnas against Table 30, plus PVR's yogakaraka rule re-derived).
## 11. Baadhaka

`BaadhakaCalculator` — PVR §13.3 (Table 31, `SRC_PVR_INTEGRATED`): for a rasi/house falling in
a movable/fixed/dual sign, its baadhaka sthaana ("troubling spot") is the 11th/9th/7th house
from it, and the baadhaka ("troublemaker") is that sthaana's lord (`HouseEngine.GetSignLord`).
`For(sign)` is Lagna-agnostic — reusable for any house or arudha pada in any divisional chart,
per PVR's own framing. `For(lagnaSign, houseNumber)` is a convenience overload that also
reports which house from that Lagna the sthaana falls in. `enum SignModality { Movable, Fixed,
Dual }`. Known divergence: PVR's Table 31 additionally names Rahu/Ketu as co-baadhakas on the
two rows whose sthaana lands in Aquarius/Scorpio (his Table 6 co-ownership of those signs) —
this project keeps the classical 7-planet-only rulership used everywhere else, so only
Saturn/Mars are returned there (same divergence tracked for Ch. 3 in
`docs/research/domain/pvr-coverage.md`). Computed on demand, not persisted. Check:
`verify-baadhaka`.

## 11. Avasthas (planetary states)

Star-schema (`tbl_Dim_PlanetaryState` + `tbl_Rule_AgeState` / `tbl_Rule_WakefulnessState` /
`tbl_Rule_PostureStateFormula` → `tbl_Fact_PlanetaryState`), written by `PlanetaryStateComputer`.

- **Bālādi** (age from within-sign degree) — odd signs 0–6 Bāla / 6–12 Kumāra / 12–18 Yuva /
  18–24 Vṛddha / 24–30 Mṛta; even signs reversed. Effect fraction from `tbl_Rule_AgeState`.
  **D1 only.**
- **Jāgradādi** (waking state from `DignityStatus`) — Exalted/MT/Own → Jāgrat; friend tiers →
  Svapna; enemy tiers → Suṣupti. **Every chart type.**
- **Sayanaadi** (`PostureStateCalculator`, PVR §15.4.4, Table 36; 12 states, Sayana .. Nidraa) —
  `index = ((C×P×A) + M + G + L) mod 12` (remainder 0 → 12): C = the planet's own nakshatra
  number, P = its index (Sun=1..Ketu=9), A = its navamsa within its own rasi (1-9), M = Moon's
  nakshatra number, G = the ghati running at birth (`tbl_Chart_Panchanga.JanmaGhatis`,
  `FEAT-DATA-06`), L = Lagna's rasi number. **D1 only** (needs the continuous within-sign
  degree and Janma Ghatis). The nakshatra lookup must index against
  `AstroMath.NakshatraCanonicalNames`, not `Enum.TryParse<ConstellationName>` — that enum's
  member spellings are a legacy form unrelated to the stored display names (its own doc
  comment says so; this cost one wrong-on-7-of-9-planets round trip before the fix).
- **Dīptādi + Lajjitādi** (PVR §15.4.3; any chart type): `DeeptadiStateCalculator`,
  `LajjitadiStateCalculator`. Dīptādi follows JHora's Mood column — placement flag (Deepta/Swastha)
  plus the compound relationship to the sign lord, Vikala = at least two natural malefics, Khala =
  Sun/Mars/Saturn sign lord, Kopita = Sun within 5° ([decision 012](../../decisions/012-deeptadi-jhora-mood-reading.md)).
  Check: `AvasthaJhoraMoodGoldenTests` (4 reference charts). Open: node relationship tiers.
- Not built: Śayanaadi's own secondary Cheṣṭā/Dṛṣṭi/Vicheṣṭā strength refinement (needs the native's
  given-name first sound; Table 37's groups follow the vowel order a, i, u, e, o). Check: `verify-avastha`.

## 12. Jaimini Chara Karakas & special points

`CharaKaraka` + `PointKind` columns on `tbl_Chart_KeyDetails` (migration 14). Check:
`verify-jaimini` (against the `1_Ramakrishnan` JHora export).

- **Chara Karakas** (Aṣṭa, 8-karaka) — rank the 8 grahas (Sun…Saturn, Rahu) by degree within
  sign, descending; **Rahu's key is `30 − degreeInSign`**. AK → AmK BK MK PiK PK GK → DK.
  Ketu not ranked. Computed once per person from D1, stamped on every chart type. Cited by
  `tbl_Rule_Karaka` WHERE `KarakaScheme='Chara'` (migration 085, `SRC_PVR_INTEGRATED` §8.2
  Table 13) — the table was schema-designed for exactly this rule
  (`KarakaScheme`/`OrderIndex`/`ReverseForRahu`) but sat reserved and empty since migration 18.
  `CharaKarakaCalculator` stays hardcoded; CLI `verify-jaimini` cross-checks its order against
  the table.
- **Special points** — special lagnas, upagrahas and Punya Saham have one D1 longitude each,
  projected into all 21 vargas with the same `IVargaSignRule` a planet uses. **Arudha padas are
  not projected** (since 2026-10-01): PVR §9.2 and §9.5 define them "in all the divisional
  charts" from that chart's own placements, so `VargaChartComputer` computes them inside each
  varga from its own Lagna and grahas, at the varga Lagna's degree-in-sign (bhava) or the
  graha's varga degree (graha). Their `NirayanaLongitudeDegrees` in a varga carries the same
  varga-space value — a pada computed in a varga has no real ecliptic longitude.
  - **AL + 12 Bhāva Arudhas** (`ArudhaCalculator`, `PointKind = Arudha`) — Parāśari pada per
    house, with the 1st/7th → 10th exception; A1 emitted as `AL`. For a house in Scorpio or
    Aquarius the lord is the **stronger co-lord** (`StrongerCoLord`, PVR §15.5.1: Mars vs Ketu,
    Saturn vs Rahu — basic rule, then joined-by, Jupiter/Mercury/dispositor, exaltation, sign
    modality, advancement). Cited by `tbl_Rule_ArudhaFormula` (migration 085,
    `SRC_PVR_INTEGRATED` §9.2); CLI `verify-jaimini` asserts the row exists and cites the right
    source. CLI **`verify-varga-arudha`** checks the AL of every chart shared with JHora against
    the AL in each grid of `docs/artifacts/reference-charts/*_Jagannatha.txt` (live, and flags
    stored rows that are stale); 59/59 pass, one grid skipped where JHora's varga variant has a
    different Lagna (Ramya D45).
  - **Graha Arudhas** (`GrahaArudhaCalculator`, `PointKind = GrahaArudha`, `GA_Sun`…`GA_Ketu`,
    PVR §9.5) — for a planet owning two signs, the stronger sign by `StrongerRasiComparator`
    (§15.5.2's six rules). For Aquarius / Scorpio both co-lords count in rule 2 (Exercise 26:
    "Rahu's aspect also counts") and the stronger co-lord is the lord in rules 4 and 6.
  - **Hora / Bhaava / Ghati Lagna** (`HoraLagnaCalculator` / `BhaavaLagnaCalculator` /
    `GhatiLagnaCalculator`, `PointKind = SpecialLagna`, codes `HL`/`BL`/`GL`) — Sun's sidereal
    longitude at the Vedic day's opening sunrise + 0.5° / 0.25° / 1.25° per clock-minute since
    it (PVR §5.2-5.4; `tbl_Rule_SpecialLagnaTimeRate`). The time-of-day-difference shortcut is
    valid for all three because each rate × 1440min lands on an exact multiple of 360°.
  - **Sree Lagna** (`SreeLagnaCalculator`, code `SL`) — natal Lagna + (Moon's fraction of its
    own nakshatra) × 360° (PVR §5.7; `tbl_Rule_SpecialLagnaFraction`). Pure — no sunrise
    dependency, unlike BL/HL/GL.
  - **All 11 upagrahas** (`SubPlanetCalculator`, `PointKind = Upagraha`) from migration-27
    rules under `SRC_PVR_INTEGRATED`: Sun chain (Dhūma / Vyatīpāta / Parivesha / Indrachāpa /
    Upaketu) + 6 time points (Kāla / Mṛtyu / Ardhaprahara / Yamaghaṇṭaka / Gulika = Saturn's
    eighth midpoint, Maandi = its start). Check: `verify-upagrahas` (in-memory);
    `verify-jaimini` expects stored charts regenerated under PVR names.
- **Karakāṁśa** (AK's D9 sign, PVR §7.3.6) — no dedicated calculator: `CharaKaraka` is already
  stamped onto every chart type's `tbl_Chart_KeyDetails` rows, D9 included, so the D9 row with
  `CharaKaraka = 'AK'` already carries it. `vw_ChartKarakamsa` (migration 082) is the read.
- Sunrise/sunset from `SwissEphemerisProvider.GetSunTimes` (`swe_rise_trans`,
  `SE_BIT_DISC_CENTER | SE_BIT_NO_REFRACTION`).
- **Deliberately not built** — `SRC_PVR_INTEGRATED` §5.7 states outright these are "beyond the
  scope of this book", and no other registered source covers them: Vighati Lagna, Varnada
  Lagna, Pranapada Lagna, Indu Lagna, Bhṛgu Bindu. Also not built: Swāṁśa chart, Jaimini rāśi
  dashas.

## 13. Strength (Ṣaḍbala / Bhāva Bala)

`ShadbalaCalculator` / `BhavaBalaCalculator`, PVR-first profile in `tbl_Rule_ShadbalaComponent`
/ `tbl_Rule_BhavaBalaComponent` with formula provenance, → `tbl_Fact_PlanetaryStrength*` /
`tbl_Fact_BhavaStrength*`. Available: total Ṣaḍbala, the six sub-totals, named component rows,
Kālabala, strongest-planet ranking, Bhāva Bala, `tbl_Fact_Vargottama` (explicit D1/D9 same-sign
facts). **DB contracts landed** (migrations 071–073): `tbl_Rule_ShadbalaMinimumRupas` (the
classical 5/6/5/7/6.5/5.5/5 rūpa thresholds — reproduce JHora %Strength within rounding),
`tbl_Rule_PlanetaryWar` (Yuddha orb + adjustment), `RuleParametersJson` on the six deferred
Kālabala sub-components, and `tbl_Dim_ShadbalaBenchmarkValues` (JHora golden totals).
**Dina / Horā / Tribhāga Bala and Graha Yuddha detection landed** (`FEAT-STRENGTH-01`, CLI
slice): `ShadbalaCalculator` reuses `PanchangaCalculator`'s own verified weekday lord and Hora
Lord (no re-derivation) for Dina Bala (45 virupas to the weekday lord) and Hora Bala (60 to the
running hora lord); Tribhāga Bala (day/night split into thirds ruled Mercury/Sun/Saturn by day,
Moon/Venus/Mars by night, 60 virupas to the part's lord, Jupiter classically exempt and always
scoring 60) reuses the Panchanga layer's Janma Ghaṭis. Graha Yuddha (`ComputeYuddha`) detects
the five tara grahas within 1° of D1 longitude and picks the winner by ecliptic latitude
(further north wins) — the diameter-based delta *magnitude* is deliberately left at 0 virupas
(no war present in `1_Ramakrishnan`'s chart either way): `SRC_RAMAN_GRAHA_BHAVA_BALAS` is a
DJVU with no text extract, so fabricating a number for it would violate this engine's
source-honesty rule. `verify-strength` checks both the engine (self-consistency against the
JHora export's own sunrise/sunset/birth-time data — the export prints only the Shadbala grand
total, no per-component breakdown to cross-check against) and the persisted
`tbl_Fact_PlanetaryStrengthComponent` rows; `tests/Ikiastrro.Yoga.Tests/ShadbalaKalaBalaTests`
(5) cover the same ground plus a synthetic war-winner case. Remaining (engine): Varṣa / Māsa /
Ayana Bala (need the cited Raman edition); the Yuddha Bala magnitude (same source blocker);
populate `MinimumRequiredRupas` + the Rashmi / Parāśara Iṣṭa/Kaṣṭa columns; Cheṣṭā
reconciliation; Vimśopaka Bala.

## 14. Yoga

`ProductionYogaEngine` is the composition root over every implemented Raman/PVR evaluator +
the Raman 201–300 ledger. Chart generation persists all source variants per chart
(`tbl_Fact_YogaInputEvaluations`, `tbl_Rule_Yoga` + applicability/context-requirement tables).
P0 inputs: required vargas, exact longitude, day/night, lunar phase, source-specific subject
sex — missing P0 context yields `NOT_EVALUATED`, not `ABSENT`. In progress: predicates
201–300, PVR P0 additions (Subha, Asubha, Guru-Mangala, Chamara, Khadga, Lagnaadhi, Saarada,
Dharma-Karmādhipati, Vipareeta Rāja), structured missing-requirement codes.

## Chart-reading method (JHora-anchored)

The universal varga-reading loop, per-varga significators, and worked lenses over the
`1_Ramakrishnan` reference export are consolidated here at a high level; the deep per-chart
reading guides (D1 / D9 / D10 / D60 + the short index for the rest) are retained under
`docs/research/domain/` as reference material.

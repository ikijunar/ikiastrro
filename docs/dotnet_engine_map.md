# Ikiastrro.Core — .NET engine map

A D2 diagram of the calculation engine (`src/Ikiastrro.Core/`) plus a per-file
reference, grouped by folder. Sibling projects (`Ikiastrro.Cli`, `Ikiastrro.Data`,
`Ikiastrro.Web`) are **not** shown — Core has no dependency on them; they consume it.

> `d2` **is** installed (`C:\Program Files\D2\d2`, v0.8.2) and the block below is
> verified to compile with it. `d2` does not read fenced blocks out of Markdown,
> so first copy the diagram into a `.d2` file (or `sed -n` the fence range), then
> `d2 that-file.d2 out.svg`.

Folders beyond the seven originally requested: **`Jaimini/`** and **`SpecialPoints/`**
were added 2026-09-01 (Chara Karakas + special points) and are included so the map is
complete.

---

## Diagram

```d2
direction: down

title: Ikiastrro.Core - engine data flow { shape: text; near: top-center; style.font-size: 24 }

# ============================ containers ============================

Models: {
  style.fill: "#f4f1ea"
  BirthDetails
  ChartResult
  ChartKeyDetail
  PlanetPosition
  VargaScheme
  DTOs: "+ 14 reference / result DTOs" { style.multiple: true }
}

Geocoding: {
  style.fill: "#eaf1f4"
  IPlaceResolver
  NominatimPlaceResolver
  ManualPlaceResolver
}

Astro: {
  style.fill: "#eef4ea"
  SwissEphemerisProvider: "SwissEphemerisProvider\n(swe_calc / swe_rise_trans)"
  AstroMath
  AstroIds
  VargaSignRuleFactory
  IVargaSignRule
  SignRules: "17 IVargaSignRule impls\n(D2..D60 + D2-US)" { style.multiple: true }
  Enums: "ZodiacName · ConstellationName · PlanetName" { style.multiple: true }
}

Calculators: {
  style.fill: "#f4eeea"
  BirthMomentFactory
  ChartCalculationOrchestrator
  IChartCalculator
  D1RasiCalculator
  VargaCalculator
  D1ChartComputer
  VargaChartComputer
  ChartAnalysisInput
  ChartAnalyzer
  ClassicalDignity
  ClassicalRelationships
  ClassicalCombustion
  LagnaFunctionalNature
  PlanetAvasthaComputer
  BaaladiAvastha
  JagradadiAvastha
  ChartViewModel
}

SpecialPoints: {
  style.fill: "#f0eaf4"
  SpecialPointCalculator
  ArudhaCalculator
  HoraLagnaCalculator
  UpagrahaCalculator
  SpecialPointSeed
  SpecialPointProjector
}

Jaimini: {
  style.fill: "#f4eaf0"
  CharaKarakaCalculator
  CharaKaraka
}

Dasha: {
  style.fill: "#eaf4f0"
  VimshottariDashaCalculator
  DashaPeriod
  DashaPeriodRecord
  LifeWeek
}

Transits: {
  style.fill: "#f4f4ea"
  PlanetTransitEventFinder
}

LifeAreas: {
  style.fill: "#eaeef4"
  LifeAreaMap
}

# ============================ flow ============================

Models.BirthDetails -> Geocoding.NominatimPlaceResolver: "\"City, Country\""
Geocoding.IPlaceResolver -> Geocoding.NominatimPlaceResolver: impl
Geocoding.IPlaceResolver -> Geocoding.ManualPlaceResolver: impl
Geocoding.NominatimPlaceResolver -> Calculators.BirthMomentFactory: lat / long / offset
Calculators.BirthMomentFactory -> Astro.SwissEphemerisProvider: local moment

Astro.SwissEphemerisProvider -> Calculators.D1ChartComputer: sidereal longitudes
Astro.SwissEphemerisProvider -> Calculators.VargaChartComputer: sidereal longitudes
Astro.VargaSignRuleFactory -> Astro.IVargaSignRule: builds
Astro.IVargaSignRule <- Astro.SignRules: implements
Astro.IVargaSignRule -> Calculators.VargaChartComputer: varga sign

Calculators.ChartCalculationOrchestrator -> Calculators.IChartCalculator: drives all
Calculators.D1RasiCalculator -> Calculators.IChartCalculator: impl
Calculators.VargaCalculator -> Calculators.IChartCalculator: impl
Calculators.D1RasiCalculator -> Calculators.D1ChartComputer
Calculators.VargaCalculator -> Calculators.VargaChartComputer
Calculators.D1ChartComputer -> Calculators.ChartAnalysisInput
Calculators.VargaChartComputer -> Calculators.ChartAnalysisInput
Calculators.ChartAnalysisInput -> Calculators.ChartAnalyzer
Calculators.ChartAnalyzer -> Calculators.ClassicalDignity
Calculators.ChartAnalyzer -> Calculators.ClassicalRelationships
Calculators.ChartAnalyzer -> Calculators.ClassicalCombustion
Calculators.ChartAnalyzer -> Models.ChartKeyDetail: "KeyDetail / HouseLord / Conjunction / Aspect rows"
Calculators.PlanetAvasthaComputer -> Calculators.BaaladiAvastha
Calculators.PlanetAvasthaComputer -> Calculators.JagradadiAvastha
Calculators.ChartAnalyzer -> Calculators.PlanetAvasthaComputer: "via ChartGenerationService"

Calculators.ChartCalculationOrchestrator -> SpecialPoints.SpecialPointCalculator: seed once per person
SpecialPoints.SpecialPointCalculator -> SpecialPoints.ArudhaCalculator
SpecialPoints.SpecialPointCalculator -> SpecialPoints.HoraLagnaCalculator
SpecialPoints.SpecialPointCalculator -> SpecialPoints.UpagrahaCalculator
SpecialPoints.HoraLagnaCalculator -> Astro.SwissEphemerisProvider: GetSunTimes
SpecialPoints.UpagrahaCalculator -> Astro.SwissEphemerisProvider: GetSunTimes
SpecialPoints.SpecialPointCalculator -> SpecialPoints.SpecialPointSeed
SpecialPoints.SpecialPointSeed -> SpecialPoints.SpecialPointProjector
SpecialPoints.SpecialPointProjector -> Astro.IVargaSignRule: reuses per chart
SpecialPoints.SpecialPointProjector -> Calculators.ChartAnalysisInput: "SpecialPoints list"

Jaimini.CharaKarakaCalculator -> Jaimini.CharaKaraka: "ranks, then labels"
Jaimini.CharaKarakaCalculator -> Models.ChartKeyDetail: "CharaKaraka label (via ChartGenerationService)"

Astro.SwissEphemerisProvider -> Dasha.VimshottariDashaCalculator: Moon position
Dasha.VimshottariDashaCalculator -> Dasha.DashaPeriod: tree
Dasha.DashaPeriod -> Dasha.DashaPeriodRecord: "flattened in Data"
Dasha.DashaPeriodRecord -> Dasha.LifeWeek: "tvf in Data"

Astro.SwissEphemerisProvider -> Transits.PlanetTransitEventFinder: day-walk + bisection

Calculators.ChartViewModel -> LifeAreas.LifeAreaMap: groups charts for the Web workspace
Astro.AstroIds -> Models.ChartKeyDetail: PlanetId / SignId FKs
Astro.AstroMath <- Calculators.D1ChartComputer: sign / degree / nakshatra / house
Astro.AstroMath <- Astro.SignRules: GetXSign helpers
```

---

## Astro/ — raw astronomy + sidereal math (25 files)

The only folder that talks to the ephemeris. Everything downstream consumes its
outputs (`SiderealPositions`, `SunTimes`) or its pure helpers.

| File | Type | Purpose |
|---|---|---|
| `SwissEphemerisProvider.cs` | static + `record SiderealPositions` / `record SunTimes` | Wraps **SwissEphNet** (Moshier, Lahiri sidereal). `GetSiderealPositions` → Ascendant + 9 graha longitudes / latitudes / speeds + ayanamsha + LST. `GetSunTimes` (`swe_rise_trans`) → the Vedic-day sunrise / sunset / next-sunrise frame + night-birth flag. Replaced VedAstro.Library. |
| `AstroMath.cs` | static | Pure classical math on a sidereal longitude: sign, D-M-S formatting, nakshatra + pada, `GetNavamsaSign` / `GetDasamsaSign` / `GetShashtamsaSign` / …, `GetVargaLongitude` (`Normalize(lon × N)`), `Normalize`, `CountFromSignToSign` (Whole-Sign house count), `GetNakshatraLord` / sub-lord, canonical nakshatra names. |
| `AstroIds.cs` | static | Core enum → reference-table PK: `PlanetId = (int)PlanetName + 1`, `SignId = (int)ZodiacName + 1`. Write-path source of truth for the integer FK columns. |
| `VargaSignRuleFactory.cs` | static | `For(signRuleKey, divisionFactor)` → the matching `IVargaSignRule`. One `switch`; its cases are the contract with `tbl_Rule_VargaScheme` seed data. |
| `IVargaSignRule.cs` | interface | `ZodiacName SignFor(double siderealLongitude)` — maps a real longitude to its sign in one divisional chart. One impl per varga scheme. |
| `LinearVargaSignRule.cs` | sealed class | The "l-th part, `stride` signs forward from the rasi sign" family — D3 (3, 4), D4 (4, 3), D12 (12, 1), D60 (60, 1); also the D1 identity `(1, 1)`. |
| `HoraD2ClassicSignRule.cs` | sealed class | D2 classical two-sign Hora (odd 1st-half → Leo, 2nd → Cancer; even reversed). |
| `HoraD2UmaShambuSignRule.cs` | sealed class | D2 "Uma Shambu" — JHora's default D2 (`D-2 (US)`), PyJHora `__parivritti_even_reverse`. |
| `NavamsaD9SignRule.cs` | sealed class | D9 — wraps `AstroMath.GetNavamsaSign`. |
| `DasamsaD10SignRule.cs` | sealed class | D10 — wraps `AstroMath.GetDasamsaSign` (odd from self, even from 9th). |
| `RudramsaD11SignRule.cs` | sealed class | D11 — wraps `AstroMath.GetRudramsaSign` (Sanjay Rath method). |
| `ShashtamsaD6SignRule.cs` | sealed class | D6 — wraps `AstroMath.GetShashtamsaSign`. |
| `PanchamsaD5SignRule.cs` | sealed class | D5 — five 6° parts; odd / even signs map the l-th part to a fixed sign list. |
| `SaptamsaD7SignRule.cs` | sealed class | D7 — odd signs count from the sign itself, even from the 7th. |
| `AshtamsaD8SignRule.cs` | sealed class | D8 — movable / fixed / dual signs count the l-th part from Aries / Leo / Sagittarius. |
| `ShodasamsaD16SignRule.cs` | sealed class | D16 (Kalamsa) — movable / fixed / dual start points. |
| `VimsamsaD20SignRule.cs` | sealed class | D20 — movable / dual / fixed start points. |
| `SiddhamsaD24SignRule.cs` | sealed class | D24 (Chaturvimsamsa) — odd from Leo, even from Cancer. |
| `NakshatramsaD27SignRule.cs` | sealed class | D27 (Bhamsa) — fire / earth / air / water signs start from Aries / Cancer / Libra / Capricorn. |
| `TrimsamsaD30SignRule.cs` | sealed class | D30 — the classical **unequal** five-part scheme (5 / 5 / 8 / 7 / 5°). |
| `KhavedamsaD40SignRule.cs` | sealed class | D40 (Chatvarimsamsa) — odd signs from Aries, even from Libra. |
| `AkshavedamsaD45SignRule.cs` | sealed class | D45 — movable / fixed / dual from Aries / Leo / Sagittarius. |
| `PlanetName.cs` | enum + `PlanetNames.All9` | The 7 grahas + Rahu + Ketu, VedAstro spelling / order. |
| `ZodiacName.cs` | enum | 12 rasis, Aries-first; `Capricornus` (Latin) kept for stored-data compatibility. |
| `ConstellationName.cs` | enum | 27 nakshatras, index-only (display names come from `AstroMath.NakshatraCanonicalNames`). |

---

## Calculators/ — chart assembly + classical analysis (17 files)

The pipeline: `IChartCalculator` impls turn a `BirthDetails` into a `ChartAnalysisInput`;
`ChartAnalyzer` turns that into the derived-table rows.

| File | Type | Purpose |
|---|---|---|
| `BirthMomentFactory.cs` | internal static | `BirthDetails` → local-offset `DateTimeOffset` for the astro engine. |
| `IChartCalculator.cs` | interface | `ChartType`, `ComputeAnalysisInput(bd, specialPoints?)`, `BuildResult(bd, input)` — the plug-in point for every chart type. |
| `ChartCalculationOrchestrator.cs` | class | Holds every registered `IChartCalculator`. `CreateDefault(schemes)` = `D1RasiCalculator` + one `VargaCalculator` per `tbl_Rule_VargaScheme` row. `CalculateAll` computes the special-point seeds once, then runs every calculator. |
| `D1RasiCalculator.cs` | class : `IChartCalculator` | D1 — delegates to `D1ChartComputer`, serialises `ResultJson`. |
| `VargaCalculator.cs` | sealed class : `IChartCalculator` | One instance per varga scheme row; delegates to `VargaChartComputer` + the scheme's `IVargaSignRule`; stamps `ChartResult.VargaMethod`. Replaced the 6 bespoke D2/D6/D9/D10/D11 calculators. |
| `D1ChartComputer.cs` | static | The actual D1 computation: Ascendant + 9 grahas' sign / degree / nakshatra / house from `SwissEphemerisProvider`; projects the special-point seeds with the identity rule. |
| `VargaChartComputer.cs` | static | Any divisional chart from an injected division factor + `IVargaSignRule`: real longitude + varga longitude + Whole-Sign house from the varga Lagna; projects the special points with that chart's rule. |
| `ChartAnalysisInput.cs` | record | The one shape every calculator hands to `ChartAnalyzer`: `ChartType`, `AscendantSign`, `List<PlanetPosition> Planets`, `IReadOnlyList<PlanetPosition> SpecialPoints`. |
| `ChartAnalyzer.cs` | static | Chart-type-agnostic. Builds `ChartKeyDetail` / `ChartHouseLord` / `ChartConjunction` / `ChartAspect` rows — dignity, house-lordship (3 reckonings), conjunctions, aspects — plus position-only rows for the special points (`PointKind ≠ Graha`). |
| `ClassicalDignity.cs` | static + `record DignityResult` | Panchadha Maitri: own / exaltation / debilitation / Moolatrikona sign+range, sign lord, and the 9-tier `DignityStatus`. `GetSignLord`, `GetHouseSign`. |
| `ClassicalRelationships.cs` | static + `record ConjunctionResult` / aspect result | Graha Yuti (same sign) + Graha Drishti (7th always; Mars 4/8, Jupiter 5/9, Saturn 3/10; Rahu/Ketu Jupiter-style). Pure rules on `ChartAnalysisInput`. |
| `ClassicalCombustion.cs` | static + `record CombustionResult` | Asta orbs (BPHS / Phaladeepika) for the 6 applicable grahas, narrowed when retrograde; evaluated in the chart's own zodiac (real longitude for D1, varga longitude otherwise). |
| `LagnaFunctionalNature.cs` | static | Parashari functional Benefic / Malefic / Neutral / Yogakaraka of a planet w.r.t. a Lagna, from house-lordship. Sole source (the `tbl_Dim_LagnaFunctionalNature` mirror was removed). |
| `PlanetAvasthaComputer.cs` | static | Builds `tbl_Fact_PlanetAvastha` rows from the `ChartKeyDetail` list — Baaladi (D1 only) + Jagradadi (every chart); skips Ascendant and non-Graha rows. |
| `BaaladiAvastha.cs` | static | Planet "age" from degree-within-sign: Baala / Kumara / Yuva / Vriddha / Mrita (odd signs forward, even reversed) + effect fraction. Data-driven from `tbl_Rule_BaaladiState`. |
| `JagradadiAvastha.cs` | static | Planet "waking state" from `DignityStatus`: Jagrat / Swapna / Sushupti. Data-driven from `tbl_Rule_JagradadiState`. |
| `ChartViewModel.cs` | static + `record PlanetRow` | Presentation helpers for the Web/CLI: `PlanetGlyph` ("Su", "Mo", …), `DignityToken`, `BuildPlanetRows`, `BuildAspectedByGlyphs`. |

---

## SpecialPoints/ — Jaimini Arudhas + special lagnas + upagrahas (6 files, 2026-09-01)

Each point is one D1 longitude (`SpecialPointSeed`), then projected into all 21 vargas
by `SpecialPointProjector` using the same `IVargaSignRule` a planet uses.

| File | Type | Purpose |
|---|---|---|
| `SpecialPointSeed.cs` | sealed record | `(string Code, string PointKind, double NirayanaLongitudeDegrees)` — a point's D1 longitude before any varga projection. `PointKind` ∈ `Arudha` / `SpecialLagna` / `Upagraha`. |
| `SpecialPointCalculator.cs` | static | `ComputeSeeds(bd)` — the façade: Arudhas from the D1 chart + HL + Gulika + Maandi (the latter three via `GetSunTimes`). |
| `ArudhaCalculator.cs` | static | AL + the 12 Bhava Arudhas (Parashari: `n` signs from the house lord; 1st/7th exception → 10th; placed at the natal Lagna degree). |
| `HoraLagnaCalculator.cs` | static | Hora Lagna — Sun's longitude at the Vedic day's opening sunrise + 0.5°/clock-minute of time-of-day since it (negative for a night birth). |
| `UpagrahaCalculator.cs` | static | Gulika + Maandi — day/night arc ÷ 8; the Ascendant at the **start** of Saturn's part is Gulika, at its **middle** is Maandi; weekday is the Vedic day's. |
| `SpecialPointProjector.cs` | static | `Project(seeds, rule, divisionFactor, lagnaSign)` → `List<PlanetPosition>` (sign / varga longitude / house). Shared by `D1ChartComputer` and `VargaChartComputer`. |

---

## Jaimini/ — Chara Karakas (2 files, 2026-09-01)

| File | Type | Purpose |
|---|---|---|
| `CharaKaraka.cs` | enum | `AK, AmK, BK, MK, PiK, PK, GK, DK` — the 8 movable significators, ranked (AK strongest). |
| `CharaKarakaCalculator.cs` | static | `Assign(degreeInSignByPlanet)` — rank Sun…Saturn + Rahu by degree-within-sign descending; **Rahu keyed `30 − deg`**; Ketu unranked. Highest → AK, lowest → DK. |

---

## Dasha/ — Vimshottari periods (4 files)

| File | Type | Purpose |
|---|---|---|
| `VimshottariDashaCalculator.cs` | static | Classical 3-level Maha / Antar / Pratyantar sequence from the Moon's nakshatra position; all 3 levels correctly partial-at-birth. |
| `DashaPeriod.cs` | class | One period at any level — a **tree** (`Children` = next level down). The write-side shape `VimshottariDashaRepository` flattens into `tbl_Chart_DashaPeriods`. |
| `DashaPeriodRecord.cs` | class | The **read-back** shape from `tbl_Chart_DashaPeriods` — flat (Dapper-friendly), with `Id` / `ParentDashaPeriodId` so `BuildTree` rebuilds the hierarchy for the UI. |
| `LifeWeek.cs` | class | One row of `tvf_Chart_LifeWeeks` — week 1–4000 with its active Maha/Antar/Pratyantar lord. Backs `LifeWeeks.razor`. |

---

## Geocoding/ — place → lat/long/offset (3 files)

| File | Type | Purpose |
|---|---|---|
| `IPlaceResolver.cs` | interface + `record ResolvedPlace` | `"City, Country" + birth date` → `(Latitude, Longitude, IanaTimeZoneId, UtcOffset)`. Kept behind an interface so the strategy can be swapped. |
| `NominatimPlaceResolver.cs` | class : `IPlaceResolver` | OpenStreetMap Nominatim geocode (no key), then offline lat/long + date → IANA zone → **historical** UTC offset via GeoTimeZone + TimeZoneConverter. |
| `ManualPlaceResolver.cs` | class : `IPlaceResolver` | Offline fallback — the caller supplies lat / long / offset directly. |

---

## Transits/ — slow-planet sign crossings (1 file)

| File | Type | Purpose |
|---|---|---|
| `PlanetTransitEventFinder.cs` | class + `record PlanetTransitEvent` | Day-walk + bisection to find every Saturn / Jupiter / Rahu sign-boundary crossing (incl. retrograde re-entries) 1930–2060. Feeds `tbl_PlanetSignTransitEvents`. |

---

## LifeAreas/ — Web workspace grouping (1 file)

| File | Type | Purpose |
|---|---|---|
| `LifeAreaMap.cs` | static + `enum LifeArea` | The four life-area groupings (`PersonalityHealth`, `Relationships`, `Career`, `Money`) the Web workspace organises charts around. Interpretation stays out of scope. |

---

## Models/ — DTOs & DB row shapes (19 files)

Plain data. No behaviour. Populated by `Calculators` / `SpecialPoints` / `Dasha`,
persisted by `Ikiastrro.Data`.

| File | Type | Purpose |
|---|---|---|
| `BirthDetails.cs` | class | The input record — name / DOB / TOB / place. Never changes shape as chart types are added. |
| `ChartResult.cs` | class | One computed chart/calculation artifact for a `BirthDetails`; `ChartType` string + numeric `AyanamshaDegrees` / `SiderealTimeHours` / `VargaMethod` + `ResultJson` (frozen audit snapshot). |
| `PlanetPosition.cs` | class | One point's placement within a computed chart — sign, degrees, nirayana + varga longitude, latitude / speed, house, `IsRetrograde`, `PointKind`. |
| `ChartKeyDetail.cs` | class | One row of `tbl_Chart_KeyDetails` — flattened position + dignity + nakshatra + combustion + aspects, plus `PointKind` (row discriminator) + `CharaKaraka` label. |
| `ChartHouseLord.cs` | class | One row of `tbl_Chart_HouseLords` — house → sign → lord → where the lord sits (3 reckonings + dignity). |
| `ChartConjunction.cs` | class | One row of `tbl_Chart_Conjunctions` — a canonical (`Planet1Id < Planet2Id`) graha pair sharing a sign; `DegreeSeparation` D1-only. |
| `ChartAspect.cs` | class | One row of `tbl_Chart_Aspects` — one directional Graha Drishti; target is a graha or `"Ascendant"`. |
| `AvasthaModels.cs` | records | `AvasthaStateRow` / `BaaladiRuleRow` / `JagradadiRuleRow` / `AvasthaRuleSet` — the `tbl_Dim_AvasthaState` + `tbl_Rule_*` shapes the avastha computers read. |
| `VargaScheme.cs` | record | One row of `tbl_Rule_VargaScheme` — `DivisionFactor`, `MethodCode`, `SignRuleKind`, `SignRuleKey`, `RuleSetId`. D1 not represented (identity). |
| `ChartTypeRow.cs` | record | One row of `tbl_Dim_ChartType` — controlled vocabulary for `ChartResults.ChartTypeId`. |
| `GenerationReport.cs` | record | Outcome of a `ChartGenerationService` run — what was (re)written, whether Dasha ran, what was skipped. |
| `SadeSatiPeriod.cs` | record | One row of `tvf_Chart_SadeSatiPeriods` — a Saturn-from-natal-Moon affliction window (Dhaiya / Kantaka / Ashtama). |
| `PlanetTransitSnapshot.cs` | record | Where a slow planet sits sidereally as of a date + when it next changes sign. |
| `HouseNakshatraSpanRow.cs` | record | One row of `vw_Chart_HouseNakshatraSpan` — a house → sign → nakshatra-pada slice. |
| `PlanetReference.cs` | record | One row of `tbl_Planets` (static graha reference — Vimshottari years / order, natural nature). |
| `SignAttributeReference.cs` | record | One row of `tbl_SignAttributes` (element, chara/sthira/dwiswabhava, exaltation/debilitation). |
| `NakshatraReference.cs` | record | One row of `tbl_Nakshatras` (ruling planet, primary rasi, boundary-straddle flag). |
| `NakshatraPadaReference.cs` | record | One row of `tbl_NakshatraPadas` — the four 3°20′ padas of each nakshatra (1:1 with navamsa). |
| `NakshatraSubLordReference.cs` | record | One row of `tbl_NakshatraSubLords` — KP Vimshottari sub-lord hierarchy, levels 1–2 only. |

---

## Cross-folder dependency summary

| From | Depends on | Why |
|---|---|---|
| `Calculators` | `Astro` | longitudes (`SwissEphemerisProvider`), sign math (`AstroMath`), sign rules (`IVargaSignRule`), FK ids (`AstroIds`) |
| `Calculators` | `Models` | produces `ChartKeyDetail` / `ChartHouseLord` / `ChartConjunction` / `ChartAspect`; consumes `PlanetPosition` |
| `SpecialPoints` | `Astro` + `Calculators` | `GetSunTimes`, `IVargaSignRule`, `D1ChartComputer`, `ClassicalDignity.GetSignLord` |
| `Jaimini` | `Astro` | `PlanetName` only (pure ranking) |
| `Dasha` | `Astro` | Moon position from `SwissEphemerisProvider` (via the CLI/Data caller) |
| `Transits` | `Astro` | repeated `SwissEphemerisProvider` calls in a day-walk |
| `LifeAreas` | *(none in Core)* | consumed by `Ikiastrro.Web` only |
| `Models` | `Astro` | enum types (`ZodiacName`, `PlanetName`) in some DTOs |
| `Astro` | *(nothing in Core)* | leaf layer — only SwissEphNet + BCL |

Orchestration and persistence live **outside** this project: `ChartCalculationOrchestrator`
is driven by `Ikiastrro.Data.ChartGenerationService`, which also owns the
`CharaKarakaCalculator` → `ChartKeyDetail.CharaKaraka` stamping and the
`PlanetAvasthaComputer` call.

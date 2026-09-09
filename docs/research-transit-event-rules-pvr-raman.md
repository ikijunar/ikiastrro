---
last_updated: 2026-09-07
---

# Transit-event rules: PVR, B. V. Raman, and database mapping

This note inventories transit rules found in the local PVR extract and B. V. Raman
Volumes I and II, then maps them to the current ikiastrro model. Ikiastrro uses
whole-sign houses. Transit calculations must use the same sidereal zodiac and
ayanamsa as the natal chart.

## Shared foundation

Both authors distinguish the fixed natal positions from the temporary positions of
planets on a selected date. A transit event is therefore evaluated as:

```text
transit timestamp -> sidereal transit longitude/sign
natal chart       -> reference sign, natal houses, planets, lords and significators
comparison        -> house-from-reference, occupation, aspect, conjunction and timing
```

The transit does not modify the birth chart. It activates natal houses, planets,
lords, karakas, arudhas, or divisional-chart factors already present in it.

## Rules found in the extracts

| Rule family | PVR extract | Raman extracts | DB consequence |
|---|---|---|---|
| Transit from natal Moon (Janma Rasi) | Standard 1st–12th results for Sun, Moon, Mars, Mercury, Jupiter, Venus and Saturn; Rahu follows Saturn and Ketu follows Mars | Gochara is secondary and must be checked against the natal promise and dasha | Store a rule matrix keyed by transit planet and house-from-Moon; evaluate against natal planet significations |
| Transit from Lagna and other references | Use Lagna, Paaka Lagna, Arudha, special lagnas and other natal references; judge houses and planets in the transited/aspected signs | Raman repeatedly uses Lagna, Moon, Bhava, Mandi and house lords as transit anchors | Reuse `tbl_Dim_HouseReference`; populate `tbl_Fact_HouseFromReference` for each transit snapshot |
| Transit planet’s natal role | Transit results depend on the planet’s natural significations and natal lordship/occupation | Event timing requires the relevant house lord, occupant, karaka and dasha links | Add rule predicates for natal lordship, occupation, dignity, aspect and karaka rather than storing unconditional results |
| Transit aspects | A transiting planet influences natal houses and planets in the signs it occupies or aspects; Jupiter over natal 7th house/7th lord is a marriage trigger example | Transit aspects are used with natal house and lord strength | Existing `tbl_Chart_Aspects` is natal-only; add transit-to-natal aspect facts with exact timestamp/orb/source |
| Natal divisional chart × transit Rasi | Coarse timing: transit Rasi planet activates houses/planets in natal D-chart | Not a central Raman method in these extracts | Add `NatalChartResultId`, `TransitChartType='Rasi'`, and target D-chart fact keys to a transit observation table |
| Natal Rasi × transit divisional chart | Fine timing: transit D-chart shows momentary forces acting on natal Rasi | Not established as a Raman rule in the extracts | Keep as PVR research rule; do not mix with ordinary Rasi transit rows |
| Ashtakavarga | BAV/SAV judge whether a transit sign is benefic; 5+ references good, 3 or fewer bad; 6–7 rekhas excellent, 0–1 very poor; SAV >30 favorable and <25 unfavorable | Raman Vol. I notes Ashtakavarga but treats it as a separate method | Add BAV/SAV tables by natal chart/varga, transit planet and sign; store threshold evaluation separately |
| Kakshya / Prastara | Divide each sign into eight 3°45′ segments; judge the kakshya lord and PAV rekha; useful only for fine timing with dasha/Tajaka | No matching detailed rule located | Store transit degree, kakshya index/lord and PAV result; mark as fine-timing evidence |
| Sodhya Pinda | Multiply relevant BAV rekhas by Sodhya Pinda; remainder identifies a nakshatra or sign where Saturn is harmful and Jupiter beneficial | Raman gives analogous longitude-difference and sum methods in death/mother timing examples | Add derived timing-point facts with formula, source and target matter; keep experimental because PVR flags unresolved variants |
| Saturn cycles / Sade Sati | Transit from natal Moon is the main reference; PVR also stresses chart-specific adaptation | Saturn through 12th, 1st and 2nd from Moon is Sade Sati; Saturn transit is secondary to maraka dasha and natal indications | Existing `tvf_Chart_SadeSatiPeriods` and transit event table cover the basic sign windows |
| Saturn house destruction / longevity | Transit of Saturn over relevant natal houses, lords or calculated points can activate suffering | Saturn over the sign of the 8th lord, 22nd Drekkana lord, Mandi-derived points, or their trines; only meaningful when natal/dasha indicators agree | Store as qualified, source-specific rules; do not label a transit alone as death |
| Jupiter event activation | Transit Jupiter over/aspecting natal 7th house, 7th lord, Venus or marriage Saham can time marriage | Jupiter through points/signs derived from longevity formulas can be a death indicator in qualified cases | Add event-specific target-point and aspect rules; avoid a generic “Jupiter good” flag |
| Sun and Moon trigger transits | Sun/Moon transits over natal houses, planets, nakshatras and calculated points can activate results | Raman gives Sun through relevant Dwadasamsa/Navamsa and Moon through 8th-lord, Sun, Mandi and Lagna-related signs | Requires degree-aware transit observations and derived natal reference points |

## What the current database already supports

- `tbl_PlanetSignTransitEvents` stores sign-boundary events for Saturn, Jupiter and
  Rahu, with Ketu derived by the existing view/function.
- `tvf_PlanetSignAtDate(@PlanetId, @AsOfDateUtc)` returns the sign at a selected UTC
  timestamp.
- `tvf_Chart_SadeSatiPeriods(@BirthDetailId)` derives Saturn’s 12th/1st/2nd-from-Moon
  windows, including retrograde re-entries.
- Natal chart facts already provide planetary positions, house lords, aspects,
  conjunctions, dignity/state and house references.
- `GocharaRepository` provides the current slow-planet snapshot for the planned UI.

## Gaps before interpretation can be stored

1. Transit events currently contain sign crossings, not a full point-in-time row for
   every graha with longitude, nakshatra, retrograde state and speed.
2. `tbl_Fact_HouseFromReference` is currently empty, so house-from-Moon, Lagna,
   Paaka, Arudha and other reference projections are not persisted generically.
3. Natal `tbl_Chart_Aspects` cannot represent transit-to-natal aspects.
4. Ashtakavarga BAV/SAV, Kakshya/PAV and Sodhya Pinda facts are not built.
5. No transit rule table or rule-evaluation result table exists.

## Proposed additive DB path

### 1. Point-in-time transit observation

Add `tbl_Fact_TransitPosition`:

```text
Id, BirthDetailId, AsOfUtc, PlanetId, Longitude, SignId, DegreeInSign,
NakshatraId, MotionDirection, IsRetrograde, AyanamsaId, CalculationVersion
```

This is the reusable input for every rule. The birth chart remains linked through
`BirthDetailId`; the transit timestamp is independent of the birth timestamp.

### 2. Reference and house projection

Add `tbl_Fact_TransitHouseFromReference`:

```text
TransitPositionId, NatalChartResultId, ReferenceId, ReferenceSignId,
TransitHouseNumber, TargetHouseId, TargetPlanetId, IsOccupied, IsAspected
```

This supports Janma Rasi, Lagna, Paaka Lagna, Arudha and selected divisional-chart
references without changing the whole-sign house engine.

### 3. Rule definitions and evaluations

Add `tbl_Rule_TransitEvent` for sourced rules and
`tbl_Fact_TransitRuleEvaluation` for results:

```text
RuleCode, RuleSetId, SourceRefCode, TransitPlanetId, ReferenceId,
HouseFromReference, TargetType, TargetKey, RequiredConditions, ResultClass
```

```text
BirthDetailId, AsOfUtc, RuleId, NatalChartResultId, TransitPositionId,
MatchedConditions, StrengthScore, ResultClass, Confidence, Explanation
```

`RequiredConditions` should be structured predicates in the long term. A temporary
JSON column is acceptable for research, but the source, rule version and matched
conditions must remain queryable.

### 4. Build order

1. General transit snapshot for all nine grahas, degree and retrograde state.
2. Generic whole-sign projection from Moon, Lagna and existing house references.
3. PVR Moon/Lagna house-result matrix and transit-aspect facts.
4. Raman-qualified Saturn/Jupiter/Sun/Moon event rules.
5. BAV/SAV and Sade Sati refinements.
6. Kakshya, Sodhya Pinda and divisional-chart research features.

The first production slice should be a transparent Saturn/Jupiter snapshot with
house-from-Moon and Lagna, natal targets, source-backed explanation, and no fatal
prediction label. Raman’s warnings that transits are secondary and require natal
and dasha support must be encoded as a rule-confidence condition.
### Retrograde coverage and sign versus nakshatra

The live `tbl_PlanetSignTransitEvents` table preserves sign re-entry: Saturn has 109 rows including 28 retrograde crossings, Jupiter has 229 including 48, and Rahu has 84. Each row records `MotionDirection` and `IsReentry`, so sign-level queries can show direct entry, retrograde return, and later direct re-entry. Ketu is derived from Rahu by the existing view/function.

This is sufficient for the first gochara layer, but it is not a full retrograde ephemeris. The table has no longitude, station time, nakshatra, pada, or speed, so it cannot answer whether a planet is retrograde inside a particular nakshatra or whether it crossed a natal degree three times.

The sources use both levels for different purposes:

- **Rāśi/sign** is primary for house-from-Moon or Lagna, Sade Sati, Saturn/Jupiter sign transits, natal-house activation, and transit aspects.
- **Nakshatra** refines timing. PVR’s Sodhya Pinda method derives a target nakshatra, and its Kakshya/Prastara method uses degree segments within a sign. Raman also uses nakshatra, Navamsa and Dwadasamsa transit points in qualified timing rules.

The DB should retain sign events for the baseline and add a point-in-time position table with longitude, nakshatra/pada, speed and retrograde state before implementing nakshatra-sensitive rules.

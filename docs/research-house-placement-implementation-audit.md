---
last_updated: 2026-09-07
---

# House-placement rules: current coverage and next additions

This audit compares the three local reference extracts with the current ikiastrro
implementation. The project convention is **whole-sign houses**: the sign containing a
reference point is house 1, and subsequent signs are counted inclusively. PVR is the
geometry baseline; Raman's two volumes supply supplementary placement interpretations.

Sources:

- `SRC_PVR_INTEGRATED`: `BookExtracts\pvr-integrated-approach-raw.txt`.
- `SRC_RAMAN_HTJH`: Volume I, `BookExtracts\how-to-judge-a-horoscope-1.md`.
- `SRC_RAMAN_HTJH`: Volume II, `BookExtracts\how-to-judge-a-horoscope-2-ocr.md`; full
  extraction quality is recorded in [the Volume II report](research-raman-volume2-extraction.md).

## What is already implemented

| Capability | Current evidence | Source coverage | State |
|---|---|---|---|
| Whole-sign house geometry | `HouseEngine.GetHouseSign`; `reference-calculations.md` | PVR chapter 7 §7.5 | Implemented and verified by existing engine structure |
| House lord and lord placement | `tbl_Chart_HouseLords` + `HouseEngine.BuildHouseLords`; D1 and vargas | PVR §7; Raman Vol. I/II house-lord chapters | Computed facts, not interpretations |
| Placement from Lagna, Sun and Moon | `ChartKeyDetail.HouseNumberFromLagna/FromSun/FromMoon`; persisted key details | PVR §7.3 | Implemented for these three references |
| House meanings | 12-house dimension plus `tbl_Rule_HouseSignification` | PVR §7.2; Raman Vol. I/II introductions | 121 active PVR rows |
| House categories | `tbl_Dim_HouseCategory` | PVR §7.4 | 8 categories: Kendra, Trikona, Panaphara, Apoklima, Upachaya, Dusthana, Chaturasra, Maraka |
| House attributes | `tbl_Rule_HouseAttribute` | PVR §7.4 and Table 12 | 38 active rows: 12 general character, 12 category effects, 14 natural-significator mappings |
| Reference-point catalogue | `tbl_Dim_HouseReference` | PVR §7.3 | 17 definitions, including Lagna, Chandra/Ravi, Arudha, Paaka, Karakamsa, Ghati, Hora, Sree, Bhaava and seven graha references |
| House reference matter | `tbl_Rule_HouseReferenceMatter` | PVR Table 12 | 14 rows; rule definitions exist |
| House lord functional nature | `LagnaFunctionalNature` | Raman Vol. I pp. 6–8; PVR lordship framework | Computed classifier and verification mode; documented heuristic |
| Graha aspects | `RelationshipEngine`, `tbl_Chart_Aspects`, `tbl_Rule_AspectOffset` | PVR/Parāśari graha dṛṣṭi | Computed facts available to future placement rules |
| Conjunctions | `RelationshipEngine`, pair and multi-graha tables | Raman/PVR placement qualifications | Computed facts available |
| Planetary dignity and states | `DignityEngine`, `PvrDignityEvaluator`, planetary-state facts | PVR; Raman qualifications | Available as modifiers; strength work remains partial |
| Bhava strength | `BhavaBalaCalculator`, `tbl_Fact_BhavaStrength` and components | PVR component; Raman's referenced Bhava Bala tradition | Foundational comparative score, not a complete classical verdict |
| Divisional confirmation | 21 chart types and house-lord rows per chart | PVR §7.3/§13.4; Raman asks for Rasi/Navamsa checks | Chart facts available; placement interpretation not automated |

The live database check found 42 whole-sign chart rows, 504 house-lord rows, 533 aspect
rows and 200 conjunction rows. Ramakrishnan's D1 house-lord facts are therefore already
available: Mars rules houses 1/8 and is in 1; Venus rules 2/7 and is in 1; Sun rules 5
and is in 1; Jupiter rules 9/12 and is in 6; Saturn rules 10/11 and is in 6; Moon rules
4 and is in 8.

## What is not yet a placement interpretation

`tbl_Rule_HouseSignification` describes what a house signifies. It does not express
“lord of house X in house Y, when condition Z, supports result R.” The following are
also empty or only reserved:

| Gap | Evidence | Consequence |
|---|---|---|
| Conditional house-lord placement rules | No `tbl_Rule_HouseLordPlacement` or equivalent table; no placement-rule consumer | Raman's 12 × 12 lord-placement prose is not executable |
| Conditional planet-in-house rules | No `tbl_Rule_PlanetHousePlacement` table or consumer | Raman's occupant sections are reference-only |
| Yoga rules | `tbl_Rule_Yoga` = 0 rows; `IYogaEngine` is a reserved seam | PVR Harsha/Sarala/Vimala and Rāja/Dhana candidates cannot be detected |
| Karaka rules | `tbl_Rule_Karaka` = 0 rows; Sthira/Naisargika mapping remains partly static | Karaka-based confirmation is incomplete |
| House-from-reference facts | `tbl_Fact_HouseFromReference` = 0 rows | The 17 reference definitions are not projected per chart |
| Rāśi dṛṣṭi and argala | Only graha dṛṣṭi is implemented | PVR §13.4 influence synthesis is incomplete |
| Ashtakavarga | Not built | Raman/PVR house-strength qualifications cannot use BAV/SAV |
| Full classical strength thresholds | Bhava Bala and Shadbala are partial/current-profile calculations | “Strong”, “weak”, “fortified” cannot yet be reduced to one authoritative threshold |

## Rules that can be added next under whole-sign houses

These are the best first additions because their conditions are explicit, they map to facts
already stored, and they do not require cusp calculations.

| Priority | Rule family | Source candidates | Required inputs | Recommended first slice |
|---|---|---|---|---|
| 1 | House-lord placement, houses 1–6 | Raman Vol. I | lord house, occupied house, dignity, aspects, conjunctions, Navamsa lord placement | First through sixth lord, with favourable/afflicted branches kept separate |
| 2 | House-lord placement, houses 7–12 | Raman Vol. II | same, plus each chapter's karaka and explicit conjunction conditions | Seventh through twelfth lord; Volume II now supplies the missing sections |
| 3 | Simple own-house/dusthana yogas | PVR §11.6 | lord equals occupied house; strength/affliction modifier | Harsha (6th lord in 6th), Sarala (8th lord in 8th), Vimala (12th lord in 12th) |
| 4 | House and occupant rule pairs | Raman Vol. I/II | planet, house, dignity, aspects and conjunctions | Planet in 1st/3rd/7th/8th/9th/10th/11th/12th, where the reviewed passages are clearest |
| 5 | Relative reference projection | PVR §§7.3, 13.4 | reference sign, target house, chart type, reference code | Fill `tbl_Fact_HouseFromReference` for Lagna, Moon, Sun, Arudha and Karakamsa where calculable |
| 6 | Influence synthesis | PVR §§7.4, 13.4 | house categories, graha dṛṣṭi, conjunctions, later argala/rāśi dṛṣṭi | Generate evidence rows showing sustaining, flourishing, growth and obstacle influences |

## Rules requiring care before implementation

- Raman repeatedly qualifies results with “well disposed”, “strong”, “afflicted” and
  “fortified”. Store these as conditions first. Do not invent a score threshold by silently
  equating them with dignity or Shadbala.
- Some rules require conjunction with another lord, a specific karaka, a Navamsa condition
  or a benefic/malefic flank. A row for “lord X in house Y” alone is incomplete.
- Keep separate result topics. Wealth, family, health, spouse, career and timing are not
  one undifferentiated prediction even when one placement affects several topics.
- Raman's Bhava-sandhi examples are not directly executable under ikiastrro's whole-sign
  convention. Preserve them as source notes or reformulate them explicitly for whole-sign
  use after review.
- PVR's reference-lagna framework is broader than the three currently persisted house
  numbers. Arudha, Paaka, Karakamsa and graha-lagna rules need a reference-specific fact
  model; they should not be squeezed into `HouseNumberFromLagna`.
- A PVR referral to Raman is provenance, not independent corroboration. Store each source
  assertion separately and compare them in the research layer.

## Recommended data shape

The existing rule-table pattern supports a new versioned table such as
`tbl_Rule_HouseLordPlacement` with `RuleSetId`, `SourceRefCode`, `OwnedHouseNumber`,
`OccupiedHouseNumber`, `ChartScope`, `ReferenceCode`, `ConditionJson`, `ResultCode`,
`TopicCode`, `CalculationNarrative`, `Priority` and `IsActive`. A companion
`tbl_Rule_PlanetHousePlacement` would carry `PlanetId` and `OccupiedHouseNumber`.

The engine should emit evidence rather than a single verdict: matched rule, unmet or
unknown condition, source, topic, chart/reference scope, and any conflicting source rule.
That lets Raman's detailed conditional prose enrich the PVR baseline without hiding
differences.

## Audit conclusion

The placement geometry and raw evidence layer are already strong. The missing feature is
the interpretation layer: versioned, conditional, source-traceable rules for a lord or
planet occupying a whole-sign house. The safest first implementation is Raman's explicit
house-lord rules, starting with houses 1–6, paired with PVR's three own-house dusthana
yogas and the existing dignity/aspect/conjunction facts. The next slice should be a
first-house pilot with verification against Ramakrishnan's stored D1, then expand to
houses 2–12.

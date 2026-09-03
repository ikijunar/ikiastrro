# Graha characters — PVR (consolidated)

Normalized form of rammyps's PVR-consolidated graha-attributes worksheet. Backs
`db/26_add_rule_graha_attributes.sql`. Source content is the classical BPHS ch. 3
(*Grahaguṇasvarūpādhyāya*) material as presented / consolidated by rammyps from
P. V. R. Narasimha Rao, *Vedic Astrology: An Integrated Approach*.

`SourceRefCode = SRC_PVR_INTEGRATED` on every seeded row. A blank cell means **no value
defined for this source** (not "not implemented yet") — no row is inserted for it.

## Storage

| Worksheet column | Where it lands |
|---|---|
| `Strength` (directional-strength house) | `tbl_Rule_DigBala` (`DigBalaHouse` 1–12, Lagna = 1) — a **strength** concept, kept out of the generic bag |
| everything else | `tbl_Rule_GrahaAttribute` — one row per `(RuleSetId, GrahaId, AttributeCode)`, `ValueCode` (canonical token) + `ValueText` (label) |
| attribute list | `tbl_Dim_GrahaAttribute` (16 rows) |

`tbl_Planets` (the graha master) is unchanged. `RuleSetId = 1`; a BPHS variant, if ever
wanted, becomes a second rule-set.

## `tbl_Rule_DigBala`

| Graha | `DigBalaHouse` |
|---|--:|
| Sun | 10 |
| Moon | 4 |
| Mars | 10 |
| Mercury | 1 (Lagna) |
| Jupiter | 1 (Lagna) |
| Venus | 4 |
| Saturn | 7 |

Rāhu / Ketu: no value in this source.

## `tbl_Dim_GrahaAttribute`

| `AttributeCode` | Meaning | `ValueKind` |
|---|---|---|
| `SUBSTANCE_CLASS` | dhātu / mūla / jīva — the class of substance the graha governs | CODE |
| `BODY_DHATU` | the bodily tissue (sapta-dhātu) the graha signifies | CODE |
| `TIME_PERIOD` | the unit of time (kāla) the graha lords | CODE |
| `DIURNAL_STRENGTH` | when the graha is strong — day / night / always | CODE |
| `RITU` | the season (ṛtu) the graha lords (6-fold; the Sun lords the ayana, not a ṛtu) | CODE |
| `NATURAL_SIGNIFICATION` | the single core matter the graha governs | CODE |
| `COLOR` | colour | CODE |
| `ROYAL_STATUS` | royal-cabinet rank | CODE |
| `PRESIDING_DEITY` | presiding deity (adhidevatā) | CODE |
| `GENDER` | gender (liṅga) | CODE |
| `TATTVA` | element (pañca-tattva) | CODE |
| `GENERAL_CHARACTER` | free-text disposition summary | TEXT |
| `VARNA` | varṇa (caste) | CODE |
| `VARNA_TRAIT` | the behavioural trait implied by the varṇa | CODE |
| `GUNA` | guṇa | CODE |
| `RESIDENCE` | residence (vāsa-sthāna) | CODE |

## `tbl_Rule_GrahaAttribute` — the normalized grid

`·` = blank in the source (no row inserted). `ValueCode` shown; `ValueText` is the readable label.

| Graha | SUBSTANCE_CLASS | BODY_DHATU | TIME_PERIOD | DIURNAL_STRENGTH | RITU | NATURAL_SIGNIFICATION | COLOR | ROYAL_STATUS | PRESIDING_DEITY | GENDER | TATTVA | GENERAL_CHARACTER | VARNA | VARNA_TRAIT | GUNA | RESIDENCE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Sun | VEGETABLE | ASTHI | SIX_MONTHS | DAY | · | SOUL | BLOOD_RED | KING | AGNI | MALE | AGNI | · | KSHATRIYA | BRAVERY | SATTVA | TEMPLE |
| Moon | MINERAL | RAKTA | MINUTE | NIGHT | RAINY | MIND | TAWNY | KING | VARUNA | FEMALE | JALA | · | VAISHYA | SOCIABILITY | SATTVA | WATERY_PLACE |
| Mars | MINERAL | MAJJA | WEEK | NIGHT | SUMMER | STRENGTH | BLOOD_RED | ARMY_CHIEF | SUBRAHMANYA | MALE | AGNI | "Leadership, enterprise" | KSHATRIYA | BRAVERY | TAMAS | · |
| Mercury | ANIMAL | TWAK | TWO_MONTHS | ALWAYS | DEW | SPEECH | GRASS_GREEN | PRINCE | MAHA_VISHNU | FEMALE † | PRITHVI | "Memory, logical abilities" | VAISHYA ‡ | SOCIABILITY | RAJAS | SPORTS_GROUND |
| Jupiter | ANIMAL | MEDAS | MONTH | DAY | WINTER | KNOWLEDGE_HAPPINESS | TAWNY | MINISTER | INDRA | MALE | AKASHA | "Wisdom, intelligence, perceiving knowledge" | BRAHMANA | LEARNING | SATTVA | TREASURE_HOUSE |
| Venus | VEGETABLE | SHUKRA | FORTNIGHT | DAY | SPRING | POTENCY | VARIEGATED | MINISTER | SACHI | FEMALE | JALA | "Imagination, creative work" | BRAHMANA | LEARNING | RAJAS | · |
| Saturn | MINERAL | SNAYU | YEAR | NIGHT | FALL | GRIEF | BLACK | SERVANT | BRAHMA | FEMALE † | VAYU | "Wandering, free spirit" | SHUDRA | DILIGENCE | TAMAS | FILTHY_AREA |
| Rāhu | MINERAL | · | · | · | · | · | · | SOLDIER | · | · | · | · | · | · | · | · |
| Ketu | ANIMAL | · | · | · | · | · | · | SOLDIER | · | · | · | · | · | · | · | · |

**† Gender divergence** — the PVR-consolidated worksheet gives Mercury and Saturn as **Female**;
classical BPHS ch. 3 assigns them *napuṃsaka* (neuter). Seeded as the worksheet has it; the `Notes`
column on those two rows records the discrepancy.

**‡ Varṇa divergence** — the worksheet gives Mercury **Vaiśya**; several BPHS renderings give Śūdra.
Recorded in `Notes`.

## Value vocabularies

| Attribute | `ValueCode` → `ValueText` |
|---|---|
| SUBSTANCE_CLASS | MINERAL "Metals and materials" · VEGETABLE "Roots and vegetables" · ANIMAL "Living beings" |
| BODY_DHATU | ASTHI "Bones" · RAKTA "Blood" · MAJJA "Marrow" · TWAK "Skin" · MEDAS "Fat" · SHUKRA "Semen" · SNAYU "Muscles" |
| TIME_PERIOD | SIX_MONTHS "6 months" · MINUTE "Minute" · WEEK "Week" · TWO_MONTHS "2 months" · MONTH "Month" · FORTNIGHT "Fortnight" · YEAR "Year" |
| DIURNAL_STRENGTH | DAY "Day" · NIGHT "Night" · ALWAYS "Always" |
| RITU | SPRING "Spring" · SUMMER "Summer" · RAINY "Rainy season" · DEW "Dew (autumn)" · WINTER "Winter" · FALL "Fall (late winter)" |
| NATURAL_SIGNIFICATION | SOUL · MIND · STRENGTH · SPEECH · KNOWLEDGE_HAPPINESS "Knowledge and happiness" · POTENCY · GRIEF |
| COLOR | BLOOD_RED "Blood red" · TAWNY · GRASS_GREEN "Grass green" · VARIEGATED · BLACK |
| ROYAL_STATUS | KING · PRINCE · MINISTER · ARMY_CHIEF "Army chief" · SERVANT · SOLDIER |
| PRESIDING_DEITY | AGNI "Agni (fire god)" · VARUNA "Varuna (rain god)" · SUBRAHMANYA "Subrahmanya (army-chief god)" · MAHA_VISHNU "Maha Vishnu (supreme sustaining force)" · INDRA "Indra (ruler of the gods)" · SACHI "Sachi Devi (Indra's consort)" · BRAHMA "Brahma (the creator)" |
| GENDER | MALE · FEMALE (NEUTER available, unused by this source) |
| TATTVA | AGNI "Agni (fire)" · JALA "Jala (water)" · PRITHVI "Prithvi (earth)" · VAYU "Vayu (air)" · AKASHA "Akasha (ether)" |
| VARNA | BRAHMANA · KSHATRIYA · VAISHYA · SHUDRA |
| VARNA_TRAIT | BRAVERY · SOCIABILITY "Getting along with others" · LEARNING "Learning and intelligence" · DILIGENCE "Hard working" |
| GUNA | SATTVA "Sattva (pure, truthful)" · RAJAS "Rajas (passionate, energetic, impure)" · TAMAS "Tamas (dark, mean, depraved)" |
| RESIDENCE | TEMPLE · WATERY_PLACE "Watery place" · SPORTS_GROUND "Sports ground" · TREASURE_HOUSE "Treasure house" · FILTHY_AREA "Filthy area" |

## Downstream

- `VARNA_TRAIT` is a 1:1 function of `VARNA` (Kṣatriya→Bravery, Vaiśya→Sociability, Brāhmaṇa→Learning,
  Śūdra→Diligence) — kept as its own attribute only to preserve the worksheet 1:1; a consumer can
  ignore it and derive from `VARNA`.
- `DIG_BALA_HOUSE` feeds the future Ṣaḍbala / Dig Bala calculation — it is *one* strength input, not
  overall planetary strength.
- `TIME_PERIOD` slots into the Ayana / Ṛtu / Māsa / Pakṣa / Vāra / Muhūrta kāla hierarchy.

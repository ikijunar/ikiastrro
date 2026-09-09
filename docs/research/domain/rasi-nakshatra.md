# Research — Rāśi and Nakṣatra Characteristics

**Status:** research baseline — descriptive reference, not a prediction engine
**Created:** 2026-09-06
**Purpose:** collect the structural attributes, traditional significance, and implementation
notes needed for the rāśi/sign and nakṣatra reference layer.

## Reading and source policy

In Jyotiṣa, a rāśi is a 30° sector of the sidereal zodiac. A nakṣatra is a 13°20′ sector
of the same 360° circle; the usual 27-nakṣatra scheme gives four 3°20′ padas per
nakṣatra. Parāśara presents the nakṣatras as the fixed circle through which the moving
grahas pass, and the rāśi rising at birth as the lagna from which chart results are
judged (`SRC_BPHS`).

The rows below separate structural attributes, classical descriptive attributes, and
interpretive synthesis. The synthesis is a study aid, not a standalone verdict about a
person. Fields whose assignment varies by text or lineage must retain a source and rule
set in the database.

## Rāśi structure

| # | Rāśi | Sanskrit | Element | Modality | Polarity | Ruler | Symbol | Core interpretive function |
|---:|---|---|---|---|---|---|---|---|
| 1 | Aries | Meṣa | Fire | Movable / Chara | Masculine | Mars | Ram | Initiation, assertion, independent action |
| 2 | Taurus | Vṛṣabha | Earth | Fixed / Sthira | Feminine | Venus | Bull | Stabilisation, value, resources, embodiment |
| 3 | Gemini | Mithuna | Air | Dual / Dvisvabhāva | Masculine | Mercury | Twins | Exchange, language, learning, variety |
| 4 | Cancer | Karkaṭa | Water | Movable / Chara | Feminine | Moon | Crab | Protection, belonging, nourishment, memory |
| 5 | Leo | Siṃha | Fire | Fixed / Sthira | Masculine | Sun | Lion | Radiance, authorship, authority, creative expression |
| 6 | Virgo | Kanyā | Earth | Dual / Dvisvabhāva | Feminine | Mercury | Maiden | Discrimination, craft, service, repair |
| 7 | Libra | Tulā | Air | Movable / Chara | Masculine | Venus | Scales | Balance, agreement, exchange, relationship |
| 8 | Scorpio | Vṛścika | Water | Fixed / Sthira | Feminine | Mars | Scorpion | Containment, depth, crisis, transformation |
| 9 | Sagittarius | Dhanu | Fire | Dual / Dvisvabhāva | Masculine | Jupiter | Archer | Meaning, teaching, direction, aspiration |
| 10 | Capricorn | Makara | Earth | Movable / Chara | Feminine | Saturn | Sea-goat | Structure, duty, endurance, achievement |
| 11 | Aquarius | Kumbha | Air | Fixed / Sthira | Masculine | Saturn | Water-bearer | Systems, collectives, principles, reform |
| 12 | Pisces | Mīna | Water | Dual / Dvisvabhāva | Feminine | Jupiter | Fish | Synthesis, compassion, imagination, release |

### How the rāśi attributes work

- **Element** describes the medium: fire acts, earth consolidates, air connects and
  conceptualises, and water feels, protects, and absorbs.
- **Modality** describes movement: movable begins, fixed sustains, and dual signs
  mediate or adapt. Element and modality are independent.
- **Polarity** is a traditional masculine/feminine alternation beginning with Aries. It
  is a symbolic polarity, not a statement about a person's sex or identity.
- **Ruler** supplies the dispositor and the sign's operating style. A planet in a sign
  must be judged through the sign lord, dignity, strength, and house context.
- **Significance** changes with use. A sign on the lagna describes the field of body and
  identity; on a house cusp it colours the house; occupied by a planet it becomes that
  planet's environment; as the Moon sign it becomes a mental/emotional lens.

The existing `tbl_SignAttributes` design also includes direction, rising type, animal
class, kālapuruṣa body part, exaltation/debilitation points, and mūlatrikoṇa ranges.
These should remain separate attributes or rule rows. Rising type, day/night strength,
benefic/malefic sign character, animal class, and exact body-part mappings need a named
edition before seeding because later tables do not always agree.

## Nakṣatra structure and significance

| # | Nakṣatra | Range | Vimśottarī lord | Deity | Symbol | Core function |
|---:|---|---|---|---|---|---|
| 1 | Aśvinī | Aries 0°–13°20′ | Ketu | Aśvinī Kumāras | Horse's head | Swift beginnings, healing, rescue |
| 2 | Bharaṇī | Aries 13°20′–26°40′ | Venus | Yama | Womb / vessel | Bearing, containment, consequence |
| 3 | Kṛttikā | Aries 26°40′–Taurus 10° | Sun | Agni | Razor / flame | Cutting, purification, nourishment |
| 4 | Rohiṇī | Taurus 10°–23°20′ | Moon | Prajāpati / Brahmā | Chariot / cart | Growth, fertility, attraction, creation |
| 5 | Mṛgaśīrṣa | Taurus 23°20′–Gemini 6°40′ | Mars | Soma | Deer's head | Searching, curiosity, pursuit |
| 6 | Ārdrā | Gemini 6°40′–20° | Rahu | Rudra | Teardrop / storm | Breaking open, intensity, renewal |
| 7 | Punarvasū | Gemini 20°–Cancer 3°20′ | Jupiter | Aditi | Quiver of arrows | Return, restoration, spaciousness |
| 8 | Puṣya | Cancer 3°20′–16°40′ | Saturn | Bṛhaspati | Cow's udder / flower | Nourishment, teaching, support |
| 9 | Āśleṣā | Cancer 16°40′–30° | Mercury | Nāgas | Coiled serpent | Binding, penetration, strategy, hidden knowledge |
| 10 | Maghā | Leo 0°–13°20′ | Ketu | Pitṛs | Throne / palanquin | Ancestry, authority, inheritance |
| 11 | Pūrva Phalgunī | Leo 13°20′–26°40′ | Venus | Bhaga | Front legs of a bed | Pleasure, union, creativity, rest |
| 12 | Uttara Phalgunī | Leo 26°40′–Virgo 10° | Sun | Aryaman | Back legs of a bed | Commitment, patronage, durable alliance |
| 13 | Hasta | Virgo 10°–23°20′ | Moon | Savitṛ | Hand / fist | Skill, shaping, control, making tangible |
| 14 | Citrā | Virgo 23°20′–Libra 6°40′ | Mars | Tvaṣṭṛ / Viśvakarmā | Jewel / bright pearl | Design, beauty, differentiation |
| 15 | Svātī | Libra 6°40′–20° | Rahu | Vāyu | Young shoot / sword | Independence, movement, learning to bend |
| 16 | Viśākhā | Libra 20°–Scorpio 3°20′ | Jupiter | Indra–Agni | Triumphal arch / forked branch | Focused aim, achievement, convergence |
| 17 | Anurādhā | Scorpio 3°20′–16°40′ | Saturn | Mitra | Staff / lotus | Friendship, devotion, ordered cooperation |
| 18 | Jyeṣṭhā | Scorpio 16°40′–30° | Mercury | Indra | Earring / circular talisman | Seniority, protection, responsibility |
| 19 | Mūla | Sagittarius 0°–13°20′ | Ketu | Nirṛti | Roots / tied roots | Root-cause inquiry, uprooting, release |
| 20 | Pūrvāṣāḍhā | Sagittarius 13°20′–26°40′ | Venus | Āpas | Fan / winnowing basket | Persuasion, cleansing, invigoration |
| 21 | Uttarāṣāḍhā | Sagittarius 26°40′–Capricorn 10° | Sun | Viśvedevas | Elephant tusk | Finality, principled victory, leadership |
| 22 | Śravaṇa | Capricorn 10°–23°20′ | Moon | Viṣṇu | Ear / three footprints | Listening, transmission, preservation |
| 23 | Dhaniṣṭhā | Capricorn 23°20′–Aquarius 6°40′ | Mars | Vasus | Drum / flute | Rhythm, wealth, coordinated action |
| 24 | Śatabhiṣaj | Aquarius 6°40′–20° | Rahu | Varuṇa | Empty circle / hundred healers | Diagnosis, healing, secrecy, boundary |
| 25 | Pūrvabhādrapadā | Aquarius 20°–Pisces 3°20′ | Jupiter | Aja Ekapāda | Funeral cot front / sword | Intensity, austerity, radical vision |
| 26 | Uttarabhādrapadā | Pisces 3°20′–16°40′ | Saturn | Ahirbudhnya | Funeral cot back / deep serpent | Depth, stillness, containment, transition |
| 27 | Revatī | Pisces 16°40′–30° | Mercury | Pūṣan | Fish / drum | Safe passage, guidance, completion |

### What a nakṣatra contributes

- The Moon's nakṣatra is the janma/tārā basis for Vimśottarī dasha and a major lens for
  temperament, habit, and timing.
- Any planet's nakṣatra gives it a finer field of expression than the rāśi alone. The
  nakṣatra lord becomes a key dispositor in interpretation and dasha sequencing.
- The pada narrows the position to 3°20′ and maps one-to-one to a navāṃśa, making it a
  technically important D9 bridge as well as a phonetic/naming unit in some traditions.
- Deity and symbol provide mythic and functional vocabulary; they are interpretive
  anchors, not independent chart verdicts.
- Gana, yoni, nadi, varṇa, and tattva belong mainly to matching and specialised rule
  sets. They should not be treated as universal personality labels.

## Implementation recommendations

1. Keep `tbl_SignAttributes` as the stable 12-row structural table. Put contested
   assignments in rule tables with `RuleSetId` and `SourceRefCode`.
2. Keep `tbl_Nakshatras` at 27 rows for the existing Vimśottarī implementation. Treat
   Abhijit as an optional alternative rule set rather than silently adding row 28.
3. Keep `tbl_NakshatraPadas` at 108 rows. Store sidereal range, parent nakṣatra, D1 rāśi,
   and D9 navāṃśa sign, and validate both independent pada-to-navāṃśa calculations.
4. Store deity, symbol, and descriptive significance with source metadata. Store
   compatibility taxonomies separately and mark their tradition dependence.
5. Keep KP sub-lord resolution separate: it subdivides nakṣatra space and must not be
   confused with the nakṣatra's Vimśottarī lord.

## Sources

- `SRC_BPHS` — Parāśara's 27 fixed nakṣatras, twelve rāśis, and lagna framework:
  [Brihat Parashara Hora Shastra, chapter 3](https://parashara.net/index.php?page=chapter3).
- `SRC_BPHS_34_45` — deity, lord, symbol, range, and descriptive nakṣatra material:
  [Sanskrit Documents, BPHS chapters 34–45](https://sanskritdocuments.org/doc_z_misc_sociology_astrology/horaashaastraEng34-45-te.pdf).
- `SRC_BRIHAT_JATAKA_1` — Varāhamihira's sign classifications:
  [The Twelve Signs](https://vedacharts.com/learn/study/study-guide-brihat-jataka/lessons/sgbj-l-2).
- Existing project sources: `SRC_PVR_INTEGRATED`, `SRC_JHORA`, and `SRC_SWISSEPH`, as
  registered in [`reference-sources.md`](reference-sources.md).

## Open research questions

- Which edition is canonical for rising type, day/night, animal class, and body-part
  mappings?
- Which deity/symbol spellings and compatibility taxonomies should be seeded when
  editions disagree?
- Should the UI expose the interpretive “core function” text, or only sourced fields?
- Should an alternative 28-nakṣatra/Abhijit rule set be represented in the schema?

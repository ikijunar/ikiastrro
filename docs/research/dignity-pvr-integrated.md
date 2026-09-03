# Graha dignity — PVR *Integrated Approach* (Table 6 + special-degree rules)

Research note backing `2026-09-04-graha-dignity-rule-layer-and-conjunction-groups-design.md`.
Captures the source, the derived segment model, and every point where it diverges from
what `DignityEngine.cs` computes today and from the `tbl_SignAttributes` seed.

## Source

| Code | Work | Author | Scope used here |
|---|---|---|---|
| `SRC_PVR_INTEGRATED` | *Vedic Astrology: An Integrated Approach* | P. V. R. Narasimha Rao | Part 1 "Chart Analysis", Table 6 (Dignities of Planets) + the 7 special-degree notes |

New citation code — to be added to `docs/research/reference-sources.md` and `tbl_Dim_Source`.
Distinct from `SRC_JHORA` (the same author's desktop software, already registered) and the
`SRC_BPHS` umbrella.

## Table 6 — Dignities of Planets (as printed)

| Planet | Own rāśis | Exaltation rāśi (deep pt) | Debilitation rāśi (deep pt) | Moolatrikona |
|---|---|---|---|---|
| Sun | Le | Ar (10°) | Li (10°) | Le |
| Moon | Cn | Ta (3°) | Sc (3°) | Ta |
| Mars | Ar & Sc | Cp (28°) | Cn (28°) | Ar |
| Mercury | Ge & Vi | Vi (15°) | Pi (15°) | Vi |
| Jupiter | Sg & Pi | Cn (5°) | Cp (5°) | Sg |
| Venus | Ta & Li | Pi (27°) | Vi (27°) | Li |
| Saturn | Cp & Aq | Li (20°) | Ar (20°) | Aq |
| Rahu | Aq | Ge | Sg | Vi |
| Ketu | Sc | Sg | Ge | Pi |

## Special-degree notes (1)–(7) — own-sign vs moolatrikona split

The moolatrikona sign is **not** wholly moolatrikona; part of it gives own-rāśi results,
and for Mercury part gives exaltation results:

| # | Planet | Sign | 0° → | → | → 30° |
|---|---|---|---|---|---|
| 1 | Sun | Leo | Moolatrikona 0–20° | | Own 20–30° |
| 2 | Moon | Taurus | Exaltation 0–3° | | Moolatrikona 3–30° |
| 3 | Mars | Aries | Moolatrikona 0–12° | | Own 12–30° |
| 4 | Mercury | Virgo | Exaltation 0–15° | Moolatrikona 15–20° | Own 20–30° |
| 5 | Jupiter | Sagittarius | Moolatrikona 0–10° | | Own 10–30° |
| 6 | Venus | Libra | Moolatrikona 0–15° | | Own 15–30° |
| 7 | Saturn | Aquarius | Moolatrikona 0–20° | | Own 20–30° |

> **Note (4) has a typo in the book.** Its prose reads "Mars gives the results of being in
> moolatrikona in the first 12º of **Leo**", which contradicts Table 6 (Mars moolatrikona =
> **Aries**) and note (3) itself. Encoded as Aries; the `CalculationNarrative` records the typo.

## Derived segment model (what the rule table stores)

One row per (planet, sign, dignity-segment). Whole-sign dignities are Start 0 / End 30.
Exaltation & debilitation rows also carry the deep-degree point. Segments for a given
(planet, sign) tile 0–30° with no gap or overlap.

| Planet | Segment | Sign | Start | End | Deep |
|---|---|---|---|---|---|
| Sun | Exalted | Aries | 0 | 30 | 10 |
| Sun | Debilitated | Libra | 0 | 30 | 10 |
| Sun | Moolatrikona | Leo | 0 | 20 | — |
| Sun | Own | Leo | 20 | 30 | — |
| Moon | Exalted | Taurus | 0 | 3 | 3 |
| Moon | Moolatrikona | Taurus | 3 | 30 | — |
| Moon | Debilitated | Scorpio | 0 | 30 | 3 |
| Moon | Own | Cancer | 0 | 30 | — |
| Mars | Exalted | Capricorn | 0 | 30 | 28 |
| Mars | Debilitated | Cancer | 0 | 30 | 28 |
| Mars | Moolatrikona | Aries | 0 | 12 | — |
| Mars | Own | Aries | 12 | 30 | — |
| Mars | Own | Scorpio | 0 | 30 | — |
| Mercury | Exalted | Virgo | 0 | 15 | 15 |
| Mercury | Moolatrikona | Virgo | 15 | 20 | — |
| Mercury | Own | Virgo | 20 | 30 | — |
| Mercury | Own | Gemini | 0 | 30 | — |
| Mercury | Debilitated | Pisces | 0 | 30 | 15 |
| Jupiter | Exalted | Cancer | 0 | 30 | 5 |
| Jupiter | Debilitated | Capricorn | 0 | 30 | 5 |
| Jupiter | Moolatrikona | Sagittarius | 0 | 10 | — |
| Jupiter | Own | Sagittarius | 10 | 30 | — |
| Jupiter | Own | Pisces | 0 | 30 | — |
| Venus | Exalted | Pisces | 0 | 30 | 27 |
| Venus | Debilitated | Virgo | 0 | 30 | 27 |
| Venus | Moolatrikona | Libra | 0 | 15 | — |
| Venus | Own | Libra | 15 | 30 | — |
| Venus | Own | Taurus | 0 | 30 | — |
| Saturn | Exalted | Libra | 0 | 30 | 20 |
| Saturn | Debilitated | Aries | 0 | 30 | 20 |
| Saturn | Moolatrikona | Aquarius | 0 | 20 | — |
| Saturn | Own | Aquarius | 20 | 30 | — |
| Saturn | Own | Capricorn | 0 | 30 | — |
| Rahu | Exalted | Gemini | 0 | 30 | — |
| Rahu | Debilitated | Sagittarius | 0 | 30 | — |
| Rahu | Moolatrikona | Virgo | 0 | 30 | — |
| Rahu | Own | Aquarius | 0 | 30 | — |
| Ketu | Exalted | Sagittarius | 0 | 30 | — |
| Ketu | Debilitated | Gemini | 0 | 30 | — |
| Ketu | Moolatrikona | Pisces | 0 | 30 | — |
| Ketu | Own | Scorpio | 0 | 30 | — |

PVR gives no deep degree for the nodes. PVR does not split the nodes' moolatrikona /
own signs by degree, so those are whole-sign.

## Divergence from current IkiAstrro

### vs. `DignityEngine.cs` (hard-coded dicts, today's behaviour)

| Item | PVR (this note) | `DignityEngine.cs` now | Effect of switching |
|---|---|---|---|
| Rahu exaltation | Gemini | Taurus | changes `DignityStatus` for Rahu in Ge/Ta |
| Rahu debilitation | Sagittarius | Scorpio | changes for Rahu in Sg/Sc |
| Ketu exaltation | Sagittarius | Scorpio | changes for Ketu in Sg/Sc |
| Ketu debilitation | Gemini | Taurus | changes for Ketu in Ge/Ta |
| Rahu own / moolatrikona | Aquarius / Virgo | *(none — shadow planet)* | Rahu can now be Own Sign / Moolatrikona |
| Ketu own / moolatrikona | Scorpio / Pisces | *(none)* | Ketu can now be Own Sign / Moolatrikona |
| Moon moolatrikona start | 3° Taurus | 4° Taurus (BPHS) | Moon 3–4° Taurus: Own → Moolatrikona |
| Mercury moolatrikona start | 15° Virgo | 16° Virgo (BPHS) | Mercury 15–16° Virgo: Exalted → Moolatrikona |
| Own-vs-moolatrikona split for Su/Ma/Ju/Ve/Sa | explicit (notes 1,3,5,6,7) | single BPHS range only | engine gains exact Own-Sign resolution above the moolatrikona range |

The engine currently forces Rahu/Ketu to `Neutral` when not exalted/debilitated and skips
them in the Naisargika Maitri table. Under PVR they gain Own Sign / Moolatrikona; Panchadha
Maitri tiers for a node are **still** out of scope (PVR Table 6 covers dignity only, not
node friendships) — a node not in a dignity stays `Neutral`. Flagged as an open decision.

### vs. `tbl_SignAttributes` seed

The seed already carries this book's values in `ExaltedDegree` / `DebilitatedDegree` /
`MooltrikonaRangeStart` / `MooltrikonaRangeEnd` (e.g. Aries: Mars moolatrikona 0–12°, Sun
deep-exalt 10°, Saturn deep-debil 20°). The engine ignores those columns. Once
`tbl_Rule_GrahaDignity` is the source of truth, `verify-dignity` should assert the two agree
for the classical seven; the node rows are new.

## Downstream use

- **Interpretation "mood"** — each dignity type carries a mood + tendency (Exalted = elevated /
  enthusiastic; Moolatrikona = dutiful / purposeful; Own = comfortable / natural; Neutral =
  functional; Debilitated = uncomfortable / struggling). Feeds the future personality model.
- **Deep exaltation** stays a *degree-specific strength input* for a later shadbala/strength
  methodology — no falloff curve is encoded now.
- **Conjunction group members** store each participant's `DignityStatus` for exaltation /
  debilitation / own-house reasoning inside a stellium, and for yoga deduction.

## Two axes — dignity vs. Panchadhā Maitrī

PVR Table 6 (and this note) is **axis A** only: `EXALTED` / `MOOLATRIKONA` / `OWN` / `DEBILITATED`,
a function of planet + sign + degree. The friend/neutral/enemy standing of the *other* 8 signs is
**axis B** — Panchadhā Maitrī — which is chart-specific (Tatkālika/temporary friendship needs the
sign-lord's live position) and already fully built in IkiAstrro: `tbl_Rule_NaturalRelationship` (42
rows) + `tbl_Rule_TemporaryFriendshipDistance` (12 rows) + `DignityEngine.CombineToPanchadha` →
`Great Friend` / `Friend` / `Neutral` / `Enemy` / `Great Enemy`.

`tbl_Rule_GrahaDignity` holds **axis A rows only** — no `FRIEND` / `NEUTRAL` / `ENEMY` rows.
`DignityEngine.Evaluate` already merges the two by priority (axis A wins where it applies) into the
single 9-value `DignityStatus`. Re-deriving axis B into a static planet×sign table was rejected: it
would drop `Great Friend` / `Great Enemy` (they need the temporal layer), duplicate
`tbl_Rule_NaturalRelationship`, and collide on the words "Friend"/"Enemy".

## Dignity-type metadata (columns on `tbl_Rule_GrahaDignity`, constant per `DignityTypeCode`)

| `DignityTypeCode` | `Mood` | `InterpretationTendency` | `Analogy` (PVR home/office/picnic) |
|---|---|---|---|
| `EXALTED` | Elevated | Performs exceptionally and enthusiastically. | Favourite picnic/party — excited, at its best. |
| `MOOLATRIKONA` | Dutiful | Powerful, purposeful, responsible. | Office — executes its formal duty, like it or not. |
| `OWN` | Comfortable | Natural, authentic, relaxed. | Home — most natural and at ease. |
| `DEBILITATED` | Uncomfortable | Struggles to express its natural qualities. | Worst party — unhappy, stuck where it hates to be. |

`DignityRationale` (PVR's per-planet "why", non-NULL only where the book gives it):

| Planet | Sign | Segment(s) | Rationale (abridged from *Integrated Approach* Part 1) |
|---|---|---|---|
| Jupiter | Pisces | Own | 12th house of the natural zodiac; sāttwik & ethery — his home, a peaceful Brahmin doing pooja. |
| Jupiter | Sagittarius | Moolatrikona / Own | 9th house — upholding dharma is his duty; like a rāja-purohit who must take hard decisions. |
| Jupiter | Cancer | Exalted | 4th house — excited to do imaginative (watery) learning. |
| Jupiter | Capricorn | Debilitated | 10th house, tāmasik — hates well-defined tāmasik karma; against his nature. |
| Mercury | Gemini | Own | 3rd house (communication) — what he is most comfortable with. |
| Mercury | Virgo | Exalted **and** Moolatrikona | 6th house (debate) — his official job, and he loves it; Virgo is both office and picnic spot. |
| Ketu | Scorpio | Own | 8th house — most comfortable with occult activity. |
| Ketu | Pisces | Moolatrikona | 12th house — his duty is giving upāsanā and mokṣa. |

Where a (planet, sign) spans two segment rows the same text is copied to both.

## `DignityScore` — coarse ordinal over the final 9-value `DignityStatus`

rammyps's ladder (2026-09-04). Axis A carries four values; the axis-B Panchadhā tiers fill the
`+1 … −1` band. The two **"great"** Maitrī tiers keep their distinction in the `DignityStatus`
*string* but collapse to their base tier's number for scoring:

| `DignityStatus` | Score | Axis |
|---|--:|---|
| Exalted | +4 | A |
| Moolatrikona | +3 | A |
| Own Sign — primary segment **and** "other own sign" | +2 | A |
| Great Friend / Friend | +1 | B |
| Neutral | 0 | B |
| Enemy / Great Enemy | −1 | B |
| Debilitated | −2 | A |

The 4 axis-A values (`EXALTED +4`, `MOOLATRIKONA +3`, `OWN +2` on **every** own row, `DEBILITATED
−2`) are stored as a `DignityScore` column on `tbl_Rule_GrahaDignity`; the axis-B values (`+1` /
`0` / `−1`) are returned by `CombineToPanchadha`. `verify-dignity` holds the canonical 9-row map
(`Great Friend`→`+1`, `Great Enemy`→`−1`) and reconciles both. `CHECK (DignityScore BETWEEN -2 AND 4)`.

### Every own sign is `OWN +2`

Both the own segment of a planet's moolatrikona sign (e.g. Leo 20–30° for Sun) **and** its
whole-sign "other own sign" score `+2`. The 7 "other own sign" rows (`OWN 0–30`):

| Planet | Other own sign | Planet | Other own sign |
|---|---|---|---|
| Mercury | Gemini | Venus | Taurus |
| Mars | Scorpio | Saturn | Capricorn |
| Jupiter | Pisces | Rahu\* | Aquarius |
| | | Ketu\* | Scorpio |

\* PVR node scheme (source-dependent). `IsPrimary` marks the PVR "home" own sign (Gemini, Pisces,
Taurus, Capricorn for Me/Ju/Ve/Sa; sole own sign for Sun/Moon/nodes); it does **not** affect
`DignityScore`. Mars has no PVR "home" prose — Aries (MT sign) own segment proposed `IsPrimary = 1`
(spec §8.11).

### Derived segment model — with `DignityScore`

The "Derived segment model" table above is the axis-A seed. Each row's `DignityScore` follows its
segment type: `Exalted` → +4, `Moolatrikona` → +3, `Own` → +2 (all own rows), `Debilitated` → −2.
Nodes carry no `DeepDegree`. No `Neutral` / `Friend` / `Enemy` rows (axis B).

# Argala and avasthas — what each looks at, and how they are linked

Research and design note, 2026-10-17 (rammyps's request). A **synthesis note**, not a book extract: the avastha
definitions are PVR §15.4 (`SRC_PVR_INTEGRATED`, raw extract lines 7126–7250, printed pp.190–192); the argala method is
PVR §10.5–10.7 (see [[argala-virodhargala-drishti-lifematters]]). The way the two are *combined* is this project's own
(`PROJECT_SYNTHESIS`) and is labelled as such wherever the UI shows it. Used by the v10 statistical build, see the
"Avasthas and argala" sections of `v10-statisticalbuild.md` and `v10-phase0-feature-dictionary.md`.

## 1. What each avastha set looks at

| Set | PVR | Inputs it uses | Computed for | How this app reads it |
|---|---|---|---|---|
| Baaladi (age) | 15.4.1 | Degree within the sign; odd or even sign | **D1 only** (needs the exact degree) | Five degree bands (`tbl_Rule_AgeState`), each with PVR's share of results: Baala ¼, Kumara ½, Yuva all, Vriddha some, Mrita none |
| Jagradadi (alertness) | 15.4.2 | **Dignity only** | Every chart | Dignity → awake / dreaming / asleep (`tbl_Rule_WakefulnessState`); moolatrikona counts with own sign, great enemy and debilitation with enemy |
| Deeptadi (9 states) | 15.4.3 | Exaltation or own sign; friendship with the sign lord; conjunction with malefics (Vikala); sign lord a malefic (Khala); closeness to the Sun (Kopita) | Every chart | Follows Jagannatha Hora's Mood column (decision 012): relationship tier kept beside Deepta/Swastha; Vikala needs ≥ 2 natural malefics; Kopita orb 5°; fixed malefic set |
| Lajjitadi (6 more states) | 15.4.3 | House position (Lajjita: 5th); conjunctions; **aspects received**; friend/enemy of the joining or aspecting planets; watery sign; exaltation or moolatrikona | Every chart | Directional Panchadha friendship; every match kept; only Lajjita and Garvita can be checked against JHora |
| Sayanaadi (12 states) | 15.4.4 | Own nakshatra, planet index, navamsa, Moon's nakshatra, birth ghati, lagna sign | **D1 only** (needs exact degree and birth ghati) | PVR's formula exactly; matches JHora; PVR calls it "the most important of all states" |

So conjunctions, aspects, dignity, friendship and house position **are** inside the states — but not uniformly:
Jagradadi is a pure re-coding of dignity; Deeptadi is dignity plus sign-lord friendship plus two conjunction/closeness
tests; Lajjitadi is the set that uses aspects.

## 2. What no avastha set looks at

These sit *beside* the states and are shown once, never folded into them:

- functional nature for the lagna (benefic or malefic by the houses owned) — decides subhaargala vs paapaargala;
- houses owned and the house stood in, beyond Lajjita's 5th;
- retrogression, and combustion as a test in its own right (Kopita covers closeness to the Sun only);
- strength measures: Ṣaḍbala, Aṣṭakavarga, Vimśopaka;
- aspects the planet **casts** (avasthas read only those it receives);
- the argala structure itself: position (2nd/4th/11th/5th), pairing with virodhargala, planet counts, the Ketu
  reverse count, the malefic-3rd exception.

The same list is shown in the UI (`AvasthaPvrNotes.NotCoveredByAvasthas`, the "What each avastha set looks at"
panel on Astro Facts → About Planets).

## 3. The counted-once rule

PVR's own instruction after the states is to weigh them "with dignity, strength, houses and aspects". Because the sets
already contain dignity, friendship, conjunctions and aspects, adding those again beside an avastha counts the same fact
twice. The rule used everywhere:

1. **One fact, one place.** A planet row shows *condition* (the avastha reasons), *role* (functional nature, houses
   owned and occupied) and *strength* (dignity, Ṣaḍbala) as three separate groups; no input appears in two of them.
2. **The argala winner never reads avasthas.** More planets wins; equal numbers are decided by dignity (PVR §10.7, the
   same as `ArgalaCalculator.Compare`). Jagradadi is dignity again, so letting it into the verdict would count dignity
   twice. Avasthas only *describe* what the winning planets can deliver.
3. This mirrors the v10 precedent for Independent Bhava Bala, which excludes the lord's Ṣaḍbala from Context so the
   lord is not in Capacity and Context at once.

## 4. How argala and avasthas are linked

Two PVR statements carry the link, both from §15.4:

- **Delivery (15.4.2).** Awake (Jagrat) gives full results, dreaming (Swapna) medium, asleep (Sushupti) negligible. An
  argala planet is "a planet intervening in a house"; how much it can deliver is its alertness.
- **Damaged house matters (15.4.3).** A planet in Lajjita, Kshudhita or Kshobhita affects the significations of the house
  it stands in; PVR's examples are Lajjita in the 5th (losses related to progeny) and Kshobhita in the 7th (loss of
  spouse). `AvasthaPvrNotes.HouseResults` already encodes these.

What the app builds on them (`ArgalaForceModel`, `PlanetStateInfo`):

| Piece | Rule | Source |
|---|---|---|
| Planet delivery | Jagrat = Full, Swapna = Medium, Sushupti = Negligible | PVR 15.4.2 |
| Damaging states | Lajjita, Kshudhita, Kshobhita, with the house result when PVR names one | PVR 15.4.3 |
| Lane delivery (arrow thickness) | The **strongest** delivery among the lane's argala planets: one awake planet is enough | PROJECT_SYNTHESIS |
| Deciding planet | When the win is by one planet (or was level and dignity decided it), every planet on the winning side is deciding: remove one and the winner could change | PROJECT_SYNTHESIS |
| Caveat | A deciding planet that is asleep, or damaged, is flagged: "this win rests on Mars, who is asleep" | PROJECT_SYNTHESIS |

Where it shows: the planet lines in the Argala interpretation (each with its alertness, the reason, and any damaged-house
result); a caveat line when a win rests on a weak planet; on the left chart, arrow thickness by lane delivery, an
alertness tag per planet (`Jg` awake, `Sw` dreaming, `Su` asleep), a warning mark for a damaging state, and a ring on the
deciding planet.

## 5. How each state "got there"

Every state now carries a one-line reason built by the same code that picks the state (`Evaluate` returns the id and the
`StateReason` together, so they cannot disagree): "Dignity is Great Friend", "Compound relationship to its sign lord Mars
is Great Friend", "Joined by 2 natural malefics: Saturn, Rahu", "3.8° from the Sun (within the 5° orb)", "In the 5th house,
joined by Saturn", the Sayanaadi formula with all six inputs. Reasons are not stored (the stored facts carry state ids
only); the page recomputes them from the chart's key details, conjunctions and aspects with `AvasthaLive`, which also
makes Jagradadi, Deeptadi and Lajjitadi available for **every** varga, not only the D1 rows that are stored.

## 6. Open items

- **Deeptadi relationship tiers** read the sign-lord friendship only, as JHora does, not PVR's wording about conjunction
  and aspect by friends; the nodes still use the dignity label because `DignityEngine` has no node Naisargika table
  (decision 012).
- **Lajjitadi** beyond Lajjita and Garvita has no JHora counterpart to check against.
- **Kopita's 5° orb** is a round value inside JHora's observed gap, not a measured cut-off.
- **Baaladi and Sayanaadi** are D1-only, so on a varga the reading uses Jagradadi and Lajjitadi alone.
- **Whether avasthas should ever change the verdict** is a project decision, not PVR's; for now they describe, they do not
  count. The v10 notes record the collinearity reason.
- **House-lord text** (Baseline / Well-disposed / Afflicted) is paraphrased from sources that include B.V. Raman; it needs
  an audit against PVR before the reading uses it (plan step 0, not started).

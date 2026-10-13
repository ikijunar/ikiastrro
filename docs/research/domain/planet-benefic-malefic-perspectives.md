---
last_updated: 2026-10-13
status: research-complete-design-pending
workstream: database
---

# Planet benefic / malefic — what each school says, by axis

Research for a new Astro Facts table (requested 2026-10-13, rammyps): is a planet benefic or
malefic, judged by **its nature, its house, its sign, its nakshatra and, in the vargas, its varga**,
and where astrologers disagree. Extends [[sign-benefic-malefic]] (which settled that dignity,
karaka and benefic/malefic are three separate axes) and [[planetary-roles-avastha]].

**Verification legend.** Every row says how it was checked. **[read]** = I read the passage in a
local text this session (path in the Sources list). **[web]** = a single web page I fetched, not a
book. **[recall]** = from general knowledge of the tradition, *not* confirmed in any local text —
do not seed a `tbl_Rule_*` row from a [recall] claim without a source pass. The registry codes
are in [`../sources.md`](../sources.md).

## 0. The one-paragraph answer

Every school separates **what a planet is** (natural benefic/malefic, fixed or near-fixed) from
**what it does in this chart** (functional, from the houses it rules). Nobody in the local library
gives a benefic/malefic verdict for a *sign* or a *nakshatra* as such: a sign or nakshatra changes
a planet's **condition** (dignity, who rules the sign/star, whether that ruler is itself good or
bad for the Lagna), not its nature. The one place the classics do label a division itself good or
bad is the **D60 shashtiamsa** (BPHS), which the repo already stores. So the table is naturally
**four independent reads per planet per chart** (nature · house · sign · nakshatra), repeated for
each varga with that varga's own Lagna — kept side by side, not blended, per the existing
"no blended score" decision.

## 1. Axis 1 — Natural nature (naisargika)

| Source | Benefic | Malefic | Moon | Mercury | How checked |
|---|---|---|---|---|---|
| **BPHS ch. 3** (Santhanam, Vol 1 p.27-28) | everything not listed | Sun, Saturn, Mars, **waning** Moon, Rahu, Ketu | waning = malefic, waxing = benefic; a waning Moon joined or aspected by a benefic "turns a benefic". Translator's note: some commentators (Yavana view, from Saravali) grade the Moon by elongation instead — 0-120° medium, 120-240° very auspicious, 240-360° bereft of strength | malefic **if joined to a malefic** | [read] `bphs1.txt` 1189-1225 |
| **Saravali ch. 3 v.9** (Kalyana Varma) | Jupiter, Mercury, Venus | Saturn, Mars, Sun | waning Moon malefic | malefic in the company of malefics | [read] |
| **Phaladeepika ch. 2 v.27** (Mantreswara, tr. Kapoor) | — | waning Moon, Sun, Mars, Saturn, Rahu, Ketu | waxing = benefic (translator's note) | malefic if associated with malefics; benefic if not | [read] |
| **Phaladeepika ch. 4** (Kala Bala note) | Moon, Mercury, Jupiter, Venus | Sun, Mars, Saturn | — | Keshavi Jataka: malefic when associated; "some Acharyas" and the translator: **always benefic for Kala Bala** | [read] |
| **Laghu Parashari / Jataka Chandrika** (commentary) | Jupiter, Venus, Mercury, Moon | Sun, Saturn, Mars | Moon is *weak* from the 8th dark-half day to the 7th bright-half day; "some commentators" treat the weak Moon as malefic | malefic when associated with malefics | [read] `Laghu_Parasari.txt` 1130-1150 |
| **PVR, *Integrated Approach* §3.2.2** | Jupiter, Venus; waxing (Sukla) Moon; Mercury alone or with **more** benefics | Sun, Mars, Rahu, Ketu; waning (Krishna) Moon; Mercury joined by **more** malefics | binary by paksha | decided by **majority** of companions, not by any single malefic | [read] extract 1287-1293 |
| **PVR §13.2** | Jupiter, Venus, waxing Moon, well-associated Mercury | Sun, Mars, Saturn, Rahu, Ketu, waning Moon, ill-associated Mercury | — | — | [read] extract 6379 (Saturn is named here though §3.2.2's list omits it) |
| **Raman, HTJH ch.1** | natural benefics assumed (Jupiter, Venus, Mercury, Moon) | natural malefics assumed (Sun, Mars, Saturn) | — | — | [read] `how-to-judge-a-horoscope-1.md` ~586-615 |

**Where they differ**

1. **Moon: binary vs graded.** BPHS, Phaladeepika, PVR use paksha. The Yavana view and Jataka
   Chandrika's "weak from K8 to S7" make it a strength question (near the Sun). BPHS also lets a
   benefic's conjunction or aspect rescue a waning Moon. PVR's `Table 30` footnote makes the Moon's
   *functional* nature depend on phase too, so the repo already carries a `MoonPhase` helper.
2. **Mercury: three rules.** (a) any malefic conjunct → malefic (BPHS, Phaladeepika main text,
   Saravali); (b) PVR: majority of companions; (c) always benefic when computing Kala Bala
   (Phaladeepika translator). The repo has two live variants: `ClassicalMalefics` (static, never
   conditional — chosen because it reproduces JHora's Mood column, decision 012) and
   `ArgalaCalculator.IsNaturalMalefic` (conditional).
3. **Rahu/Ketu.** Malefic everywhere they are mentioned (BPHS, Phaladeepika, PVR). Saravali's
   verse lists only the seven classical planets.
4. **Saturn is malefic in every source**, but PVR §3.2.2's two-line list does not name him; §13.2
   does. Treat that as an omission, not a disagreement.

## 2. Axis 2 — Functional nature (from house lordship)

This is the axis the repo already built (`LagnaFunctionalNature`, PVR Table 30). The research
finding is that **PVR is not the only reading, and the schools disagree on exactly one thing that
matters: what owning a kendra does.**

| Question | BPHS ch. 34 (Poddar/SJC tr.) | Laghu Parashari / Jataka Chandrika | PVR §13.2 + Table 30 | Raman, HTJH ch.1 |
|---|---|---|---|---|
| **Natural benefic owns a kendra** | loses beneficence → **neutral** ("not said that benefics become malefics") | same: neutral | **functional MALEFIC** | **malefic** ("kendradhipati"); ceases to act as a malefic when it *occupies* that kendra sign |
| **Natural malefic owns a kendra** | loses malefic nature → **neutral** | same: neutral | **neutral** | **"temporal benefic"**; ceases to be one when it occupies that kendra sign |
| Trikona (5, 9) lord | auspicious always; Lagna lord especially so | auspicious | functional benefic | benefic; 9th > 5th |
| 3 / 6 / 11 lord | inauspicious (strength 3 < 6 < 11) | inauspicious (Trishadaya) | functional malefic | malefic; 11th worst, 6th less, 3rd least |
| 2 / 12 lord | **neutral**, takes nature from the other house or from association | same | neutral | neutral for Sun/Moon |
| 8 lord | neutral but "never auspicious" (12th from the 9th); with 3/7/11 lordship "extremely harmful"; with a trikona "auspicious"; **Sun and Moon do not suffer 8th lordship** | — | neutral, the **worst of the neutrals** | Sun/Moon as 8th lord neutral |
| Two-house lord | gives the result of the *predominant* house and stalls the lesser; house strength order 2,12,5,9,3,6,11,8,1,4,7,10 | trikona lord who also owns 6/8: Vinayaka Sastri — benefic trikona lord is "somewhat auspicious", malefic one "somewhat inauspicious" | Table 30 hand-resolves every Lagna ("judiciously combine"); it is the verdict | cancels a kendra/trikona lord's blemish when the 9th/10th pair is involved |
| Yogakaraka | kendra + kona lord, placed in kendra/kona | same | kendra + trikona | Mars/Saturn/Venus only, by Lagna |
| How checked | [read] `Yogakarakas.txt` (SJC translation of BPHS ch. 34) | [read] `Laghu_Parasari.txt` | [read] extract 6382-6400 | [read] `how-to-judge-a-horoscope-1.md` 578-615 |

**Consequences for the table.**
- A natural benefic like Jupiter for a Taurus or Libra Lagna is **neutral** by BPHS/Laghu Parashari
  and **malefic** by PVR/Raman. The repo (PVR) calls it malefic. A faithful multi-school table
  shows both.
- A natural malefic owning a kendra is neutral (BPHS, PVR) or benefic (Raman). Repo: neutral.
- The Sun and Moon exemption for the 8th house is stated by BPHS ch. 34 and echoed by Raman; PVR's
  Table 30 already gives Sun and Moon "neutral" in the Cancer/Leo/… rows where they own the 8th,
  so that exemption is not a disagreement in practice.

## 3. Axis 3 — House placement

What does a benefic or a malefic *do* in each house?

| Source | Rule | How checked |
|---|---|---|
| **PVR §13.2** | A functional benefic in a quadrant or trine (1, 4, 5, 7, 9, 10) gives good results. A functional malefic there is "not good, unless it is very strong". A functional malefic in the **3rd and in the dusthanas (6, 8, 12)** gives good results "by spoiling the significations of the bad houses". | [read] extract 6434-6440 |
| **Saravali ch. 30 v.86-87** | Malefics in any house **other than 6, 8, 12** harm that bhava; benefics there increase it. Malefics are auspicious in 6/8/12; **benefics are adverse in 6/8/12**. Strength of yogakaraka, friendship, aspects and exaltation/debilitation vary the result "maximum, medium or nil". | [read] `Kalyana_Varmas_Saravali…txt` 4501-4506 |
| **BPHS 74.25** (as translated on enjoylearningsanskrit.com; not yet checked against Santhanam) | "Where many malefics stand in a house, that house is ruined." Auspicious from benefics in the **6th and 12th** and from malefics in the **3rd, 6th, 11th**. | [web] page fetched 2026-10-13. Note the benefic-in-6/12 reading **contradicts** Saravali above; the Sanskrit compound `viriṣphāriśubhaiḥ` is ambiguous, so confirm in Santhanam before relying on it. |
| **Phaladeepika, translator's notes (ch. 6)** | "Malefics do good if posited in the 6th" (the house of enemies); "Mars, Saturn and Rahu are good in the 3rd, 6th and 11th"; "benefics do good to the house where they are posited, and their aspect improves the house". | [read] `Mantreswara_s__Phaladeeplka_.txt` 2760-2775 |
| **Phaladeepika, Vipareeta (Harsha/Sarala/Vimala)** | A dusthana lord in another dusthana loses its evil and gives good results. Translator: Parashara **does not endorse** this; Raman (300 Combinations) says the sting of the lordship is only *modified*, not removed. | [read] 2895-2965 |
| **Phaladeepika, own-house rule** | A planet in its **own house** is good and strong even when that house is the 6th, 8th or 12th. | [read] 2895 |
| **Raman, HTJH** | A kendradhipati malefic (natural benefic owning a kendra) stops acting as a malefic when it sits in that kendra's own sign. | [read] ~605-620 |
| **BPHS ch. 74.25 / Phaladeepika upachaya** | Benefics in the upachayas (3, 6, 10, 11) form wealth yogas (Vasumati/Dhana). | [read] Saravali 1605, Phaladeepika 2339 |

**Where they differ**
1. **Benefic in 6/8/12.** Saravali, PVR and common practice: weakened or adverse. BPHS 74.25 as
   translated: benefics in 6 and 12 are *good* (needs a Santhanam check).
2. **Malefic in 11th.** PVR does not list the 11th for malefics (3rd and the dusthanas only);
   Saravali's blanket "good in 6/8/12" omits 3 and 11; BPHS and Phaladeepika include 3, 6, 11.
3. **Natural vs functional.** Saravali and BPHS speak of natural benefic/malefic; PVR states the rule
   for the **functional** kind and says the two must not be confused ("a nice person may harm one and
   a bad person may do good").
4. **Strength overrides.** Every school hedges by strength: PVR "unless very strong", Saravali
   "maximum, medium or nil", Phaladeepika "provided strong".

## 4. Axis 4 — Sign placement

Nothing in the local texts labels a **sign** benefic or malefic for a planet. What a sign changes:

| Lens | Rule | Already in the repo? | How checked |
|---|---|---|---|
| **Dignity** | Exalted / Moolatrikona / Own / Friend / Neutral / Enemy / Debilitated | Yes (`DignityStatus`, `tbl_Rule_GrahaDignity`, PVR) | n/a |
| **Nature of the sign's lord** | Phaladeepika Dīptādi: Shanta ("calm") in the varga of a benefic, Khala ("base") in the varga of a malefic. PVR/JHora Mood uses the static malefic set `{Sun, Mars, Saturn, Rahu, Ketu}` (decision 012) | Yes (`DeeptadiStateCalculator`, `ClassicalMalefics`) | [read] Phaladeepika 1214-1218 |
| **Own-sign cancellation** | Kendradhipati dosha cancelled when the planet is in its own kendra sign (Raman); Phaladeepika: own house is good even if 6/8/12 | Dosha flagged; cancellation **not** modelled | [read] |
| **Odd/even sign × hora** | Phaladeepika: malefics in odd signs in the Sun's hora (first 15°) and benefics in even signs in the Moon's hora are strong | No | [read] 1152-1154 (the translator warns the rule is traditionally cited for strength, not nature) |
| **Debilitation / Neecha Bhanga** | A debilitated planet's nature is unchanged but its delivery is reversed or restored by Neecha Bhanga | Neecha Bhanga: [recall] — check `yoga` tables | — |

So in the table the **Sign** column is best reported as *dignity plus the nature of the sign
lord* (benefic / malefic / the planet itself), not as a verdict on the sign.

## 5. Axis 5 — Nakshatra placement

| Lens | Rule | Status |
|---|---|---|
| **Nakshatra lord's nature** | A planet's results are coloured by the lord of its nakshatra: that lord's natural and functional nature, house, and dignity. This is the foundation of KP (star lord → sub lord, K.S. Krishnamurti) and appears in Parashari dasha reading (the nakshatra lord of the dasha lord). | [recall] for KP; the local KP set (`KP/…` — *Astro-Secrets 1-6*, *KP Reader 3*, *krishnamurti-Padhdhati*) was only sampled for "sub-lord decides". **Needs a source pass.** |
| **Nakshatra temperament** | Ugra/Krura, Mridu/Maitra, Tikshna/Daruna, Laghu/Kshipra, Dhruva/Sthira, Chara, Mishra — used to time actions, not to judge a planet's nature. | [recall]; **no table in the repo** (checked `tbl_Nakshatras`: has Guna, Gana, Varna, Tatva, Direction, Yoni, Nadi only). Source is the Muhurta literature (`muhurta-prakarana.pdf`, Raman's *Muhurta*) — not read. |
| **Nakshatra aspects** | PVR §26.5: Sun/Moon aspect the 14th and 15th stars from themselves; Mars the 1st, 3rd, 7th, 8th, 15th; Mercury/Venus the 1st, 15th; Jupiter the 10th, 15th, 19th; Saturn the 3rd, 5th, 15th, 19th. "A natural benefic gives good results related to the constellations aspected by it and a natural malefic bad results." | [read] extract 12150-12160. A real, citable, nakshatra-level benefic/malefic fact (used for transit). |
| **Gandanta / Mula** | Nakshatra junction points and the Ashlesha-Magha, Jyeshtha-Mula, Revati-Ashwini boundaries are treated as inauspicious for the planet placed there. | [recall]; BPHS and Sastri contain Ganda-moola material (`Brhatparasarahorasastra…txt` 10448, 630) — not read. |
| **Tara / Vedha** | Sarvatobhadra vedha: Sun, Mars, Saturn, Rahu, Ketu pierce stars as malefics. | [web] a Sarvatobhadra site; transit-only convention. Out of scope for a natal table. |

**Finding.** For the nakshatra column, the defensible reading is **"star lord and its nature
(natural + functional)"** plus the **PVR nakshatra aspect** fact. A temperament lookup would need a
Muhurta source pass first.

## 6. Axis 6 — Vargas

| Lens | Rule | In the repo? | How checked |
|---|---|---|---|
| **Functional nature per varga** | PVR judges functional nature **from the varga's own Lagna**: "Mercury is a functional benefic in dasamsa" (Example 54). So the full nature/house table repeats per varga with that varga's Lagna. | Table 30 applies to any Lagna sign, so it is reusable per varga | [read] extract 8172 |
| **Shashtiamsa (D60) nature** | Each of the 60 parts is named and marked benefic or malefic (BPHS ch. 6, Santhanam p.83-85: "planets in benefic Shashtiamsa produce auspicious results, the opposite in malefic"). The repo's list follows JHora/PyJHora/Maitreya8 and BPHS order. Santhanam notes one part (Kalinasa, no. 27) is marked malefic "according to some". | **Yes** — `ShashtiamsaDeityTable` (`IsMalefic` per part, decision 006) | [read] `bphs1.txt` 3870-4000; code |
| **Vimshopaka / Amsabala** | Planet's dignity across the Shad/Sapta/Dasa/Shodasa-varga groups | Yes (`VimsopakaCalculator`, `AmsabalaTable`) | repo |
| **Vargottama** | Same sign in D1 and D9; strengthens, does not change nature | Yes (`tbl_Fact_Vargottama`) | repo |
| **Drekkana, Trimsamsa nature** | Named drekkanas (Sarpa, Nigada, Ayudha…) and Trimsamsa ownership by Mars/Saturn/Jupiter/Mercury/Venus carry malefic/benefic meaning. | Not stored; [recall]; BPHS ch. 6 not yet read for these | — |

**Finding.** For vargas the table is: **nature (unchanged) · functional nature from the varga
Lagna · house in that varga · sign dignity in that varga · nakshatra lord of the varga sign** — and
for D60 only, **the shashtiamsa's own benefic/malefic flag**.

## 7. Source-quality notes

- The Santhanam BPHS OCR is poor (garbled Sanskrit, merged words). Quotes above are from legible
  English paragraphs only; line numbers are into a scratch extract (`bphs1.txt`) and will not
  survive — cite chapter and printed page instead.
- The Yogakarakas file is Sarajit Poddar's **SJC (Rath school) translation** of BPHS ch. 34, not
  Santhanam. It is the best local statement of the BPHS functional-nature rules, and is
  independent of PVR.
- Web search surfaced no usable passage for Sanjay Rath, K.N. Rao or Hart de Fouw/Svoboda on this
  question. **No claim in this note is attributed to them.** *Light on Life*, K.N. Rao's *Learn
  Successful Predictive Techniques* and Rath's *Vimsottari and Udu Dasas* are in the library and
  were only searched for "natural/functional benefic" (no hits in Rao).
- Not yet read for this topic: Brihat Jataka, Jataka Parijata, Uttara Kalamrita, Sarvartha
  Chintamani, Hora Sara, *Planets and Education*, Chaudhry's *How to Study Divisional Charts*.

## 8. What the repo already has, and the gaps

| Need | Have | Gap |
|---|---|---|
| Natural nature | `ClassicalMalefics` (static); `ArgalaCalculator.IsNaturalMalefic` (conditional); `MoonPhase.Of` | No single reusable classifier — the open item in [[planetary-roles-avastha]] |
| Functional nature | `LagnaFunctionalNature` (PVR Table 30) | Only PVR; no BPHS/Laghu Parashari/Raman reading; Rahu/Ketu out of scope |
| Planet-in-house good/bad | `HouseBeneficMaleficCalculator` (house-level, Raman count) | No *planet-level* "is this placement good for it" read |
| Sign | `DignityStatus`, Dīptādi | Sign-lord nature not shown per planet |
| Nakshatra | `tbl_Nakshatras` (lord, gana, nadi…) | No nakshatra-lord nature; no PVR §26.5 aspect table; no temperament |
| Varga | `ShashtiamsaDeityTable`, Vimsopaka, per-varga key details | Nature/house reads not run for non-D1 charts |

## 9. Proposed table (for decision — nothing is built)

**"Benefic / Malefic" in Astro Facts → About Planets (1.2), under the dignity table**, one row per
graha (Sun…Ketu), chart picked by the existing Natal Chart picker (D1 and every varga):

| Planet | **Nature** (natural, with phase/association note) | **Functional** (from this chart's Lagna; PVR default, other schools on expand) | **House** (placement + Good / Mixed / Challenging by PVR rule, with Saravali/BPHS differences on expand) | **Sign** (dignity · sign lord and its nature) | **Nakshatra** (star lord · its natural + functional nature · PVR §26.5 aspected stars) | **D60 flag** (D60 only) |

Facts side by side, **no blended verdict**, each cell with its source code and, where the schools
disagree, an "other schools say" disclosure. Computed live in Core from the stored key details
(same pattern as `RudraMaheswaraTable`); the school-by-school rules go in a small rule table so
the citations are data, not code.

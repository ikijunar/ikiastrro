---
last_updated: 2026-09-07
---

# Reference sources — citation registry (`SRC_*`)

The single place classical texts, libraries, and external exports are named. Everything else
(rule tables, terminology, specs, plans, code comments) cites the `Code` — never the title or
author inline (`STANDARDS.md §M.4`). Mirrored into `dbo.tbl_Dim_Source` by
`db/15_create_dim_source.sql` / the `seed-sources` path; keep the two in sync.

**Primary reference (2026-09-04, rammyps): `SRC_PVR_INTEGRATED`.** The project is being
reconciled chapter-by-chapter against P.V.R. Narasimha Rao's *Vedic Astrology: An Integrated
Approach* — it is the canonical spine for engine behaviour, rule values, and terminology, and
its `SRC_*` code is what a `tbl_Rule_*` row cites for the whole PVR corpus even where the same
material appears in a derivative (a worksheet, JHora, a later article). Other `SRC_*` rows stay
as cross-checks or for areas the book doesn't cover. Where the shipped code deliberately differs
from the book, that is called out at the divergence (row `CalculationNarrative`, or a spec
note), never left silent. Coverage / reconciliation status per chapter:
`docs/research-pvr-book-coverage.md`.

| Code | Title | Author | Edition / Version | Tradition | Used by | Notes |
|---|---|---|---|---|---|---|
| `SRC_PVR_INTEGRATED` | Vedic Astrology: An Integrated Approach | P. V. R. Narasimha Rao | 1st ed. 2000, freely-released 2010 PDF | PVR Integrated (Parāśari, holistic) | **primary** — dignity, relationships, graha characters, upagrahas, special lagnas, vargas, houses, karakas, arudhas, aspects/argalas, yogas, strength, dasas | PDF `D:\Vedic Astrology\Vedic Astology Books\1_PVR_NarasimhaRao.pdf`; raw text extract `D:\@ClaudeSpace\BookExtracts\pvr-integrated-approach-raw.txt`. PVR's own 2010 "Looking Back" note says he has since refined several calculations — treat as canonical baseline, not infallible. Distinct from `SRC_JHORA` (same author, desktop software). |
| `SRC_BPHS` | Brihat Parashara Hora Shastra | Parāśara (attrib.) | — | Parāśari | dignity, aspects, vargas, avasthas | umbrella; prefer a chapter-scoped code below when known |
| `SRC_BPHS_26` | BPHS ch. 26 — Graha Dṛṣṭi | — | — | Parāśari | `tbl_Rule_AspectOffset` | 7th full; Mars 4/8, Jupiter 5/9, Saturn 3/10 |
| `SRC_BPHS_27` | BPHS ch. 27 — Ṣaḍbala | — | — | Parāśari | Strength engine (Plan 3) | lookup tables need a specific edition — see `research-topic-coverage.md` |
| `SRC_BPHS_COMBUSTION` | BPHS — Asta (combustion) orbs | — | — | Parāśari | `tbl_Rule_CombustionOrb` | Moon 12°, Mars 17°/8°R, Mercury 14°/12°R, Jupiter 11°, Venus 10°/8°R, Saturn 15° |
| `SRC_BPHS_AVASTHA` | BPHS — Bālādi & Jāgradādi avasthās | — | — | Parāśari | `tbl_Rule_AgeState`, `tbl_Rule_WakefulnessState` | Bāla .25 / Kumāra .50 / Yuva 1 / Vṛddha .125 / Mṛta 0 |
| `SRC_BPHS_34_45` | BPHS chapters 34–45 — nakṣatra descriptions | — | — | Parāśari | nakṣatra deity, lord, symbol, range, and descriptive effects | Cross-check edition required |
| `SRC_BRIHAT_JATAKA_1` | Bṛhat Jātaka chapter 1 | Varāhamihira | — | classical | sign modalities, polarity, and day/night groupings | — |
| `SRC_PHALADEEPIKA` | Phaladeepika | Mantreśvara | — | classical | combustion cross-check | alt. orb set |
| `SRC_RAMAN_HTJH` | How to Judge a Horoscope (vols I–II) | B. V. Raman | — | Raman | house / Lagna significations, functional nature | OCR extract under `D:\@ClaudeSpace\BookExtracts\_work\how-to-judje-a-horoscope-i_p1-312\`; consulted extract does not specify an alternate D2 or D11 sign-rule formula |
| `SRC_RAMAN_HINDU_PREDICTIVE` | Hindu Predictive Astrology | B. V. Raman | — | Raman | general | — |
| `SRC_RAMAN_GRAHA_BHAVA_BALAS` | Graha and Bhava Balas | B. V. Raman | Thirteenth edition, 1992 | Raman | detailed Shadbala and Bhava Bala arithmetic referred to by PVR chapter 15 | Local DJVU: `D:\Vedic Astrology\Vedic Astology Books\B. V. Raman\Bhava and Graha Balas.djvu` |
| `SRC_PYJHORA` | PyJHora (source) | B. Satya Prakash (`pyjhora`) | vendored `_research/PyJHora` | mixed | varga formulae, special-lagna / upagraha algorithms | AGPL — vendored for reference, not linked |
| `SRC_JHORA` | Jagannatha Hora (desktop) | P. V. R. Narasimha Rao | v8.x | mixed | golden-record verification | — |
| `SRC_JHORA_EXPORT_RAMAKRISHNAN` | JHora natal export — 1_Ramakrishnan | — | 22 Apr 1981 05:30 Chennai | — | `verify-vargas`, `verify-jaimini` golden values | file `docs/artifacts/reference-charts/Rammy_Jagannatha.txt` |
| `SRC_RATH_VARGA` | Vedic Astrology / varga methods | Sanjay Rath | — | Jaimini / SJC | argala and historical D11 alternative | retained for argala / comparison; not the active D11 rule |
| `SRC_VEDASTRO` | VedAstro.Library | (open source) | pre-2026-08-24 | mixed | historical — replaced by SwissEphNet | enum spellings (`Capricornus`, `Aswini`) inherited from here |
| `SRC_SWISSEPH` | Swiss Ephemeris / SwissEphNet | Astrodienst / port | SwissEphNet 2.8.0.2 | astronomy | `SwissEphemerisProvider` | Moshier mode, Lahiri sidereal |

Add a row the same change that first cites a new source. A chapter/verse-scoped code
(`SRC_BPHS_27`) is preferred over the umbrella (`SRC_BPHS`) once the location is confirmed.

## House-placement comparison evidence (2026-09-07)

Evidence keys are passage locators, not new SRC codes or database source rows. They support
[the comparison note](../research-house-placement-pvr-raman.md).

`SRC_PVR_INTEGRATED`: consulted the registered local raw extract; author-hosted discovery
page: [Astrology resources](https://www.vedicastrologer.org/articles/).

| Key | Passage |
|---|---|
| PVR-H1 | Chapter 7 sections 7.1–7.2, printed pp. 67–69: house counting and twelve-house meanings; 7.2 explicitly refers readers to Raman volumes I–II for further house results. |
| PVR-H2 | Chapter 7 sections 7.3–7.4, printed pp. 69–77: divisional context, reference lagnas, planetary references and relative-house categories. |
| PVR-H3 | Chapter 7 section 7.5, printed pp. 77–78: whole-sign recommendation and explicit rejection of the described Bhava/chalit alternatives. |
| PVR-H4 | Chapter 13 section 13.4.1, printed pp. 169–170: chart, house, reference, arudha, influences, standard placement results and strength/avastha/yoga modifiers. |

`SRC_RAMAN_HTJH`: Volume I prose consulted from the finalised extract
`D:\@ClaudeSpace\BookExtracts\how-to-judge-a-horoscope-1.md`, rather than the old `_work`
path in the source table. Printed pages differ from the extract's scan PAGE markers.
Chart-grid transcription is not used as evidence in this comparison.

| Key | Passage |
|---|---|
| RAM-H0 | Volume I, General Introduction, printed pp. 3–4 (scan 12–13): conditional interpretation and twelve-house meanings. |
| RAM-H1 | Volume I, Considerations in Judging a House, printed pp. 5–6 (scan 14–15): eight-factor checklist, modifying influences and period activation. |
| RAM-H2 | Volume I, Concerning the First House, printed pp. 20–21 (scan 29–30): distinction between Bhava and Rasi; Navamsa; house, lord, occupants and karaka. |
| RAM-H3 | Volume I, first-lord placements, printed pp. 21–23 (scan 30–32); fifth-lord placements, printed pp. 205–206 (scan 214–215). |
| RAM-H4 | Volume I, Planets in the First House, printed pp. 39–41 (scan 48–50); Planets in the Third House, printed pp. 129–130 (scan 138–139). |

Volume II source supplied 2026-09-07:
`D:\Vedic Astrology\Vedic Astology Books\B. V. Raman\How to Judje a Horoscope - II.djvu`.
Full-scan OCR destination: `D:\@ClaudeSpace\BookExtracts\how-to-judge-a-horoscope-2-ocr.md`.
Extraction completed: 482/482 pages, no failed or empty pages. Full OCR is a draft; selected rule passages were checked against scans. Volume II is Fourth Edition, Delhi, 1992, Motilal Banarsidass, ISBN 81-208-0845-2, as recorded on scan page 2. Edition metadata is read from OCR; this is not a new database source record.
Additional comparison locators:

| Key | Passage |
|---|---|
| PVR-H5 | Chapter 11, printed p. 133: Harsha, Sarala and Vimala Yoga definitions and results; Amala Yoga appears earlier in the same chapter (search exact heading). |
| RAM-H6 | Volume I: second-lord-in-first, printed p. 81 (scan 90); fourth-lord placements, printed p. 160 (scan 169); sixth-lord placements, printed pp. 245–248 (scan 254–257). |
| RAM-II7 | Volume II: chapter XI, seventh-lord placements, printed pp. 2–5 (scan 9–12); occupants and qualifications, printed pp. 9–12 (scan 16–19); timing/context, printed pp. 13–14 (scan 20–21). |
| RAM-II8 | Volume II: chapter XII, eighth-lord placements and qualifications, printed pp. 71–76 (scan 78–83); occupants, printed pp. 85–88 (scan 92–95). Eighth-lord-in-eighth paragraph on scan 81 visually verified. |
| RAM-II9 | Volume II: chapter XIII, ninth-lord placements and qualifications, printed pp. 180–183 (scan 187–190); occupants, printed pp. 186–189 (scan 193–196). Afflicted ninth-lord-in-fourth wording on scan 188 visually verified and left unresolved. |
| RAM-II10 | Volume II: chapter XIV, tenth-lord placements and occupational caveat, printed pp. 239–242 (scan 246–249); occupants, printed pp. 248–251 (scan 255–258). |
| RAM-II11 | Volume II: chapter XV, eleventh-lord placements and qualifications, printed pp. 362–365 (scan 369–372); occupants, printed pp. 367–368 (scan 374–375); Moon-based timing, printed p. 369 (scan 376). |
| RAM-II12 | Volume II: chapter XVI, twelfth-lord placements and qualifications, printed pp. 411–414 (scan 418–421); occupants and Venus exaltation exception, printed pp. 424–425 (scan 431–432). Twelfth-lord-in-twelfth paragraph on scan 421 visually verified. |
| RAM-IIV | Volume II, printed p. 58 (scan 65): Venus in seventh / karako-bhavanasaya qualification in the Venus–Mars discussion; visually verified. |
| RAM-IIB | Volume II, printed p. 248 (scan 255): prose explanation for Chart 115 discounts Moon at Bhava-sandhi; visually verified. The underlying chart grid was not transcribed or independently recalculated. |
| RAM-IIK | Volume II, printed pp. 278–279 (scan 285–286): Karakamsa definition and occupational use; printed p. 422 (scan 429): Ketu twelfth from Karakamsa, unverifiability of post-death predictions and beginning of house-karaka list. Scan 429 visually verified. |
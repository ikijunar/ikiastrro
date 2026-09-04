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
| `SRC_PHALADEEPIKA` | Phaladeepika | Mantreśvara | — | classical | combustion cross-check | alt. orb set |
| `SRC_RAMAN_HTJH` | How to Judge a Horoscope (vols I–II) | B. V. Raman | — | Raman | house / Lagna significations, functional nature | OCR extract under `D:\@ClaudeSpace\BookExtracts\_work\how-to-judje-a-horoscope-i_p1-312\` |
| `SRC_RAMAN_HINDU_PREDICTIVE` | Hindu Predictive Astrology | B. V. Raman | — | Raman | general | — |
| `SRC_PYJHORA` | PyJHora (source) | B. Satya Prakash (`pyjhora`) | vendored `_research/PyJHora` | mixed | varga formulae, special-lagna / upagraha algorithms | AGPL — vendored for reference, not linked |
| `SRC_JHORA` | Jagannatha Hora (desktop) | P. V. R. Narasimha Rao | v8.x | mixed | golden-record verification | — |
| `SRC_JHORA_EXPORT_RAMAKRISHNAN` | JHora natal export — 1_Ramakrishnan | — | 22 Apr 1981 05:30 Chennai | — | `verify-vargas`, `verify-jaimini` golden values | file `docs/artifacts/reference-charts/Rammy_Jagannatha.txt` |
| `SRC_RATH_VARGA` | Vedic Astrology / varga methods | Sanjay Rath | — | Jaimini / SJC | D11 (Rudramsa), argala | one method-ambiguous varga |
| `SRC_VEDASTRO` | VedAstro.Library | (open source) | pre-2026-08-24 | mixed | historical — replaced by SwissEphNet | enum spellings (`Capricornus`, `Aswini`) inherited from here |
| `SRC_SWISSEPH` | Swiss Ephemeris / SwissEphNet | Astrodienst / port | SwissEphNet 2.8.0.2 | astronomy | `SwissEphemerisProvider` | Moshier mode, Lahiri sidereal |

Add a row the same change that first cites a new source. A chapter/verse-scoped code
(`SRC_BPHS_27`) is preferred over the umbrella (`SRC_BPHS`) once the location is confirmed.

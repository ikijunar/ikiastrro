---
last_updated: 2026-09-08
---

# Yoga corpus expansion beyond Raman's 300

Additions in this document use the `OTHERS` source corpus unless a definition is
specifically sourced to P. V. R. Narasimha Rao, in which case it uses
`PVR-SPECIFIC`. Raman's original numbered collection remains `BVR-300`.

## Scope and method

Candidates were taken first from `SRC_PVR_INTEGRATED`, chapter 11, and compared
by exact name with the complete Raman OCR. Name absence is only a discovery signal:
every formula must still be compared semantically to detect aliases and variants.

Local BPHS, *Saravali*, and *Brihat Jataka* scans need OCR plus edition/translator
metadata before they can support active rule rows. They form a second research wave.

## Recommended PVR additions

| Priority | YogaCode | PVR locator | Relationship to Raman | Inputs |
|---|---|---|---|---|
| P0 | `YOGA_SUBHA` | §11.6, p.124 | New named formation | D1, natural nature |
| P0 | `YOGA_ASUBHA` | §11.6, p.124 | New inverse formation | D1, natural nature |
| P0 | `YOGA_GURU_MANGALA` | §11.6, p.125 | New; not Raman 24 Chandra-Mangala | D1, conjunction/opposition |
| P0 | `YOGA_CHAMARA` | §11.6, p.126 | New normalized yoga | D1, dignity, aspect |
| P0 | `YOGA_KHADGA` | §11.6, p.127 | New normalized yoga | D1, exchange/lordship |
| P0 | `YOGA_LAGNAADHI` | §11.6, p.129 | New Lagna counterpart to Adhi | D1, nature, aspects |
| P0 | `YOGA_SAARADA` | §11.6, p.131 | New normalized yoga | D1, strength |
| P0 | `YOGA_DHARMA_KARMADHIPATI` | §11.7.1, p.134 | PVR Raja-yoga specialization | D1, lord association |
| P0 | `YOGA_VIPAREETA_RAJA` | §11.7.1, pp.134–135 | Umbrella over dusthana-lord interactions; retain Raman Harsha/Sarala/Vimala | D1, lord association |
| P1 | `YOGA_BASIC_RAJA` | §11.7.1, pp.133–134 | Compare with Raman 245–263 | Any chart, lord association |
| P1 | `YOGA_HARI` | §11.6, p.129 | PVR split of Raman 51A | D1 |
| P1 | `YOGA_HARA` | §11.6, p.129 | PVR split of Raman 51B | D1 |
| P1 | `YOGA_BRAHMA_TRIMURTI` | §11.6, p.129 | PVR split of Raman 51C; not Brahma Yoga | D1 |
| P1 | existing `YOGA_PARIJATHA` | §11.6, pp.127–129 | Kalpadruma/Parijata synonym; add variant, not concept | D1+D9 |

## Additional PVR variants

- `PVR_CH11_BHASKARA` adds Moon twelfth from Sun; compare Raman 159.
- `PVR_CH11_KULAVARDHANA` clarifies Raman 70: each planet may be fifth from
  Lagna, Moon, or Sun.
- Keep PVR Matsya, Koorma, Kusuma, and Kalanidhi separate from Raman variants.
- Transcribe PVR's Raja-sambandha, Dhana, and Daridra clauses after the named P0 set.

## Classical-source wave

1. OCR edition, translator, contents, and yoga chapters for BPHS, *Saravali*, and
   *Brihat Jataka*.
2. Register edition-specific `SRC_*` rows before activating rules.
3. Compare predicates, not names, against Raman and PVR.
4. Prefer source variants for shared concepts; create new `YogaCode` only for a
   distinct formation.
5. Keep disease, death, sex, caste, and moral claims neutral and source-attributed.

## Next implementation slice

Implement the nine P0 PVR additions, then resolve the four P1 identity/alias cases.
Prepare applicability rows but defer database application until corpus review.

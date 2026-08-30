# Action Plan — 8-Point Bhava Analysis Reference Data

**Purpose:** Consolidates what B.V. Raman's *How to Judge a Horoscope* (complete Volume 1 extract)
actually contains for each of the 8 classical Bhava-analysis points, cross-referenced against
`vedic_horo_gen`'s current coverage (`bhava-coverage.md`), and lays out concrete next steps per point.

> **Source file:** `D:\@ClaudeSpace\BookExtracts\how-to-judge-a-horoscope-1.md` — the finalised
> Volume 1 extract (produced via the [[book_to_md]] pipeline). This work drew on the book's
> *prose* only (Tier 1 in that file's `reliability` map), so it is unaffected by the chart-grid
> transcription gaps noted there.
>
> *Superseded names, for anyone chasing older references:* this file was renamed 2026-08-30 from
> `how-to-judje-a-horoscope-i_p1-312.md`; the `_work\...\body.md` raw-OCR path cited in earlier
> drafts has no front matter or chart transcriptions and should not be used. See the 2026-08-30
> entry in `cproj_vedic_horo_gen.md`.
**As of:** 2026-08-30
**Status:** Design/action-plan doc — migrations 030/031 below are approved-and-buildable now;
everything else is scoped but not yet built.

---

## Per-point findings

| # | Point | In this book? | What's usable now | Action |
|---|---|---|---|---|
| 1 | Strength/aspects/conjunctions/location of house lord | ✅ Stated as a general principle (p.14-15) — placement, aspect quality, and Dasha/Bhukti timing all matter | No new reference data — this is a synthesis-logic point, and the app's `tbl_Chart_HouseLords`/`Conjunctions`/`Aspects` already supply the raw facts | No action here — covered by existing architecture per the earlier coverage doc |
| 2 | Strength of the house itself (Bhava Bala) | ❌ **Not in this book** — explicitly deferred by the author to his separate book *Graha and Bhava Balas* | Nothing extractable from this extract | **Blocked on this source.** Needs either that other book or a cited BPHS-style Bhava Bala method before it can be built — do not guess a formula |
| 3 | Natural qualities of house/lord/occupants | ✅ **House significations: full 12-house list found** (p.12-13). ✅ Sthira Karaka roles confirmed via scattered usage across 20+ case studies (Thanukaraka=Sun/1st, Dhanakaraka=Jupiter/2nd, Bhratrukaraka=Mars/3rd, Matrukaraka=Moon/4th, Putrakaraka=Jupiter/5th, Ayushkaraka=Saturn/8th — matches the Sthira Karaka table already designed) | House significations ready to seed. Karaka roles cross-validated, not contradicted. | **Migration 030 — ready now** (see below). One gap: a clean *general* planet-signification list (independent of house-role) wasn't found as a single table in this extract — planet-personality traits (e.g. "Mars = courage, energy") are scattered, not consolidated |
| 4 | Yogas altering house influence | ✅ **Extensively covered** — Rajayoga formation rules, Yogakaraka concept (a planet owning both a Kendra and Trikona), Chandramangala Yoga, Gajakesari Yoga, Papakarthari/Subhakarthari Yoga, and a stated **12° orb rule** for yoga-forming conjunctions | Rich material, but as descriptive case-study prose, not an enumerable rule table ready for DB rows | **Too large for this pass.** Recommend its own scoped brainstorming session — yoga detection needs conditional logic (house-lordship combinations + orb thresholds), not just static reference rows |
| 5 | Exaltation/debilitation of house lords | ✅ Confirmed as core method | Nothing new — `ClassicalDignity.cs` already covers this | No action — already ✅ per the coverage doc |
| 6 | House-lord's situation in Navamsa | ✅ Confirmed as core method — constant Rasi/Navamsa cross-checking throughout every case study in the book | Validates, doesn't add new data | No new table — this confirms the existing "D9 within-sign degree" backlog item (ICE 7.7) is the right next build, not a data problem |
| 7 | Age, position, status, sex of subject | 🟡 Checklist line only (p.14) — no consolidated rule section surfaced in a targeted search of this extract (a female-horoscope distinction is mentioned once in a fertility-timing passage, not as a general rule) | Not enough to act on | **Not actionable from this extract.** Would need either a deeper full-text pass or a different source; the app also has no gender/status fields today (confirmed earlier), so this is a two-part gap (data model + reference rules) |
| 8 | Sign-specific well/ill-disposed planets by Lagna | ✅ **All 12 signs found** (p.16-18) | 81 of 84 rows fully specified. 3 genuine gaps confirmed *in the book itself* (not an OCR miss — re-checked against the raw text): **Aries→Moon, Gemini→Saturn, Aquarius→Saturn** are simply never classified | **Migration 031 — ready now, with 3 rows explicitly flagged NULL** rather than waiting further, since this source won't produce them |

---

## Migration 030 — ready to build now

`tbl_Dim_HouseSignification`, `tbl_Dim_PlanetSignification` (Sthira-Karaka-scoped: the planet's
role *as a house significator*, book-confirmed), `tbl_Dim_PlanetHouseKaraka` — as designed earlier
in this conversation. Data source: this book for house significations + karaka roles; Rahu/Ketu
rows still use the previously-agreed modern convention (8th/12th), since Raman's Sthira Karaka
list — like most classical sources — doesn't assign them a fixed house role either.

## Migration 031 — ready to build now, 3 rows flagged

`tbl_Dim_LagnaFunctionalNature`, all 12 signs, sourced from p.16-18 of this book. **3 rows seeded
as `FunctionalNature = NULL, Notes = 'Not classified in source (How to Judge a Horoscope, Raman)'`**
rather than guessed:
- Aries Lagna → Moon
- Gemini Lagna → Saturn
- Aquarius Lagna → Saturn

## Deferred / not actionable this pass

- **Point 2 (Bhava Bala)** — needs a different source entirely.
- **Point 4 (Yoga detection)** — needs its own scoped design; flagged in the earlier coverage doc
  as a gap that was never entered into the ICE backlog — this extract confirms there's real
  material to build from (Rajayoga/Yogakaraka logic, the 12° orb rule), but it's a multi-session
  effort, not a same-batch addition to 030/031.
- **Point 7 (subject context)** — no usable rule material found yet; would need a dedicated deeper
  pass through the extract or a different source, plus new `tbl_BirthDetails` fields (gender at
  minimum) that don't exist today.

## Suggested next action

Proceed with migrations 030 and 031 as scoped (house significations, karaka roles, Lagna
functional nature with 3 explicit gaps) — this closes out points 3 and 8 to the extent this source
allows. Then decide separately, as its own brainstorming pass, whether Yoga detection (point 4) is
worth the larger scoping effort next, given it's the single biggest, richest-sourced gap remaining.

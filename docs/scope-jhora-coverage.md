# Key Computations — JHora Export vs. vedic_horo_gen Coverage

**Purpose:** Line-by-line comparison of a full Jagannatha Hora (JHora) "Natal Chart" text
export against what `vedic_horo_gen` computes and stores today, to isolate exactly which
computations still need to be added and in what order.
**As of:** 2026-08-30
**Reference export:** JHora natal chart for `1_Ramakrishnan` — 22 Apr 1981, 05:30, Chennai
(the project's long-standing verification chart). Full text pasted into the 2026-08-30
working session; an identical copy is saved at `D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt`
(same person/data, includes all 23 divisional-chart grids D1–D144 and all six dasha listings —
byte-for-byte the same source analysed here, not a second chart).
**Sources:** `README.md`, `ikiastrro.md`, `docs/scope-bhava-coverage.md`,
`docs/reference-vedic-data-tables.md`, `methods_prodmag.md` (Opportunity Backlog), live grep of
`src/`.

> The JHora export is being used as a **feature-coverage checklist**, not as a numeric
> oracle — planetary-longitude accuracy is already independently verified (SwissEphNet vs.
> Prokerala/AstroSage, ~0.4–1.0 arcmin, see README). What the export adds here is a
> complete enumeration of *derived* quantities a mature Jyotish engine produces.

---

## 1. Scorecard — every block in the export

| JHora export block | Status in `vedic_horo_gen` | Notes |
|---|---|---|
| Birth data (date/time/TZ/lat-long/place) | ✅ Covered | `tbl_BirthDetails`. **Altitude** is the one field not captured (export shows `0.00 m`). |
| Ayanamsa value (`23-34-49.57`) | 🟡 Used, not surfaced | Lahiri comes straight from `SE_SIDM_LAHIRI`; the numeric value is never written to `tbl_ChartResults` or shown. |
| Sidereal Time (`19:20:55`) | ❌ Missing | Not computed/exposed. |
| Sunrise / Sunset (`05:56:39` / `18:18:53`) | ❌ Missing | **Blocks upagrahas, special lagnas, Ishta Kaala, Hora lord.** SwissEphNet provides `swe_rise_trans` natively. |
| Janma Ghatis (`58.8892`) | ❌ Missing | Trivial once sunrise is available (ghatis since sunrise). |
| Lunar Yr-Mo (`Durmati - Chaitra`) | ❌ Missing | 60-year Samvatsara name + lunar (amanta/purnimanta) month. |
| Tithi (`Krishna Tritiya`, % left) | ❌ Missing | Sun–Moon elongation / 12°. |
| Vedic Weekday (`Tuesday`) | ❌ Missing | Sunrise-to-sunrise weekday, not civil date. |
| Nakshatra + % left (`Anuraadha 70.30%`) | ✅ Covered | `AstroMath.GetNakshatraIndexAndFractionElapsed` + `tbl_Chart_KeyDetails` linkage (name/pada/lord/KP sub-lord). |
| Nitya Yoga (`Vyatipata`, % left) | ❌ Missing | (Sun long + Moon long) / 13°20′ → 27 yogas. Distinct from "yoga detection" (Raj yogas). |
| Karana (`Vanija`, % left) | ❌ Missing | Half-tithi, 11-karana cycle. |
| Hora Lord / Mahakala Hora / Kaala Lord (`Venus`) | ❌ Missing | Planetary-hour rulers from weekday + time since sunrise. |
| **Body table — Longitude / Nakshatra / Pada / Rasi / Navamsa** | ✅ Covered | 10 points (Asc + 7 grahas + Rahu + Ketu): sidereal longitude, D1 degree-in-sign, nakshatra, pada, Rasi sign, Navamsa sign, retrograde. |
| **Body table — Chara-karaka suffixes** (`- PiK`, `- AK`, `- AmK`, …) | ❌ Missing | Jaimini **Chara Karakas** (8-karaka scheme). *Not* the project's planned Sthira Karaka (`reference-house-lagna-significations.md`) — that's the fixed/natural karaka, a different construct. |
| Special / Upa Lagnas — Bhava, Hora, Ghati, Vighati, Varnada, Sree, Pranapada, Indu Lagna | ❌ Missing | Arithmetic off the Lagna + time-since-sunrise. PyJHora has each. |
| `V2`–`V12` (Varnada Lagna per house) | ❌ Missing | Extension of Varnada Lagna. |
| Bhrigu Bindu (`10 Vi 10′`) | ❌ Missing | Rahu–Moon midpoint. One line. |
| Upagrahas — Maandi, Gulika, Dhooma, Vyatipata, Parivesha, Indra Chapa, Upaketu, Kaala, Mrityu, Artha Prahara, Yama Ghantaka | ❌ Missing | Shadow sub-planets. Gulika/Maandi need sunrise/sunset day-night eighths; the rest are longitude offsets from Sun/other upagrahas. |
| Sphutas — Prana, Deha, Mrityu, Sookshma-Tri, Tithi, Yoga, Rahu-Tithi, Kshetra, Beeja, Tri, Chatus, Pancha, Kunda, Avayoga, Yoga (Sun-Moon), Dhooma-derived group | ❌ Missing | All deterministic longitude arithmetic (sums/differences of graha longitudes, some mod 360). |
| **Chara Karaka portfolio table** (AK→Rahu Self, AmK→Venus, …) | ❌ Missing | Direct output of the Chara Karaka calc above; feeds Karakamsa (AK in D9) and every Jaimini rasi dasha. |
| **Ashtakavarga of Rasi Chart** — 8 Bhinnashtakavarga rows + implied Sarva | ❌ Missing (was "out of scope pending source") | See §3 — the sourcing blocker is now resolvable in-repo. |
| Sodhya Pinda / Rasi Pinda / Graha Pinda | ❌ Missing | Ashtakavarga reduction (Trikona + Ekadhipatya Shodhana → pinda). |
| **Shadbala** table (rupas / % strength / IshtaPhala / KashtaPhala) | ❌ Missing | ICE-scored 3.7, parked "Later". `_research/jyotishganit/.../strengths.py` (MIT, tested) implements it. |
| **Vaiseshikamsa** (Dasa Varga 10 / Shodasa Varga 16) — Simhasana, Paaraavata, Gopura, … | ❌ Missing | Needs the full 16-varga set first (§2). |
| **Vimsopaka Bala** (Shad/Sapta/Dasa/Shodasa Varga, with %) | ❌ Missing | Weighted varga-dignity score; needs full 16 vargas + a benefic-weight table. |
| **Planet Age** (Baala/Kumara/Yuva/Vriddha) | ❌ Missing | Baladi Avastha — from degree-in-sign (odd/even sign rule). |
| **Alertness** (Jaagrita/Swapna/Sushupta) | ❌ Missing | Jagrat–Swapna–Sushupti Avastha. |
| **Mood** (Deepta/Deena/Khala/Garvita/…) | ❌ Missing | Deeptadi Avastha (9 states) — dignity + conjunction/aspect driven. |
| **Activity** (Aagama/Gamana/Kautuka/Bhojana/…) | ❌ Missing | Cheshta Avastha. |
| **Divisional chart grids** — 23 in the export (D1 … D144) | 🟡 6 of 23 | Have D1, D2, D6, D9, D10, D11. Missing 17, incl. 12 of the classical Shodashavarga 16. Full per-varga table + D2 variant note in §2. |
| **Vimshottari Dasa** | ✅ Covered — and ahead | Project computes 3 levels (Maha/Antar/Prat) + life-weeks grid; export shows 2. |
| **Moola Dasa** (Jaimini) | ❌ Missing | Jaimini rasi dasa; needs Chara Karakas. |
| **Ashtottari Dasa** (conditional nakshatra dasa) | ❌ Missing | PyJHora (AGPL — port with attribution). |
| **Kalachakra Dasa** | ❌ Missing | PyJHora has it. Complex (Savya/Apasavya, Deha/Jiva). |
| **Narayana Dasa of D-1** (Jaimini phalita rasi dasa) | ❌ Missing | PyJHora has it. |
| **Sudasa** (Jaimini, Lakshmi/Sree dasa) | ❌ Missing | PyJHora has it; needs Sree Lagna. |
| (Yogini Dasa — not in this export but standard) | ❌ Missing | PyJHora, or classical 36-yr table. Noting for completeness. |

**Net:** of ~35 distinct computation blocks in the export, **~7 are fully covered**
(planet longitudes/signs/nakshatra/pada/dignity/retrograde, Rasi + Navamsa placement,
Vimshottari Dasha, and — from the wider project, not this export — house lordship,
conjunctions, Graha Drishti aspects, combustion, planet sign-transit history / Sade Sati).
**~4 are partial** (varga set 6/16, ayanamsa value, Vargottama derivable-but-not-flagged,
Vimshottari depth). **~24 are absent.**

---

## 2. Divisional charts — 6 of 23 grids in the export

The project has walked the `IChartCalculator` add-a-varga path five times (D9, then
D2/D6/D10/D11) and the README documents it as near-zero-code. The export renders **23
divisional grids** (D1 + 22 vargas). The `SV` column marks membership in the classical
strength groupings the export's own Vimsopaka table uses: **6** = Shadvarga, **7** =
Saptavarga, **10** = Dashavarga, **16** = Shodashavarga.

| Chart | JHora grid label | Classical use | SV | Status | Formula source |
|---|---|---|---|---|---|
| D1 | Rasi | Body, all matters | 6·7·10·16 | ✅ Built (+ Web UI) | own `AstroMath` |
| D2 | Hora **(US)** | Wealth | 6·7·10·16 | 🟡 Built — **different variant** (see below) | PyJHora `chart_method=1` |
| D3 | Drekkana (Trd) | Siblings, courage | 6·7·10·16 | ❌ Missing | PyJHora / jyotishganit |
| D4 | Chaturthamsa | Property, fortune | 16 | ❌ Missing | PyJHora / jyotishganit |
| D5 | Panchamsa | (JHora extra — not in Parashari 16) | — | ❌ Missing | PyJHora |
| D6 | Shashthamsa | Health / disease | — | ✅ Built (DB/CLI) | own `AstroMath` |
| D7 | Saptamsa (Trd) | Children, progeny | 7·10·16 | ❌ Missing | PyJHora / jyotishganit |
| D8 | Ashtamsa | (JHora extra) | — | ❌ Missing | PyJHora |
| D9 | Navamsa | Spouse, dharma, overall strength | 6·7·10·16 | ✅ Built (+ Web UI) | own `AstroMath` |
| D10 | Dasamsa (Trd) | Career, status | 10·16 | ✅ Built (DB/CLI) | own `AstroMath` |
| D11 | Rudramsa | Gains / death (Rath convention) | — | ✅ Built (DB/CLI) | own `AstroMath` |
| D12 | Dwadasamsa (Trd) | Parents | 6·7·10·16 | ❌ Missing | PyJHora / jyotishganit |
| D16 | Shodasamsa (Trd) | Vehicles, comforts | 10·16 | ❌ Missing | PyJHora / jyotishganit |
| D20 | Vimsamsa (Trd) | Spiritual practice | 16 | ❌ Missing | PyJHora / jyotishganit |
| D24 | Siddhamsa (Trd) | Education, learning | 16 | ❌ Missing | PyJHora / jyotishganit |
| D27 | Nakshatramsa/Bhamsa (Trd) | Strengths & weaknesses | 16 | ❌ Missing | PyJHora / jyotishganit |
| D30 | Trimsamsa | Misfortunes, evils | 6·7·10·16 | ❌ Missing — **method-ambiguous** | PyJHora `chart_method=1` |
| D40 | Khavedamsa | Maternal legacy | 16 | ❌ Missing | PyJHora / jyotishganit |
| D45 | Akshavedamsa | Paternal legacy | 16 | ❌ Missing | PyJHora / jyotishganit |
| D60 | Shashtiamsa (Trd) | Past-life karma, all matters | 10·16 | ❌ Missing — **method-ambiguous** | PyJHora `chart_method=1` |
| D81 | Navanavamsa | (extended) | — | ❌ Missing | PyJHora |
| D108 | Ashtottaramsa | (extended) | — | ❌ Missing | PyJHora |
| D144 | Dwadasa-Dwadasamsa | (extended) | — | ❌ Missing | PyJHora |

**17 vargas missing.** For the classical **Shodashavarga (16)** specifically, the gap is
D3, D4, D7, D12, D16, D20, D24, D27, D30, D40, D45, D60 — twelve charts. (Note D6 and D11,
which the project *has*, are not members of any classical strength grouping — they were
picked for life-area coverage, not Vimsopaka.)

- **D30 (Trimsamsa)** — unequal 5/5/8/7/5-degree parts, no navamsa-style seed sign;
  odd/even signs map to different planet sequences.
- **D60 (Shashtiamsa)** — "counted from the sign itself" vs. the 60-deity name-table
  variant. Each needs the same one-line documented-decision treatment D11's Rudramsa got.
- **Variant divergence — D2:** the export header reads `D-2 (US)` = **Uma Shambu** 12-sign
  Hora. The project **deliberately chose the classical two-sign Hora** (README, decision
  D-1), so D2 sign placements here will *not* reconcile with this JHora profile. If matching
  this profile matters, add a selectable Hora method (`GetHoraSign` already isolates the
  rule). `(Trd)` on D3/D7/D10/D12/D16/D20/D24/D27/D60 = Traditional Parashari — the
  `chart_method=1` the project already uses; consistent, nothing to change.

**Why this is the unlock:** Vimsopaka Bala and the Vaiseshikamsa grade (§1, both absent)
require the complete strength-grouping set. They cannot be built until at least the
Shodashavarga 16 is complete.

---

## 3. The "pending a cited source" blocker — what's actually resolved

The README parks **Ashtakavarga** as "out of scope for now — its 56 classical contribution
tables need a cited source before any seeding." That specific blocker **is now resolved** by
code already vendored under `_research/`. What each source actually ships (verified by reading
the files — several jyotishganit modules are empty stubs, noted below):

| Item | In-repo reference | Ships? | License |
|---|---|---|---|
| Bhinnashtakavarga + Sarvashtakavarga (BPHS benefic-place tables) | `_research/jyotishganit/.../components/ashtakavarga.py` (211 lines) + `tests/test_ashtakavarga.py` | ✅ yes | **MIT** |
| Shadbala — all 6 balas incl. Cheshtabala; rupas | `_research/jyotishganit/.../components/strengths.py` (1319 lines) + `tests/test_strengths.py` | ✅ yes | **MIT** |
| Panchanga (tithi/karana/yoga/weekday/nakshatra+deity) | `_research/jyotishganit/.../components/panchanga.py` (242 lines) + `tests/test_panchanga.py` | ✅ yes | **MIT** |
| Divisional charts (varga formulas) | `_research/jyotishganit/.../components/divisional_charts.py` (500 lines) | ✅ yes | **MIT** |
| Vimshottari dasha | `_research/jyotishganit/.../dasha/vimshottari.py` (398 lines) | ✅ yes (already built here) | **MIT** |
| **Avasthas** (Baladi / Jagrat-Swapna-Sushupti / Deeptadi / Cheshta) | jyotishganit `components/states.py` | ❌ **empty stub** | — |
| **Ashtottari / Yogini dashas** | jyotishganit `dasha/ashtottari.py`, `dasha/yogini.py` | ❌ **empty stubs** | — |
| Avasthas, Ashtottari/Yogini, Upagrahas, special lagnas, sphutas, Chara Karakas, Kalachakra/Narayana/Sudasa/Moola dashas, Trimsamsa/Shashtiamsa, Vimsopaka/Vaiseshikamsa | `_research/PyJHora/src/jhora/...` | ✅ yes | **AGPL** — port with attribution, or cross-check oracle only |
| Trikona/Ekadhipatya Shodhana → Sodhya/Rasi/Graha Pinda | neither ships it cleanly | ⚠️ derive from BPHS text + PyJHora | — |

**Net:** MIT-licensed, test-covered C#-portable source now exists for **Ashtakavarga (Bhinna +
Sarva), Shadbala, and Panchanga** — the three items the README/backlog treated as
source-blocked. **Avasthas and the extra dashas are *not* covered by jyotishganit** (stub
files); they still depend on PyJHora (AGPL, port-with-attribution) or a classical text.

**Recommendation:** reword the README's Ashtakavarga "needs a cited source" line and add a
sourcing note to `reference-vedic-data-tables.md` — see the exact edits applied 2026-08-30 below.

---

## 3a. Dasha systems — 1 of 6 in the export

| Dasha (export section) | Type | Status | Notes / source |
|---|---|---|---|
| Vimsottari Dasa | Nakshatra (120-yr) | ✅ Built — 3 levels + life-weeks grid | `VimshottariDashaCalculator`. Export prints 2 levels; project is deeper. |
| Moola Dasa | Jaimini rasi dasa | ❌ Missing | Needs Chara Karakas (§4 Tier 1 #3). PyJHora. |
| Ashtottari Dasa | Conditional nakshatra (108-yr) | ❌ Missing | PyJHora (jyotishganit's `ashtottari.py` is an empty stub). Applicability rule (Rahu in a quadrant/trine from lord of lagna) must be evaluated first. |
| Kalachakra Dasa | Nakshatra-pada → rasi | ❌ Missing | Savya/Apasavya, Deha/Jiva, Paramayush. Most complex; lowest priority. PyJHora. |
| Narayana Dasa (of D-1) | Jaimini phalita rasi dasa | ❌ Missing | PyJHora. Also runnable on any varga (JHora offers "Narayana Dasa of D-9" etc.). |
| Sudasa (Sree/Lakshmi dasa) | Jaimini rasi dasa | ❌ Missing | Needs Sree Lagna (§4 Tier 2 #7). PyJHora. |
| *(Yogini Dasa — not in this export)* | Nakshatra (36-yr) | ❌ Missing | PyJHora, or classical 36-yr table (jyotishganit's `yogini.py` is an empty stub). Standard in most tools; noted for completeness. |

Each is a self-contained calculator in the mould of `VimshottariDashaCalculator` (pure
computation + a `tbl_ChartResults` row with `ChartType="<name>Dasha"` + child periods in
`tbl_Chart_DashaPeriods`, which is already self-referencing and dasha-system-agnostic). **No
schema change** — same pattern as Vimshottari. The Jaimini three (Narayana, Sudasa, Moola)
share a dependency on Chara Karakas and Jaimini rasi-progression rules, so they're best done
as one batch after Tier 1.

---

## 4. Proposed additions, tiered

Ordered so each tier unblocks the next, and cheap/deterministic work comes before anything
needing reference tables. Framed for ICE scoring in `methods_prodmag.md` — not yet scored.

### Tier 1 — small pure functions, no reference tables, high parity value

1. **Sunrise / Sunset / Sidereal Time / Janma Ghatis / Ayanamsa-value surfacing.**
   SwissEphNet native (`swe_rise_trans`, `swe_sidtime`, `swe_get_ayanamsa_ex`). Write the
   ayanamsa value onto `tbl_ChartResults`. *Everything in Tier 2 depends on sunrise/sunset.*
2. **Panchanga block:** Tithi, Karana, Nitya Yoga, Vedic weekday, Samvatsara + lunar month.
   Pure arithmetic on Sun/Moon longitudes + the sunrise weekday. jyotishganit `panchanga.py`.
3. **Jaimini Chara Karakas (8-karaka)** + the portfolio table. Rank the 7 grahas + Rahu by
   degrees-within-sign (Rahu counted in reverse). One pure function; deterministic; feeds
   every Jaimini dasha and Karakamsa.
4. **Bhrigu Bindu + the Sphuta family** (Prana/Deha/Mrityu/Beeja/Kshetra/Tri/Chatus/Pancha/
   Tithi/Yoga Sphuta, Kunda, Avayoga). Longitude sums/differences mod 360. One file.

### Tier 2 — mechanical, pattern already proven, needs Tier 1's sunrise

5. **Remaining vargas** — the 12 Shodashavarga charts first (D3, D4, D7, D12, D16, D20, D24,
   D27, D30, D40, D45, D60), then the JHora extras (D5, D8) and extended (D81, D108, D144),
   as `IChartCalculator` pairs (PyJHora `chart_method=1`). D30/D60 each get a documented
   method decision. Completing the Shodashavarga 16 is what unblocks Tier 3 #11.
6. **Upagrahas** (Gulika, Maandi, Dhooma, Vyatipata, Parivesha, Indrachapa, Upaketu, Kaala,
   Mrityu, Ardhaprahara, Yamaghantaka) — day/night eighths for Gulika/Maandi, longitude
   offsets for the rest.
7. **Special / Upa Lagnas** (Bhava, Hora, Ghati, Vighati, Varnada + V2–V12, Sree, Pranapada,
   Indu) — Lagna + elapsed-time arithmetic.

### Tier 3 — strength & state systems

8. **Ashtakavarga** — Bhinna (8 rows) + Sarva + Trikona/Ekadhipatya Shodhana + Sodhya/Rasi/
   Graha Pinda. New `tbl_Chart_Ashtakavarga` (or `tbl_Fact_*` per current naming standard).
   Port from jyotishganit `ashtakavarga.py` (MIT); verify against this export's numbers for
   the reference chart. (Pinda reduction: derive from BPHS + PyJHora — jyotishganit stops at BAV/SAV.)
9. **Shadbala** + Ishta/Kashta Phala — six sub-balas, rupas, % strength. Port from
   jyotishganit `strengths.py` (MIT, 1319 lines, tested); the export gives a full row-by-row
   verification target.
10. **Avasthas** — Baladi (Age), Jagrat/Swapna/Sushupta (Alertness), Deeptadi (Mood),
    Cheshta (Activity). **No MIT source** — jyotishganit `states.py` is an empty stub; use
    PyJHora (AGPL) or a classical rule table.
11. **Vimsopaka Bala + Vaiseshikamsa grade** — after Tier 2 delivers all 16 vargas.

### Tier 4 — additional dasha engines (each its own calculator, like `VimshottariDashaCalculator`)

12. **Ashtottari Dasa**, **Yogini Dasa** — conditional/short nakshatra dashas. PyJHora
    (AGPL) — jyotishganit's stubs don't cover these.
13. **Jaimini rasi dashas** — Narayana Dasa (D1), Sudasa, Moola Dasa — depend on Chara Karakas
    (Tier 1 #3) and Sree Lagna (Tier 2 #7). PyJHora as the porting/cross-check reference.
14. **Kalachakra Dasa** — most complex; lowest priority. PyJHora.
15. **Vimshottari depth** — optionally extend the existing 3-level output to 5 (Sookshma/Prana);
    the recursion already generalises.

---

## 5. Not in the export but adjacent — worth noting

- **Bhava Chalit / cusp chart.** JHora can produce Sripati/Placidus bhava charts; this export
  is Whole-Sign only, and so is the project. No gap *against this export*, but a selectable
  house system is a known deferred item.
- **Vargottama flag.** Derivable today from D1↔D9 sign match, but the project deliberately
  waits on D9 within-sign degree (ICE 7.7, top "Now" item) so it can apply the strict
  first-navamsa trigger. Unchanged by this analysis — just confirming the export surfaces
  nothing that removes that dependency.
- **`tbl_BirthDetails.Altitude`.** Add the column if elevation-sensitive rise/set times are
  ever wanted; negligible for chart work.

---

## 6. Suggested next action

Two concrete, low-risk moves that don't need a prioritisation pass first:

1. **Correct the "pending a cited source" language** in `README.md` and
   `docs/reference-vedic-data-tables.md` — jyotishganit (MIT, vendored, tested) is that source for
   **Ashtakavarga (Bhinna + Sarva), Shadbala, and Panchanga**. (Avasthas and the extra dasha
   systems remain PyJHora-only — jyotishganit ships stub files for those.) *Applied 2026-08-30
   — see the edit records in both files.*
2. **Build Tier 1 as one batch** (sunrise/sunset + panchanga + Chara Karakas + sphutas) —
   all pure functions, no schema change beyond surfacing the ayanamsa value and (optionally)
   a `tbl_Chart_Panchanga` row, and it unblocks Tiers 2–4. This mirrors how D2/D6/D10/D11
   shipped: a coherent DB+CLI batch, Web UI deferred.

Then ICE-score Tiers 2–4 alongside the already-listed backlog items (D9 sub-degree,
ayanamsha-selectable, chart-style selection, Ashtakoota matching).

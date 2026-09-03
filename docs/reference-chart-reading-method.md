# Reference — Divisional Chart Reading Method (JHora-anchored)

**Purpose:** the shared, chart-agnostic procedure for *reading* a divisional chart
(varga). Each per-chart guide (`reference-chart-d1-rasi.md`,
`reference-chart-d9-navamsa.md`, `reference-chart-d10-dasamsa.md`,
`reference-chart-d60-shashtiamsa.md`) assumes this method and only documents what is
*specific* to that varga. `reference-chart-varga-index.md` lists every remaining varga
with a short note.

**Scope of these docs:** interpretation methodology and the classical significations the
`ikiastrro` engine is built to serve — *how a chart is read*, not new calculation rules.
The maths and their sources live in
[`reference-calculations.md`](reference-calculations.md); the classical master-data
tables in [`reference-vedic-data-tables.md`](reference-vedic-data-tables.md); the
house/planet significations and functional-nature rules in
[`reference-house-lagna-significations.md`](reference-house-lagna-significations.md). The
block-by-block JHora export map is
[`scope-jhora-coverage.md`](scope-jhora-coverage.md).

**Anchor:** every guide is written against a **Jagannatha Hora (JHora)** "Natal Chart"
text export — the project's long-standing verification oracle. The reference export used
throughout is `scratch/Rammy_Jagannatha.txt` (1_Ramakrishnan — 22 Apr 1981, 05:30,
Chennai). All JHora grid labels, method tags, column names and abbreviations named below
are taken from that file.

**As of:** 2026-09-01. **Status:** living.

> These are interpretive frameworks. A varga weights probabilities and describes *where
> to look*; it does not fix events. Nothing here is a prediction.

---

## 1. Prerequisites before you read anything

| Requirement | Value in this project | Why it matters |
|---|---|---|
| Rectified birth data | date, clock time, TZ, lat/long, place — `tbl_BirthDetails` | Higher vargas (D24+, D60) shift with ~1 min of clock time. A varga read on an unrectified time is directional only. |
| Ayanamsa | **Lahiri (Chitrapaksha)**, `SE_SIDM_LAHIRI` — JHora export shows `23-34-49.57` | Every sign and division depends on it. Must be the same ayanamsa across D1 and the varga. |
| House system | **Whole Sign** everywhere | "House *n* from X" = the *n*th sign from X's sign. No cusp maths in the reading. |
| D1 already read | see `reference-chart-d1-rasi.md` | Every varga is read **against D1**. D1 promises; the varga shows delivery and detail. |
| Varga method known | `tbl_Rule_VargaScheme` / the `Method / sign rule` column in `reference-calculations.md` §2 | JHora tags each grid `(Trd)` = Traditional Parashari or `(US)` = Uma-Shambu. Placements only reconcile if the method matches (see §6). |

---

## 2. The universal varga-reading loop

Run these steps in order for any varga. The per-chart guides specialise steps 2, 4 and 7
to their subject.

1. **Read D1 first.** Note the varga's subject as it stands in the birth chart — the
   relevant house, its lord, the natural (sthira) karaka, any yoga touching it. This is
   the promise the varga is being asked to confirm or deny.

2. **Fix the varga's subject.** Each varga governs a domain (D9 = spouse/dharma,
   D10 = career, D60 = accumulated karma). Identify:
   - the **primary bhava(s)** for that domain (e.g. 7th for spouse, 10th for career),
   - the **natural karaka** planet(s) (e.g. Venus for spouse, Saturn/Sun/Mercury for
     career),
   - the **chara karaka** overlay from the D1 body table where the tradition uses one
     (e.g. Dara Karaka → D9 spouse, Amatya Karaka → D10 career). JHora prints these as
     suffixes: `Sun - PiK`, `Venus - AmK`, `Rahu - AK`, etc. See §4.

3. **Locate the varga Lagna and its lord.** The varga has its own ascendant (the
   `As` cell in the JHora grid). Read its sign, its lord's varga placement and dignity,
   and any planet conjunct/aspecting it. This is the "self" of that domain.

4. **Assess each significator inside the varga.** For the bhava lord, the karaka(s), and
   the varga-Lagna lord, record four things:
   - **varga house** counted from the varga Lagna,
   - **varga sign dignity** — exalted / debilitated / own / friend / neutral / enemy
     (dignity tables: `reference-calculations.md`, `reference-vedic-data-tables.md`),
   - **retrograde** — JHora prints `R` after the body name in the grid (`JuR`, `SaR`),
   - **combustion / war** where relevant (real-body flags, identical to D1).

5. **Vargottama check.** Same sign in D1 and this varga = *vargottama*: the planet (or
   Lagna) delivers its D1 result stably and with force in that domain. A planet strong in
   D1 but fallen in the varga = the D1 promise is diluted; strong in both = it matures
   fully; weak in both = little to expect there.

6. **Yoga inside the varga.** Look for the same yoga-forming relationships you would in
   D1 — conjunction, mutual aspect, **Parivartana** (sign exchange) between the varga
   Lagna lord and a bhava lord, kendra/trikona links. A varga can *form* a yoga that D1
   only hints at, and can *break* one D1 seems to promise.

7. **House-by-house / significator checklist.** Walk the varga's own houses for the
   domain's sub-topics (the per-chart guide gives the list — e.g. D10: 10th for
   profession, 7th for business/public dealing, 6th for service/employment, 3rd for
   effort). Note benefics vs malefics in and aspecting each.

8. **Read JHora's overlay cells.** JHora drops non-planet points into varga grids —
   `AL` (Arudha Lagna), `GL` (Ghati Lagna), `HL` (Hora Lagna), plus `Md` (Maandi) and
   `Gk` (Gulika). Their varga-house position adds a layer: `AL` in the varga = how the
   domain is *perceived*; `Md`/`Gk` mark friction. Treat them as supporting, not
   primary.

9. **Strength score — Vimsopaka / Vaiseshikamsa.** JHora's `Vimsopaka` table scores each
   planet's dignity across the Shad- (6), Sapta- (7), Dasa- (10) and Shodasa-varga (16)
   groupings, as points out of 20 and a percent. Its `Vaiseshikamsas` table names the
   grade a planet reaches when it holds good dignity across many vargas
   (`5-Simhasana`, `6-Paaraavata`, …). Use these as a cross-varga sanity read on a
   planet's overall quality — a high Vimsopaka planet that looks weak in one varga is
   probably still reliable; a low one that looks strong in one varga is not.

10. **Time it with the D1 dasha.** Run the D1 Vimshottari sequence (JHora `Vimsottari
    Dasa`; the engine computes Maha/Antar/Prat). When a period lord's turn comes, judge
    the event by *that lord's condition in the relevant varga*, not only in D1. A
    career event in a Mercury period is graded by Mercury in D10; a marriage event by the
    dasha lord and Venus/DK in D9.

11. **Verdict discipline.** State the varga result as a modifier on the D1 reading, not a
    replacement. Only let a varga overturn D1 when (a) the birth time is confidently
    rectified and (b) the varga signal is unambiguous and corroborated (dignity + house +
    karaka + a second varga all agreeing). Otherwise report it as a caution.

---

## 3. Dignity, strength and functional nature — where the tables live

Reading steps 4–5 and 9 depend on classical tables the engine already encodes. Do not
restate them in the per-chart guides — link to:

- **Exaltation / debilitation / moolatrikona / own / friendship** —
  `reference-calculations.md` §"dignity", `reference-vedic-data-tables.md`
  (`tbl_SignAttributes`, `tbl_Planets`).
- **Natural (sthira) karaka per house, functional benefic/malefic per Lagna** —
  `reference-house-lagna-significations.md` (sourced from B.V. Raman).
- **Graha Drishti (planetary aspects)** — the special 3/4/8/7/10 aspects of
  Mars/Jupiter/Saturn plus the universal 7th; `reference-calculations.md`.
- **Combustion orbs, retrograde, Sade Sati** — `reference-calculations.md`.

---

## 4. Chara Karakas (Jaimini) — the portable overlay

JHora's body table suffixes each planet with its **chara karaka** in the 8-karaka
(Ashta) scheme, ranked by degrees-in-sign (highest = AK):

| Code | Karaka | Portfolio (JHora wording) | Reads onto |
|---|---|---|---|
| `AK` | Atma | Self | the whole chart; AK-in-D9 = *Karakamsa* |
| `AmK` | Amatya | Minister (advisor) | career / D10 |
| `BK` | Bhratru | Brother (associate) | siblings, effort / D3 |
| `MK` | Matru | Mother (nourisher) | mother, property, learning / D4, D12, D24 |
| `PiK` | Pitru | Father (authority) | father, dharma / D9, D12 |
| `PK` | Putra | Son (follower) | children / D7 |
| `GK` | Gnati | Cousin (rival) | conflict, disease, litigation / D6 |
| `DK` | Dara | Spouse (partner) | marriage / D9 |

In the reference export: `AK Rahu, AmK Venus, BK Saturn, MK Jupiter, PiK Sun, PK Moon,
GK Mars, DK Mercury`. These are **not** the project's planned Sthira Karaka (fixed
natural karaka) — that is a different construct (`reference-house-lagna-significations.md`).
Use the chara karaka as a second significator alongside the natural karaka and the bhava
lord.

---

## 5. Reading the JHora grid

JHora renders every varga as a **South-Indian** fixed grid (Aries top-left second cell,
zodiac clockwise). What you see in a cell:

- **Planet codes** — `As Su Mo Ma Me Ju Ve Sa Ra Ke`. Trailing `R` = retrograde
  (`JuR`, `SaR`). `As` = the varga ascendant.
- **Overlay codes** — `AL` Arudha Lagna, `GL` Ghati Lagna, `HL` Hora Lagna,
  `Md` Maandi, `Gk` Gulika. (The full special-lagna set — Bhava/Vighati/Varnada/Sree/
  Pranapada/Indu — appears in the body table, not usually in the varga grids.)
- **Centre label** — chart name + JHora code: `Rasi`, `Navamsa / D-9`,
  `Dasamsa / D-10 (Trd)`, `Shashtiamsa / D-60 (Trd)`, `Hora / D-2 (US)`.

To read a varga house: find `As`, count signs clockwise (Whole Sign), that is house 2,
3, … from the varga Lagna.

---

## 6. Method tags — `(Trd)` vs `(US)` and where placements diverge

JHora labels the sign rule it used. This project's chosen methods
(`reference-calculations.md` §2, `tbl_Rule_VargaScheme`):

| Tag in export | Meaning | Project match? |
|---|---|---|
| `(Trd)` on D3/D7/D10/D12/D16/D20/D24/D27/D60 | Traditional Parashari — `chart_method=1` | **Yes** — placements reconcile. |
| `(US)` on **D-2** | Uma-Shambu 12-sign Hora | **No** — project deliberately uses the classical two-sign Leo/Cancer Hora (decision D-1). D2 sign placements will *not* match this JHora profile. A separate `D2-US` chart type exists for when matching JHora matters. |
| D30, D60 | "method-ambiguous" — unequal-parts vs 60-deity-name variants | Project uses one documented rule; see the D60 guide. Cross-checks against JHora use the same `(Trd)` rule. |

When a per-chart guide shows a worked lens over the reference export, it uses the method
JHora used for that grid.

---

## 7. Abbreviation glossary (as they appear in the export)

**Body / overlay:** `As` ascendant · `Su Mo Ma Me Ju Ve Sa` grahas · `Ra` Rahu ·
`Ke` Ketu · `R` retrograde suffix · `Md` Maandi · `Gk` Gulika · `AL` Arudha Lagna ·
`GL` Ghati Lagna · `HL` Hora Lagna · `BL` Bhava Lagna · `V2…V12` Varnada Lagna per house.

**Chara karaka:** `AK AmK BK MK PiK PK GK DK` (see §4).

**Signs:** `Ar Ta Ge Cn Le Vi Li Sc Sg Cp Aq Pi`.

**Panchanga header:** `Tithi`, `Yoga` (Nitya, not Raj yoga), `Karana`, `Nakshatra` +
`% left`, `Hora Lord`, `Janma Ghatis`, `Sidereal Time`, `Ayanamsa`.

**Avastha columns (D-1):** `Age` = Baladi (Baala/Kumara/Yuva/Vriddha/Mrita) ·
`Alertness` = Jagrat / Swapna / Sushupti · `Mood` = Deeptadi (9 states) ·
`Activity` = Cheshta. JHora prints these for D1 only.

**Strength tables:** `Vimsopaka` (points/20 + % across Shad/Sapta/Dasa/Shodasa groupings)
· `Vaiseshikamsas` (named grade: Simhasana, Paaraavata, Gopura, Parijaata, …).

---

## 8. Caveats

- **Birth-time sensitivity** rises with the division factor. D1–D12 tolerate a minute or
  two; D24/D27/D30/D40/D45/D60 do not. If the time is not rectified, treat those vargas
  as suggestive only and say so.
- **Method ambiguity** (D30, D60, D2) — always state which rule was used. A varga read
  with the wrong rule is worse than no varga.
- **Software dependence** — hand computation is error-prone at this resolution. The grids
  come from the engine (SwissEphNet), verified cell-by-cell against the JHora export
  (281 checks, `verify-vargas`).
- **Vimsopaka needs the full set** — the score is only meaningful once all 16 strength-
  group vargas are computed; partial coverage makes step 9 provisional.
- **Framework, not prophecy** — see the banner at the top.

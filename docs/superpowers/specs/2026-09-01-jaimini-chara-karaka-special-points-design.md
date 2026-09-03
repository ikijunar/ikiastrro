# Design Spec — Chara Karakas & Special Points

**Status:** Approved design, pre-implementation
**Owner:** rammyps
**Created:** 2026-09-01
**Approach:** extend the existing chart-fact pipeline (see "Approaches considered")
**Sequenced before:** `2026-09-01-varga-centric-web-ui-design.md` (the UI consumes this)
**Related:**
- `scratch/Rammy_Jagannatha.txt` — the Jagannatha Hora natal export this matches (person `1_Ramakrishnan`)
- `scratch/D1-D9-CombinedChart.png` — the combined-chart reference image
- `docs/research-topic-coverage.md` §2 (Chara Karaka = "MISSING", Naisargika Karaka Sapta-vs-Ashta note)
- `docs/reference-calculations.md` §2 (varga sign rules), §10 (avastha layer — the star-schema precedent)
- `docs/superpowers/specs/2026-08-31-divisional-chart-completion-design.md` (the `VargaChartComputer` / `IVargaSignRule` this reuses)
- `db/ikiastrro.sql` (baseline — `tbl_Chart_KeyDetails` is the table that grows)

---

## 1. Goal

Compute and store, for every saved person, the two Jaimini/Parashari layers Jagannatha Hora
prints that the project does not yet have:

1. **Chara Karakas** (Jaimini, 8-karaka / Ashta scheme) — the movable significators
   `AK AmK BK MK PiK PK GK DK`, derived from the D1 planetary degrees.
2. **Special points** — Arudha Lagna (AL) + the 12 Bhava Arudhas (A1…A12), Hora Lagna (HL),
   and the upagrahas Gulika and Maandi — each a longitude that is carried through **all 21
   divisional charts** exactly as a planet is.

Plus a **static combined D1+D9 chart** (`CombinedD1D9Grid`) matching the reference image:
one South-Indian frame, inner = Rasi occupants, outer band = Navamsa occupants, corner
labels for the special points.

**Interpretation is out of scope.** This computes and stores classical facts. Karakamsa,
Jaimini rasi dashas (Narayana / Sudasa / Moola), and any judgement built on the karakas are
future work (`docs/research-topic-coverage.md` §2 roadmap items 4–5).

---

## 2. Decisions locked (brainstorming Q&A, 2026-09-01)

| # | Decision |
|---|---|
| K1 | **Chara Karaka scheme = 8-karaka (Ashta)** — 8 grahas incl. Rahu, `AK AmK BK MK PiK PK GK DK`. Matches the user's list (has both PiK and PK) and the JHora export. |
| K2 | **Special-point set** = AL + A2…A12 (12 Bhava Arudhas, A1 ≡ AL) + HL + Gulika + Maandi. The exact labels in `D1-D9-CombinedChart.png`. |
| K3 | Special points are computed through **all 21 divisional charts** — each gets a `Sign`, `VargaLongitudeDegrees`, and house per chart via the same `IVargaSignRule` a planet uses. |
| K4 | **Chara Karaka storage** = a `CharaKaraka` column on `tbl_Chart_KeyDetails`, set on the 8 graha rows of every chart type (the label is computed once from D1 and copied). Not a separate person-level table. |
| K5 | **Special-point storage** = extra rows in `tbl_Chart_KeyDetails`, discriminated by a new `PointKind` column (`Graha` \| `SpecialLagna` \| `Arudha` \| `Upagraha`). They flow through the existing grid / positions renderer. Not a separate table. |
| K6 | **Combined D1+D9 chart** = static, nested-band single grid (`CombinedD1D9Grid.razor`), side-by-side fallback on narrow screens. Non-interactive. |
| K7 | Ketu is **excluded** from the Chara Karaka ranking (standard for both schemes). |

---

## 3. Scope

**In:** §4–§9 below.
**Out:** Karakamsa (D9-from-AK) chart; Jaimini rasi dashas; Argala / Jaimini aspects; the
other special lagnas (Bhava, Ghati, Vighati, Varnada, Sree, Pranapada, Indu) and Bhrigu
Bindu; the ~40 sphutas in the export; Sapta (7) karaka scheme; any interpretation text.
**Touches Core:** new `Ikiastrro.Core.Jaimini` (1 class) + `Ikiastrro.Core.SpecialPoints`
(3 classes); `SwissEphemerisProvider` gains sunrise/sunset; `ChartAnalysisInput` /
`VargaChartComputer` / `ChartAnalyzer` gain a special-points channel.
**Touches Data:** `ChartGenerationService` computes + persists the new layers;
`ChartKeyDetailsRepository` INSERT/columns.
**Touches CLI:** `Program.cs` — new `verify-jaimini` mode.
**Touches DB:** migration `14` (2 columns on `tbl_Chart_KeyDetails`) + baseline fold.
**Touches Web:** `CombinedD1D9Grid.razor` only (the rest of the UI is Spec 2).

---

## 4. Chara Karakas

### 4.1 Algorithm (`CharaKarakaCalculator`, `Ikiastrro.Core.Jaimini`, pure)

```csharp
namespace Ikiastrro.Core.Jaimini;

public enum CharaKaraka { AK, AmK, BK, MK, PiK, PK, GK, DK }   // ranked order

public static class CharaKarakaCalculator
{
    /// <summary>
    /// 8-karaka (Ashta) assignment from the D1 chart. Ranks the 8 grahas
    /// (Sun..Saturn, Rahu) by longitude WITHIN their sign, descending;
    /// Rahu's key is (30 - itsDegreeInSign) because it is always retrograde.
    /// Highest -> AK, lowest -> DK. Ketu is not ranked.
    /// </summary>
    /// <param name="degreeInSignByPlanet">
    /// D1 within-sign degree [0,30) for each of Sun,Moon,Mars,Mercury,Jupiter,Venus,Saturn,Rahu.
    /// </param>
    public static IReadOnlyDictionary<PlanetName, CharaKaraka> Assign(
        IReadOnlyDictionary<PlanetName, double> degreeInSignByPlanet);
}
```

- Input is the D1 `DegreesInSignDecimal` already on `tbl_Chart_KeyDetails` (or the equivalent
  from `ChartAnalysisInput`). No tie-break rule is needed for real ephemeris data; if two
  keys are ever exactly equal, fall back to the fixed graha order `Sun<Moon<...<Rahu`
  (documented, asserted never to fire on the seeded people).
- The returned `CharaKaraka` enum value is stored as its **string name** (`"AK"`, `"AmK"`, …)
  in the `CharaKaraka` column.

### 4.2 Golden values — `1_Ramakrishnan` (from the export)

| Karaka | Planet | within-sign deg used |
|---|---|---|
| AK | Rahu | 16.941 (= 30 − 13.059) |
| AmK | Venus | 11.992 |
| BK | Saturn | 10.956 |
| MK | Jupiter | 8.727 |
| PiK | Sun | 8.205 |
| PK | Moon | 7.293 |
| GK | Mars | 3.950 |
| DK | Mercury | 1.842 |

### 4.3 Storage & wiring

- `CharaKaraka` is computed **once per person** from the D1 chart, inside
  `ChartGenerationService`, and passed into the analytics build for **every** chart type.
- `ChartAnalyzer` sets `ChartKeyDetail.CharaKaraka` on each of the 8 graha rows (NULL on the
  Ascendant row, the Ketu row, and every special-point row).
- Same value on D1, D9, D30, … — the karaka does not re-rank per varga (classical: the
  karaka is a property of the graha, it travels).

---

## 5. Special points

### 5.1 The set

| Code(s) | Kind | Needs sunrise? | Source |
|---|---|---|---|
| `AL` = `A1` | `Arudha` | no | Arudha pada of the 1st house |
| `A2`…`A12` | `Arudha` | no | Arudha pada of houses 2–12 |
| `HL` | `SpecialLagna` | **yes** | Hora Lagna |
| `Gulika` | `Upagraha` | **yes** | Gulika (weekday day/night eighth) |
| `Maandi` | `Upagraha` | **yes** | Maandi / Mandi |

`A1` is stored under the code `AL` (never `A1`) so the UI label matches convention; `A2…A12`
keep the `A`-prefixed codes JHora uses.

### 5.2 Arudha padas (`ArudhaCalculator`, `Ikiastrro.Core.SpecialPoints`, pure)

```csharp
/// <summary>
/// Arudha pada of a house (Parashara): count L signs from the house to its lord
/// (inclusive), then L signs on from the lord's sign (inclusive). If the result
/// is the 1st or 7th sign FROM the house, take the 10th sign from that result.
/// The pada is placed at the same degree-in-sign as the natal Lagna (JHora
/// convention) so it has a longitude for varga transformation.
/// </summary>
public static IReadOnlyList<ArudhaPada> Compute(
    ZodiacName lagnaSign,
    double lagnaDegreeInSign,
    IReadOnlyDictionary<ZodiacName, PlanetName> signLords,      // whole-sign lord of each sign
    IReadOnlyDictionary<PlanetName, ZodiacName> planetSigns);   // D1 sign of each graha

public sealed record ArudhaPada(int House, string Code, ZodiacName Sign, double NirayanaLongitudeDegrees);
```

- `signLords` / `planetSigns` come from the D1 `ChartAnalysisInput` — no new data.
- **Golden value** (`1_Ramakrishnan`): Lagna Aries; lord Mars in Aries ⇒ L = 1; 1 sign on
  from Aries = Aries; Aries is the 1st from the house ⇒ exception ⇒ 10th from Aries =
  **Capricorn**. Matches the export (`AL` sits with Ketu, and Ketu is in Capricorn).
- Degree-in-sign of every pada = the natal Lagna's degree-in-sign (`0 Ar 38' 50.97"` for
  Ramakrishnan ⇒ `AL` longitude = `270 + 0.6475 = 270.6475°`).

### 5.3 Sunrise/sunset (`SwissEphemerisProvider.GetSunTimes`)

```csharp
public sealed record SunTimes(DateTime SunriseUtc, DateTime SunsetUtc, DateTime PriorSunriseUtc);

/// <summary>swe_rise_trans for the birth date/place; PriorSunriseUtc is the sunrise
/// immediately BEFORE the birth moment (used when birth falls between midnight and
/// sunrise — a "night birth", as 1_Ramakrishnan's 05:30 vs 05:56 sunrise is).</summary>
public static SunTimes GetSunTimes(BirthDetails birthDetails);
```

- Uses `SEFLG_MOSEPH` + the same lat/long the chart uses; disc-centre, no refraction (JHora
  default). The plan pins the exact `swe_rise_trans` flags against the export's
  `Sunrise: 5:56:39 (April 21)` / `Sunset: 18:18:53 (April 21)` lines.
- **Day vs night birth:** birth is a "day birth" if `PriorSunrise ≤ birth < Sunset` of that
  arc, else "night birth". `1_Ramakrishnan` (05:30, prior sunrise 05:56 the day before,
  i.e. birth is after the prior sunrise but the *current* day's sunrise is 05:56 → birth is
  in the night arc that started at the prior sunset) → **night birth**.

### 5.4 Hora Lagna (`HoraLagnaCalculator`)

Classical: HL coincides with the Lagna at sunrise and advances **30° per hour** of elapsed
time since (the most recent) sunrise. `HL_long = siderealSunLongAtSunrise + elapsedHours * 30°`,
normalised. `elapsedHours` = (birth − most-recent-sunrise) in hours.
**Golden value** (`1_Ramakrishnan`): export `Hora Lagna 23 Pi 55' 08.17"` ⇒ **Pisces**,
Navamsa **Aquarius**. The plan locks the sunrise-Sun-longitude source and the elapsed-time
reference (prior sunrise for a night birth) against this.

### 5.5 Gulika & Maandi (`UpagrahaCalculator`)

Weekday-based: the day (sunrise→sunset) and night (sunset→next sunrise) are each split into
8 parts; the part ruled by Saturn (by the standard weekday day-part / night-part tables)
gives Gulika; Maandi/Mandi uses the start of that part (some traditions use the end — the
plan picks the one that reproduces the export and documents it). The Lagna rising at that
instant, taken to the same degree-in-sign as the natal Lagna, is the upagraha's longitude.
**Golden values** (`1_Ramakrishnan`): `Gulika 7 Li 44' 38.12"` ⇒ **Libra**;
`Maandi 18 Li 07' 00.73"` ⇒ **Libra** (both with `Md Gk` in the export's Rasi grid).

### 5.6 Carrying points through the vargas

`ChartAnalysisInput` gains:

```csharp
public sealed record SpecialPointPosition(
    string Code,                 // "AL","A2".."A12","HL","Gulika","Maandi"
    string PointKind,            // "Arudha" | "SpecialLagna" | "Upagraha"
    double NirayanaLongitudeDegrees);
```

`VargaChartComputer.Compute` takes the D1 special points and, for each chart, applies the
**same `IVargaSignRule` + `AstroMath.GetVargaLongitude(realLon, N)`** it already applies to
planets, producing per-chart `Sign` / `VargaLongitudeDegrees` / `DegreesInSign*` /
`HouseNumberFromLagna` for every point. D1 uses the point's nirayana longitude directly.
`ChartAnalyzer` emits one `ChartKeyDetail` per point with `PointKind` set and the
graha-only fields (`PlanetId`, dignity block, nakshatra block, combustion block,
`AspectingPlanets`, `CharaKaraka`, `EclipticLatitudeDegrees`, `SpeedLongitudeDegPerDay`,
`IsRetrograde`) left NULL. `HouseNumberFromSun` / `HouseNumberFromMoon` are still computed
(they are pure sign arithmetic) so the `NOT NULL` columns are satisfied.

---

## 6. Schema — migration 14

`db/14_add_karaka_and_pointkind.sql` (numbered, idempotent, self-recording in
`dbo.SchemaMigrations`), then folded into `db/ikiastrro.sql` as a plan task (same pattern as
Plan A migrations 10–13 → Task 19).

```sql
ALTER TABLE dbo.tbl_Chart_KeyDetails
    ADD PointKind  VARCHAR(12) NOT NULL CONSTRAINT DF_KeyDetails_PointKind DEFAULT 'Graha',
        CharaKaraka VARCHAR(4)  NULL;
GO
ALTER TABLE dbo.tbl_Chart_KeyDetails
    ADD CONSTRAINT CK_KeyDetails_PointKind
        CHECK (PointKind IN ('Graha','SpecialLagna','Arudha','Upagraha'));
ALTER TABLE dbo.tbl_Chart_KeyDetails
    ADD CONSTRAINT CK_KeyDetails_CharaKaraka
        CHECK (CharaKaraka IS NULL OR CharaKaraka IN ('AK','AmK','BK','MK','PiK','PK','GK','DK'));
-- non-Graha rows must not carry graha-only analytics
ALTER TABLE dbo.tbl_Chart_KeyDetails
    ADD CONSTRAINT CK_KeyDetails_NonGrahaNulls
        CHECK (PointKind = 'Graha'
            OR (PlanetId IS NULL AND DignityStatus IS NULL AND Nakshatra IS NULL
                AND CharaKaraka IS NULL AND AspectingPlanets IS NULL AND IsCombust IS NULL));
```

Backfill is implicit — every existing row takes the `'Graha'` default; `CharaKaraka`
starts NULL and is filled by the first `recompute-keydetails` after the code lands.

`vw_Chart_Consolidated` gains `kd.PointKind` and `kd.CharaKaraka`.

`verify-schema` changes:
- `KeyDetails.PlanetId populated (non-Ascendant)` → `... WHERE PointKind='Graha' AND Planet<>'Ascendant'`.
- new: `non-Graha KeyDetails carry no graha-only analytics` (0 violations).
- new: `every chart has exactly 8 CharaKaraka-labelled rows` (one per Ashta karaka), and the
  8 labels are distinct.

---

## 7. `CombinedD1D9Grid.razor` (Web — static)

Non-interactive. Reuses `SouthIndianGrid`'s 4×4 cell geometry (the 12 outer cells + centre).

- **Each sign-cell:** planet glyphs for the **Rasi** occupants in the cell body; a thin
  **outer band** (top edge of the cell) holds the **Navamsa** occupants of the same sign.
  Glyphs use the `PlanetChip` colour treatment (Spec 2 §2.6) but at mini size.
- **Corner mini-labels:** `AL` and `A2…A12` where each Arudha sits (Rasi position); `HL` /
  `Gk` / `Md` likewise. Small, muted, top-left of the cell (as in the reference image).
- **Centre:** two lines — "Outer: Navamsa" / "Inner: Rasi" — plus the Lagna sign.
- **Responsive:** below ~560 px the nested band is dropped and the component renders two
  stacked `SouthIndianGrid`s (D1, then D9) with the special-point labels on each.
- Rendered on the workspace (Spec 2 §4.1) and in `/charts/{id}/print`.

Colour uses the `--planet-*` tokens + minimal `PlanetChip` landed in this spec's Phase 6
(§8); Spec 2 Phase 1 builds the full `PlanetChip` on top. No other new tokens.

---

## 8. Build sequencing (plan phases)

1. **Sunrise** — `SwissEphemerisProvider.GetSunTimes` + a `verify-jaimini` sub-check
   asserting the export's sunrise/sunset to the second.
2. **Chara Karakas** — `CharaKarakaCalculator`; thread a `IReadOnlyDictionary<PlanetName,
   CharaKaraka>` from `ChartGenerationService` into `ChartAnalyzer`; migration 14 column;
   `verify-jaimini` karaka asserts.
3. **Arudhas** — `ArudhaCalculator`; `SpecialPointPosition` channel through
   `ChartAnalysisInput` / `VargaChartComputer` / `ChartAnalyzer`; AL + A2–A12 rows in all 21
   charts; `verify-jaimini` AL assert.
4. **HL / Gulika / Maandi** — `HoraLagnaCalculator` + `UpagrahaCalculator`; add to the
   special-points channel; `verify-jaimini` HL/Gulika/Maandi asserts.
5. **Migration 14 baseline fold** + `vw_Chart_Consolidated` + `verify-schema` new rules;
   regenerate all 5 seeded people (`compute-all`); full `verify-*` sweep.
6. **`CombinedD1D9Grid.razor`** + a `/charts/{id}` placeholder mount (full workspace is Spec 2).
7. **Docs** — `docs/reference-calculations.md` (new §), `ARCHITECTURE.md`,
   `docs/dbdesign-star-schema-rules-engine.md` (PointKind), `master_ikiastrro.md`,
   `../ikiastrro.md`, memory.

App builds and `verify-*` pass after every phase.

---

## 9. Verification (no test project)

- **Build gate** after every phase — `dotnet build Ikiastrro.slnx` 0/0.
- **`verify-jaimini`** (new CLI mode, sibling of `verify-vargas`) — hard-coded asserts
  against `scratch/Rammy_Jagannatha.txt`:
  - sunrise `05:56:39` / sunset `18:18:53` (±1 s), night-birth classification;
  - the 8 karakas exactly as §4.2;
  - `AL` → Capricorn; `HL` → Pisces (D1) / Aquarius (D9); `Gulika` → Libra; `Maandi` → Libra;
  - a spot-check that `AL`'s D9 sign equals what the `NavamsaD9` rule gives for `AL`'s D1
    longitude (channel-integrity check);
  - exit 1 on any FAIL.
- **`verify-schema`** — the three new rules in §6 pass; existing rules still pass.
- **`verify-vargas`** — unchanged, still ALL PASS (special-point rows must not perturb the
  planet-only grid assertions — they are filtered by `Planet <> 'Ascendant'` today, so add
  `AND PointKind = 'Graha'`).
- **Golden record** — `compute-all Ramakrishnan` then eyeball `/charts/{id}` combined grid
  against `scratch/D1-D9-CombinedChart.png`: AL in Capricorn, HL in Pisces, Gulika+Maandi in
  Libra, karaka labels on the 8 grahas.
- **Idempotency** — `recompute-keydetails` twice ⇒ 0 net row change; `verify-*` still green.

---

## 10. Open questions / risks

1. **Maandi start-vs-end of the Saturn part** — traditions differ. The plan picks whichever
   reproduces `Maandi 18 Li 07'` and records the choice in a code comment + the spec addendum.
2. **`swe_rise_trans` flag set** — disc centre / true altitude / refraction all shift
   sunrise by tens of seconds. Phase 1 tunes to the export's `05:56:39` before anything
   downstream is built on it.
3. **HL elapsed-time reference for a night birth** — prior sunrise vs the day's sunrise
   changes HL by a sign or more. Locked in Phase 4 against `23 Pi 55'`.
4. **`CK_KeyDetails_NonGrahaNulls` and future graha analytics** — if a later feature adds a
   new graha-only column, remember to add it to this CHECK. Noted here and in the migration
   comment.
5. **Row-count growth** — 21 charts × (10 grahas + Asc + 13 special points) ≈ 504 KeyDetail
   rows per person vs ~231 today. Fine at 5 people; no index change needed
   (`IX_Chart_KeyDetails_ChartResultId` already covers the read path).
6. **`PlanetChip` dependency** — `CombinedD1D9Grid` wants the Spec 2 `PlanetChip`/`--planet-*`
   tokens. Phase 6 either lands a minimal `PlanetChip` here or defers the combined grid's
   colour polish to Spec 2 Phase 1. Recommend: land `--planet-*` tokens + a minimal
   `PlanetChip` in this spec's Phase 6, Spec 2 builds on them.

---

## Approaches considered

- **Extend the chart-fact pipeline (chosen).** Special points ride the existing
  `ChartAnalysisInput → VargaChartComputer → ChartAnalyzer → tbl_Chart_KeyDetails` path with
  a `PointKind` discriminator; Chara Karaka is one column. Zero new tables, the whole varga
  engine and every downstream reader (grid, positions table, `vw_Chart_Consolidated`) get
  the new rows for free. Cost: a CHECK constraint to keep non-graha rows honest.
- **Separate `tbl_Chart_SpecialPoint` + `tbl_Chart_CharaKaraka`.** Cleaner type separation,
  but every UI surface and the consolidated view needs a second/third join, and the varga
  transformation logic would be duplicated or refactored out. More plumbing for a private
  2–3 user tool.
- **Compute on read (no storage).** Keeps the schema still, but breaks the project's
  "everything computed is stored in a typed column" invariant (spec
  `2026-08-31-divisional-chart-completion-design.md` §2) that the Python comparison layer
  depends on, and re-does sunrise math on every page load.

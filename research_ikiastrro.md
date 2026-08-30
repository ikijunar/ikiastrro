# research_ikiastrro.md — topic research & coverage master

Master index for **research spikes** on ikiastrro: what a classical technique is, whether
the raw facts it needs are already captured, and where the gaps are. Each topic gets a
section here plus, where a picture helps, a **D2 diagram** under `docs/research/`.

**Naming convention**

| Kind | Location | Pattern | Example |
|---|---|---|---|
| This master index | repo root | `research_ikiastrro.md` | — |
| Diagram source (D2 language) | `docs/research/` | `<topic-kebab>.d2` | `docs/research/planetary-roles.d2` |
| Rendered diagram (optional) | `docs/research/` | `<topic-kebab>.svg` | `d2 planetary-roles.d2 planetary-roles.svg` |
| Deep-dive design (if a topic graduates to a build) | `docs/` | `<topic-kebab>.md` | `docs/house-lagna-significations.md` |

> `d2` CLI is **not installed** on this machine — the `.d2` files are the deliverable; render
> later with `d2` or paste into <https://play.d2lang.com>.

---

## Topic 1 — Planetary roles & Avastha states (2026-08-31)

**Question:** are the raw facts for the five planetary *roles* and the five *avastha* systems
already captured, and what's missing?

Diagrams:
- [`docs/research/planetary-roles.d2`](docs/research/planetary-roles.d2) — the 5 roles, their inputs, coverage
- [`docs/research/planetary-avasthas.d2`](docs/research/planetary-avasthas.d2) — the 5 avastha systems, classical inputs → stored columns, states, status
- [`docs/research/karaka-avastha-linkage.d2`](docs/research/karaka-avastha-linkage.d2) — how roles + avasthas combine at the judgment layer

### 1a. Planetary roles — coverage

| Role | Computed? | Stored? | Where / engine | Inputs present? | Verdict |
|---|---|---|---|---|---|
| **House Lord** | ✅ | ✅ | `tbl_Chart_HouseLords` (house 1-12 per chart type: `HouseSign`, `LordPlanet`, `LordPlacedInHouseFrom{Lagna,Sun,Moon}`, `LordPlacedInSign`, `LordDignityStatus`); `vw_Chart_Consolidated.RulesHouseNumbers`; `ClassicalDignity.GetSignLord` | ✅ | **FULLY CAPTURED** |
| **Nakshatra Lord** | ✅ | ✅ | `tbl_Chart_KeyDetails.NakshatraLordPlanet` (+ `NakshatraSubLordPlanet`, KP L2) — every chart type; `AstroMath.GetNakshatraLord` / `GetNakshatraSubLord`; ref `tbl_Nakshatras` (27), `tbl_NakshatraSubLords` (243) | ✅ | **FULLY CAPTURED** |
| **Functional Lordship** | ✅ | ❌ | `Core/Calculators/LagnaFunctionalNature.For(lagnaSign, planet)` → `FunctionalNatureResult { Nature (Benefic/Malefic/Neutral/Yogakaraka), RuledHouses, IsMaraka, KendradhipatiDosha, Rationale }`. Web D1 planet table only. CLI `verify-functional-nature`. **No persisted column** — the Raman mirror `tbl_Dim_LagnaFunctionalNature` was removed 2026-08-31. | ✅ | **COMPUTED, NOT STORED** |
| **Naisargika Karaka** | partial | ❌ | Hardcoded subset in `Core/LifeAreas/LifeAreaMap.cs` (`Karakas` per life-area). Migration 030 (`tbl_Dim_PlanetHouseKaraka` / `tbl_Dim_PlanetSignification`) is **designed but not applied** (`docs/house-lagna-significations.md`). Sapta (7) vs Ashta (8) convention undecided. | n/a (reference data) | **PARTIAL — no data table** |
| **Chara Karaka** | ❌ | ❌ | Not built. Jaimini 8-fold (AK/AmK/BK/MK/PK/GK/DK + variant 8th), rank planets by degrees-within-sign (Rahu reversed). Input ready: `DegreesInSignDecimal`. `key_comp_astro.md` Tier-1 #3. Feeds Karakamsa + Jaimini rasi dashas. | ✅ (input only) | **MISSING** |

### 1b. Avastha states — coverage

**No avastha is computed or stored today** (`grep -ri avastha src/` → one doc-comment only).
`jyotishganit/components/states.py` is an empty stub; the vendored PyJHora copy has no
avastha module. Source for the formulae: **BPHS** (public-domain) + **B.V. Raman, *Graha and
Bhava Balas*** (not in repo — same cited-edition discipline as the descriptive nakshatra fields).

| System | States | Classical inputs | Mapped to stored fact? | Verdict |
|---|---|---|---|---|
| **Baaladi** (infancy) | 5: Baala / Kumara / Yuva / Vriddha / Mrita | degree-in-sign, odd/even sign | ✅ `DegreesInSignDecimal` (D1) + `Sign` | **INPUTS READY** — pure one-line fn, D1 only |
| **Jagradadi** (waking) | 3: Jagrat / Swapna / Sushupti | dignity (own/exalted → awake; friend/neutral → dream; enemy/debil → sleep) | ✅ `DignityStatus` | **INPUTS READY** — pure map; works for every chart type |
| **Deeptadi** (mood) | 9: Deepta / Swastha / Mudita / Shanta / Deena / Vikala / Peedita / Khala / Garvita | dignity + combustion + benefic/malefic association, precedence-ordered | ⚠️ `DignityStatus`, `IsCombust`, `AspectingPlanets`, `tbl_Chart_Conjunctions` present; **needs a natural benefic/malefic classifier**; textual variants | **MOSTLY READY** |
| **Lajjitadi** (dignity/shame) | 6: Lajjita / Garvita / Kshudhita / Trishita / Mudita / Kshobhita | 5th-house placement, exalt/debil, conjunction with Sun/Mars/Saturn/Rahu/Ketu vs Jupiter/Mercury, malefic/benefic aspect, watery sign | ⚠️ `HouseNumberFromLagna`, `DignityStatus`, `tbl_Chart_Conjunctions`, `AspectingPlanets` present; needs the same classifier + sign-element | **MOSTLY READY** |
| **Lajjitadi relationships** | — | the planet-to-planet associations the 6 states key off (conjunct Jupiter → Mudita, Saturn → Kshudhita, Sun → Kshobhita, node → Lajjita, …) | ✅ raw conjunctions/aspects stored per chart type; ❌ the classification (benefic / malefic / node / luminary) and the Lajjitadi mapping | **RAW FACTS STORED, interpretation missing** |
| **Sayanadi** (posture) — *[future]* | 12: Sayana … Nidra (+ a Cheshta/Drishti sub-state) | birth-time (janma ghatis) + nakshatra + lagna + planet-number formula; Cheshta partly from motion speed | ⚠️ `NirayanaLongitudeDegrees` / `NakshatraId` present; **janma ghatis not persisted per chart**; needs a planet-number map; `SpeedLongitudeDegPerDay` now stored (helps Cheshta). High textual variance → needs a cited edition. | **BLOCKED — future** |

### 1c. Key finding — how roles and avasthas connect

They **share no computation.** An avastha is a *state of a planet*; a karaka is a
*significator assignment*. They meet **only at the judgment layer**: BPHS / Raman (*How to
Judge a Horoscope*, p.14, p.20-21) — judge a bhava/topic from **house + its lord + its
occupants + its karaka**, and weigh **each** of those four by its **strength and avastha**.
e.g. Jupiter (Putra-karaka) in Baaladi *Mrita* + Deeptadi *Khala* → obstructed children.
The tightest textual link is **Sayanadi**, which has named predictive results *by avastha*
for a planet in its karaka role. **Jaimini Chara Karaka** connects via Karakamsa (the AK's
D9 sign); Parashari avasthas still apply to the Chara AK unchanged.

### 1d. Gaps, ranked

1. **No avastha layer at all** — biggest gap. Baaladi + Jagradadi are trivially buildable
   now (all inputs present, no ambiguity).
2. **No Naisargika Karaka table** — migration 030 designed, not applied; `LifeAreaMap`
   hardcodes a subset. Sapta-vs-Ashta convention needs a decision.
3. **No Chara Karaka** — nothing computed; blocks Karakamsa and all Jaimini dashas.
4. **Functional Lordship not persisted** — computed per request in the Web layer only; not
   queryable, not on `vw_Chart_Consolidated`.
5. **No natural benefic/malefic classifier as a reusable unit** — needed by Deeptadi,
   Lajjitadi, and (already) some functional-nature logic; today it's inline.
6. **Janma ghatis not persisted** — blocks Sayanadi and several `key_comp_astro.md`
   upagraha / special-lagna items.
7. **Degree-in-sign is D1-only** — Baaladi (and Vargottama, and Chara Karaka on vargas)
   need per-varga within-sign degree (the standing ICE-7.7 item).

### 1e. Suggested build order

1. **Baaladi + Jagradadi** — pure functions, all inputs present. New `tbl_Chart_PlanetAvastha`
   (one row per planet, D1), `BaaladiAvastha` / `JagradadiAvastha` in `Core/Calculators/`,
   wired through `ChartAnalyzer`, CLI `verify-avastha`.
2. **Naisargika Karaka table** — apply migration 030 (`tbl_Dim_PlanetHouseKaraka`), decide
   Sapta vs Ashta, switch `LifeAreaMap` to read it.
3. **Deeptadi + Lajjitadi** — extract a `NaturalBenefics` / node / luminary classifier as a
   shared `Core/Astro` unit; pin BPHS/Raman for the state precedence; extend
   `tbl_Chart_PlanetAvastha`.
4. **Chara Karaka** — `CharaKarakaCalculator` (Jaimini 8-fold), stored as its own table;
   unblocks Karakamsa + Jaimini rasi dashas.
5. **Sayanadi** *(future)* — persist janma ghatis on `tbl_ChartResults`, add a planet-number
   map, pin a cited edition.
6. **Synthesis** — `vw_Chart_KarakaCondition`: per topic, the Karaka planet joined to its
   avastha row + its House-Lord / Functional-Lordship context.

---

## References

- `docs/house-lagna-significations.md` — migration 030 design (Naisargika Karaka / Sthira
  Karaka roles), migration 031 record (removed 2026-08-31)
- `docs/bhava-coverage.md` — 8-point Bhava-analysis scorecard
- `key_comp_astro.md` — JHora feature-gap tiers (Chara Karaka = Tier-1 #3; Avasthas = Tier-3 #10)
- `ikiastrro_calculations.md` — dignity, combustion, nakshatra-lord, functional-nature engines
- `ARCHITECTURE.md` — `tbl_Chart_KeyDetails` / `tbl_Chart_HouseLords` / `tbl_Chart_Conjunctions` column detail
- BPHS ch. "Graha Avastha"; B.V. Raman, *Graha and Bhava Balas*; *How to Judge a Horoscope* Vol. 1

# Product Scope — Vedic Astrology Reference/Master Data Tables

**Status:** Implemented (migrations 019-021, applied to `ikiastrro` 2026-08-30) — points
1-3 live. See "Implementation record" at the end for what shipped vs. what's still open.
**Owner:** rammyps
**Created:** 2026-08-29
**Purpose:** Classical Vedic astrology reference (master) data, modeled as its own set of
tables — static domain knowledge (sign/planet attributes) as distinct from
`ikiastrro`'s existing chart-specific tables (`tbl_BirthDetails`, `tbl_ChartResults`,
`tbl_Chart_*`), which store *computed results for a specific person's chart*, not the
underlying classical rules those computations draw on.

**Open question:** should these tables live inside the `ikiastrro` database, or a
separate reference DB? Noting for now — decide once the full table list is gathered.

---

## 1. Sign (Rashi) attributes — requested name `tbl_planet_houses`

**Naming note:** what's described here is Rashi (sign — Aries, Taurus, …), a fixed
zodiacal property, not Bhava (house — position relative to Ascendant, already covered by
`tbl_Chart_HouseLords` per-chart). Recommend **`tbl_SignAttributes`** to avoid colliding
with the existing "house" meaning in this project; keeping the requested name as a fallback
if preferred. Column names below use the `type_house_*` naming as given — note this is
snake_case where the rest of the DB uses `tbl_PascalCase`/`PascalCase` columns
(`STANDARDS.md` §D) — flagging for consistency, not blocking on it.

One row per sign, 12 rows total.

| Column | Type | Description |
|---|---|---|
| `Id` | `TINYINT` (PK) | 1–12, Aries=1 through Pisces=12 |
| `SignName` | `VARCHAR(20)` | English name — Aries, Taurus, Gemini, Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces |
| `SignNameSanskrit` | `VARCHAR(20)` | Mesha, Vrishabha, Mithuna, Karka, Simha, Kanya, Tula, Vrishchika, Dhanu, Makara, Kumbha, Meena |
| `RulingPlanet` | `VARCHAR(20)` | Classical (Parashari, 7-graha) rulership — Mars: Aries, Scorpio · Venus: Taurus, Libra · Mercury: Gemini, Virgo · Moon: Cancer · Sun: Leo · Jupiter: Sagittarius, Pisces · Saturn: Capricorn, Aquarius |
| `RulingPlanetNature` | `VARCHAR(10)` | Natural benefic/malefic status of the ruling planet — Benefic: Jupiter, Venus · Malefic: Sun, Mars, Saturn · Conditional: Mercury (benefic unless afflicted), Moon (benefic when waxing/Shukla paksha) — store the conditional cases as `'Conditional'` with the rule in a notes field, don't force a boolean |
| `type_house_element` | `VARCHAR(10)` | Fire (Agni): Aries, Leo, Sagittarius · Earth (Prithvi): Taurus, Virgo, Capricorn · Air (Vayu): Gemini, Libra, Aquarius · Water (Jala): Cancer, Scorpio, Pisces |
| `type_house_keyattri` | `VARCHAR(15)` | Chara (Movable): Aries, Cancer, Libra, Capricorn · Sthira (Fixed): Taurus, Leo, Scorpio, Aquarius · Dwiswabhava (Dual): Gemini, Virgo, Sagittarius, Pisces |
| `Gender` | `VARCHAR(10)` | Purusha (Male) — odd signs · Stri (Female) — even signs |
| `Direction` | `VARCHAR(10)` | Cardinal direction by element group — Fire→East, Earth→South, Air→West, Water→North |
| `RisingType` | `VARCHAR(15)` | Sirshodaya (head-rising) / Prishthodaya (back-rising) / Ubhayodaya (both) — **note: classical texts disagree on exact sign-by-sign assignment here**, needs an explicit source citation before data entry, same way Rahu/Ketu dignity convention was pinned down by rammyps's explicit choice |
| `SymbolAnimalType` | `VARCHAR(20)` | Yoni classification — Chatushpada (quadruped), Dwipada (biped), Keeta (insect), Jalachara (aquatic) |
| `SymbolDescription` | `VARCHAR(50)` | Ram, Bull, Twins, Crab, Lion, Virgin, Scales, Scorpion, Archer/Centaur, Sea-goat, Water-bearer, Fish |
| `KalapurushaBodyPart` | `VARCHAR(30)` | Cosmic-man body-part correspondence — head, face, arms/shoulders, chest, heart, stomach, navel/pelvis, genitals, thighs, knees, calves/ankles, feet |
| `ExaltedPlanet` | `VARCHAR(20)` NULL | Planet exalted in this sign (e.g. Sun in Aries) |
| `ExaltedDegree` | `DECIMAL(5,2)` NULL | Exact exaltation degree (e.g. Sun at 10° Aries) |
| `DebilitatedPlanet` | `VARCHAR(20)` NULL | Planet debilitated in this sign |
| `DebilitatedDegree` | `DECIMAL(5,2)` NULL | Exact debilitation degree |
| `MooltrikonaPlanet` | `VARCHAR(20)` NULL | Planet whose Mooltrikona range falls in this sign |
| `MooltrikonaRangeStart` / `MooltrikonaRangeEnd` | `DECIMAL(5,2)` NULL | Degree range within the sign |

**Cross-check against existing code:** `ikiastrro` already encodes ruling-planet,
exaltation/debilitation, and Rahu/Ketu convention choices in `ClassicalDignity.cs` — if
these tables are meant to feed that engine (rather than just document it), the values here
need to match that file exactly, not be independently re-derived. Worth confirming before
data entry.

---

## 1a. Revision — `tbl_Planets` extended to a full 9-row planet master

Point 2 (below) needs Rahu on the same planet lookup as the 7 sign-ruling grahas, so
`tbl_Planets` (originally scoped in point 1 as just the 7-item ruling-planet dropdown) is
extended to be the **one shared planet master** for the whole domain, with a flag marking
which rows are valid for the "rules a sign" dropdown.

| Column | Type | Notes |
|---|---|---|
| `Id` | `TINYINT` PK | 1–9 |
| `PlanetName` | `VARCHAR(15)` UNIQUE | Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Rahu, Ketu |
| `PlanetNameSanskrit` | `VARCHAR(15)` | Surya, Chandra, Mangala, Budha, Guru, Shukra, Shani, Rahu, Ketu |
| `NaturalNature` | `VARCHAR(12)` | Benefic / Malefic / Conditional — Rahu, Ketu = Malefic |
| `ConditionalRule` | `VARCHAR(100)` NULL | as before, for Mercury/Moon |
| `RulesSign` | `BIT` | **1** for the 7 classical grahas, **0** for Rahu/Ketu — `tbl_SignAttributes.RulingPlanetId`'s dropdown is `WHERE RulesSign = 1` |

`tbl_SignAttributes` (point 1) is unchanged otherwise — its `RulingPlanetId` FK still only
ever resolves to the 7 `RulesSign = 1` rows.

---

## 2. Slow-planet transit history — `tbl_PlanetSignTransitEvents`

One row per **actual sign-boundary crossing** for Saturn, Jupiter, Rahu — and optionally
Mars (open decision, see below) — covering **1930-01-01 to 2060-12-31**. Ketu is never
stored here; derive it (see view below).

| Column | Type | Notes |
|---|---|---|
| `Id` | `INT IDENTITY` PK | |
| `PlanetId` | `TINYINT` FK → `tbl_Planets.Id` | Constrained to Saturn/Jupiter/Rahu(/Mars) — enforce with a `CHECK (PlanetId IN (...))` naming the specific IDs, since `tbl_Planets` itself has 9 rows |
| `EventDateTimeUtc` | `DATETIME2(0)` | Exact moment of crossing into `SignId`, computed from SwissEphNet (already integrated — no new ephemeris dependency) |
| `SignId` | `TINYINT` FK → `tbl_SignAttributes.Id` | The sign being **entered** at this event |
| `MotionDirection` | `VARCHAR(10)` CHECK | `Direct` / `Retrograde` — direction of motion *at the moment of crossing* |
| `IsReentry` | `BIT` | 1 if this crossing is a retrograde-then-forward re-entry into a sign the planet already visited on this same apparition (the "double-dip" case) |
| `Notes` | `VARCHAR(100)` NULL | e.g. "Re-entry after retrograde station" |

**Derived — Ketu's sign, no storage:** `vw_KetuSignTransitEvents` = same rows as Rahu's,
with `SignId` replaced by `((SignId + 5) % 12) + 1` (i.e. +6 signs / 180°) and `PlanetId`
swapped to Ketu's row. Matches the project's existing "Ketu = Rahu+180°, always derived"
convention — never computed or stored independently.

**Lookup helper (matches the `tvf_Chart_LifeWeeks` pattern already in the codebase):**
`tvf_PlanetSignAtDate(@PlanetId, @Date)` — returns the `SignId` most recently entered by
that planet before `@Date`. This is the actual query shape most consumers need ("what sign
was Saturn in on this date"), rather than scanning raw events.

**Rough row-count expectations (1930–2060, 130 years)** — useful for the Mars decision:
- Rahu: ~84 rows (mean node, smooth retrograde, no re-entries — cleanest dataset)
- Saturn: ~70–90 rows (some re-entries near sign boundaries)
- Jupiter: ~150–180 rows (more frequent re-entries — annual retrograde stations)
- Mars, if included: **~700–900 rows** — an order of magnitude more than the other three,
  since its ~45-day nominal dwell is short enough that even modest retrograde variance
  creates many more boundary crossings

**Data generation plan:** no external transit dataset needed — walk `SwissEphemerisProvider`
day-by-day (or coarser with bisection refinement at each detected sign change) across
1930–2060 for each included planet, detect sign-index changes and `PlanetSpeeds` sign for
`MotionDirection`, write one event row per crossing. This is a new CLI backfill mode,
following the existing `backfill-analytics`/`backfill-dasha` pattern.

**Verification plan:** spot-check generated dates against well-documented reference dates
(e.g. published Saturn Sade Sati start/end dates, Jupiter's well-known recent sign changes)
before trusting the full 130-year backfill — same discipline as the manual chart-verification
audits already used elsewhere in this project.

---

## 3. Nakshatras — `tbl_Nakshatras`, `tbl_NakshatraPadas`, and the KP sub-lord hierarchy

### 3a. `tbl_Nakshatras` (27 rows — Abhijit excluded, per rammyps's call to stay consistent
with the existing 27-lord Vimshottari cycle already in `AstroMath.GetNakshatraLord`)

Groups confirmed: **Core + Descriptive + Compatibility-matching**, **Paya removed**
(tradition-dependent, dropped per rammyps's call).

| Column | Type | Notes |
|---|---|---|
| `Id` | `TINYINT` PK | 1–27 |
| `NakshatraName` | `VARCHAR(20)` UNIQUE | Ashwini … Revati |
| `StartDegree` / `EndDegree` | `DECIMAL(6,3)` | absolute 0–360° sidereal, each exactly 13°20' wide |
| `RulingPlanetId` | `TINYINT` FK → `tbl_Planets.Id` | Vimshottari Dasha lord — **must match `AstroMath.GetNakshatraLord` exactly**, not re-derived |
| `SequenceNumber` | `TINYINT` | 1–27 |
| `RulingDeity` | `VARCHAR(30)` | Core |
| `Symbol` | `VARCHAR(40)` | Core |
| `Guna` | `VARCHAR(10)` CHECK | Satva / Rajas / Tamas — Descriptive |
| `Gana` | `VARCHAR(10)` CHECK | Deva / Manushya / Rakshasa — Compatibility |
| `YoniAnimal` | `VARCHAR(15)` | 14 animal pairings — Compatibility |
| `YoniGender` | `VARCHAR(6)` CHECK | Male / Female — Compatibility |
| `Nadi` | `VARCHAR(6)` CHECK | Vata / Pitta / Kapha — Compatibility |
| `Varna` | `VARCHAR(12)` CHECK | Brahmin / Kshatriya / Vaishya / Shudra — Compatibility |
| `Tatva` | `VARCHAR(10)` CHECK | Prithvi / Jal / Agni / Vayu / Akash — Compatibility |
| `Direction` | `VARCHAR(10)` | Descriptive |

### 3b. `tbl_NakshatraPadas` (108 rows — 27 × 4)

Confirmed sensible per the math check: a Pada (3°20') and a Navamsa division (1/9 of a 30°
Rasi) are the *same* 3°20' grid (108 = 27×4 = 12×9), so Pada↔Navamsa is a genuine 1:1
mapping, not an approximation.

| Column | Type | Notes |
|---|---|---|
| `Id` | `INT IDENTITY` PK | |
| `NakshatraId` | `TINYINT` FK → `tbl_Nakshatras.Id` | parent |
| `PadaNumber` | `TINYINT` CHECK 1–4 | |
| `StartDegree` / `EndDegree` | `DECIMAL(6,3)` | each exactly 3°20' wide |
| `RasiId` | `TINYINT` FK → `tbl_SignAttributes.Id` | D1 sign this Pada falls in — **not** always the same as the Nakshatra's own signs, since Nakshatras cross Rasi boundaries (e.g. Krittika spans Aries pada 4 + Taurus padas 1–3) |
| `NavamsaSignId` | `TINYINT` FK → `tbl_SignAttributes.Id` | D9 sign — compute via the classical movable/fixed/dual rule off `RasiId.type_house_keyattri`, **cross-check** against the raw 108-slot grid position when seeding (both methods must agree — built-in regression check) |

`NakshatraLordId` is deliberately **not** duplicated onto this table (it's a property of the
whole Nakshatra, identical across all 4 Padas — storing it here would be a denormalized copy
with sync risk). Exposed instead via:

`vw_NakshatraPadaDetails` — joins `tbl_NakshatraPadas` + `tbl_Nakshatras` + `tbl_Planets` +
`tbl_SignAttributes` (twice, aliased for Rasi and Navamsa) into one denormalized read view.
Matches the existing `vw_` convention (`vw_Chart_DashaTimeline`, etc.).

### 3c. KP sub-lord hierarchy — Sub, Pratyantar-Sub, Sookshma-Sub, Prana-Sub, Deha-Sub

**How these relate to Nakshatra and Sub Lord — explanation:**

This is the Krishnamurti Paddhati (KP) technique of recursively subdividing a Nakshatra's
13°20' span by the *same* 9-planet Vimshottari year-ratios (Ketu 7, Venus 20, Sun 6, Moon 10,
Mars 7, Rahu 18, Jupiter 16, Saturn 19, Mercury 17 — totaling 120) that the app's own
`VimshottariDashaCalculator` already uses for **time**. Here the identical ratios are applied
to **space** (degrees) instead:

- **Nakshatra Lord** (already have it) — which of the 9 planets owns this 13°20' span.
- **Sub Lord** — the 13°20' Nakshatra span is itself divided into 9 unequal parts,
  proportional to each planet's year-count out of 120, with the sequence *starting from the
  Nakshatra's own lord* and cycling through the standard Vimshottari order. So Ketu's sub
  within a Ketu-lorded Nakshatra is only 13°20'×(7/120) wide, while Venus's sub is
  13°20'×(20/120) — same asymmetric ratios as Dasha years, now widths.
- **Pratyantar-Sub** *(read as "Parti-sub" — flagging this interpretation, correct me if a
  different term was meant)*, **Sookshma-Sub**, **Prana-Sub**, **Deha-Sub** — each level
  recursively repeats the exact same operation one more time: take the previous level's
  (unequal) span, divide it into 9 parts by the same 120-year ratio, starting from *that
  span's own lord*. This mirrors the Mahadasha→Antardasha→Pratyantardasha→Sookshma→Prana→Deha
  naming your Dasha engine already uses — same algorithm, same names, but resolving a **fixed
  zodiacal degree** (a planet's or a house cusp's position) rather than a **date**.

**Important distinction to flag:** this is a *different* engine from
`VimshottariDashaCalculator`, not an extension of it — that calculator produces **time**
periods across a person's life; this produces **space** subdivisions of the zodiac itself,
evaluated for whichever specific degree you're asking about (a planet's longitude, or a house
cusp, in KP's cuspal/significator analysis). They share the ratio table, not the algorithm's
output type.

**Suggestion — storage is impractical past level 2, use a function instead:**

Each level multiplies the row count by 9 if fully enumerated as static rows:

| Level | Rows if stored |
|---|---|
| 1 — Nakshatra | 27 |
| 2 — Sub | 27 × 9 = 243 |
| 3 — Pratyantar-Sub | 243 × 9 = 2,187 |
| 4 — Sookshma-Sub | 19,683 |
| 5 — Prana-Sub | 177,147 |
| 6 — Deha-Sub | 1,594,323 |

Levels 1–2 are a reasonable, genuinely useful static reference table (`tbl_NakshatraSubLords`,
243 rows — real KP practitioners look these up directly). Levels 3–6 are **not** — by Deha-Sub
the width is a tiny fraction of an arc-second, and pre-enumerating 1.5M+ rows for a value
that's only ever needed for a *specific* chart's planets/cusps (≈20 points per chart) is pure
waste. **Recommend:** store only `tbl_NakshatraSubLords` (levels 1–2), and compute levels 3–6
on demand via a table-valued function, `fn_ResolveDegreeSubChain(@Degree)`, that recursively
narrows the range 4 more times using the same ratio logic — called only when a specific
chart's specific points need KP analysis, never pre-populated for the whole zodiac.

| Table/Object | Rows/Shape | Notes |
|---|---|---|
| `tbl_NakshatraSubLords` | 243 rows | `Id`, `NakshatraId` FK, `SubSequenceNumber` (1–9), `SubLordId` FK → `tbl_Planets.Id` (**all 9** planets participate here, not just the 7 sign-rulers — this is why `tbl_Planets` needed the Rahu/Ketu rows added in 1a), `StartDegree`/`EndDegree` `DECIMAL(9,6)` (finer precision than the whole-Nakshatra/Pada tables, since sub-widths run much smaller) |
| `fn_ResolveDegreeSubChain(@Degree)` | table-valued function | Returns one row: `NakshatraLordId`, `SubLordId`, `PratyantarSubLordId`, `SookshmaSubLordId`, `PranaSubLordId`, `DehaSubLordId` — computed live, not stored |

### 1b. Revision — `tbl_Planets` gains `VimshottariYears`

Both the existing Dasha engine and this new Sub-Lord hierarchy need the same 9-planet
120-year ratio table. Rather than hardcoding it a second time (drift risk), add one column to
`tbl_Planets` (from point 1a):

| Column | Type | Notes |
|---|---|---|
| `VimshottariYears` | `TINYINT` | Ketu 7, Venus 20, Sun 6, Moon 10, Mars 7, Rahu 18, Jupiter 16, Saturn 19, Mercury 17 — **must match `VimshottariDashaCalculator`'s existing values exactly** |

---

## Points still to be gathered

Points 1–3 provided so far. Add further points here as they come in — each becomes its own
numbered table section above, following the same shape.

---

## Open decisions

**Resolved:**
- Table names: `tbl_SignAttributes`, `tbl_Planets`, `tbl_Nakshatras`, `tbl_NakshatraPadas`,
  `tbl_NakshatraSubLords` (recommended names adopted; literal `tbl_planet_houses` dropped in
  favor of `tbl_SignAttributes` unless rammyps objects before implementation).
- Nakshatra count: 27, Abhijit excluded — stays consistent with the existing Dasha lord cycle.
- `tbl_Nakshatras` groups: Core + Descriptive + Compatibility-matching; Paya removed.
- KP sub-lord levels 3–6: computed via `fn_ResolveDegreeSubChain`, not stored as rows.

**Still open:**
4. Point 2: include Mars in `tbl_PlanetSignTransitEvents` or not — **decided for now: excluded**
   (Saturn/Jupiter/Rahu only, per the original scope). Adding it later is an additive change
   (new rows + relaxing the `CK_PlanetSignTransitEvents_PlanetId` check constraint).

---

## Implementation record (2026-08-30)

Migrations `019`-`021` applied to `ikiastrro` (`ikiastrro\db\`). Resolved on
the way to implementation:

- **Column casing:** kept `tbl_PascalCase` throughout, except `type_house_element` /
  `type_house_keyattri` (literal snake_case as originally requested).
- **Database:** same `ikiastrro` DB, alongside the existing `tbl_Chart_*` tables.
- **Documentation vs. engine-feeding:** cross-checked live against the actual C# source before
  seeding — `ZodiacName.cs`, `ClassicalDignity.cs`, `AstroMath.cs`
  (`NakshatraLordOrder`/`GetNavamsaSign`), `VimshottariDashaCalculator.cs`
  (`YearsByLord`) — rather than re-deriving from memory. One real finding from that check:
  `ClassicalDignity.cs` stores exaltation/debilitation as **sign only**, no exact degree — so
  `ExaltedDegree`/`DebilitatedDegree` in `tbl_SignAttributes` are supplementary classical (BPHS)
  reference values, not read by the engine today. Flagged in the migration's own comments.
- **"Parti-sub" confirmed as Pratyantar-Sub** — moot in the end, since only levels 1-2
  (Nakshatra Lord, Sub Lord → `tbl_NakshatraSubLords`) were built, per rammyps's explicit call
  to stop there. Levels 3-6 (`fn_ResolveDegreeSubChain`) were not built.

**A real bug caught before running against the live DB:** the first draft of
`tbl_SignAttributes`'s exaltation/debilitation seed data paired several signs with the wrong
debilitated planet (e.g. had Venus debilitated in Aries instead of Saturn). Re-deriving
systematically — a planet's debilitation sign is always the *opposite* of that same planet's
own exaltation sign — caught errors in 5 of 12 rows (Aries, Taurus, Cancer, Scorpio, Capricorn)
before they were seeded. Also corrected: `SymbolAnimalType` had Cancer as `Keeta` (insect)
instead of `Jalachara` (aquatic).

**Deliberately left NULL, not guessed:** `tbl_SignAttributes.RisingType` (all 12 rows) and
`tbl_Nakshatras.Guna`/`Gana`/`YoniAnimal`/`YoniGender`/`Nadi`/`Varna`/`Tatva`/`Direction` (all
27 rows) — these are legitimate single-answer classical tables, but reproducing all of them
correctly from memory without one cited source risked exactly the kind of subtle, silently-wrong
data this project has caught before (VedAstro's ayanamsha/Ketu bugs). Structure is ready;
populate via an `UPDATE` once a specific source is picked and cross-checked.

> **Sourcing status (reviewed 2026-08-30, `docs/scope-jhora-coverage.md`):** the vendored computation
> libraries under `_research/` do **not** close this gap — these are *descriptive /
> compatibility* attributes (Gana, Nadi, Yoni, Varna, RisingType…), not computed quantities,
> and `jyotishganit`'s `panchanga.py` carries only nakshatra *deities*, nothing else from this
> list. These fields still need a named classical reference (BPHS / a Nakshatra compendium),
> not a code port. **This is a different blocker from the one on Ashtakavarga/Shadbala** —
> those *are* now unblocked: `_research/jyotishganit/.../components/ashtakavarga.py` and
> `.../strengths.py` (both MIT, test-covered) supply the previously-missing BPHS contribution
> and strength tables and can be ported with attribution. Avastha (Baladi/Deeptadi/…) tables
> remain PyJHora-only (jyotishganit ships an empty `states.py` stub).

**Verified post-apply:** row counts (9/12/27/108/243) match expected; all 27
`tbl_NakshatraSubLords` groups sum to exactly 13.333333° per nakshatra; `vw_NakshatraPadaDetails`
spot-checked (Ashwini Pada 1 → Aries Navamsa, matching `AstroMath.GetNavamsaSign`'s own worked
example); `VimshottariYears` sums to 120; `tvf_PlanetSignAtDate` and `vw_KetuSignTransitEvents`
exist and are queryable.

**Not yet done — next steps:**
1. ~~`tbl_PlanetSignTransitEvents` has zero rows~~ — **done 2026-08-30**, see below.
2. Populate the intentionally-NULL descriptive/compatibility fields once a **classical
   reference** (BPHS / Nakshatra compendium) is picked — the `_research/` code libraries don't
   carry this data (see "Sourcing status" note above).
3. Decide whether/when the app's calculation engine should actually *read* these tables
   (replacing the hardcoded C# lookups) versus leaving them as a parallel, cross-checked
   reference layer — not decided either way yet.
4. Ashtakavarga + Shadbala reference tables — no longer source-blocked (jyotishganit, MIT).
   Tracked in `docs/scope-jhora-coverage.md` (Tier 3), not in this doc's table scope.

---

## Implementation record, part 2 (2026-08-30, same day) — transit backfill CLI

Built `PlanetTransitEventFinder` (`Ikiastrro.Core/Transits/`) — walks `SwissEphemerisProvider`
day-by-day (safe at daily granularity specifically because Saturn/Jupiter/Rahu are slow enough
that none can cross a 30° sign boundary within one day), bisects each detected crossing down to
within 1 minute, and tags `IsReentry` by checking whether a planet's sign 2-events-back matches
(the retrograde "double-dip" signature). Two new CLI modes:

- **`precheck-planet-transits`** — walks a recent ~10-year window only, prints results, no DB
  writes. Run first and cross-checked against two independently published Lahiri/K.P.-sidereal
  sources before trusting anything wider: Saturn→Capricorn (mine: 2020-01-24; published: Jan 24,
  2020 ✓), Saturn→Aquarius (mine: 2023-01-17; published: Jan 17, 2023 ✓), Jupiter→Taurus (mine:
  2024-05-01; published, K.P. ayanamsha: May 1, 2024 ✓ same date, ~11hr time-of-day difference
  explained by the small Lahiri/K.P. ayanamsha offset, not a bug). Deliberately did **not**
  hardcode "known good" dates into the precheck tool itself — tropical (Western) transit dates
  run ~23-24 days ahead of sidereal ones at the current ayanamsha, so a wrong hardcoded reference
  risked comparing against the wrong zodiac system entirely; print-and-manually-cross-check was
  the safer verification step.
- **`backfill-planet-transits`** — the real 1930-2060 run. Completed in 27 seconds: **Saturn 109
  rows (55 re-entries), Jupiter 229 rows (96 re-entries), Rahu 84 rows (0 re-entries)**. Rahu's
  zero re-entries is itself a correctness signal — the mean lunar node moves smoothly retrograde
  with no stations, so it structurally can't double-dip, exactly as expected.

**Further cross-check post-load:** `tvf_PlanetSignAtDate(Saturn, '1981-04-22')` returns Virgo,
matching this project's own earlier hand-verified data (Ramakrishnan's chart audit, same era,
recorded "Saturn retrograde in Virgo").

Code: `Ikiastrro.Core/Transits/PlanetTransitEventFinder.cs`,
`Ikiastrro.Data/PlanetSignTransitEventsRepository.cs`, two new modes in
`Ikiastrro.Cli/Program.cs`. Idempotent per-planet (checks `CountByPlanet` before inserting),
so re-running is safe; not yet de-duplicating if forcibly re-run after already populating (noted
in the CLI's own output if that happens).

**Still not done:** Mars was not added (excluded per the original point-2 scope); no Blazor Web
UI wiring for transit data yet (CLI/DB only, matching how retrograde/combustion shipped CLI-first
too).

---

## Implementation record, part 3 (2026-08-30, same day) — Nakshatra Lord on child tables

`tbl_Nakshatras.RulingPlanetId` already existed; added the same value onto
`tbl_NakshatraPadas` and `tbl_NakshatraSubLords` as a **computed column** (migration `022`,
`RulingPlanetId AS (dbo.fn_GetNakshatraRulingPlanetId(NakshatraId))`) rather than a physically
stored duplicate — selectable directly with no JOIN, but always reads live off `tbl_Nakshatras`,
so it can never drift out of sync (verified: 0 mismatches across all 108 Pada rows and all 243
Sub-Lord rows). On `tbl_NakshatraSubLords` this sits alongside the existing `SubLordId`, so a KP
query gets Nakshatra Lord and Sub Lord in one row without a join — spot-checked on Ashwini (all 9
rows correctly show Ketu as `RulingPlanetId`, `SubLordId` cycling Ketu→Venus→Sun→...→Mercury).

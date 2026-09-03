# Reference — Reading the D60 (Shashtiamsa) Chart

**JHora grid label:** `Shashtiamsa / D-60 (Trd)`. **Division factor:** N = 60 (each sign
→ sixty 0°30′ parts). **Strength groups:** Dashavarga · Shodashavarga. In Parashara's
Vimsopaka weighting the D60 carries the **single highest share** of any varga — it is the
tie-breaker of the whole horoscope.

Read [`reference-chart-reading-method.md`](reference-chart-reading-method.md) first.
**As of:** 2026-09-01. **Status:** living.

---

## 1. Significance and weight

The D60 is the chart of **accumulated past-life karma** — the sum and final verdict of
the horoscope. Its uses:

- **The deciding vote.** For any life area, after D1 promises and the subject varga
  grades, the D60 says whether the karma is actually there to be collected. Parashara
  gives it more Vimsopaka weight than D1's own share within the Shodashavarga group, which
  is why the tradition calls it the chart that "overrides all."
- **The subtle cause.** It shows *why* an event happens — the karmic backstory behind a
  gain, a loss, an illness, a relationship — where D1 only shows *that* it happens.
- **Birth-time rectification and twin separation.** At 0°30′ per division the D60 Lagna
  and every planet's D60 sign change with roughly **one minute of clock time**. A D60
  read on an unrectified time is close to meaningless; conversely, matching known life
  events to the D60 is a standard rectification technique.
- **The shashtiamsha "deity" layer.** Each of the 60 parts carries a name with a benefic
  or malefic nature (§4); the part a planet falls in adds a karmic quality on top of its
  sign dignity.

## 2. JHora derivation

- **Sign rule (`ShashtyamsaD60`, Traditional Parashari — `LinearVargaSignRule(60, 1)`):**
  take the degrees traversed within the sign, multiply by 2, take the integer part, add 1
  → the shashtiamsha number **1–60**. Count that many signs **from the sign itself**
  (stride 1), wrapping mod 12, to get the D60 sign. Same rule for odd and even signs in
  this (Parashari/`chart_method=1`) convention.
  - Worked: a planet at **8°12′ Aries**. 8.205° × 2 = 16.41 → floor 16, +1 = **17th
    shashtiamsha**. 17 signs from Aries = (17 − 1) mod 12 = 4 → **Leo**. (Matches the
    export: `Sun 8 Ar 12' … Navamsa Ge`, and its D60 sign in the grid.)
- **The name table.** The shashtiamsha *number* also indexes a 60-name list
  (Ghora, Rakshasa, Deva, …) whose benefic/malefic value is read as a karmic tag. The
  odd-sign list runs 1→60; the even-sign list is the **reverse** (part 1 of an even sign
  takes name 60, "Ghora"-end). The full list is in §4.
- **Method ambiguity (tracked).** Two things are debated for D60: (a) the count basis —
  "from the sign itself" (used here) vs. variants — and (b) whether the even-sign name
  list reverses (used here) or not. `reference-calculations.md` §2 and
  `scope-jhora-coverage.md` §2 flag D60 as method-ambiguous; the engine uses the one
  documented rule above and is verified cell-by-cell against the JHora `(Trd)` grid.
  Always state which rule a reading used.
- **Engine:** shared `VargaCalculator` + `VargaScheme` row.
  `VargaLongitudeDegrees = Normalize(realLon × 60)`; D60 degree-in-sign = that mod 30.

## 3. Inputs needed to read it

- **A rectified birth time above all** — plus a note of how confident that rectification
  is. Everything downstream inherits that confidence.
- D1 fully read, and the subject varga for whatever question you carry.
- Each planet's D60 sign (from the `D-60 (Trd)` grid) and, ideally, D60 degree-in-sign.
- Each planet's **shashtiamsha number** and its **deity name + nature** (§4).
- The D60 Lagna and its lord.
- Vargottama status D1 ↔ D60 for the Lagna and each planet.
- Vimsopaka Shodasa-varga scores from the export (D60 is a member) for a cross-check.

## 4. The 60 shashtiamsha names and their nature

Number → name → nature. Benefic (B) parts support the significations of a planet placed
there; malefic (M) parts burden them. (Classical set per BPHS; used as a qualitative tag,
not a separate calculation. For **odd** signs read 1→60 in order; for **even** signs the
order **reverses**, so part 1 of an even sign = name 60.)

| # | Name | | # | Name | | # | Name | | # | Name |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Ghora — M | | 16 | Sarpa — M | | 31 | Kaala — M | | 46 | Paasha — M |
| 2 | Rakshasa — M | | 17 | Amrita — B | | 32 | Davaagni — M | | 47 | Danda-udyata — M |
| 3 | Deva — B | | 18 | Chandra (Indu) — B | | 33 | Ghora — M | | 48 | Bhaya — M |
| 4 | Kubera — B | | 19 | Mridu — B | | 34 | Yama — M | | 49 | Yaksha — B |
| 5 | Yaksha — B | | 20 | Komala — B | | 35 | Ganda-antaka (Kantaka) — M | | 50 | Kinnara — B |
| 6 | Kinnara — B | | 21 | Heramba — B | | 36 | Sudha — B | | 51 | Bhrashta — M |
| 7 | Bhrashta — M | | 22 | Brahma — B | | 37 | Amrita — B | | 52 | Kulaghna — M |
| 8 | Kulaghna — M | | 23 | Vishnu — B | | 38 | Poorna-Chandra — B | | 53 | Mukhya — B |
| 9 | Garala (Visha) — M | | 24 | Maheshwara — B | | 39 | Visha-daghdha — M | | 54 | Vamsha-kshaya — M |
| 10 | Vahni (Agni) — M | | 25 | Deva — B | | 40 | Kulanaasha — M | | 55 | Utpaata — M |
| 11 | Maaya — M | | 26 | Ardra — M | | 41 | Vamsha-kshaya — M | | 56 | Kaala — M |
| 12 | Purishaka — M | | 27 | Kalinaasha — M | | 42 | Utpaata — M | | 57 | Saumya — B |
| 13 | Apampati (Varuna) — B | | 28 | Kshiteesha (Kshudra) — M | | 43 | Kaala — M | | 58 | Komala — B |
| 14 | Marut (Vaayu) — B | | 29 | Amrita — B | | 44 | Saumya — B | | 59 | Sheetala — B |
| 15 | Kaala — M | | 30 | Payodhi — B | | 45 | Komala — B | | 60 | Karaala-damshtra (Ghora) — M |

> Sources vary on a handful of names/natures (notably 26–28 and the 40s). The engine, if
> it surfaces this layer, should carry `MethodSource` the same way the varga rules do;
> until then treat the nature column as indicative and lean on sign dignity as primary.

Reading it: a functional benefic for the Lagna sitting in a **benefic** shashtiamsha, in
good D60 sign dignity, is karma "paid up" for its significations. The same planet in a
**malefic** shashtiamsha (Ghora, Rakshasa, Sarpa, Kaala, Yama…) carries an unpaid debt in
that area even if the D1 looks fine.

## 5. Step-by-step reading

1. **Confidence gate.** State how well-rectified the birth time is. If it is not solid,
   read the D60 for *themes only* and flag every conclusion as provisional.
2. **D60 Lagna and its lord** — the karmic "self." Its sign, its lord's D60 house and
   dignity, and the shashtiamsha name of the Lagna degree set the base karmic tone.
3. **Vargottama sweep D1 ↔ D60** — any planet or the Lagna in the same sign in both is a
   karmically "sealed" significator: whatever it promises in D1 is confirmed as earned.
4. **Per planet, four reads:** D60 house (from D60 Lagna) · D60 sign dignity ·
   shashtiamsha name/nature · vargottama or not. Combine into a net "karma supports /
   karma burdens" for that planet's significations.
5. **Carry your question in.** For the life area you are reading (from D1 + its subject
   varga), look at: the D60 house for that area, its D60 lord, the natural karaka's D60
   condition, and — where the tradition uses one — the chara karaka's D60 condition.
   Agreement with the subject varga = firm; contradiction with a solid birth time = the
   D60 wins and you downgrade; contradiction with a shaky birth time = caution only.
6. **Yogas in D60** — a Raja/Dhana-type link formed *only* in D60 points to a result that
   arrives "out of nowhere" karmically; one *broken* in D60 explains a D1 promise that
   never lands.
7. **Overlays** — `AL` in D60 = the karmic reputation; `Md`/`Gk` mark the houses where
   past-life debt bites.
8. **Timing** — run the D1 Vimshottari dasha; when a period lord is strong in D1 but
   afflicted in D60 (bad sign + malefic shashtiamsha), expect the period to under-deliver
   relative to its D1 look, and vice versa.
9. **Final synthesis** — write the verdict as: *D1 promise → subject-varga grade → D60
   vote*. The D60 vote is decisive only under a confident birth time.

## 6. Significator checklist

There is no special house scheme — read the D60's twelve houses with their **standard**
significations (see `reference-chart-d1-rasi.md` §5), but interpreted as *the karmic
account* for each matter:

| D60 house | Karmic account for |
|---|---|
| 1 | the body and self carried from the past; overall karmic vitality |
| 2 | family and wealth karma; what is owed/earned through speech and resources |
| 4 | mother, home, inner peace — emotional karma |
| 5 | children and merit (purva-punya) — the past-life good/bad balance itself |
| 6 | disease, debt, enemies — karmic liabilities coming due |
| 7 | spouse and partnership karma |
| 8 | longevity, upheaval, hidden karma, inheritance of debt |
| 9 | father, dharma, fortune — the past-life dharmic credit |
| 10 | karma of action and status in the world |
| 11 | gains that are karmically owed to the person |
| 12 | loss, confinement, moksha — the closing of accounts |

Plus the **shashtiamsha name** of the Lagna and of each key planet (§4) as the qualitative
tag over the house reading.

## 7. Cross-varga confirmation

The D60 *is* the confirmation layer, so the flow always ends here:

- **Any topic:** D1 house/lord/karaka → subject varga (D9 spouse, D10 career, D7
  children, D4 property, D24 education, …) → **D60** house/lord/karaka + shashtiamsha
  nature.
- **Whole-chart strength:** compare the export's **Vimsopaka Shodasa-varga** column
  (which includes D60) against the by-eye D60 dignity — a planet high there but weak in
  the D60 grid alone is still broadly reliable; low there and weak in D60 is a genuine
  karmic deficit.
- **Rectification loop:** shift the birth time in ±1-minute steps, recompute the D60, and
  keep the time whose D60 best matches dated life events across several houses.

## 8. Common misreadings

- **Reading the D60 on an unrectified time.** The most common and most serious error —
  the Lagna and half the planets can be in the wrong sign. Gate on birth-time confidence.
- **Letting the D60 overturn a clear D1 + varga reading when the time is soft.** Only a
  confident birth time earns the D60 the deciding vote (method doc §2 step 11).
- **Using the deity name instead of the sign.** The shashtiamsha name is a qualitative
  tag *on top of* dignity, not a replacement for it. A planet in Ghora but exalted is not
  simply "bad."
- **Mixing methods.** "From the sign itself" vs. variant counts, and reversing vs. not
  reversing the even-sign name list, give different results. State the rule; the engine
  uses the `(Trd)` rule in §2.
- **Forgetting vargottama.** A D1↔D60 vargottama planet is one of the strongest
  statements in the whole horoscope and is easy to miss without an explicit sweep.

## 9. Worked lens — reference export (1_Ramakrishnan)

*What to inspect, not a prediction. Birth time here is the project's standard
verification time; treat the D60 read as illustrative.*

- **D60 Lagna = Taurus** (verified against the `D-60 (Trd)` grid): Lagna 0°38′50″ Aries →
  0.647° × 2 = 1.29 → part **2** → 2 signs from Aries = Taurus; Aries is odd → name #2 =
  **Rakshasa (M)** — a burdened karmic ascendant tag. Weigh it against the D60 Lagna
  lord (Venus) — Venus's own D60 sign and dignity carry the actual verdict.
- **Sun 8°12′ Aries → part 17 → D60 sign Leo** (matches the grid — Sun is in the Leo
  cell), name #17 = **Amrita (B)**: the father/authority karaka (PiK) sits in a benefic
  shashtiamsha and in its own sign — a "paid-up" karmic signature for Sun's matters.
  Contrast with a planet like the D1-debilitated Moon: track its D60 sign and name and
  expect the karmic cross-check to keep it the weak link.
- **Vargottama sweep:** step through each planet's D1 sign vs its `D-60` grid sign and
  mark matches — those are the sealed significators.
- **Carry the questions in:** career → D60 10th house + its lord + Sun/Saturn/AmK-Venus in
  D60, cross-checked with the D10 reading; marriage → D60 7th + Venus + DK-Mercury in D60,
  cross-checked with D9.
- **Corroborate with Vimsopaka Shodasa-varga** (includes D60): Mars 85%, Saturn ~63%,
  Sun ~68%, Moon ~49% — Moon (debilitated in D1) stays the weakest link on the karmic
  cross-check; Mars stays the strongest.

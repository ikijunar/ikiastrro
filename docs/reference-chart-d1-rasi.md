# Reference — Reading the D1 (Rasi) Chart

**JHora grid label:** `Rasi` (no method tag — it is the real sidereal chart).
**Division factor:** N = 1. **Strength groups:** Shadvarga · Saptavarga · Dashavarga ·
Shodashavarga (member of all four).

Read [`reference-chart-reading-method.md`](reference-chart-reading-method.md) first — the
universal loop, dignity/karaka tables, grid mechanics and abbreviations are not repeated
here. **As of:** 2026-09-01. **Status:** living.

---

## 1. Significance and weight

The D1 is the birth chart — the real ecliptic positions of the ascendant and nine grahas
in the sidereal zodiac. It is the **body of the horoscope**: physical existence,
personality, the actual events of the life, and the total field within which every other
varga is a magnified view of one room. Every divisional chart is read *against* D1, never
instead of it:

- D1 **promises** — it says whether a thing is on offer at all (a house occupied/aspected,
  its lord placed well, the karaka strong).
- The varga **delivers and details** — it says how much of the promise matures, in what
  form, and with what quality.

If D1 denies something outright (house, lord and karaka all wrecked, no yoga), a glowing
varga rarely rescues it. If D1 promises it, the varga decides the grade.

## 2. JHora derivation

- **Sign rule:** none — each planet's real sidereal longitude (Lahiri) placed directly.
  `VargaLongitudeDegrees` for D1 is just the real longitude.
- **Engine:** `D1RasiCalculator` (the one non-shared calculator). Longitudes from
  SwissEphNet, independently checked vs Prokerala/AstroSage to ~0.4–1.0 arcmin.
- **In the export:** the body table gives `Longitude / Nakshatra / Pada / Rasi /
  Navamsa` for `Lagna` + `Sun…Ketu`, each suffixed with its chara karaka
  (`Sun - PiK`, `Rahu - AK`). Retrograde shown as `(R)` in the table, `R` in the grid.
- **D1-only extras JHora prints:** Panchanga header (Tithi, Nitya Yoga, Karana,
  Nakshatra %, Hora Lord, Janma Ghatis, Sidereal Time, Ayanamsa), the four avastha
  columns (Age/Alertness/Mood/Activity), Ashtakavarga, Shadbala. These are read *on* D1;
  vargas do not get their own.

## 3. Inputs needed to read it

- Rectified birth data; Lahiri ayanamsa; Whole Sign houses.
- The body table: every planet's sign, degree-in-sign, nakshatra + pada, retrograde,
  combustion, chara karaka.
- Lagna sign and Lagna lord.
- Functional benefic/malefic list **for this Lagna**
  (`reference-house-lagna-significations.md`).
- Natural karakas per house; dignity tables.
- The D1 Vimshottari dasha sequence and current period.

## 4. Step-by-step reading

Specialises the universal loop (method doc §2):

1. **Lagna and Lagna lord.** Sign of the 1st = constitution, temperament, body. Place,
   dignity and aspects of the Lagna lord = the steering of the whole life. A Lagna lord
   in a kendra/trikona in dignity is the single best D1 signature.
2. **The luminaries.** Moon sign + nakshatra = mind, emotional baseline, and the *second
   ascendant* — re-read the whole chart from the Moon as Lagna. Sun sign + house = soul,
   authority, father, vitality.
3. **Benefics and malefics by house.** Natural benefics (Jupiter, Venus, unafflicted
   Mercury, waxing Moon) in kendras/trikonas support; natural malefics (Saturn, Mars,
   Sun, Rahu, Ketu, waning Moon) in 3/6/11 give drive, in kendras/trikonas (other than as
   yogakaraka) they pressure.
4. **House by house (bhava, bhava lord, bhava karaka).** For each of the 12: is the house
   occupied, by whom, benefic or malefic; where is its lord, in what dignity; where is
   its natural karaka. All three strong = the matter thrives. This is the 8-point bhava
   method scored in [`scope-bhava-coverage.md`](scope-bhava-coverage.md).
5. **Yogas.** Raja yogas (kendra lord + trikona lord linked), Dhana yogas (2/5/9/11 lord
   links), Pancha Mahapurusha (Ma/Me/Ju/Ve/Sa in own/exalted sign in a kendra),
   Gajakesari, Neechabhanga, Vipareeta, Kemadruma, Kala Sarpa. Note the yoga, the planets
   forming it, and whether a malefic or a debilitation cripples it.
6. **Afflictions.** Combustion, debilitation, enemy sign, hemmed between malefics
   (papakartari), aspected only by malefics, retrograde malefic on a trikona lord.
7. **Chara-karaka overlay.** AK = the soul's agenda; its house and sign colour the life's
   theme. AmK, DK, PK etc. are cross-checked in their vargas (method doc §4).
8. **Avasthas** (D1 only). Age (Baladi) scales a planet's potency — an "infant" or "old"
   planet under-delivers even in dignity. Mood (Deeptadi) and Alertness qualify how
   comfortably it acts.
9. **Strength.** Shadbala (rupas / %), Ashtakavarga bindus in each sign (transit
   strength), Vimsopaka across the varga groups. Cross-read against the by-eye dignity.
10. **Dasha.** Locate the running Maha/Antar lord; its house-lordship and condition set
    the tenor of the current years. Confirm each life area's timing against its own varga
    (method doc §2 step 10).

## 5. House-by-house significator checklist

| Bhava | Core topics | Natural karaka(s) | Also read in |
|---|---|---|---|
| 1 | body, self, vitality, temperament | Sun | — |
| 2 | wealth, family, speech, food | Jupiter | D2 |
| 3 | courage, siblings, effort, skill | Mars | D3 |
| 4 | mother, home, land, vehicles, heart | Moon, Mercury | D4, D12, D16 |
| 5 | children, intellect, purva-punya, mantra | Jupiter | D7, D20, D24 |
| 6 | disease, debt, enemies, service | Mars, Saturn | D6 |
| 7 | spouse, partnership, public life | Venus | D9 |
| 8 | longevity, upheaval, inheritance, occult | Saturn | D8, D30 |
| 9 | fortune, father, dharma, guru, higher learning | Jupiter, Sun | D9, D12, D24 |
| 10 | career, status, action, authority | Sun, Mercury, Jupiter, Saturn | D10 |
| 11 | gains, income, network, elder siblings | Jupiter | D11 |
| 12 | loss, expense, foreign, sleep, moksha | Saturn, Ketu | D20, D30 |

## 6. Cross-varga confirmation

D1 is the thing being confirmed, so the direction reverses: for any life area, form the
D1 verdict (house + lord + karaka + yoga), then open the subject varga to grade it, and
finally check **D60** as the overall karmic vote. Persistent agreement D1 ↔ subject varga
↔ D60 is a firm reading; disagreement with a shaky birth time is a caution.

## 7. Common misreadings

- **Reading houses from the Moon or a planet and forgetting the Lagna** — always anchor
  to the Lagna first, then add the Moon-Lagna and Karakamsa views.
- **Treating a yoga as a guarantee** — an uncancelled debilitation or a papakartari can
  hollow out a textbook Raja yoga.
- **Ignoring the dispositor** — a planet's result runs through the planet that owns its
  sign; a strong dispositor can carry a weak planet and vice versa.
- **Malefic ≠ bad** — Saturn/Mars in 3/6/11, or a yogakaraka for the Lagna, are engines
  of achievement.
- **Over-weighting a single varga against a clear D1** — see method doc §2 step 11.

## 8. Worked lens — reference export (1_Ramakrishnan)

*Descriptive of what to inspect, not a prediction.*

- **Lagna Aries**, Lagna lord **Mars in the 1st** (Aswini, own sign) — a fortified Lagna
  lord (though not vargottama: Mars is in Taurus in D9). Read the whole life as
  Mars-steered (initiative, independence, friction). Mars is chara **GK** (rival/conflict)
  — the same drive shows as contention.
- **Stellium in Aries 1st:** Sun (PiK), Mercury (DK), Venus (AmK), Mars (GK) — a heavily
  loaded self, with father, spouse, career and rivalry karakas all on the ascendant.
  Check combustion (Mercury, Venus close to Sun) and Baladi age for each before grading.
- **Moon in Scorpio 8th**, debilitated, Anuradha — the mind placed in the house of
  upheaval and depth; re-read from Scorpio Lagna and check Moon's dispositor Mars (back
  in the 1st from D1, i.e. 6th from the Moon).
- **Jupiter (R) and Saturn (R) in Virgo 6th** — two retrograde slow planets in the house
  of service/disease/conflict; Jupiter is MK, Saturn is BK.
- **Rahu Cancer 4th / Ketu Capricorn 10th**, Rahu is **AK** — the soul's axis on the
  home–career line; Karakamsa = Rahu's D9 sign (Libra).
- **Dasha at birth:** Saturn Maha (from Sep 1975); Saturn–Mercury at birth, Saturn–Ketu
  from 13 May 1981 — grade early life by Saturn (6th, retrograde, BK), then Mercury (1st,
  DK) and Ketu (10th).

Then take each life area into its varga: career → D10 with AmK Venus and Saturn; marriage
→ D9 with DK Mercury and Venus; and the whole into D60.

# Reference — Reading the D9 (Navamsa) Chart

**JHora grid label:** `Navamsa / D-9`. **Division factor:** N = 9 (each sign → nine
3°20′ parts). **Strength groups:** Shadvarga · Saptavarga · Dashavarga · Shodashavarga
(member of all four — the most heavily weighted varga after D1).

Read [`reference-chart-reading-method.md`](reference-chart-reading-method.md) first.
**As of:** 2026-09-01. **Status:** living.

---

## 1. Significance and weight

The D9 is the **fruit of the tree** — the chart of dharma, marriage and the inner
strength behind the outer life. It is read for:

- **Marriage and spouse** — its primary popular use: the 7th, its lord, Venus and the
  Dara Karaka in D9.
- **Dharma / the second half of life** — the path the person is meant to walk; classical
  texts say the D9 gains force as life proceeds and especially after marriage.
- **Underlying strength of every D1 planet** — a planet's D9 dignity tells you whether
  its D1 promise has substance. This is why D9 is checked for *every* topic, not only
  marriage: a D1 planet that runs a great yoga but sits debilitated in D9 tends to
  promise more than it delivers.
- **Vargottama** — a planet or Lagna in the same sign in D1 and D9 is exceptionally
  stable and strong in its significations.

Rule of thumb: **D1 shows the plan, D9 shows the resources.** A weak D1 planet that is
strong in D9 ("pushkara"/dignified) often outperforms its birth-chart look.

## 2. JHora derivation

- **Sign rule (`NavamsaD9`, Traditional Parashari):** from a planet's sign, count nine
  equal 3°20′ parts. The start sign of the count depends on the sign's element —
  movable signs count from themselves, fixed from the 9th, dual from the 5th — so the
  first navamsa of Aries = Aries, of Taurus = Capricorn, of Gemini = Libra, etc. The
  144 navamsas map 1:1 onto the 108 nakshatra-padas × … (the Pada ↔ Navamsa identity is
  encoded in `tbl_NakshatraPadas`).
- **Engine:** shared `VargaCalculator` + `VargaScheme` row; `AstroMath.GetNavamsaSign`.
- **Within-sign degree:** `VargaLongitudeDegrees = Normalize(realLon × 9)`; the true D9
  degree is that mod 30. (Historically a gap — see `reference-calculations.md` §"open".)
- **In the export:** the body table's `Navamsa` column gives each planet's D9 sign
  directly; the `D-9` grid places them with `As`, `AL`, `GL`, `HL` overlays.

## 3. Inputs needed to read it

- D1 read and its 7th-house verdict formed.
- Each planet's D9 sign (body-table `Navamsa` column) and, where available, D9
  degree-in-sign for vargottama-tightness and conjunction orbs.
- **Dara Karaka (DK)** from the D1 body table — the chara karaka for spouse.
- **Atma Karaka (AK)** — its D9 sign is the **Karakamsa**; read the Karakamsa Lagna for
  the soul's dharmic environment.
- Venus (natural karaka for spouse/marriage); Jupiter (for a woman's chart, and dharma);
  7th and 9th lords of D1.
- Upapada Lagna (UL) if computed — classical spouse-arudha (not in the base export grid).

## 4. Step-by-step reading

1. **D9 Lagna and its lord** — the "self" in the dharmic/marital field. Its sign and the
   lord's D9 placement/dignity set the base.
2. **Vargottama sweep** — mark every planet (and the Lagna) that holds its D1 sign in D9.
   These are the load-bearing points of the chart.
3. **Marriage core:** D9 7th house — occupants, D9 7th lord's placement and dignity,
   Venus in D9, DK in D9 and the sign 7th from DK ("Dara Karakamsa"). Benefics on/aspecting
   the D9 7th support; malefics (esp. Mars, Saturn, Rahu, Ketu, Sun) stress it — count
   how many and whether a benefic mitigates.
4. **Karakamsa line:** find AK's D9 sign. Read planets in and aspecting the Karakamsa and
   its 7th (spouse from the soul), 5th (children/mantra), 9th (dharma/guru),
   12th (moksha) — this is the Jaimini spiritual reading.
5. **Strength audit for other topics:** for whatever D1 question you are carrying, look
   up that significator's D9 dignity. Strong D1 + strong D9 = matures fully; strong D1 +
   weak D9 = discount it; weak D1 + strong D9 = quietly delivers.
6. **Yogas in D9** — Parivartana or kendra/trikona links between the D9 Lagna lord and
   the 7th/9th lords; Pancha Mahapurusha status can differ from D1.
7. **Overlays** — `AL` in D9 = how the marriage/dharma is perceived socially; `Md`/`Gk`
   in the D9 7th = friction in partnership.
8. **Timing** — marriage/dharma events are graded by the running D1 dasha lord's
   condition *in D9*, plus Venus/Jupiter/DK dashas and 7th-lord periods.

## 5. Significator checklist

| D9 house (from D9 Lagna) | Read for | Key significators |
|---|---|---|
| 1 | inner self, the person's own dharmic nature | D9 Lagna lord |
| 2 | family life after marriage, sustenance of the union | 2nd lord, Jupiter |
| 4 | domestic happiness, the marital home | Moon, 4th lord |
| 5 | children within the union, devotion, mantra-siddhi | Jupiter, 5th lord |
| 7 | **spouse, the marriage itself, partnership** | 7th lord, Venus, DK |
| 8 | in-laws, longevity of the marriage, upheavals | 8th lord, Saturn |
| 9 | dharma, fortune, guru, the second half of life | Jupiter, 9th lord, Sun |
| 12 | bed pleasures, renunciation, moksha | Venus (bed), Ketu (moksha) |

Also: **Karakamsa** (AK's D9 sign) and its 5th/7th/9th/12th for the Jaimini layer.

## 6. Cross-varga confirmation

- **Marriage:** D1 7th house + Venus → D9 (grade) → **D60** (final karmic vote). Add D2
  (family wealth) if the question is the spouse's contribution to prosperity.
- **Dharma / life-purpose:** D1 9th + Jupiter → D9 Karakamsa → D20 (spiritual practice)
  and D24 (learning/deeksha) → D60.
- **General strength of any planet:** its D9 dignity is the standard second opinion on
  its D1 condition, before you even open the topic varga.

## 7. Common misreadings

- **Reading D9 only for marriage** — its larger job is the strength check on every D1
  planet.
- **Calling a debilitated-in-D9 planet "cancelled"** — check neechabhanga in D9 the same
  way you would in D1 (dispositor in a kendra from Lagna/Moon, or the debilitation lord
  exalted).
- **Ignoring vargottama for the Lagna** — a vargottama Lagna steadies the entire life,
  not just the D9 topics.
- **Counting malefics on the D9 7th without weighing a benefic aspect** — one strong
  Jupiter aspect changes the verdict.
- **Treating Karakamsa as optional** — for dharma and spiritual questions it is the
  primary reading, not a footnote.

## 8. Worked lens — reference export (1_Ramakrishnan)

*What to inspect, not a prediction.*

- **D9 Lagna Aries**; D1 Lagna is also Aries → **vargottama Lagna** (steady life
  direction). D9 Lagna lord **Mars in Taurus** (D9 2nd) — grade Mars's D9 dignity
  (Taurus = neutral) against its powerful D1 (own sign, 1st).
- **Spouse core:** DK is **Mercury**, whose D9 sign is **Aries** (with the D9 Lagna, and
  with Saturn `SaR` and Ketu). Venus's D9 sign is **Cancer** (D9 4th). D9 7th house (from
  Aries = Libra) — check its occupants/lord and how many malefics touch it; note Mars
  (Taurus) aspects Libra by its 7th.
- **Karakamsa:** AK **Rahu**'s D9 sign is **Libra** → Karakamsa Lagna Libra. Read planets
  in/aspecting Libra and its 5th (Aquarius), 7th (Aries), 9th (Gemini), 12th (Virgo) for
  the dharmic/spiritual picture.
- **Strength second opinions:** Jupiter D9 = **Pisces** (own sign — strengthens the D1
  retrograde Virgo Jupiter); Saturn D9 = **Aries** (debilitated — discounts the D1 Virgo
  Saturn); Sun D9 = **Gemini**; Moon D9 = **Virgo** (lifts the D1 debilitated Scorpio
  Moon toward workable).
- **Vaiseshikamsa/Vimsopaka** in the export corroborate: Mars Shodasa-varga 85%
  (very strong across vargas), Moon ~49%, Saturn ~63%.

# Reference — Reading the D10 (Dasamsa) Chart

**JHora grid label:** `Dasamsa / D-10 (Trd)`. **Division factor:** N = 10 (each sign →
ten 3° parts). **Strength groups:** Dashavarga · Shodashavarga (not in Shad/Sapta).

Read [`reference-chart-reading-method.md`](reference-chart-reading-method.md) first.
**As of:** 2026-09-01. **Status:** living.

---

## 1. Significance and weight

The D10 is the chart of **karma in the world** — profession, status, authority,
achievement, reputation, and the arena of public action. It magnifies the 10th house of
D1 and, with it, the whole "what do they do and how far do they rise" question:

- **Profession and its field** — the nature of the work (planet and sign on the D10
  Lagna and 10th).
- **Rise, recognition, position** — 10th lord and benefics in D10 kendras; D10 Lagna
  lord's strength.
- **Manner of working** — service vs. enterprise vs. authority (D10 6th, 7th, 10th
  respectively), and whether success comes with struggle (malefics) or flow (benefics).
- **Amatya Karaka** — the Jaimini career significator; AmK in D10 and the sign 10th from
  AmK are read alongside the natural karakas.

D10 does not create a career D1 denies; it grades and characterises the career D1
promises (10th house/lord/karakas active).

## 2. JHora derivation

- **Sign rule (`DasamsaD10`, Traditional Parashari):** ten 3° parts per sign. From an
  **odd** sign the count starts from that sign; from an **even** sign it starts from the
  9th sign from it. So the first dasamsa of Aries = Aries; of Taurus = Capricorn; of
  Gemini = Gemini; of Cancer = Pisces.
- **Engine:** shared `VargaCalculator` + `VargaScheme` row; `AstroMath.GetDasamsaSign`.
- `VargaLongitudeDegrees = Normalize(realLon × 10)`; D10 degree-in-sign is that mod 30.
- **In the export:** `D-10 (Trd)` grid — `(Trd)` confirms the Parashari rule the engine
  uses, so placements reconcile cell-for-cell.

## 3. Inputs needed to read it

- D1 read; the D1 10th house verdict formed (10th, 10th lord, Sun/Mercury/Jupiter/Saturn
  as karakas — `reference-house-lagna-significations.md`).
- Each planet's D10 sign; degree-in-sign for conjunction tightness and vargottama.
- **Amatya Karaka (AmK)** from the D1 body table.
- The D1 10th lord and its D10 placement/dignity; the D10 Lagna and its lord.
- D1 dasha sequence (for timing career events against D10).

## 4. Step-by-step reading

1. **D10 Lagna and its lord** — the identity of the working life. Sign on the D10 Lagna
   describes the character of the work; the lord's D10 house/dignity says how empowered
   the person is to act.
2. **The D10 10th house** — occupants, its lord's placement and dignity, aspects. This is
   the profession proper. A benefic or an exalted/own-sign lord here = a strong, visible
   position; malefics without benefic support = a hard, contested climb.
3. **Mode of career:** weigh the D10 **10th** (authority/independent action), **7th**
   (business, public dealing, clients), **6th** (employment, service, competition), and
   **3rd** (self-effort, communication, hustle). Whichever is strongest and best-tenanted
   describes how the person earns.
4. **Natural karakas in D10:** Sun (authority, government, position), Mercury (commerce,
   analysis, communication trades), Jupiter (advisory, teaching, law, finance), Saturn
   (labour, service, systems, mass work), Mars (engineering, forces, surgery, land),
   Venus (arts, luxury, relationship-based trades), Moon (public, hospitality, fluids).
   The best-placed of these tints the field.
5. **Amatya-Karaka line:** AmK's D10 sign, and the 10th from it — planets there and their
   aspects flesh out the Jaimini career reading. AmK strong in D10 = a defining career.
6. **Yogas in D10:** Raja-yoga-type links (D10 kendra lord + trikona lord), Parivartana
   between the D10 Lagna lord and the 10th lord, a Pancha-Mahapurusha planet on a D10
   kendra — these mark unusual rise.
7. **Overlays:** `AL` in D10 = public image of the career; `Md`/`Gk` on the D10 10th =
   obstacles, politics, setbacks in position.
8. **Timing:** career turning points are graded by the running D1 dasha lord's condition
   **in D10** (plus 10th-lord and AmK periods). A promotion in a Saturn period is read
   through Saturn's D10 dignity and house.

## 5. Significator checklist

| D10 house (from D10 Lagna) | Read for | Key significators |
|---|---|---|
| 1 | professional identity, drive, visibility | D10 Lagna lord, Sun |
| 2 | earnings from work, resources of the career | 2nd lord |
| 3 | self-effort, initiative, communication, short travel for work | Mars, Mercury |
| 6 | employment, service, competition, subordinates, litigation at work | Saturn, Mars |
| 7 | business, partnership, clients, public dealing, market | Venus, Mercury |
| 9 | fortune in career, mentors, ethics, long journeys, higher licence | Jupiter, Sun |
| 10 | **profession itself, authority, status, government** | Sun, Saturn, Mercury, Jupiter; 10th lord; AmK |
| 11 | gains, promotions, fulfilment of ambition, professional network | Jupiter; 11th lord |

Also: **AmK's D10 sign** and the 10th house counted from AmK.

## 6. Cross-varga confirmation

- **Career field & rise:** D1 10th house + 10th lord + Sun/Saturn → **D10** (grade &
  characterise) → **D60** (karmic vote on the vocation). Add **D24** if the career rests
  on formal education/credentials; **D11** for the income and gains side.
- **Business vs job:** D10 7th vs D10 6th, cross-checked with D1 7th and the D1 3rd/6th.
- **Authority / government:** Sun in D1 10th and in D10, plus D10 Lagna in a fixed/royal
  sign; confirm with D60.

## 7. Common misreadings

- **Reading D10 for the whole life** — it is the career room only; wealth is D2/D11,
  fortune is D9, learning is D24.
- **Naming the profession from one planet** — synthesise D10 Lagna + D10 10th + the
  strongest career karaka + AmK; a single significator overstates.
- **Ignoring the D1 10th lord's D10 dignity** — a D1 10th lord that falls in D10 dilutes
  even a good-looking D10 10th house.
- **Treating malefics on the D10 10th as failure** — Saturn/Mars there often *build* the
  career (service, labour, forces, engineering); read dignity and aspect, not colour.
- **Forgetting mode** — a strong D10 10th with a weak D10 7th says "employed authority,
  not entrepreneur"; the reverse says "own venture."

## 8. Worked lens — reference export (1_Ramakrishnan)

*What to inspect, not a prediction.*

- **D10 Lagna** and its lord set the field; read the sign on the `As` cell of the
  `D-10 (Trd)` grid and place its lord.
- **AmK is Venus.** Find Venus's D10 sign and the house 10th from it; Venus AmK suggests a
  career coloured by relationship, advisory, aesthetic or diplomatic content — grade it by
  Venus's D10 dignity.
- **D1 10th house is Capricorn with Ketu**, D1 10th lord **Saturn (R) in Virgo 6th**
  (own sign, retrograde). Check where Saturn lands in D10 and its dignity there — the D1
  picture is "service/systems career with a retrograde, self-referential 10th lord in the
  6th (employment/competition)."
- **Sun (PiK, authority karaka) in D1 1st** — check Sun's D10 house; Sun on a D10 kendra
  would argue for position/recognition despite the 6th-house 10th lord.
- **Jupiter (R) and Saturn (R) both in the D1 6th** — a service/6th-house flavour to the
  vocation; see whether D10 repeats the 6th emphasis or lifts it to the 10th/7th.
- Corroborate with the export's **Vimsopaka Dasa-varga** column (D10 is a Dashavarga
  member): Mars ~91%, Saturn ~69%, Sun ~68% — Saturn and Sun both carry usable
  cross-varga strength into the career reading.

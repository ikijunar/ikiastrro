# Reference — Varga Reading Index (charts without a full guide yet)

Every divisional chart the `ikiastrro` engine computes, with the short version of *how to
read it*. The four charts with full guides — **D1**
([`reference-chart-d1-rasi.md`](reference-chart-d1-rasi.md)), **D9**
([`reference-chart-d9-navamsa.md`](reference-chart-d9-navamsa.md)), **D10**
([`reference-chart-d10-dasamsa.md`](reference-chart-d10-dasamsa.md)), **D60**
([`reference-chart-d60-shashtiamsa.md`](reference-chart-d60-shashtiamsa.md)) — are not
repeated here.

Read [`reference-chart-reading-method.md`](reference-chart-reading-method.md) for the
universal loop. Calculation rules and their sources:
[`reference-calculations.md`](reference-calculations.md) §2 and
[`scope-jhora-coverage.md`](scope-jhora-coverage.md) §2.

**As of:** 2026-09-01. **Status:** living. Each row marked `full guide: TODO` is a
candidate for its own `reference-chart-d<NN>-<name>.md` on the same pattern as the four
above.

---

## How to use a row

For each varga: (1) form the D1 verdict for its subject; (2) run the universal loop —
varga Lagna + its lord, the subject house(s) and lord(s) below, the natural karaka, then
Vargottama / dignity / yoga; (3) confirm against **D60**. The "read in order" column is
the specialisation of loop step 7 for that chart.

**Strength groups:** `6` Shadvarga · `7` Saptavarga · `10` Dashavarga · `16`
Shodashavarga. A dash = not in any classical strength grouping (picked for life-area
coverage; does not feed Vimsopaka).

---

## The charts

### D2 — Hora · wealth

- **JHora grid:** `D-2 (US)`. **N = 2.** **Groups:** 6·7·10·16.
- **Method divergence — important.** JHora's export uses **Uma-Shambu** (12-sign Hora).
  The project's `D2` chart type deliberately uses the **classical two-sign Leo/Cancer
  Hora** (`HoraD2Classic`, decision D-1) — placements will **not** match the export. A
  separate `D2-US` chart type (`HoraD2UmaShambu`, PyJHora `chart_method=1`) exists for
  when reconciling with JHora matters.
- **Read for:** wealth, liquid resources, family prosperity, sustenance.
- **Read in order:** which Hora (Sun's = self-earned/active wealth; Moon's =
  inherited/passive/fluctuating) holds the Lagna and the Moon → 2nd and 11th lords of D1
  in D2 → Jupiter (dhana karaka) and Venus in D2 → benefics vs malefics in the wealth
  Horas.
- **Confirm with:** D1 2nd/11th houses, D11 (gains), D60. `full guide: TODO`

### D3 — Drekkana · siblings, courage

- **JHora grid:** `D-3 (Trd)`. **N = 3.** **Groups:** 6·7·10·16.
- **Method:** 1/5/9 trine stride (`DrekkanaD3`) — first drekkana = own sign, second = 5th,
  third = 9th.
- **Read for:** younger/elder siblings, courage, initiative, drive, short journeys,
  hands/arms, longevity nuance (classical Drekkana use).
- **Read in order:** D3 Lagna + lord → D3 3rd house, its lord, Mars (karaka) → 11th (elder
  sibling) and 3rd (younger) → benefic/malefic support to the 3rd.
- **Confirm with:** D1 3rd/11th, Mars in D1, D60. `full guide: TODO`

### D4 — Chaturthamsa · fortune, property, home

- **JHora grid:** `D-4`. **N = 4.** **Groups:** 16.
- **Method:** kendra stride (`ChaturthamsaD4`) — 1st/4th/7th/10th from own sign for the
  four parts.
- **Read for:** fixed assets, land and buildings, the home, vehicles' base, mother's
  contribution, general fortune and comforts, inner contentment.
- **Read in order:** D4 Lagna + lord → D4 4th house, its lord, Moon and Mars (property
  karakas), Venus (comforts) → 2nd (accrued assets), 11th (acquisition) → benefics on the
  4th.
- **Confirm with:** D1 4th house, D16 (vehicles/comforts), D60. `full guide: TODO`

### D5 — Panchamsa · power, fame, authority

- **JHora grid:** `D-5`. **N = 5.** **Groups:** — (JHora extra; not in the Parashari 16).
- **Method:** odd/even 5-part lookup (`PanchamsaD5`).
- **Read for:** capacity for power and command, fame, moral/ethical authority, spiritual
  merit as it shows in influence over others.
- **Read in order:** D5 Lagna + lord → the strongest planet by D5 dignity → Sun (authority)
  and Jupiter (ethics) in D5 → 5th and 9th (purva-punya, fame).
- **Confirm with:** D1 5th/9th/10th, D10, D60. `full guide: TODO`

### D6 — Shashthamsa · health, affliction

- **JHora grid:** `D-6`. **N = 6.** **Groups:** — (project life-area pick).
- **Method:** `ShashtamsaD6` (own `AstroMath` helper).
- **Read for:** disease, chronic vs acute affliction, debts, litigation, enemies,
  accidents, the body's weak systems.
- **Read in order:** D6 Lagna + lord (constitutional weak point) → D6 6th and 8th houses,
  their lords → Mars (acute/surgery/blood), Saturn (chronic/degenerative), Sun (vitality),
  Moon (fluids/mind) in D6 → malefics on the Lagna/6th/8th; benefic protection.
- **Confirm with:** D1 6th/8th, D30 (misfortunes), D60. `full guide: TODO`

### D7 — Saptamsa · children, progeny

- **JHora grid:** `D-7 (Trd)`. **N = 7.** **Groups:** 7·10·16.
- **Method:** odd sign counts from itself, even sign from the 7th (`SaptamsaD7`).
- **Read for:** children — number, wellbeing, relationship with them — plus creative
  progeny and lineage continuation; grandchildren via the 5th-from-5th.
- **Read in order:** D7 Lagna + lord → D7 5th house, its lord, Jupiter (putra karaka),
  and the Putra Karaka (PK) chara karaka → 9th (from the 5th = grandchildren), 11th (elder
  child) → benefics vs malefics on the 5th; Saturn/Rahu/Ketu/Sun stress.
- **Confirm with:** D1 5th house, D9 5th (children within marriage), D60. `full guide: TODO`

### D8 — Ashtamsa · sudden events, longevity nuance

- **JHora grid:** `D-8`. **N = 8.** **Groups:** — (JHora extra).
- **Method:** movable/fixed/dual → Aries/Sagittarius/Leo seed (`AshtamsaD8`).
- **Read for:** sudden and unexpected events, crises, accidents, chronic danger,
  inheritance and legacies, occult exposure, the "8th-house" texture of the life.
- **Read in order:** D8 Lagna + lord → D8 8th house and lord → Saturn (longevity/decay),
  Mars (accident), Rahu/Ketu (sudden) in D8 → malefic concentration; benefic relief.
- **Confirm with:** D1 8th house, longevity method (`reference-calculations.md`), D30,
  D60. `full guide: TODO`

### D11 — Rudramsa · gains, income

- **JHora grid:** `D-11`. **N = 11.** **Groups:** — (project life-area pick).
- **Method:** **Sanjay Rath Rudramsa** (`RudramsaD11`) — one-line switch to Raman's
  variant available. State which was used.
- **Read for:** gains, income streams, fulfilment of desires, elder siblings, the "death
  of enemies/obstacles" (Rath's Rudra connotation), windfalls.
- **Read in order:** D11 Lagna + lord → D11 11th house, its lord, Jupiter (labha karaka)
  → 2nd (accrual), 5th and 9th (fortune feeding gains) → benefics on the 11th.
- **Confirm with:** D1 11th/2nd, D2 (wealth), D10 (career as the source), D60.
  `full guide: TODO`

### D12 — Dwadasamsa · parents, lineage

- **JHora grid:** `D-12 (Trd)`. **N = 12.** **Groups:** 6·7·10·16.
- **Method:** 12-from-self stride (`DwadasamsaD12`) — twelve parts starting from the own
  sign.
- **Read for:** father and mother as distinct — their condition, longevity, what is
  inherited from each — ancestry, family karma, the parental home.
- **Read in order:** D12 Lagna + lord → D12 9th (father) and 4th (mother), their lords,
  Sun (father karaka) and Moon (mother karaka) in D12 → the Matru Karaka (MK) and Pitru
  Karaka (PiK) chara karakas → 8th-from-each for parental longevity.
- **Confirm with:** D1 4th/9th, D4 (mother's property line), D60. `full guide: TODO`

### D16 — Shodasamsa (Kalamsa) · vehicles, comforts, happiness

- **JHora grid:** `D-16 (Trd)`. **N = 16.** **Groups:** 10·16.
- **Method:** movable/fixed/dual → Aries/Leo/Sagittarius seed (`ShodasamsaD16`).
- **Read for:** vehicles, luxuries and conveniences, general material happiness or
  discontent, pleasures, and (classical) the sukha of the mind.
- **Read in order:** D16 Lagna + lord → D16 4th house, its lord, Venus (vehicles/comfort
  karaka) and Moon (contentment) in D16 → benefic vs malefic on the 4th; Saturn/Mars
  affliction = vehicle trouble / lack of ease.
- **Confirm with:** D1 4th house, D4, D60. `full guide: TODO`

### D20 — Vimsamsa · spiritual practice, upasana

- **JHora grid:** `D-20 (Trd)`. **N = 20.** **Groups:** 16.
- **Method:** movable/dual/fixed → Aries/Leo/Sagittarius seed (`VimsamsaD20`).
- **Read for:** devotion, sadhana, the deity and mode of worship, progress on a spiritual
  path, mantra and tantra aptitude, religious inclination.
- **Read in order:** D20 Lagna + lord → D20 5th (mantra/upasana) and 9th (dharma/guru),
  their lords → Jupiter (wisdom), Ketu (moksha), Moon (bhakti), Sun/Saturn (discipline)
  in D20 → benefic support to the 5th/9th; the sign on the D20 Lagna hints at the
  ishta-devata line.
- **Confirm with:** D1 5th/9th/12th, D9 Karakamsa (5th/9th/12th from AK), D24, D60.
  `full guide: TODO`

### D24 — Siddhamsa (Chaturvimsamsa) · education, learning

- **JHora grid:** `D-24 (Trd)`. **N = 24.** **Groups:** 16.
- **Method:** odd sign seeds from Leo, even from Cancer (`SiddhamsaD24`).
- **Read for:** formal education, degrees, scholarship, learning capacity, teachers,
  academic success or interruption, skill acquisition.
- **Read in order:** D24 Lagna + lord → D24 4th (schooling) and 5th (intelligence,
  higher study), their lords → Mercury (learning), Jupiter (wisdom/higher education), Sun
  (Leo seed) and Moon (Cancer seed) in D24 → 9th (post-graduate / research); benefics on
  the 4th/5th.
- **Confirm with:** D1 4th/5th, Mercury and Jupiter in D1, D10 (if the career depends on
  the credential), D60. `full guide: TODO`

### D27 — Nakshatramsa / Bhamsa · innate strengths & weaknesses

- **JHora grid:** `D-27 (Trd)`. **N = 27.** **Groups:** 16.
- **Method:** by element — fire/earth/air/water seed (`NakshatramsaD27`).
- **Read for:** raw physical and mental stamina, resilience, the constitutional
  strong/weak points beneath the D1 body, disaster-resistance.
- **Read in order:** D27 Lagna + lord (baseline vitality) → each planet's D27 dignity as
  its "true" underlying strength → Sun (physical) and Moon (mental) in D27 → malefic
  clusters marking the weak systems.
- **Confirm with:** D1 Lagna/6th, D6, D60. `full guide: TODO`

### D30 — Trimsamsa · misfortunes, evils, moral fibre

- **JHora grid:** `D-30`. **N = 30.** **Groups:** 6·7·10·16.
- **Method — ambiguous (tracked).** Unequal 5-part bands (Mars 5° / Saturn 5° /
  Jupiter 8° / Mercury 7° / Venus 5°), odd and even signs mapping to different planet
  sequences (`TrimsamsaD30`), no navamsa-style seed sign. State the rule used; the engine
  uses PyJHora `chart_method=1`.
- **Read for:** troubles, vices, character flaws, chronic misfortune, moral and ethical
  constitution, punishments, hidden dangers; classically weighted for a female chart's
  chastity/character reading and for both charts' "evils."
- **Read in order:** which of the five ruling planets (Ma/Sa/Ju/Me/Ve) holds the Lagna
  and the Moon (benefic rulers Ju/Ve = protection, malefic Ma/Sa = exposure) → D30 6th,
  8th, 12th houses and their lords → malefic concentration; benefic mitigation.
- **Confirm with:** D1 6th/8th/12th, D6, D8, D60. `full guide: TODO`

### D40 — Khavedamsa (Swavedamsa) · maternal legacy, auspicious/inauspicious

- **JHora grid:** `D-40`. **N = 40.** **Groups:** 16.
- **Method:** odd sign from Aries, even sign from Libra (`KhavedamsaD40`).
- **Read for:** effects transmitted through the maternal line, general auspicious vs
  inauspicious results, matrilineal customs and blessings/curses, overall good/bad
  "vedha."
- **Read in order:** D40 Lagna + lord → Moon (mother/matriline) and the 4th house in D40
  → benefic vs malefic tenancy across the D40 kendras → any planet exalted/debilitated in
  D40 as an amplifier.
- **Confirm with:** D1 4th, D12 (mother), D60. `full guide: TODO`

### D45 — Akshavedamsa · paternal legacy, character, all matters

- **JHora grid:** `D-45`. **N = 45.** **Groups:** 16.
- **Method:** movable/fixed/dual → Aries/Leo/Sagittarius seed (`AkshavedamsaD45`).
- **Read for:** effects through the paternal line, conduct and character, general
  all-purpose fine-grade reading (classical texts allow D45 as a broad varga like D60),
  patrilineal customs and inheritance of nature.
- **Read in order:** D45 Lagna + lord → Sun (father/patriline) and the 9th house in D45 →
  each key planet's D45 dignity as a character-level read → benefic vs malefic emphasis.
- **Confirm with:** D1 9th, D12 (father), D60. `full guide: TODO`

---

## Extended vargas (Plan B — not computed yet)

D81 (Navanavamsa), D108 (Ashtottaramsa), D144 (Dwadasa-Dwadasamsa) and D150 are in the
export but out of the engine's current scope — see `scope-jhora-coverage.md` §2 and the
divisional-chart-completion spec. No reading notes until they are built.

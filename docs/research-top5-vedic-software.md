# Research: Top 5 Vedic Astrology Software (Competitive Benchmark)

**Purpose:** Feature/config benchmark of the leading Vedic astrology software, to inform product
strategy for `ikiastrro` (see [`ikiastrro.md`](ikiastrro.md) for
build decisions/history).

**Status:** Active reference doc
**Created:** 2026-08-24
**Owner:** rammyps

---

## The Top 5

1. **Parashara's Light** — commercial, Windows/Mac, the long-standing professional-client-report
   gold standard.
2. **Jagannatha Hora (JHora)** — free, Windows, deepest calculation/config surface of any tool
   here, most cited by serious practitioners. Built by P.V.R. Narasimha Rao, a software engineer
   (IIT Madras + Rice University) and Sanskrit scholar based near Boston, US — he also publishes a
   companion book, research articles, and ~400 hours of Jyotish class recordings free on the same
   site (2026-08-28 addition, see Sources).
3. **Shri Jyoti Star (now "Kala")** — commercial, Windows, favored by researchers for technical
   depth and active development.
4. **LeoStar Expert** — commercial (India-origin), strong KP/Nadi/Lal Kitab depth, favored by
   working professional astrologers.
5. **AstroSage Kundli** — free/freemium, Web + Mobile + Desktop, most consumer-accessible, best
   reference for breadth of features an end user expects "out of the box."

---

## Comparison Table

| Feature | Parashara's Light | Jagannatha Hora | Shri Jyoti Star (Kala) | LeoStar Expert | AstroSage Kundli |
|---|---|---|---|---|---|
| **Type / Price** | Commercial — ₹5,000 (Personal) to ₹30,000 (Professional) | Free | Commercial | Commercial — ₹11,999–₹59,999 | Free / Freemium |
| **Platform** | Windows, Mac | Windows | Windows | Windows (desktop) | Web, iOS, Android, Windows |
| **Ephemeris/Engine** | Proprietary + Swiss Ephemeris data | Swiss Ephemeris (XALEN, astronomical-grade) | High-precision ephemeris, 13,000 BCE–12,000 AD range | Proprietary | Proprietary |
| **Divisional charts (vargas)** | Rasi + Navamsa + full Tajika Vargas | **23 vargas total**, D1–D144, incl. multiple variant schemes for Hora/D-3/D-4/D-5/D-8/D-9/D-11/D-30/D-81/D-108 individually, plus fully custom generic D-m×n combos | Full varga set (matches JHora-class depth) | Multiple vargas ("accurately," breadth not detailed publicly) | Full Shodashavarga (16 divisional charts) |
| **Chart styles** | North, South, Circular, Bengali, Oriya | Configurable (N/S/E, circular) | North, South, East, circular, **Western wheel** — one-click style + glyph-size switch | North/South Indian | North, South, East Indian |
| **Dasha systems** | 29 systems incl. Parashara & Jaimini, down to **Prana Dasha** level; table/timeline/transit-linked views | **30+ systems** — Vimshottari, Ashtottari, Yogini, Chara, Narayana, and many conditional nakshatra dashas, nested down to **deha-antardasha** depth | 20+ systems up to **5 levels**, incl. Kala Chakra (Deha/Jeeva), Kendra Sudasha, dual Sudasha variants | Vimshottari, Yogini | Vimshottari (Udu) Dasha up to **5 levels** |
| **Ayanamsha options** | Configurable (not enumerated publicly) | 7 built-in — Lahiri, Raman, Deva-datta, Krishnamurthy, Usha-Shashi, Fagan-Bradley, **tropical (no ayanamsha)** — plus custom values | Lahiri, Raman, Krishnamurti, True Chitrapaksha; **v10 adds 11 more** (Thirukkanitham, Usha-Shashi, KP-Senthilathiban, True Revati, True Pushya, Surya Siddhanta, Aryabhata variants) | Not publicly detailed | KP Ayanamsa + standard set |
| **House system config** | Configurable | **8 named schemes** — Sripathi/Porphyry, Krishnamurthy/Placidus, Koch, Regiomontanus, Campanus, axial rotation, Polich/Page, Alcabitus, plus rasi (whole sign) and equal houses — the most house systems of any tool here, on top of separate geocentric/topocentric, true/apparent, mean/true node toggles | Configurable | Not publicly detailed | Bhava Chalit chart supported |
| **Ashtakavarga** | Included | Bhinna (BAV), Sodhita (reduced), Prastara (PAV) — across all divisional charts | Included | Not emphasized | Ashtakvarga + Prastarashtakvarga, Shadbala |
| **KP System (Krishnamurti Paddhati)** | Included (Krishnamurthy chart & Prashna, house cusps + significators) | Full module — Krishnamurthy ayanamsha **plus multi-level sub-lords**, not just an ayanamsha toggle (corrected 2026-08-28; previously understated here) | Supported | **Strong** — KP/Horary is a named product line | Full: Significators, Ruling Planets, KP Ayanamsa, Nakshatra Nadi coords, sub-sub (4-step) |
| **Nadi astrology** | Supported, but LeoStar/Parashara's Light comparisons rate **Parashara's Light more accurate** for Nadi | Not a focus | Not a focus | Named strength (Nadi astrology support) | Not emphasized |
| **Numerology / Lal Kitab / other systems** | Numerology add-on separate product | Not included | Not a focus | **Lal Kitab module** (Kundli, Dasha, Debts, Varshphal, Remedies); Bhrigupatrika, Kundlidarpan, ParasharPatrika | Kundli matching (Ashtakoota/36-point Guna Milan) |
| **Yogas / strength scoring** | Neech Bhanga yoga, dignities, combustion, retrogression, graphical aspects | **184 yoga types**; strength scoring goes well past Ashtakavarga — Shadbala, Ishta/Kashta Phala, Vimsopaka Bala (across multiple varga schemes), Vaiseshikamsa, avasthas, Tajaka balas (2026-08-28 addition — Shadbala specifically was missing from this row before) | Included | Included | Shadbala |
| **Other notable modules** | — | Panchanga calculations; annual/Tajaka charts (Tithi Pravesha, Yoga Pravesha); mundane astrology tools — not compared across the other 4 here (not researched to the same depth), flagged as JHora-specific breadth, not a claimed universal gap | — | — | — |
| **Matching / compatibility** | Included | Included | Included | Included | Ashtakoota/36-point Guna Milan, cloud sync, PDF export |
| **Reported accuracy (independent comparative study)** | ~88% (highest in cited study) | ~51% (lower in that study; worth validating independently — huge config surface can mean "accuracy" is user-config-dependent) | Not scored in that study | Not scored | Not scored |
| **Active development (2026)** | **Slowed significantly** in recent years | Actively maintained, free | **Actively developed** — v10 shipped 2026 with new ayanamshas | Actively sold/maintained | Actively developed (AI features added 2026) |
| **Best known for** | Professional client reports, remedies, interpretive text | Free depth/configurability ceiling — no contemporary tool matches its customization | Research-grade technical depth, chart-style flexibility | KP/Horary + Nadi + Lal Kitab professional practice | Consumer accessibility, breadth in one browser/app, KP depth for a free tool |

---

## Common Baseline Feature Set (candidate floor for our product)

Across all 5, this is what a credible Vedic astrology tool is expected to support at minimum:

- **D1 (Rasi) + D9 (Navamsa)** — universal floor; most tools go to full Shodashavarga (16) or up
  to D60
- **Vimshottari Dasha**, generally multi-level (3–5+ sub-period levels); Jaimini/other dasha
  systems appear in 4 of 5 as a differentiator, not a floor
- **Nakshatra + Pada** for all planets, not just Moon
- **Ayanamsha selection** — Lahiri (Chitrapaksha) is the de facto default across every tool
- **House system selection** — Whole Sign is the classical Parashari default; Placidus/Sripati/
  Equal appear as alternates in the more configurable tools
- **Ashtakavarga** (Bhinna at minimum; Sodhita/Prastara in the deeper tools) and **Shadbala**
  (strength scoring) are common in the upper half of the field
- **Chart style selection** (North/South/East Indian, at minimum) — table-stakes UI expectation
  from an Indian-market user base
- **Compatibility/matching** (Ashtakoota/Guna Milan) — present in every consumer-facing tool

## Differentiators Worth Tracking as v2+ Roadmap Items

- **Config granularity on the calculation layer** (JHora's geocentric/topocentric, true/apparent,
  mean/true node toggles) — no other tool matches this; a genuine technical moat if we build it
- **KP System** (LeoStar, AstroSage) — a distinct sub-system (sub-lords, significators, cuspal
  interlinks) layered on top of the standard Parashari chart, not a variant of it
- **Dasha depth to 5 levels** (Shri Jyoti Star, AstroSage) vs. JHora/Parashara's Light going to
  **Prana Dasha** (finer than 5 levels) — depth ceiling varies meaningfully
- **Ayanamsha breadth** (Shri Jyoti Star v10's 11 additional ayanamshas) signals this is an active
  competitive axis, not a solved problem
- **Nadi astrology accuracy** is called out specifically as a Parashara's Light strength over
  LeoStar in direct practitioner comparisons — a niche but real differentiator
- **Lal Kitab** — a distinct parallel system (LeoStar) with its own remedies/debts framework;
  optional expansion, not a core Parashari feature
- **Strength scoring beyond Ashtakavarga/Shadbala** (JHora's Ishta/Kashta Phala, Vimsopaka Bala,
  Vaiseshikamsa, avasthas, Tajaka balas — 2026-08-28 addition) — a deeper tier past the "Shadbala"
  Later-backlog item, worth noting as a possible v3+ item rather than assuming Shadbala alone
  closes this gap
- **Panchanga / annual (Tajaka) charts / mundane astrology** (JHora — 2026-08-28 addition) — not
  yet cross-checked against the other 4 tools, so not scored as a universal differentiator, but a
  real breadth axis this research hadn't captured before

## Relevance to `vedic_horo_gen` v1 Scope

Cross-checked against current project decisions (see
[`ikiastrro.md`](ikiastrro.md)):

| Our v1 decision | How it stacks up |
|---|---|
| D1 + D9 only | Matches the universal floor across all 5 tools — correct minimum viable scope |
| Lahiri ayanamsha only | Matches the de facto default everywhere; safe v1 choice, but every top-5 tool treats ayanamsha *selection* as a baseline config option, not a fixed constant — worth flagging as a likely v2 ask once users compare us to JHora/Shri Jyoti Star |
| Whole Sign houses only | Matches the classical Parashari default; same v2 flag as ayanamsha — house system selection is standard in the field |
| No Ashtakavarga/Shadbala/KP/Jaimini yet | Consistent with a deliberately staged v1; these are exactly the differentiators the top 5 lean on, so they're the natural v2/v3 backlog in roughly this order: Ashtakavarga → Shadbala → multi-level Vimshottari → KP → Jaimini |

**Suggested product-strategy takeaway:** our v1 (D1+D9, Lahiri, Whole Sign) is a defensible
*calculation-correctness* floor, not a feature-competitive product yet — every top-5 tool clears
that floor and differentiates above it on config granularity (ayanamsha/house system choice),
dasha depth, and bolt-on systems (KP, Ashtakavarga, Nadi, Lal Kitab). Treat "make ayanamsha and
house system user-selectable" as the highest-leverage next step toward parity, since it's cheap
relative to adding a whole new subsystem like KP and unlocks direct comparison against every tool
in this table.

---

## Sources

- [Compare Vedic Astrology Software (2026) — Honest Rival Comparisons](https://parasara.net/compare)
- [Best Kundali Software 2026: Top 10 Kundali Making Tools Compared](https://vedika.io/blog/best-kundali-software-2026)
- [Parashara's Light — official](https://parasharaslight.com/parasharas-light/)
- [Parashara's Light 9 New Features](https://parasharaslight.com/parasharas-light-9-new-features/)
- [Parashara's Light Pricing — Techjockey](https://www.techjockey.com/detail/parasharas-light-commercial-edition)
- [Features of Jagannatha Hora Software](https://www.vedicastrologer.org/jh/features.htm)
- [How to Read Divisional Charts (Vargas) in Jagannatha Hora](https://jagannathhora.com/divisional-charts-jagannatha-hora-guide/)
- [Jagannatha Hora — Ashtakavarga](https://jagannathahora.com/ashtakavarga)
- [P. V. R. Narasimha Rao — bio, Arsha Jyotish](https://arshajyotish.com/narasimharaodetails.html)
- [Jagannatha Hora Astrology Software — Free Download & Instructions](https://vedicastrologer.com/jagannatha-hora-astrology-software/)
- [Shri Jyoti Star 9 Pro — features](https://www.vedicsoftware.com/features9/)
- [Shri Jyoti Star 10 Updates](https://www.vedicsoftware.com/v10-updates/)
- [LeoStar Expert — Techjockey](https://www.techjockey.com/detail/leostar-professional-kundli-software)
- [LeoStar vs Parashara's Light — Techjockey Q&A](https://www.techjockey.com/question/16559/which-is-a-better-software-for-a-professional-astrologer-leostar-expert-or-parashara-s-light-regarding-calculations-of-sub-charts-time-accuracy-charts-numerology-kp-vedic-nadi-etc)
- [AstroSage Kundli — official](https://www.astrosage.com/kundli/)
- [AstroSage Kundli — Google Play](https://play.google.com/store/apps/details?id=com.ojassoft.astrosage&hl=en_US)

---

## Change log

- 2026-08-24 — Initial version. Researched and compared top 5 Vedic astrology software
  (Parashara's Light, Jagannatha Hora, Shri Jyoti Star/Kala, LeoStar Expert, AstroSage Kundli) on
  features, chart types, and configuration options; cross-referenced against `vedic_horo_gen` v1
  scope decisions.
- 2026-08-28 — Deepened the Jagannatha Hora entry: added author bio (P.V.R. Narasimha Rao),
  precise varga count (23, not "20+"), the actual 8 named house systems (previously conflated
  with node/geocentric toggles), full dasha depth ("deha-antardasha," 30+ systems), corrected the
  KP row (a full sub-lord module, not just an ayanamsha choice), added the 184-yoga-type count and
  the Shadbala/Ishta-Kashta/Vimsopaka/Vaiseshikamsa/avasthas/Tajaka-bala strength-scoring detail
  that was missing entirely before, and flagged Panchanga/annual-chart/mundane-astrology modules
  as a JHora-specific breadth axis not yet checked against the other 4 tools. No change to the
  "Relevance to v1 Scope" strategic takeaway — ayanamsha/house-system selectability stays the
  highest-leverage next step, now with a more precise JHora comparison to cite.
